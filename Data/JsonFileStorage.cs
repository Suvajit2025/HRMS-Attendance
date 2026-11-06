using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRMS.Data;

public class JsonFileStorage
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<JsonFileStorage> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonFileStorage(ILogger<JsonFileStorage> logger)
    {
        _logger = logger;
    }

    private SemaphoreSlim GetLock(string filePath)
    {
        return Locks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<List<T>> ReadListAsync<T>(string filePath)
    {
        var sem = GetLock(filePath);
        await sem.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            var items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
            return items ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading JSON file from {FilePath}", filePath);
            return new List<T>();
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<T?> ReadItemAsync<T>(string filePath) where T : class
    {
        var sem = GetLock(filePath);
        await sem.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading single JSON item from {FilePath}", filePath);
            return null;
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task WriteAsync<T>(string filePath, T data)
    {
        var sem = GetLock(filePath);
        await sem.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, JsonOptions);
            var tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            await File.WriteAllTextAsync(tempPath, json);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing JSON to {FilePath}", filePath);
            throw;
        }
        finally
        {
            sem.Release();
        }
    }
}

