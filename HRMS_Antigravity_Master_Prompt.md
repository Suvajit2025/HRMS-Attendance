# HRMS — Complete Application Development Master Prompt

## 1. Project Overview

Build a professional, modern, responsive **HRMS (Human Resource Management System)** using ASP.NET Core MVC (.NET 8).

This is a customer-presentable HRMS product prototype. It must not look like a disposable demo application.

The objective is to demonstrate the application to a potential customer. If the customer approves it, we should be able to continue developing the same application into their operational HRMS product without rebuilding the foundation.

The first release will focus on Employee Management and Industrial Attendance Management.

### Product identity

- Product name: HRMS
- Application title: HRMS — Workforce Management
- Project name: HRMS
- Solution name: HRMS
- Executable name: HRMS.exe

Do not use the words Demo, AttendanceDemo, or DemoAttendance in the product branding, project name, application title, or executable name.

Use configurable branding so customer-specific logos, company names, colours and other information can be introduced later.

---

# 2. Technology Stack

Use the following technologies:

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC (.NET 8) |
| Language | C# |
| Frontend | Razor Views (.cshtml) |
| UI | HTML5, CSS3, Bootstrap 5 |
| JavaScript | Vanilla JavaScript |
| Data storage initially | JSON files |
| Architecture | Repository Pattern |
| Dependency injection | Built-in ASP.NET Core DI |
| Database access | Storage-provider abstraction |
| Face recognition | Local ONNX-based face recognition |
| PWA | Web App Manifest and Service Worker |
| Deployment | Windows self-contained EXE |

### Important restrictions

- Use ASP.NET Core MVC.
- Use Controllers and Razor Views.
- Do not use the separate Razor Pages framework.
- Do not use Angular or React.
- Do not use Entity Framework Core.
- Do not use Dapper.
- Do not use SQL Server in the initial implementation.
- Do not use paid cloud face-recognition APIs.
- Do not put business logic inside Razor Views.
- Do not put file-storage logic inside controllers.

Keep the code simple, readable, modular and maintainable.

---

# 3. Critical Architecture — Replaceable Database

This is one of the most important requirements.

Initially, use JSON files as the data storage provider.

However, the application must be designed so that we can replace JSON with SQL Server later without changing the following:

- Controllers.
- Services.
- Business rules.
- Razor Views.
- JavaScript.
- UI.
- Dashboard calculations.
- Attendance workflows.
- Face recognition workflow.

Only the repository implementation and dependency injection/configuration should change.

## 3.1 Architecture

Use the following flow:

Controller → Service Interface → Service → Repository Interface → JSON Repository

The controller must never directly access JSON files.

The service layer must contain business rules and validations.

The repository layer must contain persistence logic.

## 3.2 Repository interfaces

Create:

- IEmployeeRepository
- IAttendanceRepository
- IDashboardRepository
- IDepartmentRepository
- IFaceTemplateRepository

## 3.3 Initial JSON implementations

Create:

- JsonEmployeeRepository
- JsonAttendanceRepository
- JsonDashboardRepository
- JsonDepartmentRepository
- JsonFaceTemplateRepository

All repositories must implement the corresponding interfaces.

Register all dependencies through Program.cs.

## 3.4 Future SQL Server implementations

Keep the architecture ready for these future implementations:

- SqlEmployeeRepository
- SqlAttendanceRepository
- SqlDashboardRepository
- SqlDepartmentRepository
- SqlFaceTemplateRepository

When SQL Server is introduced, the application should be able to select the SQL Server implementations through configuration and dependency injection.

Do not create two separate application architectures.

Do not allow JSON-specific types or file operations to leak into the service layer.

## 3.5 Storage provider configuration

Use configuration similar to:

```json
{
  "Storage": {
    "Provider": "Json"
  }
}
```

Initially, only implement the JSON provider.

Prepare a clear extension point for a future SQL Server provider. Do not claim SQL Server support until its repositories are implemented and tested.

---

# 4. JSON File Storage

Use JSON files for initial persistence.

Keep the JSON files outside the executable's installation directory.

Recommended runtime data directory:

```text
%LocalAppData%\HRMS\
    Data\
        employees.json
        attendance.json
        departments.json
        face-templates.json
        app-settings.json

    Uploads\
        Employees\
        Attendance\
```

For a future multi-user server deployment, allow the data directory to be configured separately from LocalAppData.

## 4.1 JSON storage requirements

