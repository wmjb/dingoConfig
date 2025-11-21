using application.Services;
using contracts.Devices;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/devices")]
public class DeviceController(DeviceManager deviceManager) : ControllerBase
{
    /// <summary>
    /// Get all devices
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<DeviceDto>> GetAllDevices()
    {
        var devices = deviceManager.GetAllDevices()
            .Select(device => new DeviceDto
            {
                Guid = device.Guid,
                Name = device.Name,
                BaseId = device.BaseId,
                DeviceType = GetDeviceType(device),
                Connected = device.Connected,
                Version = string.Empty // Version will be populated by device-specific DTOs
            });

        return Ok(devices);
    }

    /// <summary>
    /// Create a new device
    /// </summary>
    [HttpPost]
    public ActionResult<DeviceDto> CreateDevice([FromBody] CreateDeviceRequest request)
    {
        try
        {
            var device = deviceManager.AddDevice(request.DeviceType, request.Name, request.BaseId);

            var response = new DeviceDto
            {
                Guid = device.Guid,
                Name = device.Name,
                BaseId = device.BaseId,
                DeviceType = GetDeviceType(device),
                Connected = device.Connected,
                Version = string.Empty
            };

            return CreatedAtAction(nameof(GetDevice), new { id = device.Guid }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public ActionResult<DeviceDto> GetDevice(Guid id)
    {
        var device = deviceManager.GetDevice(id);
        if (device == null)
            return NotFound(new { error = $"Device with ID {id} not found" });

        var response = new DeviceDto
        {
            Guid = device.Guid,
            Name = device.Name,
            BaseId = device.BaseId,
            DeviceType = GetDeviceType(device),
            Connected = device.Connected,
            Version = string.Empty
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a device
    /// </summary>
    [HttpDelete("{id:guid}")]
    public ActionResult DeleteDevice(Guid id)
    {
        var device = deviceManager.GetDevice(id);
        if (device == null)
            return NotFound(new { error = $"Device with ID {id} not found" });

        deviceManager.RemoveDevice(id);

        return NoContent();
    }

    private static string GetDeviceType(domain.Interfaces.IDevice device)
    {
        var typeName = device.GetType().Name;
        // Remove "Device" suffix if present (e.g., "PdmDevice" -> "Pdm")
        return typeName.EndsWith("Device") ? typeName[..^6] : typeName;
    }
}
