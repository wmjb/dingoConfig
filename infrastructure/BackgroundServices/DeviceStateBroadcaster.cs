using application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace infrastructure.BackgroundServices;

/// <summary>
/// Background service that broadcasts device state updates to DeviceStateService at 20 Hz.
/// Passes domain models directly (no DTO mapping overhead).
/// </summary>
public class DeviceStateBroadcaster(
    DeviceManager deviceManager,
    DeviceStateService stateService,
    ILogger<DeviceStateBroadcaster> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Broadcast state at 20 Hz (every 50ms)
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));

        logger.LogInformation("DeviceStateBroadcaster started - broadcasting domain models at 20 Hz");

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Get ALL devices regardless of type
                var allDevices = deviceManager.GetAllDevices();

                foreach (var device in allDevices)
                {
                    // Broadcast domain model directly - no mapping!
                    stateService.UpdateDeviceState(device.Guid, device);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting device state");
            }
        }

        logger.LogInformation("DeviceStateBroadcaster stopped");
    }
}