- Automatically create the data directory.
- Automatically initialize missing JSON files.
- Use asynchronous file operations.
- Use System.Text.Json.
- Handle missing, empty or corrupted files safely.
- Use proper exception handling and logging.
- Prevent concurrent writes from overwriting data.
- Use a single-writer or equivalent locking mechanism.
- Write data safely using temporary files and atomic replacement where supported.
- Generate unique IDs safely.
- Validate duplicate EmployeeNo values.
- Do not expose physical file paths to the browser.
- Do not store uploaded image binaries directly in JSON.
- Store file paths for photographs.
- Store face embeddings in a suitable serializable representation, such as Base64-encoded binary data, for this initial implementation.

Do not use in-memory collections as the only persistence mechanism.

Data must remain available after application restart.

## 4.2 Backup

Provide a simple documented backup procedure for:

- JSON data files.
- Employee photographs.
- Attendance photographs.
- Face templates.

Do not delete existing customer data when updating the executable.

---

# 5. Authentication and Login

Create AccountController.

Create a professional login page.

For the initial local demonstration, use:

- Username: admin
- Password: 123

The login must use ASP.NET Core Cookie Authentication.

Requirements:

- Successful login redirects to Dashboard.
- Invalid login displays a meaningful error.
- Implement logout.
- Protect internal controllers using Authorize.
- Unauthenticated users must be redirected to login.
- Use secure cookie settings.
- Do not store passwords in plain-text JSON.
- Use a password hasher or a securely configured demo credential.
- Make the demo credential configurable so it can be changed before customer deployment.

Do not present the fixed credentials as production authentication.

Prepare the architecture for future user management and role-based authorization.

---

# 6. Employee Management Module

Create a complete Employee Management module.

## 6.1 Employee fields

| Field | Required | Description |
|---|---|---|
| EmployeeId | Yes | Auto-generated unique ID |
| EmployeeNo | Yes | Unique employee number |
| FirstName | Yes | First name |
| MiddleName | No | Middle name |
| LastName | Yes | Last name |
| DOB | Yes | Date of birth |
| PhoneNo | No | Contact number |
| JoiningDate | Yes | Date of joining |
| AadhaarNo | Yes | 12-digit identity number |
| Gender | Configurable | Gender selection |
| Photo | No | Employee photograph |
| DepartmentId | No | Department mapping |
| IsActive | Yes | Active/inactive status |

Do not introduce unnecessary employee fields in the initial release.

## 6.2 Employee pages

Create these Razor Views:

```text
Views/
    Employee/
        Index.cshtml
        Create.cshtml
        Edit.cshtml
        Details.cshtml
        _EmployeeForm.cshtml
```

## 6.3 Employee features

- Employee listing.
- Add employee.
- Edit employee.
- Employee details.
- Activate/deactivate employee.
- Search by EmployeeNo.
- Search by employee name.
- Department filter.
- Pagination.
- Photo upload.
- Camera-based photograph capture.

## 6.4 Validation

- EmployeeNo must be unique.
- FirstName is required.
- LastName is required.
- DOB is required.
- JoiningDate is required.
- AadhaarNo must contain exactly 12 digits.
- Validate image type and size.
- Maximum image size: 5 MB.
- Use generated safe filenames.
- Prevent path traversal and unsafe uploads.

Use server-side validation in addition to client-side validation.

## 6.5 Aadhaar protection

Aadhaar is sensitive personal information.

- Use dummy Aadhaar values for development.
- Never expose full Aadhaar values in employee lists.
- Mask Aadhaar in the UI.
- Never log Aadhaar values.
- Do not store real Aadhaar values in plain-text JSON.
- Protect any test identity data.
- Before actual customer deployment, implement an appropriate encryption and key-management solution.

Do not use real employee identity data for this initial prototype.

---

# 7. Department Management

Create a basic department structure.

Fields:

- DepartmentId
- DepartmentName
- IsActive

Provide seed departments:

1. Production
2. Administration
3. Human Resources
4. Security

Allow departments to be mapped to employees.

Department management can initially be simple, but keep it extensible.

---

# 8. Local Face Recognition

Implement local face recognition without paid cloud APIs.

Use a suitable maintained ONNX-based face detection and recognition library.

Before implementation:

- Verify the actual NuGet package.
- Verify the package's current API.
- Verify model compatibility.
- Verify model licensing.
- Verify commercial-use conditions.
- Verify required native dependencies.

Do not invent package methods or assume all ONNX models have the same license.

