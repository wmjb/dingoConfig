using dingoConfig.Contracts;

namespace dingoConfig.Persistence.Interfaces;

public interface IDeviceCatalogService
{
    Task LoadCatalogsAsync(string catalogDirectory);
    DeviceCatalog? GetCatalog(string deviceType);
    IEnumerable<string> GetAvailableDeviceTypes();
    Task<ValidationResult> ValidateCatalogAsync(string catalogPath);
    Task<ValidationResult> ValidateCatalogJsonAsync(string catalogJson);
    IEnumerable<DeviceCatalog> GetAllCatalogs();
    bool IsLoaded { get; }
    DateTime LastLoadTime { get; }
}