using ClosedXML.Excel;
using HRMS.Interfaces;
using HRMS.ViewModels;

namespace HRMS.Services;

public class AttendanceExcelExportService : IAttendanceExcelExportService
{
    private static readonly XLColor BrandDarkTeal = XLColor.FromArgb(15, 118, 110);    // #0F766E
    private static readonly XLColor BrandHeaderBg = XLColor.FromArgb(240, 253, 250);   // #F0FDFA
    private static readonly XLColor BorderLight = XLColor.FromArgb(226, 232, 240);     // #E2E8F0
    private static readonly XLColor BorderMedium = XLColor.FromArgb(203, 213, 225);    // #CBD5E1

    // 3 Clean Status Colors: P (Present - Green), A (Absent - Red), H (Holiday - Grey)
    private static readonly XLColor CodePBg = XLColor.FromArgb(209, 231, 221); // #D1E7DD (Light Green)
    private static readonly XLColor CodePFg = XLColor.FromArgb(15, 81, 50);    // #0F5132 (Dark Green)
    private static readonly XLColor CodeABg = XLColor.FromArgb(248, 215, 218); // #F8D7DA (Light Red)
    private static readonly XLColor CodeAFg = XLColor.FromArgb(132, 32, 41);   // #842029 (Dark Red)
    private static readonly XLColor CodeHBg = XLColor.FromArgb(233, 236, 239); // #E9ECEF (Light Grey)
    private static readonly XLColor CodeHFg = XLColor.FromArgb(73, 80, 87);    // #495057 (Dark Grey)
    private static readonly XLColor InactiveBg = XLColor.FromArgb(241, 245, 249);

    public byte[] ExportMonthlyReportToExcel(MonthlyAttendanceReportViewModel reportModel)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Employee Attendance");

        // 1. Column Widths
        ws.Column(1).Width = 3;   // Spacing col A
        ws.Column(2).Width = 28;  // Col B: Employee Name / Designation / Department
        ws.Column(3).Width = 14;  // Col C: Employee No
        ws.Column(4).Width = 16;  // Col D: Label / Report Totals

        // Day columns: Col 5 (E) to Col 35 (AI) -> 31 days
        for (int c = 5; c <= 35; c++)
        {
            ws.Column(c).Width = 4.2;
        }

        // Summary columns beyond Day 31:
        ws.Column(36).Width = 3;  // Col 36 (AJ) spacer
        ws.Column(37).Width = 14; // Col 37 (AK): Present (P)
        ws.Column(38).Width = 14; // Col 38 (AL): Absent (A)
        ws.Column(39).Width = 14; // Col 39 (AM): Holiday (H)
        ws.Column(40).Width = 16; // Col 40 (AN): Attendance %

        // 2. Report Header (Rows 2 - 4)
        ws.Row(2).Height = 28;
        ws.Cell("B2").Value = "HRMS — MONTHLY EMPLOYEE ATTENDANCE REPORT";
        ws.Cell("B2").Style.Font.FontSize = 16;
        ws.Cell("B2").Style.Font.Bold = true;
        ws.Cell("B2").Style.Font.FontColor = BrandDarkTeal;
        ws.Range("B2:AN2").Merge();

        ws.Row(3).Height = 20;
        ws.Cell("B3").Value = "PREPARED BY:";
        ws.Cell("B3").Style.Font.Bold = true;
        ws.Cell("B3").Style.Font.FontSize = 9;
        ws.Cell("B3").Style.Font.FontColor = XLColor.Gray;

        ws.Cell("C3").Value = reportModel.PreparedBy;
        ws.Cell("C3").Style.Font.FontSize = 10;
        ws.Cell("C3").Style.Font.Bold = true;

        ws.Cell("E3").Value = "MONTH AND YEAR:";
        ws.Cell("E3").Style.Font.Bold = true;
        ws.Cell("E3").Style.Font.FontSize = 9;
        ws.Cell("E3").Style.Font.FontColor = XLColor.Gray;
        ws.Range("E3:G3").Merge();