## 8.1 Face registration workflow

1. Admin opens an employee profile.
2. Admin selects Face Registration.
3. Camera opens.
4. Capture employee face.
5. Detect faces.
6. Require exactly one face.
7. Generate a face embedding.
8. Save the embedding against EmployeeId.
9. Save model/version information.
10. Display registration success.

## 8.2 Attendance face recognition workflow

1. Employee opens the attendance terminal.
2. Camera starts.
3. Capture a live photograph.
4. Detect the face.
5. Generate an embedding.
6. Compare against registered employee templates.
7. Identify the best match.
8. Display employee details and match score.
9. Allow the attendance action.
10. Save the attendance event.

## 8.3 Face recognition requirements

- Handle no-face detection.
- Handle multiple-face detection.
- Handle poor image quality.
- Handle unknown employees.
- Handle inactive employees.
- Handle missing face registration.
- Prevent duplicate face registration where practical.
- Store model version.
- Use a configurable matching threshold.
- Do not assume a universal accuracy threshold.
- Calibrate matching using suitable test data.

Face matching is not the same as liveness detection.

Do not claim that the system prevents photographs, videos or spoofing unless a real liveness mechanism has been implemented and tested.

Display a clear message that this initial face-recognition workflow is a prototype and requires security validation before production use.

---

# 9. Attendance Management Module

Create a functional attendance module.

## 9.1 Attendance features

- Attendance terminal.
- Punch IN.
- Punch OUT.
- Daily attendance.
- Employee attendance history.
- Attendance details.
- Attendance status.
- Search and date filtering.

## 9.2 Attendance business rules

For the initial implementation:

- One IN per employee per day.
- One OUT per employee per day.
- Reject duplicate IN.
- Reject OUT without IN.
- Reject duplicate OUT.
- Inactive employees cannot punch.
- An employee must have a registered face for face-based attendance.
- Use server-side timestamps.
- Do not trust the browser's local clock.
- Use a configurable application timezone.
- Save match score where applicable.
- Save captured image path where applicable.

Keep business rules inside AttendanceService.

Do not put attendance business rules in the controller, repository or JavaScript.

## 9.3 Attendance model

Create AttendanceModel with fields such as:

- AttendanceId
- EmployeeId
- AttendanceDate
- PunchType
- PunchTime
- CapturedPhotoPath
- MatchScore
- CreatedDate

Use appropriate types and validation.

---

# 10. Industrial Attendance Dashboard

This is the primary screen for the customer demonstration.

Create a professional industrial workforce dashboard.

## 10.1 Dashboard KPI cards

Display:

1. Total Employees
2. Present Today
3. Absent Today
4. Currently Inside
5. Late Arrivals
6. Attendance Percentage

Each card should have:

- Clear title.
- Large number.
- Small descriptive label.
- Appropriate icon.
- Consistent design.

## 10.2 Dashboard calculations

Calculate all dashboard values using repository/service data.

Do not hardcode dashboard numbers.

Definitions:

- Total Employees: count of active employees.
- Present Today: employees with a valid IN today.
- Absent Today: active employees without a valid IN today.
- Currently Inside: employees with IN but no OUT today.
- Late Arrivals: employees whose first IN is after configured shift start.
- Attendance Percentage: Present Today / Total Active Employees × 100.

Avoid double counting.

Make the shift start time configurable.

## 10.3 Dashboard charts

Implement:

- Present vs Absent doughnut chart.
- Last 7 days attendance trend.
- Department-wise attendance bar chart.

Use a suitable lightweight chart library.

Charts must be responsive.

## 10.4 Live attendance activity

Display the latest attendance activity:

- Employee photo.
- Employee number.
- Employee name.
- Punch type.
- Punch time.
- Face match status.

## 10.5 Today's attendance table

Columns:

- Photo
- Employee No
- Employee Name
- Department
- First IN
- Last OUT
- Working Hours
- Attendance Status

## 10.6 Dashboard interactions

Provide:

- Date filter.
- Department filter.
- Employee search.
- Manual refresh.
- Automatic refresh every 30 seconds.
- CSV export.

Use a professional and readable status system:

- Present — green.
- Absent — red.
- Late — amber.
- Currently Inside — blue.
- Not Punched Out — orange.

---

# 11. User Interface and Visual Design

The UI must look like a professional commercial HRMS product.

Do not use an outdated admin template or an unmodified generic dashboard.

## 11.1 Design direction

