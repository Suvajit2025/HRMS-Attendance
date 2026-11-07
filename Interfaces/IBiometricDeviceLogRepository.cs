using HRMS.Models;

namespace HRMS.Interfaces;

public interface IBiometricDeviceLogRepository
{
    Task<IReadOnlyList<BiometricDeviceLogModel>> GetAllAsync();
    Task<IReadOnlyList<BiometricDeviceLogModel>> GetRecentAsync(int limit = 100);
    Task<IReadOnlyList<BiometricDeviceLogModel>> GetByDateAsync(DateOnly date);
    Task AddAsync(BiometricDeviceLogModel log);
    Task AddBatchAsync(IEnumerable<BiometricDeviceLogModel> logs);
}
