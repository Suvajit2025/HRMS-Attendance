using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class DepartmentController : Controller
{
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentRepository departmentRepo, ILogger<DepartmentController> logger)
    {
        _departmentRepo = departmentRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var departments = await _departmentRepo.GetAllAsync();
        return View(departments);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DepartmentModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentModel model)
    {
        if (string.IsNullOrWhiteSpace(model.DepartmentName))
        {
            ModelState.AddModelError(nameof(model.DepartmentName), "Department Name is required.");
            return View(model);
        }

        var all = await _departmentRepo.GetAllAsync();
        if (all.Any(d => string.Equals(d.DepartmentName, model.DepartmentName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.DepartmentName), $"Department '{model.DepartmentName}' already exists.");
            return View(model);
        }

        model.DepartmentId = Guid.NewGuid().ToString();
        model.DepartmentName = model.DepartmentName.Trim();
        model.CreatedDate = DateTime.UtcNow;

        await _departmentRepo.AddAsync(model);
        TempData["SuccessMessage"] = $"Department '{model.DepartmentName}' was added successfully.";
        return RedirectToAction(nameof(Index));
    }
}

