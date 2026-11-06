using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;

namespace HRMS.Repositories;

public class JsonFaceTemplateRepository : IFaceTemplateRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonFaceTemplateRepository> _logger;

    public JsonFaceTemplateRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonFaceTemplateRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FaceTemplateModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.FaceTemplatesFilePath))
        {
            var seed = GetSeedTemplates();
            await _storage.WriteAsync(_pathProvider.FaceTemplatesFilePath, seed);
            return seed.Where(t => t.IsActive).ToList();
        }

        var templates = await _storage.ReadListAsync<FaceTemplateModel>(_pathProvider.FaceTemplatesFilePath);
        return templates.Where(t => t.IsActive).ToList();
    }

    public async Task<FaceTemplateModel?> GetByEmployeeIdAsync(string employeeId)
    {
        var templates = await GetAllAsync();
        return templates.FirstOrDefault(t => (string.Equals(t.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) || string.Equals(t.EmployeeNo, employeeId, StringComparison.OrdinalIgnoreCase)) && t.IsActive);
    }

    public async Task SaveTemplateAsync(FaceTemplateModel template)
    {
        var templates = (await GetAllAsync()).ToList();
        var index = templates.FindIndex(t => string.Equals(t.EmployeeId, template.EmployeeId, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(template.EmployeeNo) && string.Equals(t.EmployeeNo, template.EmployeeNo, StringComparison.OrdinalIgnoreCase)));
        if (index >= 0)
        {
            template.EnrolledAt = DateTime.UtcNow;
            templates[index] = template;
        }
        else
        {
            if (string.IsNullOrEmpty(template.TemplateId))
            {
                template.TemplateId = Guid.NewGuid().ToString();
            }
            template.EnrolledAt = DateTime.UtcNow;
            templates.Add(template);
        }
        await _storage.WriteAsync(_pathProvider.FaceTemplatesFilePath, templates);
    }

    public async Task DeleteByEmployeeIdAsync(string employeeId)
    {
        var templates = (await GetAllAsync()).ToList();
        var count = templates.RemoveAll(t => string.Equals(t.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) || string.Equals(t.EmployeeNo, employeeId, StringComparison.OrdinalIgnoreCase));
        if (count > 0)
        {
            await _storage.WriteAsync(_pathProvider.FaceTemplatesFilePath, templates);
        }
    }

    private static List<FaceTemplateModel> GetSeedTemplates()
    {
        // Face templates are generated exclusively from genuine facial photographs or camera captures
        return new List<FaceTemplateModel>();
    }
}