        ws.Cell("H3").Value = reportModel.MonthName.ToUpperInvariant();
        ws.Cell("H3").Style.Font.FontSize = 10;
        ws.Cell("H3").Style.Font.Bold = true;
        ws.Range("H3:K3").Merge();

        ws.Cell("M3").Value = "BUSINESS NAME:";
        ws.Cell("M3").Style.Font.Bold = true;
        ws.Cell("M3").Style.Font.FontSize = 9;
        ws.Cell("M3").Style.Font.FontColor = XLColor.Gray;
        ws.Range("M3:O3").Merge();

        ws.Cell("P3").Value = reportModel.BusinessName;
        ws.Cell("P3").Style.Font.FontSize = 10;
        ws.Cell("P3").Style.Font.Bold = true;
        ws.Range("P3:U3").Merge();

        ws.Cell("W3").Value = "GENERATED:";
        ws.Cell("W3").Style.Font.Bold = true;
        ws.Cell("W3").Style.Font.FontSize = 9;
        ws.Cell("W3").Style.Font.FontColor = XLColor.Gray;
        ws.Range("W3:Y3").Merge();

        ws.Cell("Z3").Value = reportModel.GeneratedDate.ToString("dd MMM yyyy, HH:mm");
        ws.Cell("Z3").Style.Font.FontSize = 10;
        ws.Range("Z3:AE3").Merge();

        // 3. Legend Section (Row 5 - 6) - Exactly 3 Codes: P, A, H
        ws.Row(5).Height = 16;
        ws.Cell("B5").Value = "ATTENDANCE STATUS CODES & LEGEND";
        ws.Cell("B5").Style.Font.Bold = true;
        ws.Cell("B5").Style.Font.FontSize = 9;
        ws.Cell("B5").Style.Font.FontColor = XLColor.DarkSlateGray;

