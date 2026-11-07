using HRMS.Models;

namespace HRMS.Interfaces;

public interface IBiometricDeviceService
{
    Task<(bool Success, string Message, BiometricDeviceLogModel Log)> IngestSinglePunchAsync(
        BiometricPunchIngestDto dto,
        string? apiKey = null,
        string sourceType = "API");

    Task<BiometricSyncResultDto> IngestBulkPunchesAsync(
        BiometricBulkSyncRequestDto request,
        string sourceType = "API");

    Task<BiometricSyncResultDto> SyncFromExternalDatabaseAsync();

    Task<IReadOnlyList<BiometricDeviceLogModel>> GetRecentLogsAsync(int limit = 100);

    string GetConfiguredApiKey();
    bool IsApiKeyRequired();
}
