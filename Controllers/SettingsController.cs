using HRMS.Data;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HRMS.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly JsonStorageOptions _options;
    private readonly StoragePathProvider _pathProvider;
    private readonly JsonFileStorage _storage;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IOptions<JsonStorageOptions> options,
        StoragePathProvider pathProvider,
        JsonFileStorage storage,
        ILogger<SettingsController> logger)
    {
        _options = options.Value;
        _pathProvider = pathProvider;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _storage.ReadItemAsync<AppSettingsModel>(_pathProvider.AppSettingsFilePath);
        if (settings == null)
        {
            settings = new AppSettingsModel
            {
                CompanyName = _options.CompanyName,
                AppTitle = _options.AppTitle,
                ShiftStartTime = _options.ShiftStartTime,
                ShiftEndTime = _options.ShiftEndTime,
                TimeZoneId = _options.TimeZoneId,
                FaceMatchThreshold = _options.FaceMatchThreshold
            };
            await _storage.WriteAsync(_pathProvider.AppSettingsFilePath, settings);
        }

        ViewBag.DataDirectory = _pathProvider.DataDirectory;
        ViewBag.UploadsDirectory = _pathProvider.UploadsDirectory;
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AppSettingsModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.DataDirectory = _pathProvider.DataDirectory;
            ViewBag.UploadsDirectory = _pathProvider.UploadsDirectory;
            return View(model);
        }

        await _storage.WriteAsync(_pathProvider.AppSettingsFilePath, model);
        _options.CompanyName = model.CompanyName;
        _options.AppTitle = model.AppTitle;
        _options.ShiftStartTime = model.ShiftStartTime;
        _options.ShiftEndTime = model.ShiftEndTime;
        _options.TimeZoneId = model.TimeZoneId;
        _options.FaceMatchThreshold = model.FaceMatchThreshold;

        TempData["SuccessMessage"] = "System settings updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}

