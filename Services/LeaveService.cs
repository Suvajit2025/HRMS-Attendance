using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveApplicationRepository _appRepo;
    private readonly ILeaveBalanceRepository _balanceRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly ILeavePolicyRepository _policyRepo;
    private readonly ILeaveTransactionRepository _transRepo;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveCalculationService _calcService;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveApplicationRepository appRepo,
        ILeaveBalanceRepository balanceRepo,
        ILeaveTypeRepository leaveTypeRepo,
        ILeavePolicyRepository policyRepo,
        ILeaveTransactionRepository transRepo,
        IEmployeeRepository empRepo,
        ILeaveCalculationService calcService,
        ILogger<LeaveService> logger)
    {
        _appRepo = appRepo;
        _balanceRepo = balanceRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _policyRepo = policyRepo;
        _transRepo = transRepo;
        _empRepo = empRepo;
        _calcService = calcService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveApplicationModel>> GetAllApplicationsAsync(string? employeeId = null, string? status = null, string? leaveTypeId = null)
    {
        var all = await _appRepo.GetAllAsync();
        var query = all.AsEnumerable();

        if (!string.IsNullOrEmpty(employeeId))
        {
            query = query.Where(a => string.Equals(a.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(leaveTypeId) && !string.Equals(leaveTypeId, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => string.Equals(a.LeaveTypeId, leaveTypeId, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(a => a.ApplicationDate).ToList();
    }

    public async Task<LeaveApplicationModel?> GetApplicationByIdAsync(string applicationId)
    {
        return await _appRepo.GetByIdAsync(applicationId);
    }

    public async Task<(bool Success, string? ErrorMessage, LeaveApplicationModel? Application)> SubmitLeaveApplicationAsync(LeaveApplicationModel application)
    {
        // 1. Validate employee
        var employee = await _empRepo.GetByIdAsync(application.EmployeeId);
        if (employee == null)
        {
            return (false, "Employee not found.", null);
        }

        if (!employee.IsActive)
        {
            return (false, "Cannot submit leave application for an inactive employee.", null);
        }

        // 2. Validate leave type
        var leaveType = await _leaveTypeRepo.GetByIdAsync(application.LeaveTypeId)
                        ?? await _leaveTypeRepo.GetByCodeAsync(application.LeaveTypeId);
        if (leaveType == null || !leaveType.IsActive)
        {
            return (false, "Invalid or inactive leave type selected.", null);
        }
        application.LeaveTypeId = leaveType.LeaveTypeId;

        // 3. Validate dates
        if (application.ToDate < application.FromDate)
        {
            return (false, "Leave end date cannot be earlier than start date.", null);
        }

        // 4. Validate supporting document if required
        if (leaveType.RequiresDocument && string.IsNullOrWhiteSpace(application.SupportingDocumentPath))
        {
            return (false, $"{leaveType.LeaveName} requires a supporting document to be uploaded.", null);
        }

        // 5. Check overlapping applications (Pending or Approved)
        var existingApps = await _appRepo.GetByEmployeeIdAsync(application.EmployeeId);
        var hasOverlap = existingApps.Any(a =>
            (a.Status == "Pending" || a.Status == "Approved") &&
            a.FromDate <= application.ToDate &&
            a.ToDate >= application.FromDate);

        if (hasOverlap)
        {
            return (false, "An active leave application already exists covering the selected dates.", null);
        }

        // 6. Calculate net duration
        var calcResult = await _calcService.CalculateDurationAsync(
            application.FromDate,
            application.ToDate,
            application.LeaveTypeId,
            employee.DepartmentId,
            null,
            application.IsHalfDay,
            application.HalfDaySession);

        if (calcResult.TotalDays <= 0)
        {
            return (false, "The selected date range does not contain any chargeable working days (falls entirely on holidays/weekly offs).", null);
        }

        application.TotalDays = calcResult.TotalDays;

        // 7. Balance check & reservation
        int currentYear = application.FromDate.Year;
        await EnsureEmployeeBalancesInitializedAsync(employee.EmployeeId, currentYear);

        var balance = await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(employee.EmployeeId, application.LeaveTypeId, currentYear);
        if (leaveType.IsPaidLeave && balance != null)
        {
            var policy = (await _policyRepo.GetByLeaveTypeIdAsync(application.LeaveTypeId)).FirstOrDefault();
            bool allowNegative = policy?.AllowNegativeBalance ?? false;

            if (!allowNegative && balance.AvailableBalance < application.TotalDays)
            {
                return (false, $"Insufficient leave balance. Requested: {application.TotalDays} day(s), Available: {balance.AvailableBalance} day(s).", null);
            }

            // Reserve the balance
            balance.Reserved += application.TotalDays;
            await _balanceRepo.UpdateAsync(balance);

            // Record transaction
            await _transRepo.AddAsync(new LeaveTransactionModel
            {
                EmployeeId = employee.EmployeeId,
                LeaveTypeId = application.LeaveTypeId,
                LeaveApplicationId = application.LeaveApplicationId,
                TransactionType = "LeaveReserved",
                Quantity = application.TotalDays,
                Reference = $"App Ref: {application.LeaveApplicationId}",
                Remarks = $"Reserved {application.TotalDays} day(s) for leave application",
                CreatedBy = "System"
            });
        }

        application.Status = "Pending";
        application.ApplicationDate = DateTime.UtcNow;

        await _appRepo.AddAsync(application);
        _logger.LogInformation("Leave application {AppId} submitted for Employee {EmpId}", application.LeaveApplicationId, employee.EmployeeId);

        return (true, null, application);
    }

    public async Task<(bool Success, string? ErrorMessage)> ApproveLeaveApplicationAsync(string applicationId, string approvedBy)
    {
        var application = await _appRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            return (false, "Leave application not found.");
        }

        if (application.Status == "Approved")
        {
            // Idempotent
            return (true, null);
        }

        if (application.Status != "Pending")
        {
            return (false, $"Cannot approve application in '{application.Status}' status.");
        }

        int currentYear = application.FromDate.Year;
        var balance = await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(application.EmployeeId, application.LeaveTypeId, currentYear);
        if (balance != null)
        {
            // Transition from Reserved -> Consumed
            balance.Reserved = Math.Max(0, balance.Reserved - application.TotalDays);
            balance.Used += application.TotalDays;
            await _balanceRepo.UpdateAsync(balance);

            await _transRepo.AddAsync(new LeaveTransactionModel
            {
                EmployeeId = application.EmployeeId,
                LeaveTypeId = application.LeaveTypeId,
                LeaveApplicationId = application.LeaveApplicationId,
                TransactionType = "LeaveConsumed",
                Quantity = application.TotalDays,
                Reference = $"App Ref: {application.LeaveApplicationId}",
                Remarks = $"Approved by {approvedBy}",
                CreatedBy = approvedBy
            });
        }

        application.Status = "Approved";
        application.ApprovedBy = approvedBy;
        application.ApprovedDate = DateTime.UtcNow;
        await _appRepo.UpdateAsync(application);

        _logger.LogInformation("Leave application {AppId} approved by {User}", applicationId, approvedBy);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectLeaveApplicationAsync(string applicationId, string rejectionReason, string rejectedBy)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            return (false, "A reason must be provided when rejecting a leave application.");
        }

        var application = await _appRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            return (false, "Leave application not found.");
        }

        if (application.Status != "Pending")
        {
            return (false, $"Cannot reject application in '{application.Status}' status.");
        }

        int currentYear = application.FromDate.Year;
        var balance = await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(application.EmployeeId, application.LeaveTypeId, currentYear);
        if (balance != null)
        {
            // Release reserved balance
            balance.Reserved = Math.Max(0, balance.Reserved - application.TotalDays);
            await _balanceRepo.UpdateAsync(balance);

            await _transRepo.AddAsync(new LeaveTransactionModel
            {
                EmployeeId = application.EmployeeId,
                LeaveTypeId = application.LeaveTypeId,
                LeaveApplicationId = application.LeaveApplicationId,
                TransactionType = "LeaveReleased",
                Quantity = application.TotalDays,
                Reference = $"App Ref: {application.LeaveApplicationId}",
                Remarks = $"Rejected by {rejectedBy}: {rejectionReason}",
                CreatedBy = rejectedBy
            });
        }

        application.Status = "Rejected";
        application.RejectionReason = rejectionReason;
        application.ApprovedBy = rejectedBy;
        application.ApprovedDate = DateTime.UtcNow;
        await _appRepo.UpdateAsync(application);

        _logger.LogInformation("Leave application {AppId} rejected by {User}", applicationId, rejectedBy);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> CancelLeaveApplicationAsync(string applicationId, string cancellationReason, string cancelledBy)
    {
        var application = await _appRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            return (false, "Leave application not found.");
        }

        if (application.Status == "Cancelled" || application.Status == "Rejected")
        {
            return (false, $"Application is already {application.Status}.");
        }

        int currentYear = application.FromDate.Year;
        var balance = await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(application.EmployeeId, application.LeaveTypeId, currentYear);

        if (balance != null)
        {
            if (application.Status == "Pending")
            {
                // Release reservation
                balance.Reserved = Math.Max(0, balance.Reserved - application.TotalDays);
            }
            else if (application.Status == "Approved")
            {
                // Release consumption
                balance.Used = Math.Max(0, balance.Used - application.TotalDays);
            }
            await _balanceRepo.UpdateAsync(balance);

            await _transRepo.AddAsync(new LeaveTransactionModel
            {
                EmployeeId = application.EmployeeId,
                LeaveTypeId = application.LeaveTypeId,
                LeaveApplicationId = application.LeaveApplicationId,
                TransactionType = "LeaveReleased",
                Quantity = application.TotalDays,
                Reference = $"App Ref: {application.LeaveApplicationId}",
                Remarks = $"Cancelled by {cancelledBy}: {cancellationReason}",
                CreatedBy = cancelledBy
            });
        }

        application.Status = "Cancelled";
        application.CancellationReason = cancellationReason;
        await _appRepo.UpdateAsync(application);

        _logger.LogInformation("Leave application {AppId} cancelled by {User}", applicationId, cancelledBy);
        return (true, null);
    }

    public async Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetEmployeeBalancesAsync(string employeeId, int? financialYear = null)
    {
        int year = financialYear ?? DateTime.Today.Year;
        await EnsureEmployeeBalancesInitializedAsync(employeeId, year);
        return await _balanceRepo.GetByEmployeeIdAsync(employeeId, year);
    }

    public async Task<EmployeeLeaveBalanceModel?> GetEmployeeBalanceAsync(string employeeId, string leaveTypeId, int? financialYear = null)
    {
        int year = financialYear ?? DateTime.Today.Year;
        await EnsureEmployeeBalancesInitializedAsync(employeeId, year);
        return await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(employeeId, leaveTypeId, year);
    }

    public async Task<(bool Success, string? ErrorMessage)> AdjustLeaveBalanceAsync(string employeeId, string leaveTypeId, double adjustmentAmount, string reason, string adjustedBy)
    {
        int year = DateTime.Today.Year;
        await EnsureEmployeeBalancesInitializedAsync(employeeId, year);

        var balance = await _balanceRepo.GetByEmployeeAndLeaveTypeAsync(employeeId, leaveTypeId, year);
        if (balance == null)
        {
            return (false, "Leave balance record not found.");
        }

        balance.Adjusted += adjustmentAmount;
        await _balanceRepo.UpdateAsync(balance);

        await _transRepo.AddAsync(new LeaveTransactionModel
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            TransactionType = "Adjustment",
            Quantity = adjustmentAmount,
            Reference = "Manual Balance Adjustment",
            Remarks = $"{reason} (Adjusted by {adjustedBy})",
            CreatedBy = adjustedBy
        });

        return (true, null);
    }

    public async Task<IReadOnlyList<LeaveTransactionModel>> GetTransactionsByEmployeeAsync(string employeeId)
    {
        return await _transRepo.GetByEmployeeIdAsync(employeeId);
    }

    public async Task<IReadOnlyList<LeaveTransactionModel>> GetAllTransactionsAsync()
    {
        return await _transRepo.GetAllAsync();
    }

    public async Task EnsureEmployeeBalancesInitializedAsync(string employeeId, int financialYear)
    {
        var existingBalances = await _balanceRepo.GetByEmployeeIdAsync(employeeId, financialYear);
        var existingTypes = existingBalances.Select(b => b.LeaveTypeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var policies = await _policyRepo.GetAllAsync();

        foreach (var lt in leaveTypes.Where(l => l.IsActive))
        {
            if (!existingTypes.Contains(lt.LeaveTypeId) && !existingTypes.Contains(lt.LeaveCode))
            {
                var policy = policies.FirstOrDefault(p => string.Equals(p.LeaveTypeId, lt.LeaveTypeId, StringComparison.OrdinalIgnoreCase) ||
                                                          string.Equals(p.LeaveTypeId, lt.LeaveCode, StringComparison.OrdinalIgnoreCase));

                double openingBal = policy != null ? policy.AnnualEntitlement : 0.0;

                var newBalance = new EmployeeLeaveBalanceModel
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = lt.LeaveTypeId,
                    LeavePolicyId = policy?.LeavePolicyId,
                    OpeningBalance = openingBal,
                    Accrued = 0,
                    Used = 0,
                    Reserved = 0,
                    Adjusted = 0,
                    FinancialYear = financialYear
                };

                await _balanceRepo.AddAsync(newBalance);

                if (openingBal > 0)
                {
                    await _transRepo.AddAsync(new LeaveTransactionModel
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = lt.LeaveTypeId,
                        TransactionType = "OpeningBalance",
                        Quantity = openingBal,
                        Reference = "Financial Year Initialization",
                        Remarks = $"Initial balance allocated under policy '{policy?.PolicyName ?? "Standard"}'",
                        CreatedBy = "System"
                    });
                }
            }
        }
    }

    public async Task<IReadOnlyList<LeaveApplicationModel>> GetApprovedLeavesForEmployeeAsync(string employeeId, DateOnly startDate, DateOnly endDate)
    {
        var apps = await _appRepo.GetByEmployeeIdAsync(employeeId);
        return apps.Where(a => a.Status == "Approved" && a.FromDate <= endDate && a.ToDate >= startDate).ToList();
    }
}