- Industrial workforce management theme.
- Navy and teal colour palette.
- Clean white content surfaces.
- Modern typography.
- Consistent spacing.
- Professional icons.
- Subtle shadows.
- Clear visual hierarchy.
- Responsive tables.
- Mobile-friendly forms.
- Large touch-friendly buttons.

## 11.2 Main layout

Create:

- Sidebar navigation.
- Top navigation bar.
- Main content area.
- User profile section.
- Notification area.
- Responsive mobile navigation drawer.

## 11.3 Navigation

Initial navigation:

```text
HRMS
│
├── Dashboard
│
├── Employee Management
│   ├── Employee List
│   └── Add Employee
│
├── Attendance
│   ├── Attendance Terminal
│   ├── Daily Attendance
│   └── Attendance History
│
├── Face Registration
│
├── Departments
│
└── Settings
```

Prepare disabled or clearly marked future navigation entries for:

- Leave Management.
- Shift Management.
- Payroll.
- User Management.
- Reports.

Do not display unfinished modules as completed functionality.

## 11.4 Mobile UI

The application must be fully responsive.

On mobile:

- Sidebar becomes a drawer.
- KPI cards stack appropriately.
- Tables use responsive layouts or horizontal scrolling.
- Employee forms become single-column.
- Camera view fits the screen.
- Attendance IN/OUT buttons are large and touch-friendly.
- Important information remains readable.

---

# 12. Progressive Web Application (PWA)

Make HRMS installable as a PWA.

Implement:

- manifest.json.
- service-worker.js.
- Application icons.
- Installable application metadata.
- Responsive mobile layout.
- Offline static application shell where practical.

## 12.1 Security requirements

Do not cache:

- Login responses.
- Employee records.
- Aadhaar information.
- Attendance records.
- Face templates.
- Camera images.
- Sensitive API responses.

Do not implement offline attendance synchronization in this initial version.

Do not report attendance as successful until the server confirms that it has saved the attendance event.

Camera access requires HTTPS or localhost.

---

# 13. MVC Project Structure

Use this project structure.

```text
HRMS/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── EmployeeController.cs
│   ├── AttendanceController.cs
│   ├── FaceRegistrationController.cs
│   ├── DepartmentController.cs
│   └── SettingsController.cs
│
├── Models/
│   ├── EmployeeModel.cs
│   ├── AttendanceModel.cs
│   ├── DepartmentModel.cs
│   ├── LoginModel.cs
│   ├── DashboardModel.cs
│   └── FaceMatchResult.cs
│
├── ViewModels/
│   ├── EmployeeViewModel.cs
│   ├── AttendanceViewModel.cs
│   ├── DashboardViewModel.cs
│   └── LoginViewModel.cs
│
├── Interfaces/
│   ├── IEmployeeRepository.cs
│   ├── IAttendanceRepository.cs
│   ├── IDashboardRepository.cs
│   ├── IDepartmentRepository.cs
│   ├── IFaceTemplateRepository.cs
│   ├── IEmployeeService.cs
│   ├── IAttendanceService.cs
│   └── IFaceRecognitionService.cs
│
├── Repositories/
│   ├── JsonEmployeeRepository.cs
│   ├── JsonAttendanceRepository.cs
│   ├── JsonDashboardRepository.cs
│   ├── JsonDepartmentRepository.cs
│   └── JsonFaceTemplateRepository.cs
│
├── Services/
│   ├── EmployeeService.cs
│   ├── AttendanceService.cs
│   ├── DashboardService.cs
│   └── FaceRecognitionService.cs
│
├── Data/
│   ├── JsonFileStorage.cs
│   ├── JsonStorageOptions.cs
│   └── StoragePathProvider.cs
│
├── Views/
│   ├── Account/
│   │   └── Login.cshtml
│   │
│   ├── Dashboard/
│   │   └── Index.cshtml
│   │
│   ├── Employee/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── _EmployeeForm.cshtml
│   │
│   ├── Attendance/
│   │   ├── Terminal.cshtml
│   │   ├── Daily.cshtml
│   │   └── History.cshtml
│   │
│   ├── FaceRegistration/
│   │   └── Index.cshtml
│   │
│   ├── Department/
│   │   ├── Index.cshtml
│   │   └── Create.cshtml
│   │
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _LoginLayout.cshtml
│       ├── _Sidebar.cshtml
│       ├── _Topbar.cshtml
│       └── _ValidationScriptsPartial.cshtml
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   ├── icons/
│   ├── lib/
│   ├── manifest.json
│   └── service-worker.js
│
├── Properties/
│   └── PublishProfiles/
│
├── Program.cs
├── appsettings.json
├── HRMS.csproj
└── README.md
```

