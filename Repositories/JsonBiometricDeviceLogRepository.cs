using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonBiometricDeviceLogRepository : IBiometricDeviceLogRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonBiometricDeviceLogRepository> _logger;

    public JsonBiometricDeviceLogRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonBiometricDeviceLogRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BiometricDeviceLogModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.BiometricLogsFilePath))
        {
            return new List<BiometricDeviceLogModel>();
        }
        var list = await _storage.ReadListAsync<BiometricDeviceLogModel>(_pathProvider.BiometricLogsFilePath);
        return list.OrderByDescending(l => l.DevicePunchTime).ToList();
    }

    public async Task<IReadOnlyList<BiometricDeviceLogModel>> GetRecentAsync(int limit = 100)
    {
        var all = await GetAllAsync();
        return all.Take(limit).ToList();
    }

    public async Task<IReadOnlyList<BiometricDeviceLogModel>> GetByDateAsync(DateOnly date)
    {
        var all = await GetAllAsync();
        return all.Where(l => DateOnly.FromDateTime(l.DevicePunchTime) == date).ToList();
    }

    public async Task AddAsync(BiometricDeviceLogModel log)
    {
        var all = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(log.LogId))
        {
            log.LogId = Guid.NewGuid().ToString();
        }
        log.IngestedAt = DateTime.UtcNow;
        all.Insert(0, log);

        // Keep last 5000 logs to prevent unbounded file growth in demo
        if (all.Count > 5000)
        {
            all = all.Take(5000).ToList();
        }

        await _storage.WriteAsync(_pathProvider.BiometricLogsFilePath, all);
        _logger.LogInformation("Saved biometric device punch log: {LogId} for {Identifier}", log.LogId, log.RawEmployeeIdentifier);
    }

    public async Task AddBatchAsync(IEnumerable<BiometricDeviceLogModel> logs)
    {
        var all = (await GetAllAsync()).ToList();
        foreach (var log in logs)
        {
            if (string.IsNullOrEmpty(log.LogId))
            {
                log.LogId = Guid.NewGuid().ToString();
            }
            log.IngestedAt = DateTime.UtcNow;
            all.Insert(0, log);
        }

        if (all.Count > 5000)
        {
            all = all.Take(5000).ToList();
        }

        await _storage.WriteAsync(_pathProvider.BiometricLogsFilePath, all);
        _logger.LogInformation("Saved batch of biometric device punch logs");
    }
}
