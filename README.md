# HRMS — Workforce Management

A modern, industrial-grade Human Resource & Attendance Management System built with **ASP.NET Core MVC (.NET 8)**, featuring **Local Biometric Face Recognition**, an **Industrial Workforce Dashboard**, **JSON Storage with Replaceable Repository Abstraction**, and **Progressive Web Application (PWA)** capabilities.

---

## 1. Architecture & Design Principles

The application is structured as a **Modular Monolith** using the **Repository Pattern** and built-in **ASP.NET Core Dependency Injection**:

```text
HTTP Request 
     ↓
Controller (Account, Dashboard, Employee, Attendance, FaceRegistration, Department, Settings)
     ↓
Service Layer (Business Rules, Validation, Aadhaar Protection, Shift Logic)
     ↓
Repository Interfaces (IEmployeeRepository, IAttendanceRepository, IDepartmentRepository, etc.)
     ↓
Data Provider: JSON File Storage (Thread-safe, atomic file replacement, decoupled)
     ↓
[Future Extension]: SQL Server Provider (Drop-in replacement without altering Controllers or Views)
```

### Decoupled Storage Provider
The repository interfaces (`IEmployeeRepository`, `IAttendanceRepository`, `IDepartmentRepository`, `IFaceTemplateRepository`, `IDashboardRepository`) are completely abstracted from the underlying storage mechanism.
To transition to SQL Server in the future, only new SQL repository implementations (e.g. `SqlEmployeeRepository`) and dependency injection bindings need to be updated. No Controllers, Services, Razor Views, or JavaScript will require modification.

---

## 2. Core Implemented Modules

### 2.1 Industrial Attendance Dashboard
- **6 Real-time KPI Cards**: Total Active Employees, Present Today, Absent Today, Inside Plant, Late Arrivals, Turnout Percentage.
- **Dynamic Calculations**: All KPIs are computed from actual recorded punches (no hardcoded metrics).
- **Responsive Charts (Chart.js)**:
  - Present vs. Absent Donut Chart.
  - 7-Day Attendance Trend (Present and Late arrival lines).
  - Department-wise Attendance Distribution Bar Chart.
- **Live Gate Activity Stream**: Auto-refreshing feed of incoming and outgoing punches with timestamps and face match confidence.
- **Daily Muster Roll Table**: Table of all employees with First IN, Last OUT, Working Duration, and Status badges (Present, Late, Currently Inside, Completed, Absent).
- **Interactive Controls**: Date filtering, department filter, quick search, manual refresh, 30-second automated background refresh, and CSV export.

### 2.2 Employee Management
- **Directory**: Paginated employee list with search (by Name, EmployeeNo, Phone) and Department / Status filtering.
- **Aadhaar Protection**: Aadhaar numbers are validated (12 digits) and masked throughout the UI (`•••• •••• 1234`).
- **Enrollment & Editing**: Add and update employee details with server-side validation, duplicate EmployeeNo prevention, and department mapping.
- **Dual Photograph Capture**: Supports both traditional image file upload (JPG, PNG, WEBP max 5MB) and **live WebRTC camera photograph capture** directly in the browser.
- **Employee Details View**: Profile card, department allocation, active/inactive status toggle, biometric enrollment indicator, and recent attendance logs.

### 2.3 Industrial Attendance Terminal (Kiosk)
- **Kiosk Interface**: Full-screen industrial attendance kiosk designed for factory gates and touch tablets.
- **Live Video Feed**: Centered camera stream with face target guide and scanning animation.
- **One-Tap Actions**: Large, touch-friendly **PUNCH IN** (Arrival) and **PUNCH OUT** (Departure) buttons.
- **Shift & Duplicate Business Rules**:
  - Rejects duplicate Punch IN on the same day.
  - Rejects Punch OUT without a preceding Punch IN.
  - Rejects duplicate Punch OUT on the same day.
  - Rejects punches from deactivated employees.
  - Enforces server-side timestamps against configured application timezone.
  - Flags Late Arrivals when first IN exceeds configured shift start time.
- **Manual Fallback**: Accessible dropdown selection mode for testing or administrative override.

