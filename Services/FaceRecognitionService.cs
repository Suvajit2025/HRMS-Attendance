using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace HRMS.Services;

public class FaceRecognitionService : IFaceRecognitionService
{
    private const string CurrentModelVersion = "LocalFeatureExtractor-v2.0 (Illumination-Invariant)";
    private const int FeatureVectorDimension = 128;

    private readonly IFaceTemplateRepository _faceTemplateRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly StoragePathProvider _pathProvider;
    private readonly IWebHostEnvironment _webHostEnv;
    private readonly JsonStorageOptions _options;
    private readonly ILogger<FaceRecognitionService> _logger;

    public FaceRecognitionService(
        IFaceTemplateRepository faceTemplateRepo,
        IEmployeeRepository employeeRepo,
        StoragePathProvider pathProvider,
        IWebHostEnvironment webHostEnv,
        IOptions<JsonStorageOptions> options,
        ILogger<FaceRecognitionService> logger)
    {
        _faceTemplateRepo = faceTemplateRepo;
        _employeeRepo = employeeRepo;
        _pathProvider = pathProvider;
        _webHostEnv = webHostEnv;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, FaceTemplateModel? Template)> RegisterFaceAsync(string employeeId, string imageBase64)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId) ?? await _employeeRepo.GetByEmployeeNoAsync(employeeId);
        if (employee == null)
        {
            return (false, "Employee not found.", null);
        }

        if (!employee.IsActive)
        {
            return (false, "Cannot register face for an inactive employee.", null);
        }

        var (isValid, detectionMessage, vector) = ExtractFaceFeatureVector(imageBase64);
        if (!isValid || vector == null)
        {
            return (false, detectionMessage, null);
        }

        // Check for duplicate face across other employees (above 0.98 similarity)
        var allTemplates = await _faceTemplateRepo.GetAllAsync();
        foreach (var existing in allTemplates)
        {
            if (string.Equals(existing.EmployeeId, employee.EmployeeId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(existing.EmployeeNo, employee.EmployeeNo, StringComparison.OrdinalIgnoreCase))
                continue;

            var existingVector = DecodeVector(existing.EmbeddingBase64);
            if (existingVector != null)
            {
                var sim = ComputeCosineSimilarity(vector, existingVector);
                if (sim >= 0.98f)
                {
                    var otherEmp = await _employeeRepo.GetByIdAsync(existing.EmployeeId);
                    var otherName = otherEmp?.FullName ?? existing.EmployeeNo;
                    return (false, $"Face registration conflict: This face appears almost identical to {otherName} ({existing.EmployeeNo}) with {(int)(sim * 100)}% match.", null);
                }
            }
        }

        var template = new FaceTemplateModel
        {
            TemplateId = Guid.NewGuid().ToString(),
            EmployeeId = employee.EmployeeId,
            EmployeeNo = employee.EmployeeNo,
            ModelVersion = CurrentModelVersion,
            EmbeddingBase64 = EncodeVector(vector),
            VectorDimension = FeatureVectorDimension,
            EnrolledAt = DateTime.UtcNow,
            IsActive = true
        };

        await _faceTemplateRepo.SaveTemplateAsync(template);
        _logger.LogInformation("Successfully enrolled face template for {EmployeeNo} ({EmployeeName})", employee.EmployeeNo, employee.FullName);

        return (true, $"Face template enrolled successfully for {employee.FullName}.", template);
    }

    public async Task<FaceMatchResult> MatchFaceAsync(string imageBase64)
    {
        var (isValid, detectionMessage, probeVector) = ExtractFaceFeatureVector(imageBase64);
        if (!isValid || probeVector == null)
        {
            return new FaceMatchResult
            {
                IsSuccess = false,
                FaceDetected = false,
                Confidence = 0.0f,
                Message = detectionMessage
            };
        }

        var registeredTemplates = await _faceTemplateRepo.GetAllAsync();
        if (registeredTemplates.Count == 0)
        {
            // Auto-sync from saved employee photos if templates are empty
            await SyncEmployeePhotoTemplatesAsync();
            registeredTemplates = await _faceTemplateRepo.GetAllAsync();
        }

        if (registeredTemplates.Count == 0)
        {
            return new FaceMatchResult
            {
                IsSuccess = false,
                FaceDetected = true,
                Confidence = 0.0f,
                Message = "No face templates have been registered in the system yet. Please enroll employees first."
            };
        }

        float bestScore = -1f;
        FaceTemplateModel? bestMatchTemplate = null;

        foreach (var t in registeredTemplates)
        {
            var storedVector = DecodeVector(t.EmbeddingBase64);
            if (storedVector == null || storedVector.Length != probeVector.Length)
                continue;

            var sim = ComputeCosineSimilarity(probeVector, storedVector);
            if (sim > bestScore)
            {
                bestScore = sim;
                bestMatchTemplate = t;
            }
        }

        var threshold = _options.FaceMatchThreshold > 0 ? _options.FaceMatchThreshold : 0.55f;
        if (bestMatchTemplate != null && bestScore >= threshold)
        {
            var employee = await _employeeRepo.GetByIdAsync(bestMatchTemplate.EmployeeId) ?? await _employeeRepo.GetByEmployeeNoAsync(bestMatchTemplate.EmployeeNo);
            if (employee == null)
            {
                return new FaceMatchResult
                {
                    IsSuccess = false,
                    FaceDetected = true,
                    Confidence = bestScore,
                    Message = "Matched template references an employee record that no longer exists."
                };
            }

            if (!employee.IsActive)
            {
                return new FaceMatchResult
                {
                    IsSuccess = false,
                    FaceDetected = true,
                    EmployeeId = employee.EmployeeId,
                    EmployeeNo = employee.EmployeeNo,
                    EmployeeName = employee.FullName,
                    Confidence = bestScore,
                    Message = $"Employee {employee.FullName} ({employee.EmployeeNo}) is inactive."
                };
            }

            return new FaceMatchResult
            {
                IsSuccess = true,
                FaceDetected = true,
                EmployeeId = employee.EmployeeId,
                EmployeeNo = employee.EmployeeNo,
                EmployeeName = employee.FullName,
                DepartmentName = employee.DepartmentName,
                PhotoPath = employee.Photo,
                Confidence = (float)Math.Round(bestScore, 3),
                Message = $"Face recognized: {employee.FullName}."
            };
        }

        return new FaceMatchResult
        {
            IsSuccess = false,
            FaceDetected = true,
            Confidence = bestScore > 0 ? (float)Math.Round(bestScore, 3) : 0f,
            Message = "Face not recognized. Please align your face inside the frame and try again."
        };
    }

    public async Task<bool> HasRegisteredFaceAsync(string employeeId)
    {
        var template = await _faceTemplateRepo.GetByEmployeeIdAsync(employeeId);
        return template != null && template.IsActive;
    }

    public async Task<bool> RemoveTemplateAsync(string employeeId)
    {
        await _faceTemplateRepo.DeleteByEmployeeIdAsync(employeeId);
        return true;
    }

    public async Task<int> SyncEmployeePhotoTemplatesAsync()
    {
        int synced = 0;
        try
        {
            var employees = await _employeeRepo.GetAllAsync();
            foreach (var emp in employees.Where(e => e.IsActive && !string.IsNullOrWhiteSpace(e.Photo)))
            {
                var existing = await _faceTemplateRepo.GetByEmployeeIdAsync(emp.EmployeeId) ?? await _faceTemplateRepo.GetByEmployeeIdAsync(emp.EmployeeNo);
                if (existing != null && existing.IsActive) continue;

                string? diskPath = null;
                var photo = emp.Photo!;
                var fileName = Path.GetFileName(photo);

                var uploadCandidate = Path.Combine(_pathProvider.EmployeeUploadsDirectory, fileName);
                if (File.Exists(uploadCandidate))
                {
                    diskPath = uploadCandidate;
                }
                else if (!string.IsNullOrEmpty(_webHostEnv.WebRootPath))
                {
                    var webCandidate = Path.Combine(_webHostEnv.WebRootPath, photo.TrimStart('/', '\\'));
                    if (File.Exists(webCandidate)) diskPath = webCandidate;
                }

                if (diskPath != null && File.Exists(diskPath))
                {
                    var bytes = await File.ReadAllBytesAsync(diskPath);
                    var b64 = Convert.ToBase64String(bytes);
                    var (ok, msg, tmpl) = await RegisterFaceAsync(emp.EmployeeId, b64);
                    if (ok)
                    {
                        synced++;
                        _logger.LogInformation("Auto-enrolled face template for {EmployeeNo} ({EmployeeName}) from photo file {File}", emp.EmployeeNo, emp.FullName, fileName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error syncing employee photo templates");
        }
        return synced;
    }

    public string GetModelInformation()
    {
        return $"{CurrentModelVersion}. Prototype Face Recognition Engine. Notice: Optical liveness detection must be validated with dual-sensor IR camera before unattended commercial deployment.";
    }

    private static (bool IsValid, string Message, float[]? Vector) ExtractFaceFeatureVector(string imageBase64)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
        {
            return (false, "No image data was received from the camera or upload.", null);
        }

        try
        {
            var clean = imageBase64;
            if (clean.Contains(','))
            {
                clean = clean.Substring(clean.IndexOf(',') + 1);
            }

            var bytes = Convert.FromBase64String(clean);
            if (bytes.Length < 100)
            {
                return (false, "Image payload is too small or corrupted.", null);
            }

            using var ms = new MemoryStream(bytes);
            using var original = new Bitmap(ms);

            if (original.Width < 40 || original.Height < 40)
            {
                return (false, "Image resolution too low. Please position closer to the camera.", null);
            }

            // Square center crop to prevent geometric aspect-ratio distortion between landscape video & portrait photos
            int side = (int)(Math.Min(original.Width, original.Height) * 0.85);
            if (side < 32) side = Math.Min(original.Width, original.Height);
            int cropX = (original.Width - side) / 2;
            int cropY = (original.Height - side) / 2;

            using var resized = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, 64, 64), new Rectangle(cropX, cropY, side, side), GraphicsUnit.Pixel);
            }

            // Extract luminance & calculate mean & standard deviation for illumination invariance
            double totalLum = 0;
            var grayValues = new double[64, 64];

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    var p = resized.GetPixel(x, y);
                    double lum = 0.299 * p.R + 0.587 * p.G + 0.114 * p.B;
                    grayValues[x, y] = lum;
                    totalLum += lum;
                }
            }

            double avgLum = totalLum / 4096.0;
            if (avgLum < 5.0)
            {
                return (false, "No face detected: The lighting is too dark or video feed is blank. Please ensure sufficient front lighting.", null);
            }
            if (avgLum > 252.0)
            {
                return (false, "No face detected: The image is completely washed out or overexposed.", null);
            }

            double sumVariance = 0;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    double diff = grayValues[x, y] - avgLum;
                    sumVariance += diff * diff;
                }
            }
            double stdLum = Math.Max(1.0, Math.Sqrt(sumVariance / 4096.0));

            // Extract 128-dimensional normalized spatial-gradient vector across 16 blocks (4x4 grid of 16x16 pixels)
            var vector = new float[FeatureVectorDimension];
            int vecIdx = 0;
            int blockSize = 16; // 64 / 4

            for (int by = 0; by < 4; by++)
            {
                for (int bx = 0; bx < 4; bx++)
                {
                    int startX = bx * blockSize;
                    int startY = by * blockSize;

                    double blockLum = 0;
                    double gradH = 0;
                    double gradV = 0;
                    double gradD = 0;
                    int count = 0;

                    for (int y = startY; y < startY + blockSize; y++)
                    {
                        for (int x = startX; x < startX + blockSize; x++)
                        {
                            var current = grayValues[x, y];
                            var right = x + 1 < 64 ? grayValues[x + 1, y] : current;
                            var bottom = y + 1 < 64 ? grayValues[x, y + 1] : current;
                            var diag = (x + 1 < 64 && y + 1 < 64) ? grayValues[x + 1, y + 1] : current;

                            blockLum += current;
                            gradH += Math.Abs(right - current);
                            gradV += Math.Abs(bottom - current);
                            gradD += Math.Abs(diag - current);
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        double meanBlockLum = blockLum / count;
                        // Relative contrast to whole image mean (illumination-invariant)
                        double relContrast = (meanBlockLum - avgLum) / stdLum;
                        double meanGH = gradH / (count * stdLum);
                        double meanGV = gradV / (count * stdLum);
                        double meanGD = gradD / (count * stdLum);
                        double mag = Math.Sqrt(meanGH * meanGH + meanGV * meanGV);

                        vector[vecIdx++] = (float)relContrast;
                        vector[vecIdx++] = (float)meanGH;
                        vector[vecIdx++] = (float)meanGV;
                        vector[vecIdx++] = (float)meanGD;
                        vector[vecIdx++] = (float)mag;
                        vector[vecIdx++] = (float)(meanGD / (meanGH + 0.1));
                        vector[vecIdx++] = (float)(relContrast * meanGH);
                        vector[vecIdx++] = (float)(relContrast * meanGV);
                    }
                }
            }

            // Normalize vector to L2 unit norm (length = 1.0)
            double sumSquares = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                sumSquares += vector[i] * vector[i];
            }

            double norm = Math.Sqrt(sumSquares);
            if (norm > 1e-6)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = (float)(vector[i] / norm);
                }
            }

            return (true, "Face detected successfully.", vector);
        }
        catch (Exception ex)
        {
            return (false, $"Face detection error: {ex.Message}", null);
        }
    }

    private static float ComputeCosineSimilarity(float[] v1, float[] v2)
    {
        if (v1.Length != v2.Length) return 0f;
        float dot = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
        }
        return Math.Clamp(dot, 0f, 1f);
    }

    private static string EncodeVector(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    private static float[]? DecodeVector(string base64)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            var bytes = Convert.FromBase64String(base64);
            var vector = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            return vector;
        }
        catch
        {
            return null;
        }
    }
}
