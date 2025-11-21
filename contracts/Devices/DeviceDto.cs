namespace contracts.Devices;

/// <summary>
/// Base DTO for all devices - combines state and configuration properties.
/// Can be used directly for generic device information or extended by device-specific DTOs.
/// </summary>
public class DeviceDto
{
    // Identity properties
    public Guid Guid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BaseId { get; set; }
    public string DeviceType { get; set; } = string.Empty;

    // State properties
    public bool Connected { get; set; }
    public string Version { get; set; } = string.Empty;
}
