using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[ApiController]
[Route("api/biometric")]
[Produces("application/json")]
public class BiometricApiController : ControllerBase
{
    private readonly IBiometricDeviceService _deviceService;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILogger<BiometricApiController> _logger;

    public BiometricApiController(
        IBiometricDeviceService deviceService,
        IEmployeeRepository empRepo,
        ILogger<BiometricApiController> logger)
    {
        _deviceService = deviceService;
        _empRepo = empRepo;
        _logger = logger;
    }

    /// <summary>
    /// Healthcheck and device connection handshake endpoint
    /// GET /api/biometric/status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var employees = await _empRepo.GetAllAsync();
        return Ok(new
        {
            Status = "Online",
            Service = "HRMS Biometric Ingestion Gateway",
            Version = "2.1.0",
            ServerTimeUtc = DateTime.UtcNow,
            LocalServerTime = DateTime.Now,
            ApiKeyRequired = _deviceService.IsApiKeyRequired(),
            ActiveEmployeesCount = employees.Count(e => e.IsActive)
        });
    }

    /// <summary>
    /// Ingest a real-time punch pushed by an external face recognition terminal
    /// POST /api/biometric/punch
    /// </summary>
    [HttpPost("punch")]
    public async Task<IActionResult> IngestPunch([FromBody] BiometricPunchIngestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.EmployeeNo))
        {
            return BadRequest(new { Success = false, Message = "EmployeeNo / EnrollmentId is required." });
        }

        string? apiKey = ExtractApiKey();
        var (success, message, log) = await _deviceService.IngestSinglePunchAsync(dto, apiKey, sourceType: "REST-API");

        if (!success && log.IngestionStatus == "EmployeeNotFound")
        {
            return NotFound(new
            {
                Success = false,
                Message = message,
                LogId = log.LogId
            });
        }

        if (!success)
        {
            return BadRequest(new
            {
                Success = false,
                Message = message,
                LogId = log.LogId
            });
        }

        return Ok(new
        {
            Success = true,
            Message = message,
            LogId = log.LogId,
            Employee = new
            {
                EmployeeNo = log.ResolvedEmployeeNo,
                FullName = log.ResolvedEmployeeName,
                Department = log.DepartmentName
            },
            Punch = new
            {
                Direction = log.ResolvedPunchType,
                PunchTime = log.DevicePunchTime.ToString("yyyy-MM-dd HH:mm:ss"),
                VerificationMode = log.VerificationMode
            }
        });
    }

    /// <summary>
    /// Ingest a batch of punches pushed by device middleware or polling sync
    /// POST /api/biometric/bulk-sync
    /// </summary>
    [HttpPost("bulk-sync")]
    public async Task<IActionResult> IngestBulk([FromBody] BiometricBulkSyncRequestDto request)
    {
        if (request == null || request.Punches == null || request.Punches.Count == 0)
        {
            return BadRequest(new { Success = false, Message = "No punches found in request payload." });
        }

        if (string.IsNullOrEmpty(request.DeviceApiKey))
        {
            request.DeviceApiKey = ExtractApiKey();
        }

        var result = await _deviceService.IngestBulkPunchesAsync(request, sourceType: "REST-API-Bulk");
        return Ok(result);
    }

    /// <summary>
    /// Trigger manual pull/synchronization from configured external biometric database
    /// POST /api/biometric/sync-db
    /// </summary>
    [HttpPost("sync-db")]
    public async Task<IActionResult> TriggerDatabaseSync()
    {
        var result = await _deviceService.SyncFromExternalDatabaseAsync();
        return Ok(result);
    }

    private string? ExtractApiKey()
    {
        if (Request.Headers.TryGetValue("X-Device-ApiKey", out var h1) && !string.IsNullOrWhiteSpace(h1))
            return h1.ToString();

        if (Request.Headers.TryGetValue("X-API-KEY", out var h2) && !string.IsNullOrWhiteSpace(h2))
            return h2.ToString();

        if (Request.Headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
        {
            var authStr = auth.ToString();
            if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authStr["Bearer ".Length..].Trim();
        }

        if (Request.Query.TryGetValue("apiKey", out var q) && !string.IsNullOrWhiteSpace(q))
            return q.ToString();

        return null;
    }
}

