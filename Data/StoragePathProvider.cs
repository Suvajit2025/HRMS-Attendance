using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HRMS.Data;

public class StoragePathProvider
{
    private readonly JsonStorageOptions _options;
    private readonly string _baseDirectory;
    private readonly string _dataDirectory;
    private readonly string _uploadsDirectory;

    public StoragePathProvider(IOptions<JsonStorageOptions> options, IConfiguration? configuration = null)
    {
        _options = options.Value;

        // 1. Check ConnectionStrings:JsonDatabase
        string? connString = configuration?.GetConnectionString("JsonDatabase") ?? _options.ConnectionString;
        string? resolvedDataDir = null;

        if (!string.IsNullOrWhiteSpace(connString))
        {
            var parts = connString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kvp = part.Split('=', 2);
                if (kvp.Length == 2 && string.Equals(kvp[0].Trim(), "Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedDataDir = kvp[1].Trim();
                    break;
                }
            }
        }

        // 2. Check DataDirectory option
        if (string.IsNullOrWhiteSpace(resolvedDataDir) && !string.IsNullOrWhiteSpace(_options.DataDirectory))
        {
            resolvedDataDir = _options.DataDirectory;
        }

        // 3. Fallback to LocalAppData / FolderPath
        string baseDir;
        if (!string.IsNullOrWhiteSpace(_options.FolderPath))
        {
            baseDir = Environment.ExpandEnvironmentVariables(_options.FolderPath);
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            baseDir = Path.Combine(localAppData, "HRMS");
        }

        _baseDirectory = Path.GetFullPath(baseDir);

        if (!string.IsNullOrWhiteSpace(resolvedDataDir))
        {
            var expanded = Environment.ExpandEnvironmentVariables(resolvedDataDir);
            _dataDirectory = Path.IsPathRooted(expanded) ? Path.GetFullPath(expanded) : Path.GetFullPath(Path.Combine(_baseDirectory, expanded));
        }
        else
        {
            _dataDirectory = Path.Combine(_baseDirectory, "Data");
        }

        if (!string.IsNullOrWhiteSpace(_options.UploadsDirectory))
        {
            var expandedUploads = Environment.ExpandEnvironmentVariables(_options.UploadsDirectory);
            _uploadsDirectory = Path.IsPathRooted(expandedUploads) ? Path.GetFullPath(expandedUploads) : Path.GetFullPath(Path.Combine(_baseDirectory, expandedUploads));
        }
        else
        {
            _uploadsDirectory = Path.Combine(_baseDirectory, "Uploads");
        }

        EnsureDirectoriesCreated();
    }

    public string BaseDirectory => _baseDirectory;
    public string DataDirectory => _dataDirectory;
    public string UploadsDirectory => _uploadsDirectory;
    public string EmployeeUploadsDirectory => Path.Combine(_uploadsDirectory, "Employees");
    public string AttendanceUploadsDirectory => Path.Combine(_uploadsDirectory, "Attendance");
    public string LeaveDocumentsUploadsDirectory => Path.Combine(_uploadsDirectory, "LeaveDocuments");

    public string EmployeesFilePath => Path.Combine(_dataDirectory, "employees.json");
    public string AttendanceFilePath => Path.Combine(_dataDirectory, "attendance.json");
    public string DepartmentsFilePath => Path.Combine(_dataDirectory, "departments.json");
    public string FaceTemplatesFilePath => Path.Combine(_dataDirectory, "face-templates.json");
    public string AppSettingsFilePath => Path.Combine(_dataDirectory, "app-settings.json");

    // Holiday & Weekly Off stores
    public string HolidaysFilePath => Path.Combine(_dataDirectory, "holidays.json");
    public string WeeklyOffsFilePath => Path.Combine(_dataDirectory, "weekly-offs.json");

    // Leave Management stores
    public string LeaveTypesFilePath => Path.Combine(_dataDirectory, "leave-types.json");
    public string LeavePoliciesFilePath => Path.Combine(_dataDirectory, "leave-policies.json");
    public string EmployeeLeaveBalancesFilePath => Path.Combine(_dataDirectory, "employee-leave-balances.json");
    public string LeaveApplicationsFilePath => Path.Combine(_dataDirectory, "leave-applications.json");
    public string LeaveTransactionsFilePath => Path.Combine(_dataDirectory, "leave-transactions.json");

    // Biometric Device Integration store
    public string BiometricLogsFilePath => Path.Combine(_dataDirectory, "biometric-device-logs.json");

    private void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(EmployeeUploadsDirectory);
        Directory.CreateDirectory(AttendanceUploadsDirectory);
        Directory.CreateDirectory(LeaveDocumentsUploadsDirectory);
    }
}
