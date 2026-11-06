using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonLeaveTransactionRepository : ILeaveTransactionRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonLeaveTransactionRepository> _logger;

    public JsonLeaveTransactionRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonLeaveTransactionRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveTransactionModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.LeaveTransactionsFilePath))
        {
            return new List<LeaveTransactionModel>();
        }
        var list = await _storage.ReadListAsync<LeaveTransactionModel>(_pathProvider.LeaveTransactionsFilePath);
        return list.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<IReadOnlyList<LeaveTransactionModel>> GetByEmployeeIdAsync(string employeeId)
    {
        var all = await GetAllAsync();
        return all.Where(t => string.Equals(t.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
                  .OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<IReadOnlyList<LeaveTransactionModel>> GetByApplicationIdAsync(string applicationId)
    {
        var all = await GetAllAsync();
        return all.Where(t => string.Equals(t.LeaveApplicationId, applicationId, StringComparison.OrdinalIgnoreCase))
                  .OrderBy(t => t.TransactionDate).ToList();
    }

    public async Task AddAsync(LeaveTransactionModel transaction)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(transaction.LeaveTransactionId))
        {
            transaction.LeaveTransactionId = Guid.NewGuid().ToString();
        }
        if (transaction.TransactionDate == default)
        {
            transaction.TransactionDate = DateTime.UtcNow;
        }
        list.Add(transaction);
        await _storage.WriteAsync(_pathProvider.LeaveTransactionsFilePath, list);
        _logger.LogInformation("Recorded leave transaction: {Type} ({Qty}) for Emp {Emp}", transaction.TransactionType, transaction.Quantity, transaction.EmployeeId);
    }
}

