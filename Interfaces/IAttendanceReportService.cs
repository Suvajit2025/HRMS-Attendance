using HRMS.ViewModels;

namespace HRMS.Interfaces;

public interface IAttendanceReportService
{
    Task<MonthlyAttendanceReportViewModel> GenerateMonthlyReportAsync(AttendanceReportFilterModel filter, string preparedBy);
}

