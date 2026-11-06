using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class HolidayController : Controller
{
    private readonly IHolidayService _holidayService;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IWeeklyOffService _weeklyOffService;

    public HolidayController(
        IHolidayService holidayService,
        IDepartmentRepository deptRepo,
        IWeeklyOffService weeklyOffService)
    {
        _holidayService = holidayService;
        _deptRepo = deptRepo;
        _weeklyOffService = weeklyOffService;
    }

    // GET: /Holiday
    public async Task<IActionResult> Index(int? year, string? type, string? departmentId, string? locationCode)
    {
        int selectedYear = year ?? DateTime.Today.Year;
        var holidays = await _holidayService.GetHolidaysAsync(selectedYear, type, departmentId, locationCode);
        var upcoming = await _holidayService.GetUpcomingHolidaysAsync(5);
        var departments = await _deptRepo.GetAllAsync();

        ViewBag.SelectedYear = selectedYear;
        ViewBag.SelectedType = type ?? "All";
        ViewBag.SelectedDepartmentId = departmentId;
        ViewBag.SelectedLocationCode = locationCode;
        ViewBag.Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
        ViewBag.UpcomingHolidays = upcoming;

        return View(holidays);
    }

    // GET: /Holiday/Calendar
    public async Task<IActionResult> Calendar(int? year, int? month, string? departmentId, string? locationCode, string? type)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        if (m < 1) m = 1;
        if (m > 12) m = 12;

        var holidays = await _holidayService.GetHolidaysAsync(y, type, departmentId, locationCode);
        var upcoming = await _holidayService.GetUpcomingHolidaysAsync(5);
        var weeklyOffs = await _weeklyOffService.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

        var vm = new HolidayCalendarViewModel
        {
            Year = y,
            Month = m,
            SelectedDepartmentId = departmentId,
            SelectedLocationCode = locationCode,
            SelectedHolidayType = type,
            Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList(),
            Holidays = holidays,
            UpcomingHolidays = upcoming,
            WeeklyOffs = weeklyOffs
        };

        return View(vm);
    }

    // GET: /Holiday/Create
    public async Task<IActionResult> Create()
    {
        var departments = await _deptRepo.GetAllAsync();
        ViewBag.Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
        return View(new HolidayModel { HolidayDate = DateOnly.FromDateTime(DateTime.Today), IsPaidHoliday = true, IsActive = true });
    }

    // POST: /Holiday/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayModel model)
    {
        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        var (success, error, _) = await _holidayService.CreateHolidayAsync(model);
        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to create holiday.");
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Holiday '{model.HolidayName}' created successfully.";
        return RedirectToAction(nameof(Index), new { year = model.HolidayDate.Year });
    }

    // GET: /Holiday/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var holiday = await _holidayService.GetByIdAsync(id);
        if (holiday == null) return NotFound();

        var departments = await _deptRepo.GetAllAsync();
        ViewBag.Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
        return View(holiday);
    }

    // POST: /Holiday/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, HolidayModel model)
    {
        if (id != model.HolidayId) return BadRequest();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        var (success, error) = await _holidayService.UpdateHolidayAsync(model);
        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to update holiday.");
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Holiday '{model.HolidayName}' updated successfully.";
        return RedirectToAction(nameof(Index), new { year = model.HolidayDate.Year });
    }

    // POST: /Holiday/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var (success, error) = await _holidayService.DeleteHolidayAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete holiday.";
        }
        else
        {
            TempData["SuccessMessage"] = "Holiday deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}

