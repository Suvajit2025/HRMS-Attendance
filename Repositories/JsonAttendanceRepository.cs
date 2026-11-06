using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;

namespace HRMS.Repositories;

public class JsonAttendanceRepository : IAttendanceRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonAttendanceRepository> _logger;

    public JsonAttendanceRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonAttendanceRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AttendanceModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.AttendanceFilePath))
        {
            var seed = GenerateSeedAttendance();
            await _storage.WriteAsync(_pathProvider.AttendanceFilePath, seed);
            return seed.OrderByDescending(a => a.PunchTime).ToList();
        }

        var attendance = await _storage.ReadListAsync<AttendanceModel>(_pathProvider.AttendanceFilePath);
        return attendance.OrderByDescending(a => a.PunchTime).ToList();
    }

    public async Task<IReadOnlyList<AttendanceModel>> GetByDateAsync(DateOnly date)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.AttendanceDate == date).OrderBy(a => a.PunchTime).ToList();
    }

    public async Task<IReadOnlyList<AttendanceModel>> GetDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
                  .OrderBy(a => a.PunchTime)
                  .ToList();
    }

    public async Task<IReadOnlyList<AttendanceModel>> GetByEmployeeIdAsync(string employeeId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var all = await GetAllAsync();
        var query = all.Where(a => string.Equals(a.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));

        if (startDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= endDate.Value);
        }

        return query.OrderByDescending(a => a.PunchTime).ToList();
    }

    public async Task<IReadOnlyList<AttendanceModel>> GetEmployeePunchesTodayAsync(string employeeId, DateOnly date)
    {
        var all = await GetAllAsync();
        return all.Where(a => string.Equals(a.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) && a.AttendanceDate == date)
                  .OrderBy(a => a.PunchTime)
                  .ToList();
    }

    public async Task<AttendanceModel?> GetLatestPunchTodayAsync(string employeeId, DateOnly date)
    {
        var punches = await GetEmployeePunchesTodayAsync(employeeId, date);
        return punches.LastOrDefault();
    }

    public async Task AddAsync(AttendanceModel attendance)
    {
        var all = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(attendance.AttendanceId))
        {
            attendance.AttendanceId = Guid.NewGuid().ToString();
        }
        attendance.CreatedDate = DateTime.UtcNow;
        all.Add(attendance);
        await _storage.WriteAsync(_pathProvider.AttendanceFilePath, all);
    }

    private static List<AttendanceModel> GenerateSeedAttendance()
    {
        var list = new List<AttendanceModel>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Past 7 days attendance for trend demonstration
        for (int i = 6; i >= 1; i--)
        {
            var day = today.AddDays(-i);
            var dtBase = day.ToDateTime(new TimeOnly(8, 45, 0));

            // Rajesh Sharma (EMP001) - Present on time
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-001-in",
                EmployeeId = "emp-001",
                EmployeeNo = "EMP001",
                EmployeeName = "Rajesh Sharma",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = dtBase.AddMinutes(5),
                MatchScore = 0.94f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-001-out",
                EmployeeId = "emp-001",
                EmployeeNo = "EMP001",
                EmployeeName = "Rajesh Sharma",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(17, 30, 0)),
                MatchScore = 0.93f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });

            // Priya Nair (EMP002) - Present on time
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-002-in",
                EmployeeId = "emp-002",
                EmployeeNo = "EMP002",
                EmployeeName = "Priya Kumari Nair",
                DepartmentName = "Human Resources",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = dtBase.AddMinutes(12),
                MatchScore = 0.96f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-002-out",
                EmployeeId = "emp-002",
                EmployeeNo = "EMP002",
                EmployeeName = "Priya Kumari Nair",
                DepartmentName = "Human Resources",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(18, 5, 0)),
                MatchScore = 0.95f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });

            // Amit Verma (EMP003) - Late Arrival on some days
            var isLateDay = (i % 2 == 0);
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-003-in",
                EmployeeId = "emp-003",
                EmployeeNo = "EMP003",
                EmployeeName = "Amit Verma",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = isLateDay ? day.ToDateTime(new TimeOnly(9, 25, 0)) : dtBase.AddMinutes(8),
                MatchScore = 0.92f,
                VerificationMethod = "FaceRecognition",
                IsLate = isLateDay
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-003-out",
                EmployeeId = "emp-003",
                EmployeeNo = "EMP003",
                EmployeeName = "Amit Verma",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(17, 45, 0)),
                MatchScore = 0.91f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });

            // Sunita Patel (EMP004) - Administration
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-004-in",
                EmployeeId = "emp-004",
                EmployeeNo = "EMP004",
                EmployeeName = "Sunita Patel",
                DepartmentName = "Administration",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = dtBase.AddMinutes(2),
                MatchScore = 0.95f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-004-out",
                EmployeeId = "emp-004",
                EmployeeNo = "EMP004",
                EmployeeName = "Sunita Patel",
                DepartmentName = "Administration",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(18, 0, 0)),
                MatchScore = 0.94f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });

            // Vikram Singh (EMP005) - Security (early shift)
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-005-in",
                EmployeeId = "emp-005",
                EmployeeNo = "EMP005",
                EmployeeName = "Vikram Singh",
                DepartmentName = "Security",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = day.ToDateTime(new TimeOnly(7, 50, 0)),
                MatchScore = 0.97f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-005-out",
                EmployeeId = "emp-005",
                EmployeeNo = "EMP005",
                EmployeeName = "Vikram Singh",
                DepartmentName = "Security",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(16, 30, 0)),
                MatchScore = 0.96f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });

            // Manoj Gupta (EMP007) - Present
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-007-in",
                EmployeeId = "emp-007",
                EmployeeNo = "EMP007",
                EmployeeName = "Manoj Gupta",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "IN",
                PunchTime = dtBase.AddMinutes(7),
                MatchScore = 0.93f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
            list.Add(new AttendanceModel
            {
                AttendanceId = $"att-{day:yyyyMMdd}-007-out",
                EmployeeId = "emp-007",
                EmployeeNo = "EMP007",
                EmployeeName = "Manoj Gupta",
                DepartmentName = "Production",
                AttendanceDate = day,
                PunchType = "OUT",
                PunchTime = day.ToDateTime(new TimeOnly(17, 35, 0)),
                MatchScore = 0.91f,
                VerificationMethod = "FaceRecognition",
                IsLate = false
            });
        }

        // Today's attendance - varied scenarios:
        // 1. Rajesh Sharma (EMP001): IN at 08:48 AM, currently inside (no OUT)
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-001-in",
            EmployeeId = "emp-001",
            EmployeeNo = "EMP001",
            EmployeeName = "Rajesh Sharma",
            DepartmentName = "Production",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(8, 48, 12)),
            MatchScore = 0.95f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });

        // 2. Priya Nair (EMP002): IN at 08:55 AM, currently inside (no OUT)
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-002-in",
            EmployeeId = "emp-002",
            EmployeeNo = "EMP002",
            EmployeeName = "Priya Kumari Nair",
            DepartmentName = "Human Resources",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(8, 55, 30)),
            MatchScore = 0.97f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });

        // 3. Amit Verma (EMP003): Late arrival (09:32 AM), currently inside
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-003-in",
            EmployeeId = "emp-003",
            EmployeeNo = "EMP003",
            EmployeeName = "Amit Verma",
            DepartmentName = "Production",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(9, 32, 45)),
            MatchScore = 0.91f,
            VerificationMethod = "FaceRecognition",
            IsLate = true
        });

        // 4. Sunita Patel (EMP004): IN at 08:52 AM, completed OUT at 14:15 PM (half-day permission)
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-004-in",
            EmployeeId = "emp-004",
            EmployeeNo = "EMP004",
            EmployeeName = "Sunita Patel",
            DepartmentName = "Administration",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(8, 52, 10)),
            MatchScore = 0.94f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-004-out",
            EmployeeId = "emp-004",
            EmployeeNo = "EMP004",
            EmployeeName = "Sunita Patel",
            DepartmentName = "Administration",
            AttendanceDate = today,
            PunchType = "OUT",
            PunchTime = today.ToDateTime(new TimeOnly(14, 15, 20)),
            MatchScore = 0.93f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });

        // 5. Vikram Singh (EMP005): Security, IN at 07:55 AM, currently inside
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-005-in",
            EmployeeId = "emp-005",
            EmployeeNo = "EMP005",
            EmployeeName = "Vikram Singh",
            DepartmentName = "Security",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(7, 55, 0)),
            MatchScore = 0.98f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });

        // 6. Manoj Gupta (EMP007): IN at 08:40 AM, completed OUT at 16:45 PM
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-007-in",
            EmployeeId = "emp-007",
            EmployeeNo = "EMP007",
            EmployeeName = "Manoj Gupta",
            DepartmentName = "Production",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(8, 40, 22)),
            MatchScore = 0.92f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-007-out",
            EmployeeId = "emp-007",
            EmployeeNo = "EMP007",
            EmployeeName = "Manoj Gupta",
            DepartmentName = "Production",
            AttendanceDate = today,
            PunchType = "OUT",
            PunchTime = today.ToDateTime(new TimeOnly(16, 45, 10)),
            MatchScore = 0.91f,
            VerificationMethod = "FaceRecognition",
            IsLate = false
        });

        // 7. Deepak Kumar Yadav (EMP008): Late arrival at 09:18 AM, currently inside
        list.Add(new AttendanceModel
        {
            AttendanceId = $"att-{today:yyyyMMdd}-008-in",
            EmployeeId = "emp-008",
            EmployeeNo = "EMP008",
            EmployeeName = "Deepak Kumar Yadav",
            DepartmentName = "Security",
            AttendanceDate = today,
            PunchType = "IN",
            PunchTime = today.ToDateTime(new TimeOnly(9, 18, 05)),
            MatchScore = 0.94f,
            VerificationMethod = "FaceRecognition",
            IsLate = true
        });

        // Employees 006 (Ananya Dasgupta), 009 (Kavita Chauhan), 010 (Suresh Mukherjee)
        // have NO punches today -> will be Absent Today!

        return list;
    }
}

