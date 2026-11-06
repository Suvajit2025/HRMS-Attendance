using HRMS.Models;
using HRMS.ViewModels;

namespace HRMS.Interfaces;

public interface IAttendanceService
{
    Task<(bool Success, string Message, AttendanceModel? Punch)> ProcessPunchAsync(
        string employeeId,
        string requestedPunchType,
        string verificationMethod = "Manual",
        float? matchScore = null,
        string? capturedPhotoBase64 = null);

    Task<DailyAttendanceViewModel> GetDailyAttendanceAsync(DateOnly date, string? departmentId = null, string? searchTerm = null);
    Task<AttendanceHistoryViewModel> GetEmployeeHistoryAsync(string? employeeId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<AttendanceTerminalViewModel> GetTerminalViewModelAsync();
}