You may adjust the structure when necessary, but maintain MVC, separation of concerns and repository interfaces.

---

# 14. Windows EXE Publishing

The application should support running as a Windows standalone application.

Expected executable name:

**HRMS.exe**

Use ASP.NET Core self-contained deployment.

Recommended publish settings:

- Configuration: Release.
- Runtime: win-x64.
- Deployment mode: Self-contained.
- PublishSingleFile: true.
- Include native libraries as required.

## 14.1 Expected user experience

1. User double-clicks HRMS.exe.
2. Local ASP.NET Core server starts.
3. Application checks that the server is ready.
4. Default browser opens the HRMS login page.
5. User logs in.
6. Dashboard appears.

The machine should not require a separately installed .NET runtime.

## 14.2 Startup requirements

- Use a safe configurable localhost port.
- Detect port conflicts.
- Do not open the browser before the server is ready.
- Log startup failures.
- Provide graceful application shutdown.
- Avoid launching multiple application instances accidentally.
- Do not expose the local server to the public network by default.

Use supported ASP.NET Core hosting and process-lifetime APIs.

## 14.3 Persistent data

Do not store runtime JSON files or employee uploads inside the EXE.

Use a separate writable application-data directory.

Updating HRMS.exe must not erase customer data.

## 14.4 Publishing documentation

Provide:

- Build instructions.
- Publish instructions.
- Output directory structure.
- How to run HRMS.exe.
- How to back up HRMS data.
- How to update HRMS.exe.
- How to restore data.

A separate desktop wrapper using WebView2 is not required.

---

# 15. Seed Data

Create initial JSON files containing:

- 4 departments.
- 10 dummy employees.
- Sample attendance records.
- Default application settings.

Use only fictional employee information.

Do not generate real Aadhaar numbers or real biometric identities.

Ensure sample records demonstrate:

- Present employees.
- Absent employees.
- Late arrivals.
- Employees currently inside.
- Employees with completed IN/OUT.

Dashboard calculations must use these records.

---

# 16. Security and Reliability

Implement reasonable security practices even in the initial JSON version.

Requirements:

- Cookie authentication.
- Authorization on protected controllers.
- Anti-forgery validation on state-changing MVC forms.
- Server-side input validation.
- Safe file upload validation.
- Safe file paths.
- No sensitive information in logs.
- Proper exception handling.
- Secure cookie settings.
- Configurable application timezone.
- Data protection for sensitive information.
- No hardcoded production secrets.
- No public access to JSON files.
- No public access to face-template files.
- No direct exposure of local file-system paths.

Before production deployment, perform security and privacy review.

---

# 17. Future HRMS Expansion

Do not implement every HRMS module now.

However, the foundation should allow the following modules to be added later:

1. Employee Management.
2. Attendance Management.
3. Shift Management.
4. Leave Management.
5. Payroll Management.
6. Expense Management.
7. Asset Management.
8. Recruitment / ATS.
9. Performance Management.
10. Learning Management.
11. User and Role Management.
12. Reports and Analytics.

Keep common models, services and interfaces reusable.

Do not build unnecessary microservices for this initial release.

Use a modular monolithic MVC architecture.

---

# 18. Development Phases

Implement in this order.

### Phase 1 — Foundation

- Create ASP.NET Core MVC project.
- Configure Bootstrap.
- Create shared layout.
- Create sidebar and top navigation.
- Configure dependency injection.
- Create JSON storage infrastructure.

### Phase 2 — Authentication

- Create login page.
- Implement cookie authentication.
- Protect internal controllers.
- Implement logout.

### Phase 3 — Employee Management

- Create employee models.
- Create repository interfaces.
- Implement JSON repositories.
- Create employee listing.
- Create employee form.
- Implement CRUD.
- Implement photo upload.
- Add validation.

### Phase 4 — Department Management

- Create department model.
- Implement JSON persistence.
- Map departments to employees.

### Phase 5 — Face Recognition

- Integrate the selected local recognition library.
- Implement face registration.
- Save embeddings.
- Implement face matching.
- Test with suitable sample images.

### Phase 6 — Attendance

- Implement attendance terminal.
- Implement Punch IN.
- Implement Punch OUT.
- Implement duplicate validation.
- Implement attendance history.
- Implement daily attendance.

