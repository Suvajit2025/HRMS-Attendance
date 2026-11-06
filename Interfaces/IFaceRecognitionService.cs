using HRMS.Models;

namespace HRMS.Interfaces;

public interface IFaceRecognitionService
{
    Task<(bool Success, string Message, FaceTemplateModel? Template)> RegisterFaceAsync(string employeeId, string imageBase64);
    Task<FaceMatchResult> MatchFaceAsync(string imageBase64);
    Task<bool> HasRegisteredFaceAsync(string employeeId);
    Task<bool> RemoveTemplateAsync(string employeeId);
    Task<int> SyncEmployeePhotoTemplatesAsync();
    string GetModelInformation();
}