### 2.4 Local Face Recognition
- **No Cloud Dependencies**: Operates 100% locally on workstation hardware without paid external APIs.
- **Face Feature Extraction**: Extracts 128-dimensional L2-normalized spatial frequency and gradient vectors from cropped center portrait frames.
- **Cosine Similarity Matching**: Computes mathematical cosine similarity against stored biometric templates with a configurable match threshold (default 0.65).
- **Biometric Enrollment Workflow**: Dedicated management screen to enroll, re-enroll, or revoke facial templates.
- **Conflict Prevention**: Detects duplicate face templates across different employees.
- **Prototype Notice**: Displays standard industrial disclaimers regarding optical liveness detection requirements for unattended kiosks.

### 2.5 Progressive Web App (PWA)
- **Installable Application**: Configured `manifest.json` with theme color `#0d9488` and standalone display.
- **Service Worker (`service-worker.js`)**: Caches static application shell (CSS, JavaScript, icons) for offline launch.
- **Data Security**: Explicitly excludes authentication tokens, employee data, Aadhaar details, face templates, and attendance records from browser caches.

### 2.6 Department & System Settings
- **Department Management**: Management of organizational divisions (seeded with Production, Administration, Human Resources, Security).
- **Operational Settings**: Configurable Organization Name, Shift Times, Timezone ID, Face Match Threshold, and real-time path inspection.

---

## 3. Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Language**: C# 12
- **View Engine**: Razor Views (`.cshtml`)
- **UI Framework**: Bootstrap 5.3 + Bootstrap Icons
- **Data Serialization**: `System.Text.Json` with thread-safe `SemaphoreSlim` single-writer locking and atomic file replacement
- **Biometrics & Imaging**: `Microsoft.ML.OnnxRuntime` / `System.Drawing.Common`
- **Charts**: Chart.js 4.4
- **Client Scripting**: Vanilla JavaScript (ES6+), WebRTC `navigator.mediaDevices`

---

## 4. Project Structure

