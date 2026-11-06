using HRMS.ViewModels;

namespace HRMS.Interfaces;

public interface IAttendanceExcelExportService
{
    byte[] ExportMonthlyReportToExcel(MonthlyAttendanceReportViewModel reportModel);
}

