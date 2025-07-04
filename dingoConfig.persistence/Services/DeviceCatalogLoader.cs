using System.Text.Json;
using dingoConfig.Contracts;
using Microsoft.Extensions.Logging;

namespace dingoConfig.Persistence.Services;

public class DeviceCatalogLoader
{
    private readonly ILogger<DeviceCatalogLoader> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DeviceCatalogLoader(ILogger<DeviceCatalogLoader> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    public async Task<DeviceCatalog?> LoadCatalogAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Catalog file not found: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var catalog = JsonSerializer.Deserialize<DeviceCatalog>(json, _jsonOptions);
            
            if (catalog != null)
            {
                _logger.LogDebug("Successfully loaded catalog: {DeviceType} v{Version}", 
                    catalog.DeviceType, catalog.Version);
            }

            return catalog;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse catalog JSON file: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load catalog file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<List<DeviceCatalog>> LoadCatalogsFromDirectoryAsync(string directoryPath)
    {
        var catalogs = new List<DeviceCatalog>();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Catalog directory not found: {DirectoryPath}", directoryPath);
                return catalogs;
            }

            var catalogFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Found {Count} catalog files in {DirectoryPath}", catalogFiles.Length, directoryPath);

            foreach (var filePath in catalogFiles)
            {
                var catalog = await LoadCatalogAsync(filePath);
                if (catalog != null)
                {
                    catalogs.Add(catalog);
                }
            }

            _logger.LogInformation("Successfully loaded {Count} catalogs", catalogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load catalogs from directory: {DirectoryPath}", directoryPath);
        }

        return catalogs;
    }

    public DeviceCatalog? ParseCatalogJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DeviceCatalog>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse catalog JSON");
            return null;
        }
    }
}