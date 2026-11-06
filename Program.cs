using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Repositories;
using HRMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;

if (args.Contains("--test"))
{
    var exitCode = await HrmsTestSuite.RunAllTestsAsync();
    Environment.Exit(exitCode);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 1. Storage Options & Core Infrastructure
builder.Services.Configure<JsonStorageOptions>(builder.Configuration.GetSection(JsonStorageOptions.SectionName));
builder.Services.AddSingleton<StoragePathProvider>();
builder.Services.AddSingleton<JsonFileStorage>();

// 2. Repository Layer (Decoupled & Replaceable with SQL Server)
builder.Services.AddScoped<IEmployeeRepository, JsonEmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, JsonAttendanceRepository>();
builder.Services.AddScoped<IDepartmentRepository, JsonDepartmentRepository>();
builder.Services.AddScoped<IFaceTemplateRepository, JsonFaceTemplateRepository>();
builder.Services.AddScoped<IDashboardRepository, JsonDashboardRepository>();
builder.Services.AddScoped<IHolidayRepository, JsonHolidayRepository>();
builder.Services.AddScoped<IWeeklyOffRepository, JsonWeeklyOffRepository>();
builder.Services.AddScoped<ILeaveTypeRepository, JsonLeaveTypeRepository>();
builder.Services.AddScoped<ILeavePolicyRepository, JsonLeavePolicyRepository>();
builder.Services.AddScoped<ILeaveBalanceRepository, JsonLeaveBalanceRepository>();
builder.Services.AddScoped<ILeaveApplicationRepository, JsonLeaveApplicationRepository>();
builder.Services.AddScoped<ILeaveTransactionRepository, JsonLeaveTransactionRepository>();

// 3. Service Layer
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IWeeklyOffService, WeeklyOffService>();
builder.Services.AddScoped<ILeaveCalculationService, LeaveCalculationService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAttendanceCalendarResolver, AttendanceCalendarResolver>();
builder.Services.AddScoped<IAttendanceReportService, AttendanceReportService>();
builder.Services.AddScoped<IAttendanceExcelExportService, AttendanceExcelExportService>();

// 4. Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HRMS.Session.Auth.v3";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

// Detect standalone launch or port binding (0.0.0.0 allows mobile devices on same Wi-Fi / LAN to connect)
var configuredUrl = builder.Configuration["urls"];
string serverUrl;
if (!string.IsNullOrWhiteSpace(configuredUrl))
{
    serverUrl = configuredUrl.Split(';')[0];
}
else
{
    var port = GetAvailableLocalPort(5055);
    serverUrl = $"http://0.0.0.0:{port}";
    builder.WebHost.UseUrls(serverUrl);
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// Serve dynamic uploads directory outside the binary (%LocalAppData%\HRMS\Uploads)
var pathProvider = app.Services.GetRequiredService<StoragePathProvider>();
if (!Directory.Exists(pathProvider.UploadsDirectory))
{
    Directory.CreateDirectory(pathProvider.UploadsDirectory);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pathProvider.UploadsDirectory),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Initialize seed data on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var deptRepo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
        await deptRepo.GetAllAsync();

        var empRepo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        await empRepo.GetAllAsync();

        var attRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
        await attRepo.GetAllAsync();

        var faceService = scope.ServiceProvider.GetRequiredService<IFaceRecognitionService>();
        await faceService.SyncEmployeePhotoTemplatesAsync();

        app.Logger.LogInformation("HRMS seed data & face biometric templates verified at {Path}", pathProvider.DataDirectory);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize seed repositories on startup.");
    }
}

// Automatically launch browser once server starts
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    var localPort = serverUrl.Contains(':') ? serverUrl.Split(':').Last().TrimEnd('/') : "5055";
    var launchUrl = (serverUrl.Contains("0.0.0.0") || serverUrl.Contains('*'))
        ? $"http://localhost:{localPort}/Account/Login"
        : $"{serverUrl}/Account/Login";

    app.Logger.LogInformation("HRMS Workforce Server started successfully at {ServerUrl} (Local access: http://localhost:{Port})", serverUrl, localPort);

    // Only launch browser in standalone / interactive desktop mode
    if (!builder.Environment.IsEnvironment("Testing") && !args.Contains("--no-browser") && Environment.UserInteractive)
    {
        Task.Run(async () =>
        {
            await Task.Delay(800); // Give Kestrel a short moment to start accepting connections
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launchUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning("Could not launch default browser automatically: {Message}", ex.Message);
            }
        });
    }
});

app.Run();

// Helper to find a free local TCP port
static int GetAvailableLocalPort(int defaultPort)
{
    try
    {
        using var tcpListener = new TcpListener(IPAddress.Loopback, defaultPort);
        tcpListener.Start();
        tcpListener.Stop();
        return defaultPort;
    }
    catch
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        catch
        {
            return 5055;
        }
    }
}