        ws.Row(6).Height = 22;
        // P - Present
        ws.Cell("B6").Value = "P";
        ws.Cell("B6").Style.Fill.BackgroundColor = CodePBg;
        ws.Cell("B6").Style.Font.FontColor = CodePFg;
        ws.Cell("B6").Style.Font.Bold = true;
        ws.Cell("B6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("B6").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        ws.Cell("C6").Value = "PRESENT (ATTENDED)";
        ws.Cell("C6").Style.Font.FontSize = 9;
        ws.Cell("C6").Style.Font.Bold = true;

        // A - Absent
        ws.Cell("E6").Value = "A";
        ws.Cell("E6").Style.Fill.BackgroundColor = CodeABg;
        ws.Cell("E6").Style.Font.FontColor = CodeAFg;
        ws.Cell("E6").Style.Font.Bold = true;
        ws.Cell("E6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("E6").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        ws.Cell("F6").Value = "ABSENT";
        ws.Cell("F6").Style.Font.FontSize = 9;
        ws.Cell("F6").Style.Font.Bold = true;
        ws.Range("F6:H6").Merge();

        // H - Sunday Holiday
        ws.Cell("J6").Value = "H";
        ws.Cell("J6").Style.Fill.BackgroundColor = CodeHBg;
        ws.Cell("J6").Style.Font.FontColor = CodeHFg;
        ws.Cell("J6").Style.Font.Bold = true;
        ws.Cell("J6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("J6").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        ws.Cell("K6").Value = "SUNDAY HOLIDAY";
        ws.Cell("K6").Style.Font.FontSize = 9;
        ws.Cell("K6").Style.Font.Bold = true;
        ws.Range("K6:N6").Merge();

        // Blank - Before Joining / Future
        ws.Cell("P6").Value = "-";
        ws.Cell("P6").Style.Fill.BackgroundColor = InactiveBg;
        ws.Cell("P6").Style.Font.FontColor = XLColor.Gray;
        ws.Cell("P6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("P6").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        ws.Cell("Q6").Value = "BLANK (BEFORE JOINING / FUTURE)";
        ws.Cell("Q6").Style.Font.FontSize = 9;
        ws.Cell("Q6").Style.Font.Bold = true;
        ws.Range("Q6:V6").Merge();

        // 4. Matrix Header (Row 8)
        ws.Row(8).Height = 24;
        ws.Cell("B8").Value = "EMPLOYEE DETAILS";
        ws.Cell("C8").Value = "EMP ID";
        ws.Cell("D8").Value = "RECORD";

        var headerColsRange = ws.Range("B8:D8");
        headerColsRange.Style.Fill.BackgroundColor = BrandDarkTeal;
        headerColsRange.Style.Font.FontColor = XLColor.White;
        headerColsRange.Style.Font.Bold = true;
        headerColsRange.Style.Font.FontSize = 9;
        headerColsRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerColsRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Days 1 through 31
        for (int day = 1; day <= 31; day++)
        {
            int colIdx = 4 + day; // 5 = E
            var cell = ws.Cell(8, colIdx);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.OutsideBorderColor = BorderMedium;

            if (day <= reportModel.DaysInMonth)
            {
                cell.Value = day;
                var dt = new DateOnly(reportModel.Filter.Year, reportModel.Filter.Month, day);
                if (dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(204, 251, 241); // light mint
                    cell.Style.Font.FontColor = BrandDarkTeal;
                }
                else
                {
                    cell.Style.Fill.BackgroundColor = BrandDarkTeal;
                    cell.Style.Font.FontColor = XLColor.White;
                }
            }
            else
            {
                cell.Value = "-";
                cell.Style.Fill.BackgroundColor = InactiveBg;
                cell.Style.Font.FontColor = XLColor.LightGray;
            }
        }

        // Summary columns headers (Cols 37 to 40)
        string[] summaryHeaders = { "Present (P)", "Absent (A)", "Holiday (H)", "Attendance %" };
        for (int s = 0; s < summaryHeaders.Length; s++)
        {
            int colIdx = 37 + s;
            var cell = ws.Cell(8, colIdx);
            cell.Value = summaryHeaders[s];
            cell.Style.Fill.BackgroundColor = BrandDarkTeal;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.OutsideBorderColor = BorderMedium;
        }

        // 5. Employee Report Blocks (Rows 9+)
        int currentRow = 9;

        foreach (var emp in reportModel.Employees)
        {
            int r1 = currentRow;
            int r2 = currentRow + 1;
            int r3 = currentRow + 2;

            ws.Row(r1).Height = 20;
            ws.Row(r2).Height = 17;
            ws.Row(r3).Height = 20;

            // Row 1: Employee Name
            ws.Cell(r1, 2).Value = emp.EmployeeName;
            ws.Cell(r1, 2).Style.Font.Bold = true;
            ws.Cell(r1, 2).Style.Font.FontSize = 10;
            ws.Cell(r1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 1: Employee No
            ws.Cell(r1, 3).Value = emp.EmployeeNo;
            ws.Cell(r1, 3).Style.Font.FontSize = 9;
            ws.Cell(r1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 1: Record type label
            ws.Cell(r1, 4).Value = "Attendance";
            ws.Cell(r1, 4).Style.Font.FontSize = 9;
            ws.Cell(r1, 4).Style.Font.FontColor = XLColor.Gray;
            ws.Cell(r1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 2: Designation
            ws.Cell(r2, 2).Value = emp.Designation;
            ws.Cell(r2, 2).Style.Font.Italic = true;
            ws.Cell(r2, 2).Style.Font.FontSize = 9;
            ws.Cell(r2, 2).Style.Font.FontColor = XLColor.DarkSlateGray;
            ws.Cell(r2, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 3: Department
            ws.Cell(r3, 2).Value = emp.DepartmentName;
            ws.Cell(r3, 2).Style.Font.FontSize = 9;
            ws.Cell(r3, 2).Style.Font.Bold = true;
            ws.Cell(r3, 2).Style.Font.FontColor = BrandDarkTeal;
            ws.Cell(r3, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 3: REPORT TOTALS label
            ws.Cell(r3, 4).Value = "REPORT TOTALS";
            ws.Cell(r3, 4).Style.Font.Bold = true;
            ws.Cell(r3, 4).Style.Font.FontSize = 9;
            ws.Cell(r3, 4).Style.Font.FontColor = XLColor.DarkSlateGray;
            ws.Cell(r3, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Daily Cells: Cols 5 to 35 (E to AI)
            for (int day = 1; day <= 31; day++)
            {
                int colIdx = 4 + day;
                var cellR1 = ws.Cell(r1, colIdx);
                var cellR2 = ws.Cell(r2, colIdx);
                var cellR3 = ws.Cell(r3, colIdx);

                cellR1.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                cellR1.Style.Border.OutsideBorderColor = BorderLight;
                cellR2.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                cellR2.Style.Border.OutsideBorderColor = BorderLight;
                cellR3.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                cellR3.Style.Border.OutsideBorderColor = BorderLight;

                if (day <= reportModel.DaysInMonth)
                {
                    if (emp.DailyStatuses.TryGetValue(day, out var dailyStatus) && !string.IsNullOrEmpty(dailyStatus.StatusCode))
                    {
                        cellR1.Value = dailyStatus.StatusCode;
                        cellR1.Style.Font.Bold = true;
                        cellR1.Style.Font.FontSize = 9;
                        cellR1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cellR1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // 3 Clean Status Codes: P, A, H
                        switch (dailyStatus.StatusCode)
                        {
                            case "P":
                                cellR1.Style.Fill.BackgroundColor = CodePBg;
                                cellR1.Style.Font.FontColor = CodePFg;
                                break;
                            case "A":
                                cellR1.Style.Fill.BackgroundColor = CodeABg;
                                cellR1.Style.Font.FontColor = CodeAFg;
                                break;
                            case "H":
                                cellR1.Style.Fill.BackgroundColor = CodeHBg;
                                cellR1.Style.Font.FontColor = CodeHFg;
                                break;
                        }
                    }
                }
                else
                {
                    cellR1.Style.Fill.BackgroundColor = InactiveBg;
                    cellR2.Style.Fill.BackgroundColor = InactiveBg;
                    cellR3.Style.Fill.BackgroundColor = InactiveBg;
                }
            }

            // Summary calculation cells in Row 3
            string dailyRange = $"E{r1}:AI{r1}";

            // Present (counts P)
            var cellPres = ws.Cell(r3, 37);
            cellPres.FormulaA1 = $"COUNTIF({dailyRange}, \"P\")";
            cellPres.Style.Font.Bold = true;
            cellPres.Style.Font.FontColor = CodePFg;
            cellPres.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellPres.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Absent (counts A)
            var cellAbs = ws.Cell(r3, 38);
            cellAbs.FormulaA1 = $"COUNTIF({dailyRange}, \"A\")";
            cellAbs.Style.Font.Bold = true;
            cellAbs.Style.Font.FontColor = CodeAFg;
            cellAbs.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellAbs.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Sunday Holiday (counts H)
            var cellHol = ws.Cell(r3, 39);
            cellHol.FormulaA1 = $"COUNTIF({dailyRange}, \"H\")";
            cellHol.Style.Font.Bold = true;
            cellHol.Style.Font.FontColor = CodeHFg;
            cellHol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellHol.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Attendance % Formula: Present / (Present + Absent + Holiday) * 100
            string colPres = ws.Cell(r3, 37).Address?.ToString() ?? $"AK{r3}";
            string colAbs = ws.Cell(r3, 38).Address?.ToString() ?? $"AL{r3}";
            string colHol = ws.Cell(r3, 39).Address?.ToString() ?? $"AM{r3}";

            var cellPct = ws.Cell(r3, 40);
            cellPct.FormulaA1 = $"IF(({colPres}+{colAbs}+{colHol})>0, {colPres}/({colPres}+{colAbs}+{colHol}), 0)";
            cellPct.Style.NumberFormat.Format = "0.0%";
            cellPct.Style.Font.Bold = true;
            cellPct.Style.Font.FontColor = BrandDarkTeal;
            cellPct.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellPct.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Summary columns row 1 and 2 borders
            for (int sc = 37; sc <= 40; sc++)
            {
                ws.Cell(r1, sc).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r1, sc).Style.Border.OutsideBorderColor = BorderLight;
                ws.Cell(r2, sc).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r2, sc).Style.Border.OutsideBorderColor = BorderLight;
            }

            var empBlock = ws.Range(r1, 2, r3, 40);
            empBlock.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            empBlock.Style.Border.BottomBorderColor = BorderMedium;

            currentRow += 3;
        }

        // 6. Overall Monthly Summary Block
        int summaryStartRow = currentRow + 1;
        ws.Row(summaryStartRow).Height = 24;

        ws.Cell(summaryStartRow, 2).Value = "OVERALL MONTHLY SUMMARY";
        ws.Cell(summaryStartRow, 2).Style.Font.Bold = true;
        ws.Cell(summaryStartRow, 2).Style.Font.FontSize = 11;
        ws.Cell(summaryStartRow, 2).Style.Font.FontColor = BrandDarkTeal;
        ws.Range(summaryStartRow, 2, summaryStartRow, 4).Merge();

        // Labels row
        int sumHeaderRow = summaryStartRow + 1;
        ws.Row(sumHeaderRow).Height = 20;

        string[] sumLabels = { "Total Employees", "Total Present (P)", "Total Absent (A)", "Total Holidays (H)", "Avg Attendance %" };
        int[] sumCols = { 2, 37, 38, 39, 40 };

        for (int i = 0; i < sumLabels.Length; i++)
        {
            var cell = ws.Cell(sumHeaderRow, sumCols[i]);
            cell.Value = sumLabels[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Font.FontColor = BrandDarkTeal;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.OutsideBorderColor = BorderMedium;
        }

        // Values row
        int sumValRow = sumHeaderRow + 1;
        ws.Row(sumValRow).Height = 22;

        var cellTotEmp = ws.Cell(sumValRow, 2);
        cellTotEmp.Value = reportModel.Employees.Count;
        cellTotEmp.Style.Font.Bold = true;
        cellTotEmp.Style.Font.FontSize = 10;
        cellTotEmp.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cellTotEmp.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        if (reportModel.Employees.Count > 0)
        {
            int firstR3 = 9 + 2;
            int lastR3 = currentRow - 1;

            // Total Present
            var cPres = ws.Cell(sumValRow, 37);
            cPres.FormulaA1 = $"SUM(AK{firstR3}:AK{lastR3})";
            cPres.Style.Font.Bold = true;
            cPres.Style.Font.FontColor = CodePFg;
            cPres.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cPres.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Total Absent
            var cAbs = ws.Cell(sumValRow, 38);
            cAbs.FormulaA1 = $"SUM(AL{firstR3}:AL{lastR3})";
            cAbs.Style.Font.Bold = true;
            cAbs.Style.Font.FontColor = CodeAFg;
            cAbs.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cAbs.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Total Holiday
            var cHol = ws.Cell(sumValRow, 39);
            cHol.FormulaA1 = $"SUM(AM{firstR3}:AM{lastR3})";
            cHol.Style.Font.Bold = true;
            cHol.Style.Font.FontColor = CodeHFg;
            cHol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cHol.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Avg Attendance %
            var cAvg = ws.Cell(sumValRow, 40);
            cAvg.FormulaA1 = $"AVERAGE(AN{firstR3}:AN{lastR3})";
            cAvg.Style.NumberFormat.Format = "0.0%";
            cAvg.Style.Font.Bold = true;
            cAvg.Style.Font.FontColor = BrandDarkTeal;
            cAvg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cAvg.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        // 7. Freeze Panes & Print Settings
        ws.SheetView.FreezeRows(8);
        ws.SheetView.FreezeColumns(4);

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(8, 8);
        ws.PageSetup.PrintAreas.Clear();
        ws.PageSetup.PrintAreas.Add(2, 2, sumValRow, 40);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }
}