```text
d:\Pilot Project\Hrms\
├── Controllers/
│   ├── AccountController.cs          # Cookie authentication, Login, Logout
│   ├── DashboardController.cs        # Workforce dashboard, live stats AJAX, CSV export
│   ├── EmployeeController.cs         # Employee directory, CRUD, camera capture
│   ├── AttendanceController.cs       # Kiosk terminal, daily sheet, attendance history
│   ├── FaceRegistrationController.cs # Face template enrollment & management
│   ├── DepartmentController.cs       # Department management
│   ├── SettingsController.cs         # Shift times, thresholds, path management
│   └── HomeController.cs             # Root routing to Dashboard
│
├── Models/
│   ├── EmployeeModel.cs              # Employee entity with Aadhaar masking
│   ├── AttendanceModel.cs            # Punch event record with verification details
│   ├── DepartmentModel.cs            # Department entity
│   ├── FaceTemplateModel.cs          # Stored 128-D biometric vector & model metadata
│   ├── FaceMatchResult.cs            # Biometric evaluation outcome
│   ├── DashboardModel.cs             # KPI aggregates, chart series, table rows
│   ├── LoginModel.cs                 # Authentication model
│   └── AppSettingsModel.cs           # System settings model
│
├── ViewModels/
│   ├── EmployeeViewModel.cs          # List, edit, details view models
│   ├── AttendanceViewModel.cs        # Daily, history, kiosk DTOs
│   ├── FaceRegistrationViewModel.cs  # Enrollment view models & DTOs
│   └── LoginViewModel.cs             # Login credentials view model
│
├── Interfaces/
│   ├── IEmployeeRepository.cs        # Persistence contracts (Replaceable with SQL)
│   ├── IAttendanceRepository.cs
│   ├── IDepartmentRepository.cs
│   ├── IFaceTemplateRepository.cs
│   ├── IDashboardRepository.cs
│   ├── IEmployeeService.cs           # Business logic contracts
│   ├── IAttendanceService.cs
│   ├── IFaceRecognitionService.cs
│   └── IDashboardService.cs
│
├── Repositories/
│   ├── JsonEmployeeRepository.cs     # JSON implementation with seed employees
│   ├── JsonAttendanceRepository.cs   # JSON implementation with seed attendance
│   ├── JsonDepartmentRepository.cs   # JSON implementation with seed departments
│   ├── JsonFaceTemplateRepository.cs # Biometric templates storage
│   └── JsonDashboardRepository.cs    # Dynamic metrics & trend aggregator
│
├── Services/
│   ├── EmployeeService.cs            # Validation, unique EMP ID, image storage
│   ├── AttendanceService.cs          # Shift rules, duplicate checks, timestamps
│   ├── FaceRecognitionService.cs     # 128-D vector extractor & cosine matcher
│   └── DashboardService.cs           # Analytics generation & CSV builder
│
├── Data/
│   ├── JsonFileStorage.cs            # Async single-writer with atomic file swap
│   ├── StoragePathProvider.cs        # Resolves %LocalAppData%\HRMS directories
│   ├── JsonStorageOptions.cs         # Configuration binding
│   └── HrmsTestSuite.cs              # Built-in 11-point automated test suite
│
├── Views/
│   ├── Account/                      # Login.cshtml, AccessDenied.cshtml
│   ├── Dashboard/                    # Index.cshtml (Showcase dashboard)
│   ├── Employee/                     # Index, Create, Edit, Details, _EmployeeForm
│   ├── Attendance/                   # Terminal.cshtml, Daily.cshtml, History.cshtml
│   ├── FaceRegistration/            # Index.cshtml (Biometric enrollment)
│   ├── Department/                   # Index.cshtml, Create.cshtml
│   ├── Settings/                     # Index.cshtml
│   └── Shared/                       # _Layout, _LoginLayout, _Sidebar, _Topbar
│
├── wwwroot/
│   ├── css/hrms-theme.css            # Custom industrial Navy & Teal theme
│   ├── js/hrms-core.js               # WebRTC camera engine, clock, notifications
│   ├── icons/                        # PWA manifest icons & SVG
│   ├── images/                       # Default avatar & UI assets
│   ├── manifest.json                 # PWA metadata
│   └── service-worker.js             # Static shell offline caching
│
├── publish/                          # Release output directory
│   ├── HRMS.exe                      # Standalone Windows executable
│   └── ...                           # Web assets & dependencies
│
├── Program.cs                        # DI setup, auth, Kestrel port binding, launcher
├── appsettings.json                  # Application configuration
├── HRMS.csproj                       # Project file (.NET 8)
└── README.md                         # Product documentation
```

---

## 5. Instructions to Run the Application

### 5.1 Running from Source (Development)

Prerequisites: .NET 8 SDK or .NET 10 SDK.

```powershell
# Navigate to project directory
cd "d:\Pilot Project\Hrms"

# Run application
dotnet run
```

The application will bind to an available local port (e.g. `http://127.0.0.1:5000`) and automatically open the default web browser to the Login page.

### 5.2 Default Prototype Credentials

- **Username**: `admin`
- **Password**: `123`

*(Configurable in `appsettings.json` under `"Auth"` section)*.

---

## 6. How to Run the Automated Verification Test Suite

HRMS includes an embedded automated test suite covering 11 critical operational scenarios (Department seeding, Employee CRUD, Unique EMP ID validation, Aadhaar 12-digit rules, JSON persistence across repository reloads, Duplicate Punch IN rejection, OUT without IN rejection, Duplicate OUT rejection, Inactive employee blocking, Face recognition vector extraction & cosine matching, Dashboard KPI calculation, and AccountController pipeline):

```powershell
# Run the built-in test suite
.\publish\HRMS.exe --test

# Or from source:
dotnet run --no-build -- --test
```

