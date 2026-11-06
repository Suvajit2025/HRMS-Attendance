using HRMS.Models;

namespace HRMS.Interfaces;

public interface IFaceTemplateRepository
{
    Task<IReadOnlyList<FaceTemplateModel>> GetAllAsync();
    Task<FaceTemplateModel?> GetByEmployeeIdAsync(string employeeId);
    Task SaveTemplateAsync(FaceTemplateModel template);
    Task DeleteByEmployeeIdAsync(string employeeId);
}

