using System.Drawing;
using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.Repositories;
using HRMS.Services;
using HRMS.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HRMS.Data;

public static class HrmsTestSuite
{
    public static async Task<int> RunAllTestsAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================================");
        Console.WriteLine("       HRMS AUTOMATED VERIFICATION & TEST SUITE (.NET 8)          ");
        Console.WriteLine("==================================================================");
        Console.ResetColor();

        int passed = 0;
        int failed = 0;

        // Create temporary isolated test directory
        var testDir = Path.Combine(Path.GetTempPath(), "HRMS_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var options = Options.Create(new JsonStorageOptions
            {
                DataDirectory = testDir,
                ShiftStartTime = "09:00:00",
                ShiftEndTime = "18:00:00",
                TimeZoneId = "India Standard Time",
                FaceMatchThreshold = 0.65f,
                CompanyName = "Test Apex Industries"
            });

            var pathProvider = new StoragePathProvider(options);
            var fileStorage = new JsonFileStorage(NullLogger<JsonFileStorage>.Instance);

            var deptRepo = new JsonDepartmentRepository(fileStorage, pathProvider, NullLogger<JsonDepartmentRepository>.Instance);
            var empRepo = new JsonEmployeeRepository(fileStorage, pathProvider, NullLogger<JsonEmployeeRepository>.Instance);
            var faceRepo = new JsonFaceTemplateRepository(fileStorage, pathProvider, NullLogger<JsonFaceTemplateRepository>.Instance);
            var attRepo = new JsonAttendanceRepository(fileStorage, pathProvider, NullLogger<JsonAttendanceRepository>.Instance);
            var dashRepo = new JsonDashboardRepository(empRepo, attRepo, deptRepo, options);

            var faceService = new FaceRecognitionService(faceRepo, empRepo, pathProvider, null!, options, NullLogger<FaceRecognitionService>.Instance);
            var empService = new EmployeeService(empRepo, deptRepo, faceService, pathProvider, NullLogger<EmployeeService>.Instance);
            var attService = new AttendanceService(attRepo, empRepo, deptRepo, faceRepo, pathProvider, options, NullLogger<AttendanceService>.Instance);
            var dashService = new DashboardService(dashRepo);

            // TEST 1: Department Seeding
            if (await RunTest("Department Seeding (4 Seed Departments)", async () =>
            {
                var depts = await deptRepo.GetAllAsync();
                Assert(depts.Count >= 4, $"Expected at least 4 departments, found {depts.Count}");
                Assert(depts.Any(d => d.DepartmentName == "Production"), "Missing 'Production' department");
                Assert(depts.Any(d => d.DepartmentName == "Administration"), "Missing 'Administration' department");
                Assert(depts.Any(d => d.DepartmentName == "Human Resources"), "Missing 'Human Resources' department");
                Assert(depts.Any(d => d.DepartmentName == "Security"), "Missing 'Security' department");
            })) passed++; else failed++;

            // TEST 2: Employee Seeding & Aadhaar Masking
            if (await RunTest("Employee Seeding & Aadhaar Masking Protection", async () =>
            {
                var emps = await empRepo.GetAllAsync();
                Assert(emps.Count >= 10, $"Expected at least 10 seed employees, found {emps.Count}");
                var emp1 = emps.First(e => e.EmployeeNo == "EMP001");
                Assert(emp1.MaskedAadhaar.StartsWith("•••• ••••"), "Aadhaar must be masked");
                Assert(emp1.MaskedAadhaar.EndsWith(emp1.AadhaarNo[^4..]), "Masked Aadhaar must show last 4 digits only");
            })) passed++; else failed++;

            // TEST 3: Create Employee & Duplicate EmployeeNo Validation
            if (await RunTest("Create Employee & Unique EmployeeNo Validation", async () =>
            {
                var newEmp = new EmployeeEditViewModel
                {
                    EmployeeNo = "EMP999",
                    FirstName = "TestFirstName",
                    LastName = "TestLastName",
                    DOB = new DateOnly(1990, 5, 20),
                    JoiningDate = DateOnly.FromDateTime(DateTime.Today),
                    AadhaarNo = "123456789012",
                    Gender = "Male",
                    IsActive = true
                };

                var (success1, err1) = await empService.CreateEmployeeAsync(newEmp);
                Assert(success1, $"Employee creation failed: {err1}");

                // Attempt duplicate EmployeeNo
                var (success2, err2) = await empService.CreateEmployeeAsync(newEmp);
                Assert(!success2, "Duplicate EmployeeNo should have been rejected");
                Assert(err2 != null && err2.Contains("already assigned"), $"Expected duplicate error message, got: {err2}");
            })) passed++; else failed++;