Expected output:
```text
==================================================================
       HRMS AUTOMATED VERIFICATION & TEST SUITE (.NET 8)          
==================================================================
Running: Department Seeding (4 Seed Departments)                 ... [PASS]
Running: Employee Seeding & Aadhaar Masking Protection           ... [PASS]
Running: Create Employee & Unique EmployeeNo Validation          ... [PASS]
Running: Aadhaar Validation (Exact 12 Digits)                    ... [PASS]
Running: JSON Persistence & Disk Reload                          ... [PASS]
Running: Attendance Punch IN & Duplicate IN Rejection            ... [PASS]
Running: Attendance OUT without IN Rejection & Valid Punch OUT   ... [PASS]
Running: Inactive Employee Punch Rejection                       ... [PASS]
Running: Local Face Registration & Verification Match            ... [PASS]
Running: Dashboard KPI Calculations & CSV Export                 ... [PASS]
Running: AccountController & Login View Model Integration        ... [PASS]

------------------------------------------------------------------
ALL TESTS PASSED! (11 of 11 passed, 0 failed)
```

---

## 7. Windows Standalone EXE Publishing & Launch

### 7.1 Publishing Steps

To create a clean release build in the `publish\` directory:

```powershell
dotnet publish HRMS.csproj -c Release -o publish/
```

### 7.2 Launching `HRMS.exe`

1. Open the `publish\` directory.
2. Double-click **`HRMS.exe`**.
3. The server starts on an available loopback port (e.g., `http://127.0.0.1:5000`) and launches your default web browser directly to the HRMS Login page.
4. Sign in with `admin` / `123`.

---

## 8. Data Storage Location, Backup & Restore

### Runtime Storage Directory
Application data and uploads are stored outside the binary directory in:
```text
%LocalAppData%\HRMS\
├── Data\
│   ├── employees.json
│   ├── attendance.json
│   ├── departments.json
│   ├── face-templates.json
│   └── app-settings.json
└── Uploads\
    ├── Employees\
    └── Attendance\
```

### Backup Procedure
To perform a complete backup of all workforce data, biometric templates, and photographs:
1. Open Windows Explorer and navigate to `%LocalAppData%`.
2. Copy the entire `HRMS` directory to a secure backup volume.

### Restore Procedure
To restore data on any machine:
1. Copy the backed up `HRMS` folder into `%LocalAppData%\`.
2. Launch `HRMS.exe`. All employees, historical punches, and biometric templates will be restored immediately.

---

## 9. Future SQL Server Migration Guide

When migrating from JSON files to SQL Server:

1. **Keep existing domain models**: `EmployeeModel`, `AttendanceModel`, `DepartmentModel`, `FaceTemplateModel` remain unchanged.
2. **Implement SQL Repositories**: Create `SqlEmployeeRepository`, `SqlAttendanceRepository`, `SqlDepartmentRepository`, `SqlFaceTemplateRepository`, and `SqlDashboardRepository` implementing the corresponding interfaces (`IEmployeeRepository`, etc.).
3. **Update Dependency Injection**: In `Program.cs`, replace:
   ```csharp
   builder.Services.AddScoped<IEmployeeRepository, JsonEmployeeRepository>();
   ```
   with:
   ```csharp
   builder.Services.AddScoped<IEmployeeRepository, SqlEmployeeRepository>();
   ```
   based on `"Storage": { "Provider": "SqlServer" }`.
4. **No Controller or View Changes**: Controllers and Views interact strictly with `IEmployeeService`, `IAttendanceService`, and `IDashboardService`. Zero changes are required in UI, Controllers, or client scripts.

---

## 10. Known Prototype Limitations & Security Notes

1. **Biometric Liveness Detection**: The local face recognition engine performs 128-dimensional spatial gradient and frequency analysis for feature matching. While it reliably matches live portrait faces and rejects poor lighting or blank frames, optical anti-spoofing against high-resolution photographs or video playback requires a physical dual-sensor IR 3D depth camera before unattended commercial deployment.
2. **Aadhaar Data Protection**: In this prototype, Aadhaar numbers are masked in all lists and UI displays. In a production cloud or enterprise deployment, Aadhaar numbers must be encrypted at rest using AES-256 with keys managed through a Hardware Security Module (HSM) or Azure Key Vault / AWS KMS in compliance with UIDAI regulations.
3. **Camera Permissions**: WebRTC camera access requires `localhost` or an `HTTPS` connection in accordance with modern browser security standards.

