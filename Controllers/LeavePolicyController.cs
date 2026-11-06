using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeavePolicyController : Controller
{
    private readonly ILeavePolicyRepository _policyRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IDepartmentRepository _deptRepo;

    public LeavePolicyController(
        ILeavePolicyRepository policyRepo,
        ILeaveTypeRepository leaveTypeRepo,
        IDepartmentRepository deptRepo)
    {
        _policyRepo = policyRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _deptRepo = deptRepo;
    }

    // GET: /LeavePolicy
    public async Task<IActionResult> Index()
    {
        var policies = await _policyRepo.GetAllAsync();
        var types = await _leaveTypeRepo.GetAllAsync();
        var depts = await _deptRepo.GetAllAsync();

        ViewBag.LeaveTypes = types.ToDictionary(t => t.LeaveTypeId, t => $"{t.LeaveName} ({t.LeaveCode})", StringComparer.OrdinalIgnoreCase);
        ViewBag.Departments = depts.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        return View(policies);
    }

    // GET: /LeavePolicy/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new LeavePolicyModel
        {
            AnnualEntitlement = 12.0,
            AccrualFrequency = "Monthly",
            AccrualAmount = 1.0,
            MaximumBalance = 24.0,
            ExcludeWeeklyOffs = true,
            ExcludeHolidays = true,
            EnableSandwichRule = false,
            IsActive = true
        });
    }

    // POST: /LeavePolicy/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeavePolicyModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PolicyName) || string.IsNullOrWhiteSpace(model.LeaveTypeId))
        {
            ModelState.AddModelError("", "Policy Name and Leave Type are required.");
            await LoadDropdownsAsync();
            return View(model);
        }

        await _policyRepo.AddAsync(model);
        TempData["SuccessMessage"] = $"Leave Policy '{model.PolicyName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /LeavePolicy/Configure/{id}
    public async Task<IActionResult> Configure(string id)
    {
        var policy = await _policyRepo.GetByIdAsync(id);
        if (policy == null) return NotFound();

        await LoadDropdownsAsync();
        return View(policy);
    }

    // POST: /LeavePolicy/Configure/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(string id, LeavePolicyModel model)
    {
        if (id != model.LeavePolicyId) return BadRequest();

        if (string.IsNullOrWhiteSpace(model.PolicyName) || string.IsNullOrWhiteSpace(model.LeaveTypeId))
        {
            ModelState.AddModelError("", "Policy Name and Leave Type are required.");
            await LoadDropdownsAsync();
            return View(model);
        }

        await _policyRepo.UpdateAsync(model);
        TempData["SuccessMessage"] = $"Leave Policy '{model.PolicyName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /LeavePolicy/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _policyRepo.DeleteAsync(id);
        TempData["SuccessMessage"] = "Leave Policy deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdownsAsync()
    {
        var types = await _leaveTypeRepo.GetAllAsync();
        var depts = await _deptRepo.GetAllAsync();
        ViewBag.LeaveTypes = types.Where(t => t.IsActive).OrderBy(t => t.LeaveName).ToList();
        ViewBag.Departments = depts.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
    }
}

