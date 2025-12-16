using System.Collections.Concurrent;
using domain.Interfaces;

namespace application.Services;

/// <summary>
/// State container service for managing device state updates across all device types.
/// Used by Blazor components to receive real-time updates via domain models (no DTOs).
/// </summary>
public class DeviceStateService
{
    // Store domain models by device ID (regardless of type)
    private readonly ConcurrentDictionary<Guid, IDevice> _deviceStates = new();

    // Generic events for state changes - broadcasts domain models directly
    public event Action<Guid, IDevice>? OnStateChanged;

    /// <summary>
    /// Update device state and notify subscribers
    /// </summary>
    public void UpdateDeviceState(Guid deviceId, IDevice device)
    {
        _deviceStates[deviceId] = device;
        OnStateChanged?.Invoke(deviceId, device);
    }

    /// <summary>
    /// Get current state for a device
    /// </summary>
    public IDevice? GetDeviceState(Guid deviceId)
    {
        _deviceStates.TryGetValue(deviceId, out var device);
        return device;
    }

    /// <summary>
    /// Get all device states
    /// </summary>
    public IEnumerable<IDevice> GetAllDeviceStates()
    {
        return _deviceStates.Values;
    }
}
