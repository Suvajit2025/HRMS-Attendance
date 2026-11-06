using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class WeeklyOffController : Controller
{
    private readonly IWeeklyOffService _weeklyOffService;
    private readonly IDepartmentRepository _deptRepo;

    public WeeklyOffController(IWeeklyOffService weeklyOffService, IDepartmentRepository deptRepo)
    {
        _weeklyOffService = weeklyOffService;
        _deptRepo = deptRepo;
    }

    // GET: /WeeklyOff
    public async Task<IActionResult> Index()
    {
        var weeklyOffs = await _weeklyOffService.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();
        ViewBag.Departments = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);
        return View(weeklyOffs);
    }

    // GET: /WeeklyOff/Configure
    public async Task<IActionResult> Configure(string? id)
    {
        var departments = await _deptRepo.GetAllAsync();
        ViewBag.Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();

        if (string.IsNullOrEmpty(id))
        {
            return View(new WeeklyOffModel
            {
                DayOfWeek = DayOfWeek.Sunday,
                IsWeeklyOff = true,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true
            });
        }

        var existing = await _weeklyOffService.GetByIdAsync(id);
        if (existing == null) return NotFound();

        return View(existing);
    }

    // POST: /WeeklyOff/Configure
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(WeeklyOffModel model)
    {
        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        bool isNew = string.IsNullOrEmpty(model.WeeklyOffId);
        var (success, error) = isNew
            ? await _weeklyOffService.AddAsync(model)
            : await _weeklyOffService.UpdateAsync(model);

        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to save weekly off configuration.");
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
            return View(model);
        }

        TempData["SuccessMessage"] = "Weekly off configuration saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /WeeklyOff/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var (success, error) = await _weeklyOffService.DeleteAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete weekly off configuration.";
        }
        else
        {
            TempData["SuccessMessage"] = "Weekly off configuration removed.";
        }
        return RedirectToAction(nameof(Index));
    }
}

