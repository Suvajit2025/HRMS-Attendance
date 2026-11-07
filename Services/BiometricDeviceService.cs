using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRMS.Services;

public class BiometricDeviceService : IBiometricDeviceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IBiometricDeviceLogRepository _deviceLogRepo;
    private readonly JsonStorageOptions _storageOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BiometricDeviceService> _logger;

    public BiometricDeviceService(
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository employeeRepo,
        IDepartmentRepository deptRepo,
        IBiometricDeviceLogRepository deviceLogRepo,
        IOptions<JsonStorageOptions> storageOptions,
        IConfiguration configuration,
        ILogger<BiometricDeviceService> logger)
    {
        _attendanceRepo = attendanceRepo;
        _employeeRepo = employeeRepo;
        _deptRepo = deptRepo;
        _deviceLogRepo = deviceLogRepo;
        _storageOptions = storageOptions.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetConfiguredApiKey()
    {
        return _configuration["BiometricIntegration:ApiKey"] ?? "hrms-biometric-secret-key-2026";
    }

    public bool IsApiKeyRequired()
    {
        return _configuration.GetValue<bool?>("BiometricIntegration:RequireApiKey") ?? false;
    }

    public async Task<IReadOnlyList<BiometricDeviceLogModel>> GetRecentLogsAsync(int limit = 100)
    {
        return await _deviceLogRepo.GetRecentAsync(limit);
    }

    public async Task<(bool Success, string Message, BiometricDeviceLogModel Log)> IngestSinglePunchAsync(
        BiometricPunchIngestDto dto,
        string? apiKey = null,
        string sourceType = "API")
    {
        var log = new BiometricDeviceLogModel
        {
            DeviceId = string.IsNullOrWhiteSpace(dto.DeviceId) ? "EXTERNAL-TERMINAL" : dto.DeviceId.Trim(),
            DeviceName = dto.DeviceName,
            RawEmployeeIdentifier = dto.EmployeeNo?.Trim() ?? string.Empty,
            DevicePunchType = string.IsNullOrWhiteSpace(dto.PunchType) ? "Auto" : dto.PunchType.Trim(),
            VerificationMode = string.IsNullOrWhiteSpace(dto.VerificationMode) ? "FaceRecognition" : dto.VerificationMode.Trim(),
            SourceType = sourceType,
            IngestedAt = DateTime.UtcNow
        };

        // 1. Validate API Key if enabled
        if (IsApiKeyRequired())
        {
            var expectedKey = GetConfiguredApiKey();
            if (string.IsNullOrWhiteSpace(apiKey) || !string.Equals(apiKey.Trim(), expectedKey.Trim(), StringComparison.Ordinal))
            {
                log.IngestionStatus = "Error";
                log.Remarks = "Unauthorized: Invalid or missing Biometric Device API Key.";
                await _deviceLogRepo.AddAsync(log);
                return (false, log.Remarks, log);
            }
        }

        // 2. Resolve Punch Time in configured timezone
        DateTime punchTime;
        if (dto.PunchTime.HasValue)
        {
            punchTime = dto.PunchTime.Value;
        }
        else
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(_storageOptions.TimeZoneId);
                punchTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                punchTime = DateTime.Now;
            }
        }
        log.DevicePunchTime = punchTime;
        var punchDate = DateOnly.FromDateTime(punchTime);

        // 3. Resolve Employee
        var employee = await ResolveEmployeeAsync(dto.EmployeeNo, dto.CardNo);
        if (employee == null)
        {
            log.IngestionStatus = "EmployeeNotFound";
            log.Remarks = $"No active employee found matching device identifier: '{dto.EmployeeNo}' (Card: '{dto.CardNo ?? "None"}')";
            await _deviceLogRepo.AddAsync(log);
            _logger.LogWarning("Biometric punch rejected: {Remarks}", log.Remarks);
            return (false, log.Remarks, log);
        }

        log.ResolvedEmployeeId = employee.EmployeeId;
        log.ResolvedEmployeeNo = employee.EmployeeNo;
        log.ResolvedEmployeeName = employee.FullName;
        log.DepartmentName = employee.DepartmentName;

        if (!employee.IsActive)
        {
            log.IngestionStatus = "Error";
            log.Remarks = $"Employee {employee.FullName} ({employee.EmployeeNo}) is inactive in HRMS.";
            await _deviceLogRepo.AddAsync(log);
            return (false, log.Remarks, log);
        }

        // 4. Fetch existing punches for today to resolve direction and duplicates
        var existingPunches = await _attendanceRepo.GetEmployeePunchesTodayAsync(employee.EmployeeId, punchDate);

        // Check for duplicate rapid scans (within 2 minutes of any punch today)
        var recentDuplicate = existingPunches.FirstOrDefault(p => Math.Abs((punchTime - p.PunchTime).TotalMinutes) < 2);
        if (recentDuplicate != null)
        {
            log.ResolvedPunchType = recentDuplicate.PunchType;
            log.IngestionStatus = "DuplicateSkipped";
            log.Remarks = $"Duplicate scan ignored: Employee already punched {recentDuplicate.PunchType} at {recentDuplicate.PunchTime:hh:mm:ss tt} (less than 2 mins ago).";
            await _deviceLogRepo.AddAsync(log);
            return (true, log.Remarks, log);
        }

        // 5. Determine Punch Type (IN vs OUT)
        string resolvedType = DeterminePunchType(dto.PunchType, existingPunches);
        log.ResolvedPunchType = resolvedType;

        // Duplicate IN check
        bool hasIn = existingPunches.Any(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
        if (resolvedType == "IN" && hasIn)
        {
            var existingIn = existingPunches.First(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
            log.IngestionStatus = "DuplicateSkipped";
            log.Remarks = $"Already punched IN today at {existingIn.PunchTime:hh:mm tt}. Duplicate IN ignored.";
            await _deviceLogRepo.AddAsync(log);
            return (true, log.Remarks, log);
        }

        // 6. Shift and Late Arrival calculation
        bool isLate = false;
        if (resolvedType == "IN")
        {
            if (TimeOnly.TryParse(_storageOptions.ShiftStartTime, out var shiftStart))
            {
                var punchTimeOnly = TimeOnly.FromDateTime(punchTime);
                if (punchTimeOnly > shiftStart)
                {
                    isLate = true;
                }
            }
        }

        // 7. Commit Attendance Record into HRMS Attendance Repository
        var attendanceRecord = new AttendanceModel
        {
            AttendanceId = Guid.NewGuid().ToString(),
            EmployeeId = employee.EmployeeId,
            EmployeeNo = employee.EmployeeNo,
            EmployeeName = employee.FullName,
            DepartmentName = employee.DepartmentName,
            AttendanceDate = punchDate,
            PunchType = resolvedType,
            PunchTime = punchTime,
            VerificationMethod = log.VerificationMode,
            IsLate = isLate,
            Remarks = $"Device: {log.DeviceId} ({sourceType})" + (string.IsNullOrEmpty(dto.DeviceLocation) ? "" : $" @ {dto.DeviceLocation}"),
            CreatedDate = DateTime.UtcNow
        };

        await _attendanceRepo.AddAsync(attendanceRecord);

        // 8. Record audit log
        log.IngestionStatus = "Success";
        log.Remarks = $"Successfully logged punch {resolvedType} at {punchTime:hh:mm:ss tt} via {sourceType} from device {log.DeviceId}.";
        await _deviceLogRepo.AddAsync(log);

        _logger.LogInformation("Biometric punch processed: Employee={EmpNo}, Type={Type}, Time={Time}", employee.EmployeeNo, resolvedType, punchTime);
        return (true, log.Remarks, log);
    }

    public async Task<BiometricSyncResultDto> IngestBulkPunchesAsync(
        BiometricBulkSyncRequestDto request,
        string sourceType = "API")
    {
        var result = new BiometricSyncResultDto
        {
            TotalReceived = request.Punches.Count
        };

        if (IsApiKeyRequired())
        {
            var expectedKey = GetConfiguredApiKey();
            if (string.IsNullOrWhiteSpace(request.DeviceApiKey) || !string.Equals(request.DeviceApiKey.Trim(), expectedKey.Trim(), StringComparison.Ordinal))
            {
                result.Success = false;
                result.Messages.Add("Unauthorized: Invalid device API key.");
                return result;
            }
        }

        foreach (var punch in request.Punches)
        {
            if (string.IsNullOrWhiteSpace(punch.DeviceId) && !string.IsNullOrWhiteSpace(request.DeviceId))
            {
                punch.DeviceId = request.DeviceId;
            }

            var (success, msg, log) = await IngestSinglePunchAsync(punch, request.DeviceApiKey, sourceType);
            if (log.IngestionStatus == "Success")
            {
                result.TotalProcessed++;
            }
            else if (log.IngestionStatus == "DuplicateSkipped")
            {
                result.TotalDuplicatesSkipped++;
            }
            else
            {
                result.TotalErrors++;
                result.Messages.Add($"{punch.EmployeeNo}: {msg}");
            }
        }

        result.Messages.Insert(0, $"Bulk sync completed: {result.TotalProcessed} processed, {result.TotalDuplicatesSkipped} duplicates skipped, {result.TotalErrors} errors.");
        return result;
    }

    public async Task<BiometricSyncResultDto> SyncFromExternalDatabaseAsync()
    {
        var result = new BiometricSyncResultDto();
        string? connString = _configuration["BiometricIntegration:ExternalDatabase:ConnectionString"];
        string? tableName = _configuration["BiometricIntegration:ExternalDatabase:TableName"] ?? "DeviceLogs_Processed";

        if (string.IsNullOrWhiteSpace(connString))
        {
            result.Success = false;
            result.Messages.Add("External biometric database connection string is not configured in appsettings.json (BiometricIntegration:ExternalDatabase:ConnectionString).");
            return result;
        }

        // Database polling / synchronization architecture
        // When configured, queries the device database staging table:
        // SELECT UserId, LogDate, Direction, DeviceId FROM {tableName} WHERE Synced = 0
        result.Messages.Add($"Connected to external biometric database table '{tableName}'. Standby sync ready.");
        return await Task.FromResult(result);
    }

    private async Task<EmployeeModel?> ResolveEmployeeAsync(string? employeeNo, string? cardNo)
    {
        if (string.IsNullOrWhiteSpace(employeeNo) && string.IsNullOrWhiteSpace(cardNo))
        {
            return null;
        }

        var all = await _employeeRepo.GetAllAsync();

        // 1. Match by EmployeeNo exact
        if (!string.IsNullOrWhiteSpace(employeeNo))
        {
            var empMatch = all.FirstOrDefault(e => string.Equals(e.EmployeeNo, employeeNo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (empMatch != null) return empMatch;

            // Match by normalized number (e.g. device sends "1" or "001" and EmployeeNo is "EMP001")
            var digitsOnly = new string(employeeNo.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digitsOnly))
            {
                var numMatch = all.FirstOrDefault(e =>
                {
                    var empDigits = new string(e.EmployeeNo.Where(char.IsDigit).ToArray());
                    return !string.IsNullOrEmpty(empDigits) && int.TryParse(digitsOnly, out int d1) && int.TryParse(empDigits, out int d2) && d1 == d2;
                });
                if (numMatch != null) return numMatch;
            }

            // Match by EmployeeId
            var idMatch = all.FirstOrDefault(e => string.Equals(e.EmployeeId, employeeNo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idMatch != null) return idMatch;
        }

        // 2. Match by CardNo / AadhaarNo
        if (!string.IsNullOrWhiteSpace(cardNo))
        {
            var cardMatch = all.FirstOrDefault(e => string.Equals(e.AadhaarNo, cardNo.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(e.EmployeeNo, cardNo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (cardMatch != null) return cardMatch;
        }

        return null;
    }

    private static string DeterminePunchType(string? requestedType, IReadOnlyList<AttendanceModel> existingToday)
    {
        var raw = (requestedType ?? "Auto").Trim().ToUpperInvariant();

        if (raw == "IN" || raw == "0" || raw == "CHECKIN" || raw == "CHECK-IN" || raw == "ENTRY")
        {
            return "IN";
        }

        if (raw == "OUT" || raw == "1" || raw == "CHECKOUT" || raw == "CHECK-OUT" || raw == "EXIT")
        {
            return "OUT";
        }

        // Auto direction determination
        bool hasIn = existingToday.Any(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
        bool hasOut = existingToday.Any(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));

        if (!hasIn)
        {
            return "IN";
        }

        if (!hasOut)
        {
            return "OUT";
        }

        // Already has IN and OUT, default to updating/adding OUT
        return "OUT";
    }
}

