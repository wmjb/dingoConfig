using System.Collections.Concurrent;
using contracts.Devices;

namespace application.Services;

/// <summary>
/// State container service for managing device state and config updates across all device types.
/// Used by Blazor components to receive real-time updates.
/// </summary>
public class DeviceStateService
{
    // Store combined DTOs by device ID (regardless of type)
    private readonly ConcurrentDictionary<Guid, DeviceDto> _deviceStates = new();

    // Generic events for state changes
    public event Action<Guid, DeviceDto>? OnStateChanged;

    /// <summary>
    /// Update device state and notify subscribers
    /// </summary>
    public void UpdateDeviceState(Guid deviceId, DeviceDto state)
    {
        _deviceStates[deviceId] = state;
        OnStateChanged?.Invoke(deviceId, state);
    }

    /// <summary>
    /// Get current state for a device
    /// </summary>
    public DeviceDto? GetDeviceState(Guid deviceId)
    {
        _deviceStates.TryGetValue(deviceId, out var state);
        return state;
    }

    /// <summary>
    /// Get all device states
    /// </summary>
    public IEnumerable<DeviceDto> GetAllDeviceStates()
    {
        return _deviceStates.Values;
    }
}
