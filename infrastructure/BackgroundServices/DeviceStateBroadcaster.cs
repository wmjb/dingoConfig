using application.Services;
using AutoMapper;
using contracts.Devices;
using contracts.Devices.Pdm;
using contracts.Devices.Pdm.Functions;
using contracts.Devices.PdmMax;
using contracts.Devices.Canboard;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdmMax;
using domain.Devices.CanboardDevice;
using domain.Devices.dingoPdm.Functions;
using domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace infrastructure.BackgroundServices;

/// <summary>
/// Background service that broadcasts device state updates to DeviceStateService at 20 Hz.
/// Handles all device types (PDM, PDM-Max, CANBoard) and maps them to their respective DTOs.
/// </summary>
public class DeviceStateBroadcaster : BackgroundService
{
    private readonly DeviceManager _deviceManager;
    private readonly DeviceStateService _stateService;
    private readonly IMapper _mapper;
    private readonly ILogger<DeviceStateBroadcaster> _logger;

    public DeviceStateBroadcaster(
        DeviceManager deviceManager,
        DeviceStateService stateService,
        IMapper mapper,
        ILogger<DeviceStateBroadcaster> logger)
    {
        _deviceManager = deviceManager;
        _stateService = stateService;
        _mapper = mapper;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Broadcast state at 20 Hz (every 50ms) as per CLAUDE.md
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));

        _logger.LogInformation("DeviceStateBroadcaster started - broadcasting at 20 Hz");

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Get ALL devices regardless of type
                var allDevices = _deviceManager.GetAllDevices();

                foreach (var device in allDevices)
                {
                    // Map each device type to its corresponding StateDto
                    var stateDto = MapDeviceToStateDto(device);

                    if (stateDto != null)
                    {
                        _stateService.UpdateDeviceState(device.Guid, stateDto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting device state");
            }
        }

        _logger.LogInformation("DeviceStateBroadcaster stopped");
    }

    /// <summary>
    /// Pattern match on device type and map to appropriate combined DTO
    /// </summary>
    private DeviceDto? MapDeviceToStateDto(IDevice device)
    {
        return device switch
        {
            PdmMaxDevice pdmMax => MapPdmMaxDto(pdmMax),
            PdmDevice pdm => MapPdmDto(pdm),
            CanboardDevice canboard => MapCanboardDto(canboard),
            _ => null
        };
    }

    private PdmDto MapPdmDto(PdmDevice device)
    {
        // Map base device properties using AutoMapper
        var dto = _mapper.Map<PdmDto>(device);

        // Map collections using reflection (since properties are protected)
        var deviceType = device.GetType();

        dto.Inputs = MapCollection<Input, InputDto>(deviceType, device, "Inputs");
        dto.Outputs = MapCollection<Output, OutputDto>(deviceType, device, "Outputs");
        dto.CanInputs = MapCollection<CanInput, CanInputDto>(deviceType, device, "CanInputs");
        dto.VirtualInputs = MapCollection<VirtualInput, VirtualInputDto>(deviceType, device, "VirtualInputs");
        dto.Flashers = MapCollection<Flasher, FlasherDto>(deviceType, device, "Flashers");
        dto.Counters = MapCollection<Counter, CounterDto>(deviceType, device, "Counters");
        dto.Conditions = MapCollection<Condition, ConditionDto>(deviceType, device, "Conditions");

        // Map single objects
        var wiper = deviceType.GetProperty("Wipers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as Wiper;
        dto.Wipers = wiper != null ? _mapper.Map<WiperDto>(wiper) : null;

        return dto;
    }

    private PdmMaxDto MapPdmMaxDto(PdmMaxDevice device)
    {
        // PdmMax inherits from PdmDevice, so use the base mapping
        var pdmDto = MapPdmDto(device);

        // Map to PdmMaxDto (which currently inherits from PdmDto)
        var dto = _mapper.Map<PdmMaxDto>(device);

        // Copy properties from base mapping
        dto.Inputs = pdmDto.Inputs;
        dto.Outputs = pdmDto.Outputs;
        dto.CanInputs = pdmDto.CanInputs;
        dto.VirtualInputs = pdmDto.VirtualInputs;
        dto.Flashers = pdmDto.Flashers;
        dto.Counters = pdmDto.Counters;
        dto.Conditions = pdmDto.Conditions;
        dto.Wipers = pdmDto.Wipers;

        return dto;
    }

    private CanboardDto MapCanboardDto(CanboardDevice device)
    {
        // Placeholder for CANBoard DTO mapping
        // Will be expanded when CanboardDevice is fully implemented
        var dto = _mapper.Map<CanboardDto>(device);

        return dto;
    }

    /// <summary>
    /// Helper method to map collections from protected properties
    /// </summary>
    private List<TDto> MapCollection<TDomain, TDto>(Type deviceType, IDevice device, string propertyName)
    {
        var collection = deviceType.GetProperty(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as List<TDomain>;

        return collection != null ? _mapper.Map<List<TDto>>(collection) : [];
    }
}