            // TEST 4: Invalid Aadhaar Validation
            if (await RunTest("Aadhaar Validation (Exact 12 Digits)", async () =>
            {
                var invalidEmp = new EmployeeEditViewModel
                {
                    EmployeeNo = "EMP998",
                    FirstName = "Invalid",
                    LastName = "Aadhaar",
                    DOB = new DateOnly(1992, 1, 1),
                    JoiningDate = DateOnly.FromDateTime(DateTime.Today),
                    AadhaarNo = "12345", // Only 5 digits
                    IsActive = true
                };

                var (success, _) = await empService.CreateEmployeeAsync(invalidEmp);
                Assert(!success, "Invalid Aadhaar (< 12 digits) must be rejected");
            })) passed++; else failed++;

            // TEST 5: Employee Persistence & Disk Reload
            if (await RunTest("JSON Persistence & Disk Reload", async () =>
            {
                var freshEmpRepo = new JsonEmployeeRepository(fileStorage, pathProvider, NullLogger<JsonEmployeeRepository>.Instance);
                var reloaded = await freshEmpRepo.GetByEmployeeNoAsync("EMP999");
                Assert(reloaded != null, "Employee EMP999 should persist on disk and reload in a new repository instance");
                Assert(reloaded!.FirstName == "TestFirstName", "Reloaded employee first name mismatch");
            })) passed++; else failed++;

            // TEST 6: Attendance Punch IN & Duplicate IN Validation
            if (await RunTest("Attendance Punch IN & Duplicate IN Rejection", async () =>
            {
                var emps = await empRepo.GetAllAsync();
                var emp = emps.First(e => e.EmployeeNo == "EMP999");

                // Valid IN
                var (inSuccess, inMsg, punch) = await attService.ProcessPunchAsync(emp.EmployeeId, "IN", "Manual");
                Assert(inSuccess, $"Punch IN failed: {inMsg}");
                Assert(punch != null && punch.PunchType == "IN", "Punch object missing or not IN");

                // Duplicate IN should be rejected
                var (dupSuccess, dupMsg, _) = await attService.ProcessPunchAsync(emp.EmployeeId, "IN", "Manual");
                Assert(!dupSuccess, "Duplicate IN should be rejected on the same date");
                Assert(dupMsg.Contains("Duplicate Punch IN"), $"Expected duplicate message, got: {dupMsg}");
            })) passed++; else failed++;

            // TEST 7: Attendance OUT without IN Rejection & Valid OUT
            if (await RunTest("Attendance OUT without IN Rejection & Valid Punch OUT", async () =>
            {
                var empFresh = new EmployeeEditViewModel
                {
                    EmployeeNo = "EMP888",
                    FirstName = "NoIn",
                    LastName = "Employee",
                    DOB = new DateOnly(1991, 2, 2),
                    JoiningDate = DateOnly.FromDateTime(DateTime.Today),
                    AadhaarNo = "999888777666",
                    IsActive = true
                };
                await empService.CreateEmployeeAsync(empFresh);
                var fresh = (await empRepo.GetByEmployeeNoAsync("EMP888"))!;

                // OUT without IN must fail
                var (outFail, outFailMsg, _) = await attService.ProcessPunchAsync(fresh.EmployeeId, "OUT", "Manual");
                Assert(!outFail, "OUT without prior IN must be rejected");
                Assert(outFailMsg.Contains("has not punched IN"), $"Unexpected message: {outFailMsg}");

                // Now give EMP999 valid OUT (since EMP999 already has IN)
                var emp999 = (await empRepo.GetByEmployeeNoAsync("EMP999"))!;
                var (outSuccess, outMsg, punchOut) = await attService.ProcessPunchAsync(emp999.EmployeeId, "OUT", "Manual");
                Assert(outSuccess, $"Valid punch OUT failed: {outMsg}");
                Assert(punchOut != null && punchOut.PunchType == "OUT", "Punch type must be OUT");

                // Duplicate OUT must fail
                var (dupOut, dupOutMsg, _) = await attService.ProcessPunchAsync(emp999.EmployeeId, "OUT", "Manual");
                Assert(!dupOut, "Duplicate OUT must be rejected");
                Assert(dupOutMsg.Contains("Duplicate Punch OUT"), $"Unexpected duplicate OUT message: {dupOutMsg}");
            })) passed++; else failed++;

            // TEST 8: Inactive Employee Punch Rejection
            if (await RunTest("Inactive Employee Punch Rejection", async () =>
            {
                var empInactive = new EmployeeEditViewModel
                {
                    EmployeeNo = "EMP777",
                    FirstName = "Inactive",
                    LastName = "Person",
                    DOB = new DateOnly(1989, 3, 3),
                    JoiningDate = DateOnly.FromDateTime(DateTime.Today),
                    AadhaarNo = "888777666555",
                    IsActive = false
                };
                await empService.CreateEmployeeAsync(empInactive);
                var inactive = (await empRepo.GetByEmployeeNoAsync("EMP777"))!;

                var (punchFail, punchMsg, _) = await attService.ProcessPunchAsync(inactive.EmployeeId, "IN", "Manual");
                Assert(!punchFail, "Inactive employee cannot punch");
                Assert(punchMsg.Contains("is inactive"), $"Unexpected message: {punchMsg}");
            })) passed++; else failed++;

