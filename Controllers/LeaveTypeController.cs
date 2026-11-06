using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeaveTypeController : Controller
{
    private readonly ILeaveTypeRepository _leaveTypeRepo;

    public LeaveTypeController(ILeaveTypeRepository leaveTypeRepo)
    {
        _leaveTypeRepo = leaveTypeRepo;
    }

    // GET: /LeaveType
    public async Task<IActionResult> Index()
    {
        var list = await _leaveTypeRepo.GetAllAsync();
        return View(list);
    }

    // GET: /LeaveType/Create
    public IActionResult Create()
    {
        return View(new LeaveTypeModel { IsPaidLeave = true, IsHalfDayAllowed = true, RequiresApproval = true, IsActive = true });
    }

    // POST: /LeaveType/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveTypeModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LeaveCode) || string.IsNullOrWhiteSpace(model.LeaveName))
        {
            ModelState.AddModelError("", "Leave Code and Leave Name are required.");
            return View(model);
        }

        var existing = await _leaveTypeRepo.GetByCodeAsync(model.LeaveCode);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.LeaveCode), $"A leave type with code '{model.LeaveCode}' already exists.");
            return View(model);
        }

        await _leaveTypeRepo.AddAsync(model);
        TempData["SuccessMessage"] = $"Leave Type '{model.LeaveName}' ({model.LeaveCode}) created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveType/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var item = await _leaveTypeRepo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // POST: /LeaveType/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, LeaveTypeModel model)
    {
        if (id != model.LeaveTypeId) return BadRequest();

        if (string.IsNullOrWhiteSpace(model.LeaveCode) || string.IsNullOrWhiteSpace(model.LeaveName))
        {
            ModelState.AddModelError("", "Leave Code and Leave Name are required.");
            return View(model);
        }

        var existing = await _leaveTypeRepo.GetByCodeAsync(model.LeaveCode);
        if (existing != null && existing.LeaveTypeId != model.LeaveTypeId)
        {
            ModelState.AddModelError(nameof(model.LeaveCode), $"Another leave type already uses code '{model.LeaveCode}'.");
            return View(model);
        }

        await _leaveTypeRepo.UpdateAsync(model);
        TempData["SuccessMessage"] = $"Leave Type '{model.LeaveName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /LeaveType/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _leaveTypeRepo.DeleteAsync(id);
        TempData["SuccessMessage"] = "Leave Type deleted.";
        return RedirectToAction(nameof(Index));
    }
}