### Phase 7 — Dashboard

- Implement KPI calculations.
- Implement charts.
- Implement live activity.
- Implement daily attendance table.
- Implement filters and CSV export.

### Phase 8 — PWA

- Implement manifest.
- Implement service worker.
- Add icons.
- Test installation.
- Test mobile responsiveness.

### Phase 9 — EXE Publishing

- Configure self-contained publishing.
- Configure startup and browser opening.
- Configure persistent storage paths.
- Test HRMS.exe on a Windows machine.

### Phase 10 — Final Testing

Test the complete application.

Do not declare completion until the application builds and the core workflows have been tested.

---

# 19. Testing Checklist

Test these scenarios:

## Authentication

- Correct login.
- Incorrect login.
- Logout.
- Unauthorized access.

## Employee Management

- Add employee.
- Duplicate EmployeeNo.
- Missing required fields.
- Edit employee.
- Employee details.
- Deactivate employee.
- Photo upload.
- Search and pagination.

## JSON Persistence

- Create employee.
- Restart application.
- Verify employee still exists.
- Update employee.
- Verify updated data after restart.
- Handle missing JSON files.
- Handle invalid JSON files.
- Test concurrent write protection.

## Attendance

- Valid Punch IN.
- Duplicate Punch IN.
- Valid Punch OUT.
- Punch OUT without IN.
- Duplicate Punch OUT.
- Inactive employee.
- Unknown face.
- Missing face registration.

## Dashboard

- Total employees.
- Present count.
- Absent count.
- Currently inside count.
- Late arrivals.
- Attendance percentage.
- Department chart.
- Seven-day trend.
- Filters.
- CSV export.

## PWA

- Install on desktop.
- Install on mobile.
- Responsive layout.
- Static shell caching.
- No sensitive data cached.

## EXE

- Publish in Release.
- Launch HRMS.exe.
- Browser opens after server startup.
- Login works.
- JSON data persists.
- Uploads persist.
- Application closes gracefully.
- Existing data survives executable updates.

---

# 20. Important Implementation Rules for Antigravity

1. Inspect the project directory before making changes.
2. If a project already exists, preserve working code unless changes are necessary.
3. Use ASP.NET Core MVC and Razor Views.
4. Do not switch to Razor Pages.
5. Keep the repository interfaces independent of JSON.
6. Keep all business logic in services.
7. Keep all JSON persistence inside repositories and storage infrastructure.
8. Use dependency injection.
9. Use asynchronous methods consistently.
10. Use meaningful naming conventions.
11. Do not create unnecessary abstractions.
12. Do not add unnecessary NuGet packages.
13. Verify third-party library APIs before using them.
14. Do not invent methods or pretend a feature works.
15. Do not use hardcoded dashboard values.
16. Do not leave important buttons non-functional.
17. Do not use real employee or Aadhaar data.
18. Do not claim liveness detection unless implemented.
19. Build the project after implementation.
20. Fix compilation errors and test core workflows.

If a feature cannot be completed reliably, clearly document the limitation instead of pretending it is complete.

---

# 21. Final Deliverables

At completion, provide:

1. Complete HRMS ASP.NET Core MVC source code.
2. Functional Razor Views.
3. Responsive industrial UI.
4. JSON storage implementation.
5. Repository interfaces.
6. Service layer.
7. Employee Management module.
8. Department Management module.
9. Face Registration.
10. Attendance IN/OUT.
11. Attendance Dashboard.
12. PWA support.
13. Windows standalone publishing profile.
14. HRMS.exe publishing instructions.
15. README.md.
16. Test results.
17. Known limitations.
18. SQL Server migration instructions.

## Final acceptance criteria

The final application must:

- Run successfully.
- Have a professional HRMS identity.
- Use ASP.NET Core MVC and Razor Views.
- Support desktop and mobile.
- Support PWA installation.
- Store data in JSON files initially.
- Preserve data after restarting the application.
- Have clean repository and service architecture.
- Support future SQL Server migration.
- Have working employee management.
- Have working attendance IN/OUT validation.
- Have a functioning dashboard based on actual stored data.
- Support local face recognition where the required model and dependencies are available.
- Be publishable as HRMS.exe.
- Keep application data separate from the executable.

**The most important requirement: We are building HRMS as an extensible customer-facing product prototype. The initial JSON storage provider must be replaceable with SQL Server without rewriting the application.**