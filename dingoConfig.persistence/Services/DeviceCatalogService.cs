using System.Collections.Concurrent;
using dingoConfig.Contracts;
using dingoConfig.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace dingoConfig.Persistence.Services;

public class DeviceCatalogService : IDeviceCatalogService
{
    private readonly DeviceCatalogLoader _loader;
    private readonly CatalogValidator _validator;
    private readonly ILogger<DeviceCatalogService> _logger;
    private readonly ConcurrentDictionary<string, DeviceCatalog> _catalogs = new();
    private readonly object _loadLock = new();

    private string _catalogDirectory = string.Empty;

    public bool IsLoaded { get; private set; }
    public DateTime LastLoadTime { get; private set; }

    public DeviceCatalogService(
        DeviceCatalogLoader loader,
        CatalogValidator validator,
        ILogger<DeviceCatalogService> logger)
    {
        _loader = loader;
        _validator = validator;
        _logger = logger;
    }

    public async Task LoadCatalogsAsync(string catalogDirectory)
    {
        lock (_loadLock)
        {
            if (_catalogDirectory == catalogDirectory && IsLoaded)
            {
                _logger.LogDebug("Catalogs already loaded from {Directory}", catalogDirectory);
                return;
            }
        }

        _logger.LogInformation("Loading device catalogs from {Directory}", catalogDirectory);

        try
        {
            var catalogs = await _loader.LoadCatalogsFromDirectoryAsync(catalogDirectory);
            var validCatalogs = new List<DeviceCatalog>();

            foreach (var catalog in catalogs)
            {
                var validationResult = _validator.ValidateCatalog(catalog);
                if (validationResult.IsValid)
                {
                    validCatalogs.Add(catalog);
                }
                else
                {
                    _logger.LogWarning("Skipping invalid catalog {DeviceType}: {Errors}",
                        catalog.DeviceType,
                        string.Join(", ", validationResult.Errors.Select(e => e.Message)));
                }
            }

            lock (_loadLock)
            {
                _catalogs.Clear();
                foreach (var catalog in validCatalogs)
                {
                    _catalogs.TryAdd(catalog.DeviceType, catalog);
                }

                _catalogDirectory = catalogDirectory;
                IsLoaded = true;
                LastLoadTime = DateTime.UtcNow;
            }

            _logger.LogInformation("Successfully loaded {Count} valid catalogs", validCatalogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load catalogs from {Directory}", catalogDirectory);
            throw;
        }
    }

    public DeviceCatalog? GetCatalog(string deviceType)
    {
        if (!IsLoaded)
        {
            _logger.LogWarning("Attempted to get catalog {DeviceType} before catalogs were loaded", deviceType);
            return null;
        }

        _catalogs.TryGetValue(deviceType, out var catalog);
        return catalog;
    }

    public IEnumerable<string> GetAvailableDeviceTypes()
    {
        return _catalogs.Keys.ToList();
    }

    public async Task<ValidationResult> ValidateCatalogAsync(string catalogPath)
    {
        try
        {
            var catalog = await _loader.LoadCatalogAsync(catalogPath);
            if (catalog == null)
            {
                return ValidationResult.Failure("Failed to load catalog file");
            }

            return _validator.ValidateCatalog(catalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating catalog file {FilePath}", catalogPath);
            return ValidationResult.Failure($"Error validating catalog: {ex.Message}");
        }
    }

    public Task<ValidationResult> ValidateCatalogJsonAsync(string catalogJson)
    {
        try
        {
            var catalog = _loader.ParseCatalogJson(catalogJson);
            if (catalog == null)
            {
                return Task.FromResult(ValidationResult.Failure("Failed to parse catalog JSON"));
            }

            var result = _validator.ValidateCatalog(catalog);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating catalog JSON");
            return Task.FromResult(ValidationResult.Failure($"Error validating catalog JSON: {ex.Message}"));
        }
    }

    public IEnumerable<DeviceCatalog> GetAllCatalogs()
    {
        return _catalogs.Values.ToList();
    }

    public void Dispose()
    {
    }
}