            // TEST 9: Face Registration & Cosine Similarity Match
            if (await RunTest("Local Face Registration & Verification Match", async () =>
            {
                var emp = (await empRepo.GetByEmployeeNoAsync("EMP001"))!;
                var dummyBase64 = GenerateTestPortraitBase64();

                var (regSuccess, regMsg, template) = await faceService.RegisterFaceAsync(emp.EmployeeId, dummyBase64);
                Assert(regSuccess, $"Face registration failed: {regMsg}");
                Assert(template != null, "Template was null after registration");

                var hasFace = await faceService.HasRegisteredFaceAsync(emp.EmployeeId);
                Assert(hasFace, "HasRegisteredFaceAsync should return true");

                var matchResult = await faceService.MatchFaceAsync(dummyBase64);
                Assert(matchResult.IsSuccess, $"Face matching failed: {matchResult.Message}");
                Assert(matchResult.EmployeeId == emp.EmployeeId, $"Matched ID mismatch: {matchResult.EmployeeId} vs {emp.EmployeeId}");
                Assert(matchResult.Confidence >= 0.90f, $"Match confidence was too low: {matchResult.Confidence}");
            })) passed++; else failed++;

            // TEST 10: Dashboard KPI Calculations & CSV Export
            if (await RunTest("Dashboard KPI Calculations & CSV Export", async () =>
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var dashboardData = await dashService.GetDashboardDataAsync(today);

                Assert(dashboardData.TotalEmployees > 0, "Total active employees must be > 0");
                Assert(dashboardData.PresentToday > 0, "Present today should be > 0");
                Assert(dashboardData.TotalEmployees == dashboardData.PresentToday + dashboardData.AbsentToday,
                    $"Sum mismatch: Total ({dashboardData.TotalEmployees}) != Present ({dashboardData.PresentToday}) + Absent ({dashboardData.AbsentToday})");
                Assert(dashboardData.AttendancePercentage >= 0 && dashboardData.AttendancePercentage <= 100,
                    $"Attendance % out of bounds: {dashboardData.AttendancePercentage}");
                Assert(dashboardData.Last7DaysTrends.Count == 7, "Last 7 days trends must contain 7 items");

                var csvBytes = await dashService.GenerateAttendanceCsvAsync(today);
                Assert(csvBytes.Length > 100, "CSV bytes too short");
                var csvContent = System.Text.Encoding.UTF8.GetString(csvBytes);
                Assert(csvContent.Contains("Employee No,Employee Name"), "CSV header missing");
                Assert(csvContent.Contains("EMP001"), "CSV should contain employee record EMP001");
            })) passed++; else failed++;

            // TEST 11: Controller Pipeline & Razor View Integration (AccountController)
            if (await RunTest("AccountController & Login View Model Integration", async () =>
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Auth:AdminUsername", "admin" },
                        { "Auth:AdminPassword", "123" }
                    })
                    .Build();

                var accountCtrl = new HRMS.Controllers.AccountController(config, NullLogger<HRMS.Controllers.AccountController>.Instance)
                {
                    ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
                    {
                        HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
                    }
                };

                // GET Login
                var getResult = accountCtrl.Login("/Dashboard") as Microsoft.AspNetCore.Mvc.ViewResult;
                Assert(getResult != null, "GET Login should return ViewResult");
                var getModel = getResult!.Model as LoginViewModel;
                Assert(getModel != null && getModel.ReturnUrl == "/Dashboard", "LoginViewModel returnUrl mismatch");

                // POST Login with invalid credentials
                var invalidResult = await accountCtrl.Login(new LoginViewModel { Username = "wrong", Password = "bad" }) as Microsoft.AspNetCore.Mvc.ViewResult;
                Assert(invalidResult != null, "Invalid login should return ViewResult");
                var invalidModel = invalidResult!.Model as LoginViewModel;
                Assert(invalidModel != null && !string.IsNullOrEmpty(invalidModel.ErrorMessage), "Invalid login should populate ErrorMessage");

                await Task.CompletedTask;
            })) passed++; else failed++;

            // 11. Attendance Report Service & Formula Verification
            if (await RunTest("Attendance Report Service & Calendar Matrix Calculations", async () =>
            {
                var reportService = new AttendanceReportService(attRepo, empRepo, deptRepo, options, NullLogger<AttendanceReportService>.Instance);

                // November 2026 (30 days)
                var novFilter = new AttendanceReportFilterModel { Month = 11, Year = 2026 };
                var novReport = await reportService.GenerateMonthlyReportAsync(novFilter, "Test Administrator");
                Assert(novReport.DaysInMonth == 30, "November must have 30 days");
                Assert(novReport.ValidDays.Count == 30, "November valid days count must be 30");
                Assert(novReport.Employees.Count > 0, "Must have employees in report");
                Assert(novReport.BusinessName == "Test Apex Industries", "Business name mismatch");
                Assert(novReport.PreparedBy == "Test Administrator", "PreparedBy mismatch");

                // Check first employee daily statuses
                var emp1 = novReport.Employees.First();
                Assert(emp1.DailyStatuses.ContainsKey(1) && emp1.DailyStatuses.ContainsKey(30), "Must contain all 30 days in DailyStatuses");
                Assert(!emp1.DailyStatuses.ContainsKey(31), "November must not contain day 31");

                // Formula check: AttendancePercentage = PresentDays / TotalMarkedDays * 100
                int totalMarked = emp1.PresentDays + emp1.AbsentDays + emp1.HolidayDays;
                double expectedPct = totalMarked > 0 ? Math.Round((emp1.PresentDays / (double)totalMarked) * 100.0, 1) : 0.0;
                Assert(Math.Abs(emp1.AttendancePercentage - expectedPct) < 0.05, $"Attendance percentage mismatch: got {emp1.AttendancePercentage}, expected {expectedPct}");

                // Verification of Mid-month Joining Date: Days before joining must be blank (""), NOT Absent ("A")
                var midMonthEmp = new EmployeeEditViewModel
                {
                    EmployeeNo = "EMP_MIDJOIN",
                    FirstName = "MidMonth",
                    LastName = "Joiner",
                    DOB = new DateOnly(1995, 6, 15),
                    JoiningDate = new DateOnly(2026, 10, 15),
                    AadhaarNo = "998877665544",
                    Gender = "Male",
                    IsActive = true
                };
                var (midCreateSuccess, midCreateErr) = await empService.CreateEmployeeAsync(midMonthEmp);
                Assert(midCreateSuccess, $"Failed to create mid-month employee: {midCreateErr}");

                // October 2026 (all dates past)
                var octFilter = new AttendanceReportFilterModel { Month = 10, Year = 2026 };
                var octReport = await reportService.GenerateMonthlyReportAsync(octFilter, "Test Administrator");
                var midEmpData = octReport.Employees.FirstOrDefault(e => e.EmployeeNo == "EMP_MIDJOIN");
                Assert(midEmpData != null, "Mid-month joiner must be present in report");

                // Days 1..14 MUST be blank and IsBeforeJoining == true, NOT Absent ("A")
                for (int day = 1; day < 15; day++)
                {
                    var status = midEmpData!.DailyStatuses[day];
                    Assert(status.StatusCode == "", $"Day {day} before joining date must have StatusCode == '' but got '{status.StatusCode}'");
                    Assert(status.IsBeforeJoining, $"Day {day} before joining date must have IsBeforeJoining == true");
                }

                // Day 15 (joining date, Thursday) must be 'A' (Absent - no punch on working day)
                Assert(!midEmpData!.DailyStatuses[15].IsBeforeJoining, "Day 15 is joining date so IsBeforeJoining must be false");
                Assert(midEmpData.DailyStatuses[15].StatusCode == "A", $"Oct 15 2026 is Thursday unpunched, expected 'A', got '{midEmpData.DailyStatuses[15].StatusCode}'");

                // Day 18 (Sunday) must be 'H' (Sunday Holiday)
                Assert(midEmpData.DailyStatuses[18].StatusCode == "H", $"Oct 18 2026 is Sunday, expected 'H', got '{midEmpData.DailyStatuses[18].StatusCode}'");

                // February 2026 (28 days non-leap)
                var febFilter = new AttendanceReportFilterModel { Month = 2, Year = 2026 };
                var febReport = await reportService.GenerateMonthlyReportAsync(febFilter, "Test Administrator");
                Assert(febReport.DaysInMonth == 28, "February 2026 must have 28 days");
                Assert(febReport.ValidDays.Count == 28, "February valid days count must be 28");

                // October 2026 (31 days)
                Assert(octReport.DaysInMonth == 31, "October 2026 must have 31 days");
                Assert(octReport.ValidDays.Count == 31, "October valid days count must be 31");
            })) passed++; else failed++;

            // 12. Attendance Excel Export Service
            if (await RunTest("Attendance Excel Export Service (.xlsx generation & validation)", async () =>
            {
                var reportService = new AttendanceReportService(attRepo, empRepo, deptRepo, options, NullLogger<AttendanceReportService>.Instance);
                var excelService = new AttendanceExcelExportService();

                var filter = new AttendanceReportFilterModel { Month = 11, Year = 2026 };
                var report = await reportService.GenerateMonthlyReportAsync(filter, "Test Administrator");

                byte[] excelBytes = excelService.ExportMonthlyReportToExcel(report);
                Assert(excelBytes != null && excelBytes.Length > 1000, "Excel output byte array must not be empty");

                // Validate ZIP / XLSX magic number (PK 0x50, 0x4B, 0x03, 0x04)
                Assert(excelBytes![0] == 0x50 && excelBytes[1] == 0x4B, "Must be valid zip/xlsx archive");

                // Validate ClosedXML can read the generated workbook
                using var stream = new MemoryStream(excelBytes);
                using var wb = new ClosedXML.Excel.XLWorkbook(stream);
                Assert(wb.Worksheets.Count > 0, "Workbook must contain worksheets");

                var ws = wb.Worksheet("Employee Attendance");
                Assert(ws != null, "Workbook must have sheet named 'Employee Attendance'");
                Assert(ws!.Cell("B2").GetString().Contains("ATTENDANCE REPORT"), "Header title mismatch");
                Assert(ws.Cell("B3").GetString() == "PREPARED BY:", "B3 should be PREPARED BY:");

                // Validate 3 Status Codes in legend
                Assert(ws.Cell("B6").GetString() == "P", "Legend B6 should have code 'P'");
                Assert(ws.Cell("E6").GetString() == "A", "Legend E6 should have code 'A'");
                Assert(ws.Cell("J6").GetString() == "H", "Legend J6 should have code 'H'");

                // Validate summary column headers (Present, Absent, Holiday, Attendance %)
                Assert(ws.Cell(8, 37).GetString().Contains("Present", StringComparison.OrdinalIgnoreCase), "Col 37 header should be PRESENT");
                Assert(ws.Cell(8, 38).GetString().Contains("Absent", StringComparison.OrdinalIgnoreCase), "Col 38 header should be ABSENT");
                Assert(ws.Cell(8, 39).GetString().Contains("Holiday", StringComparison.OrdinalIgnoreCase), "Col 39 header should be HOLIDAY");
                Assert(ws.Cell(8, 40).GetString().Contains("Attendance %", StringComparison.OrdinalIgnoreCase), "Col 40 header should be ATTENDANCE %");

                await Task.CompletedTask;
            })) passed++; else failed++;

            // Repositories for Holiday & Leave modules
            var holidayRepo = new JsonHolidayRepository(fileStorage, pathProvider, NullLogger<JsonHolidayRepository>.Instance);
            var weeklyOffRepo = new JsonWeeklyOffRepository(fileStorage, pathProvider, NullLogger<JsonWeeklyOffRepository>.Instance);
            var leaveTypeRepo = new JsonLeaveTypeRepository(fileStorage, pathProvider, NullLogger<JsonLeaveTypeRepository>.Instance);
            var leavePolicyRepo = new JsonLeavePolicyRepository(fileStorage, pathProvider, NullLogger<JsonLeavePolicyRepository>.Instance);
            var leaveBalanceRepo = new JsonLeaveBalanceRepository(fileStorage, pathProvider, NullLogger<JsonLeaveBalanceRepository>.Instance);
            var leaveAppRepo = new JsonLeaveApplicationRepository(fileStorage, pathProvider, NullLogger<JsonLeaveApplicationRepository>.Instance);
            var leaveTxnRepo = new JsonLeaveTransactionRepository(fileStorage, pathProvider, NullLogger<JsonLeaveTransactionRepository>.Instance);

            // Services for Holiday & Leave modules
            var holidayService = new HolidayService(holidayRepo, NullLogger<HolidayService>.Instance);
            var weeklyOffService = new WeeklyOffService(weeklyOffRepo, NullLogger<WeeklyOffService>.Instance);
            var leaveCalcService = new LeaveCalculationService(weeklyOffService, holidayService, leavePolicyRepo, NullLogger<LeaveCalculationService>.Instance);
            var leaveService = new LeaveService(leaveAppRepo, leaveBalanceRepo, leaveTypeRepo, leavePolicyRepo, leaveTxnRepo, empRepo, leaveCalcService, NullLogger<LeaveService>.Instance);
            var calendarResolver = new AttendanceCalendarResolver(holidayService, weeklyOffService, leaveService, leaveTypeRepo, empRepo, attRepo);

            // TEST 14: Holiday Master & Scoping Resolution
            if (await RunTest("Holiday Master & Scoping Resolution", async () =>
            {
                var holidays = await holidayService.GetHolidaysAsync(2026, null, null, null);
                Assert(holidays.Count >= 7, $"Expected at least 7 seeded holidays in 2026, got {holidays.Count}");

                // Add scoped holiday for a specific department
                var depts = await deptRepo.GetAllAsync();
                var prodDept = depts.First(d => d.DepartmentName == "Production");

                var scopedHoliday = new HolidayModel
                {
                    HolidayName = "Production Plant Safety Day",
                    HolidayDate = new DateOnly(2026, 4, 15),
                    HolidayType = "Company",
                    DepartmentId = prodDept.DepartmentId,
                    IsPaidHoliday = true,
                    IsActive = true
                };

                var (addSuccess, addMsg, _) = await holidayService.CreateHolidayAsync(scopedHoliday);
                Assert(addSuccess, $"Adding scoped holiday failed: {addMsg}");

                // Duplicate holiday check for same date and same department
                var (dupSuccess, dupMsg, _) = await holidayService.CreateHolidayAsync(new HolidayModel
                {
                    HolidayName = "Duplicate Safety Day",
                    HolidayDate = new DateOnly(2026, 4, 15),
                    DepartmentId = prodDept.DepartmentId
                });
                Assert(!dupSuccess, "Duplicate holiday for same date & scope should be rejected");

                // Check resolution for Production employee vs other department
                var isHolidayProd = await holidayService.IsHolidayAsync(new DateOnly(2026, 4, 15), prodDept.DepartmentId, null);
                Assert(isHolidayProd, "April 15 should be a holiday for Production department");

                var isHolidayAdmin = await holidayService.IsHolidayAsync(new DateOnly(2026, 4, 15), "AdminDeptNonExistent", null);
                Assert(!isHolidayAdmin, "April 15 should NOT be a holiday for non-Production department");

                // Independence Day (Aug 15) is organization-wide
                var isAug15Org = await holidayService.IsHolidayAsync(new DateOnly(2026, 8, 15), null, null);
                Assert(isAug15Org, "August 15 must be an organization-wide holiday");
            })) passed++; else failed++;

            // TEST 15: Weekly Off Configuration (Sundays & Alternate 2nd/4th Saturdays)
            if (await RunTest("Weekly Off Configuration & Alternate Week Matching", async () =>
            {
                // Standard Sunday test
                var sunday = new DateOnly(2026, 11, 8); // Sunday
                Assert(sunday.DayOfWeek == DayOfWeek.Sunday, "Nov 8 2026 must be Sunday");
                var isSundayOff = await weeklyOffService.IsWeeklyOffAsync(sunday, null, null);
                Assert(isSundayOff, "Nov 8 2026 (Sunday) must be weekly off");

                // Configure 2nd Saturday as off
                var saturdayOff = new WeeklyOffModel
                {
                    DayOfWeek = DayOfWeek.Saturday,
                    IsAlternateWeek = true,
                    AlternateWeekNumber = 2,
                    IsActive = true
                };
                var (addSatSuccess, _) = await weeklyOffService.AddAsync(saturdayOff);
                Assert(addSatSuccess, "Failed to add Saturday weekly off");

                // Nov 7, 2026 is 1st Saturday -> Working
                var sat1 = new DateOnly(2026, 11, 7);
                Assert(sat1.DayOfWeek == DayOfWeek.Saturday, "Nov 7 must be Saturday");
                var isSat1Off = await weeklyOffService.IsWeeklyOffAsync(sat1, null, null);
                Assert(!isSat1Off, "1st Saturday (Nov 7) should NOT be off");

                // Nov 14, 2026 is 2nd Saturday -> Weekly Off
                var sat2 = new DateOnly(2026, 11, 14);
                Assert(sat2.DayOfWeek == DayOfWeek.Saturday, "Nov 14 must be Saturday");
                var isSat2Off = await weeklyOffService.IsWeeklyOffAsync(sat2, null, null);
                Assert(isSat2Off, "2nd Saturday (Nov 14) MUST be weekly off");
            })) passed++; else failed++;

            // TEST 16: Leave Calculation Service (Deductions, Half-day, Exclusions)
            if (await RunTest("Leave Duration Calculation & Policy Rules", async () =>
            {
                var clType = (await leaveTypeRepo.GetByCodeAsync("CL"))!;
                Assert(clType != null, "CL leave type must exist");

                // Mon Nov 9 to Wed Nov 11 (3 working days)
                var calcResult = await leaveCalcService.CalculateDurationAsync(
                    new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 11),
                    clType.LeaveTypeId, null, null);
                Assert(calcResult.TotalDays == 3.0, $"Expected 3.0 days, got {calcResult.TotalDays}");

                // Fri Oct 16 to Tue Oct 20 (spans Saturday Oct 17 and Sunday Oct 18)
                // Oct 17 is working (3rd sat), Oct 18 is Sunday (off)
                // Total working days = Fri (1) + Sat (1) + Mon (1) + Tue (1) = 4 days
                var spanResult = await leaveCalcService.CalculateDurationAsync(
                    new DateOnly(2026, 10, 16), new DateOnly(2026, 10, 20),
                    clType.LeaveTypeId, null, null);
                Assert(spanResult.TotalDays == 4.0, $"Expected 4.0 days excluding Sunday, got {spanResult.TotalDays}");
                Assert(spanResult.WeeklyOffDays == 1, $"Expected 1 weekly off, got {spanResult.WeeklyOffDays}");

                // Half day test
                var halfDayResult = await leaveCalcService.CalculateDurationAsync(
                    new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 9),
                    clType.LeaveTypeId, null, null,
                    isHalfDay: true, halfDaySession: "FirstHalf");
                Assert(halfDayResult.TotalDays == 0.5, $"Expected 0.5 for half day, got {halfDayResult.TotalDays}");
            })) passed++; else failed++;

            // TEST 17: Leave Application Workflow, Balance Reservation & Audit Ledger
            if (await RunTest("Leave Application Workflow, Balance Reservation & Audit Ledger", async () =>
            {
                var emp = (await empRepo.GetByEmployeeNoAsync("EMP001"))!;
                var clType = (await leaveTypeRepo.GetByCodeAsync("CL"))!;

                // Initialize balance for employee
                await leaveService.EnsureEmployeeBalancesInitializedAsync(emp.EmployeeId, 2026);
                var initialBal = await leaveService.GetEmployeeBalanceAsync(emp.EmployeeId, clType.LeaveTypeId, 2026);
                Assert(initialBal != null, "Initial balance must exist");
                double initialAvail = initialBal!.AvailableBalance;
                Assert(initialAvail > 0, "Initial available balance must be > 0");

                // Apply leave for 2 days: Nov 17 to Nov 18, 2026
                var app = new LeaveApplicationModel
                {
                    EmployeeId = emp.EmployeeId,
                    LeaveTypeId = clType.LeaveTypeId,
                    FromDate = new DateOnly(2026, 11, 17),
                    ToDate = new DateOnly(2026, 11, 18),
                    Reason = "Personal work"
                };

                var (applySuccess, applyMsg, createdApp) = await leaveService.SubmitLeaveApplicationAsync(app);
                Assert(applySuccess, $"SubmitLeaveApplication failed: {applyMsg}");
                Assert(createdApp != null && createdApp.Status == "Pending", "Status must be Pending");
                Assert(createdApp!.TotalDays == 2.0, $"Expected 2.0 days, got {createdApp.TotalDays}");

                // Verify balance reservation
                var reservedBal = await leaveService.GetEmployeeBalanceAsync(emp.EmployeeId, clType.LeaveTypeId, 2026);
                Assert(reservedBal!.Reserved == 2.0, $"Reserved balance must be 2.0, got {reservedBal.Reserved}");
                Assert(reservedBal.AvailableBalance == initialAvail - 2.0, "Available balance must be decremented by reserved amount");

                // Check audit ledger recorded reservation
                var txns = await leaveService.GetTransactionsByEmployeeAsync(emp.EmployeeId);
                Assert(txns.Any(t => t.TransactionType == "LeaveReserved" && t.LeaveApplicationId == createdApp.LeaveApplicationId),
                    "Audit ledger must contain LeaveReserved entry");

                // Approve leave
                var (apprSuccess, apprMsg) = await leaveService.ApproveLeaveApplicationAsync(createdApp.LeaveApplicationId, "HR Manager");
                Assert(apprSuccess, $"ApproveLeaveApplication failed: {apprMsg}");

                // Verify balance consumption
                var consumedBal = await leaveService.GetEmployeeBalanceAsync(emp.EmployeeId, clType.LeaveTypeId, 2026);
                Assert(consumedBal!.Reserved == 0.0, "Reserved must be 0 after approval");
                Assert(consumedBal.Used == 2.0, "Used must be 2.0 after approval");
                Assert(consumedBal.AvailableBalance == initialAvail - 2.0, "Available balance should reflect used");

                // Check audit ledger recorded consumption
                var updatedTxns = await leaveService.GetTransactionsByEmployeeAsync(emp.EmployeeId);
                Assert(updatedTxns.Any(t => t.TransactionType == "LeaveConsumed" && t.LeaveApplicationId == createdApp.LeaveApplicationId),
                    "Audit ledger must contain LeaveConsumed entry");

                // Cancel approved leave -> restores balance and records LeaveReleased
                var (cancelSuccess, cancelMsg) = await leaveService.CancelLeaveApplicationAsync(createdApp.LeaveApplicationId, "Plans changed", "EMP001");
                Assert(cancelSuccess, $"CancelLeaveApplication failed: {cancelMsg}");

                var restoredBal = await leaveService.GetEmployeeBalanceAsync(emp.EmployeeId, clType.LeaveTypeId, 2026);
                Assert(restoredBal!.Used == 0.0, "Used must be restored to 0 after cancellation");
                Assert(restoredBal.AvailableBalance == initialAvail, "Available balance must be restored to initial balance");

                var finalTxns = await leaveService.GetTransactionsByEmployeeAsync(emp.EmployeeId);
                Assert(finalTxns.Any(t => t.TransactionType == "LeaveReleased" && t.LeaveApplicationId == createdApp.LeaveApplicationId),
                    "Audit ledger must contain LeaveReleased entry");
            })) passed++; else failed++;

            // TEST 18: Attendance Calendar Resolver Integration
            if (await RunTest("Attendance Calendar Resolver Priority & Status Resolution", async () =>
            {
                var emp = (await empRepo.GetByEmployeeNoAsync("EMP001"))!;
                var clType = (await leaveTypeRepo.GetByCodeAsync("CL"))!;

                // Apply and approve a 1-day leave on Oct 28, 2026 (Wednesday, prior to 7-day seed punch window)
                var leaveDay = new DateOnly(2026, 10, 28);
                var app = new LeaveApplicationModel
                {
                    EmployeeId = emp.EmployeeId,
                    LeaveTypeId = clType.LeaveTypeId,
                    FromDate = leaveDay,
                    ToDate = leaveDay,
                    Reason = "Doctor visit"
                };
                var (applySuccess, applyMsg, createdApp) = await leaveService.SubmitLeaveApplicationAsync(app);
                Assert(applySuccess, $"SubmitLeaveApplication failed: {applyMsg}");
                Assert(createdApp != null, "createdApp is null");
                var (apprSuccess, apprMsg) = await leaveService.ApproveLeaveApplicationAsync(createdApp!.LeaveApplicationId, "HR");
                Assert(apprSuccess, $"ApproveLeaveApplication failed: {apprMsg}");

                // Case 1: Approved Paid Leave without punches -> Resolves to 'P' (Present / Paid)
                var statusLeave = await calendarResolver.ResolveDailyStatusAsync(emp.EmployeeId, leaveDay);
                Assert(statusLeave.StatusCode == "P", $"Approved paid leave without punch should resolve to 'P', got '{statusLeave.StatusCode}'");
                Assert(statusLeave.IsOnLeave, "IsOnLeave must be true");
                Assert(statusLeave.LeaveApplicationId == createdApp.LeaveApplicationId, "LeaveApplicationId must match");

                // Case 2: Public Holiday (Aug 15 2026) without punches -> Resolves to 'H'
                var statusHoliday = await calendarResolver.ResolveDailyStatusAsync(emp.EmployeeId, new DateOnly(2026, 8, 15));
                Assert(statusHoliday.StatusCode == "H", $"August 15 Holiday must resolve to 'H', got '{statusHoliday.StatusCode}'");
                Assert(statusHoliday.IsHoliday, "IsHoliday must be true");

                // Case 3: Sunday (Oct 25 2026) without punches -> Resolves to 'H'
                var statusSunday = await calendarResolver.ResolveDailyStatusAsync(emp.EmployeeId, new DateOnly(2026, 10, 25));
                Assert(statusSunday.StatusCode == "H", $"Sunday must resolve to 'H', got '{statusSunday.StatusCode}'");
                Assert(statusSunday.IsWeeklyOff, "IsWeeklyOff must be true");

                // Case 4: Working Day without punches (Oct 29 2026 Thursday) -> Resolves to 'A'
                var statusAbsent = await calendarResolver.ResolveDailyStatusAsync(emp.EmployeeId, new DateOnly(2026, 10, 29));
                Assert(statusAbsent.StatusCode == "A", $"Unpunched working day must resolve to 'A', got '{statusAbsent.StatusCode}'");
            })) passed++; else failed++;
        }
        finally
        {
            try
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch
            {
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------------------------------------------------------------------");
        Console.ResetColor();

        if (failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"ALL TESTS PASSED! ({passed} of {passed} passed, 0 failed)");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"TESTS FAILED: {failed} failed, {passed} passed.");
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task<bool> RunTest(string testName, Func<Task> action)
    {
        try
        {
            Console.Write($"Running: {testName,-55} ... ");
            await action();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[PASS]");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FAIL]");
            Console.WriteLine($"   Error: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion Failed: {message}");
        }
    }

    private static string GenerateTestPortraitBase64()
    {
        using var bmp = new Bitmap(128, 128);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(220, 210, 195));
        using var brushFace = new SolidBrush(Color.FromArgb(190, 160, 140));
        g.FillEllipse(brushFace, 24, 16, 80, 96);
        using var brushEye = new SolidBrush(Color.FromArgb(40, 30, 20));
        g.FillEllipse(brushEye, 42, 45, 12, 10);
        g.FillEllipse(brushEye, 74, 45, 12, 10);
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        return Convert.ToBase64String(ms.ToArray());
    }
}
