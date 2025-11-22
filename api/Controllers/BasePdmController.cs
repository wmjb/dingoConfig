using application.Services;
using AutoMapper;
using contracts.Devices.Pdm;
using contracts.Devices.Pdm.Functions;
using domain.Devices.dingoPdm;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>
/// Base controller for PDM device variants - handles common CRUD and device command logic
/// Supports both dingoPDM and dingoPDM-Max devices
/// </summary>
/// <typeparam name="TDevice">Device type (must inherit from PdmDevice)</typeparam>
/// <typeparam name="TDto">DTO type (must inherit from PdmDto)</typeparam>
public abstract class BasePdmController<TDevice, TDto> : ControllerBase
    where TDevice : PdmDevice
    where TDto : PdmDto
{
    protected readonly DeviceManager DeviceManager;
    protected readonly IMapper Mapper;
    protected abstract string DeviceName { get; }

    protected BasePdmController(DeviceManager deviceManager, IMapper mapper)
    {
        DeviceManager = deviceManager;
        Mapper = mapper;
    }

    /// <summary>
    /// Get device data (state and configuration)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult<TDto> GetDevice(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Use AutoMapper for device-level properties
        var deviceDto = Mapper.Map<TDto>(device);

        // Map collections using accessor methods
        deviceDto.Inputs = Mapper.Map<List<InputDto>>(device.GetInputs());
        deviceDto.Outputs = Mapper.Map<List<OutputDto>>(device.GetOutputs());
        deviceDto.CanInputs = Mapper.Map<List<CanInputDto>>(device.GetCanInputs());
        deviceDto.VirtualInputs = Mapper.Map<List<VirtualInputDto>>(device.GetVirtualInputs());
        deviceDto.Flashers = Mapper.Map<List<FlasherDto>>(device.GetFlashers());
        deviceDto.Counters = Mapper.Map<List<CounterDto>>(device.GetCounters());
        deviceDto.Conditions = Mapper.Map<List<ConditionDto>>(device.GetConditions());

        // Map single objects
        deviceDto.Wipers = Mapper.Map<WiperDto>(device.GetWipers());
        deviceDto.StarterDisable = Mapper.Map<StarterDisableDto>(device.GetStarterDisable());

        return Ok(deviceDto);
    }

    /// <summary>
    /// Initiate read device data (configuration) from device
    /// </summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult ReadDevice(Guid id, [FromBody] TDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        DeviceManager.ReadDeviceConfig(id);
        //Properties will be updated as read responses come in

        return Ok(new { message = "Device read initiated" });
    }

    /// <summary>
    /// Write device data (configuration) and write to device
    /// </summary>
    [HttpPut("{id:guid}/write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult WriteDevice(Guid id, [FromBody] TDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Map device-level properties
        Mapper.Map(deviceDto, device);

        // Map collections using accessor methods
        MapCollectionToDevice(device.GetInputs(), deviceDto.Inputs);
        MapCollectionToDevice(device.GetOutputs(), deviceDto.Outputs);
        MapCollectionToDevice(device.GetCanInputs(), deviceDto.CanInputs);
        MapCollectionToDevice(device.GetVirtualInputs(), deviceDto.VirtualInputs);
        MapCollectionToDevice(device.GetFlashers(), deviceDto.Flashers);
        MapCollectionToDevice(device.GetCounters(), deviceDto.Counters);
        MapCollectionToDevice(device.GetConditions(), deviceDto.Conditions);

        // Map single objects
        if (deviceDto.Wipers != null)
            Mapper.Map(deviceDto.Wipers, device.GetWipers());

        if (deviceDto.StarterDisable != null)
            Mapper.Map(deviceDto.StarterDisable, device.GetStarterDisable());

        DeviceManager.WriteDeviceConfig(id);

        return Ok(new { message = "Device data updated and write initiated" });
    }

    /// <summary>
    /// Send burn settings request to device
    /// </summary>
    [HttpPut("{id:guid}/burn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult SendBurn(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!DeviceManager.BurnSettings(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Burn Failed",
                Detail = $"{DeviceName} device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new { message = "Burn sent to device" });
    }

    /// <summary>
    /// Send request sleep to device
    /// </summary>
    [HttpPut("{id:guid}/sleep")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult SendSleep(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!DeviceManager.RequestSleep(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Sleep Failed",
                Detail = $"{DeviceName} device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new { message = "Sleep sent to device" });
    }

    /// <summary>
    /// Request device version
    /// </summary>
    [HttpPut("{id:guid}/version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult RequestVersion(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!DeviceManager.RequestVersion(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Version Failed",
                Detail = $"{DeviceName} device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new { message = "Request version sent to device" });
    }

    /// <summary>
    /// Send device wakeup request to device
    /// </summary>
    [HttpPut("{id:guid}/wakeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult SendWakeup(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!DeviceManager.RequestWakeup(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Wakeup Failed",
                Detail = $"{DeviceName} device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new { message = "Wakeup sent to device" });
    }

    /// <summary>
    /// Send enter bootloader request to device
    /// </summary>
    [HttpPut("{id:guid}/bootloader")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual ActionResult EnterBootloader(Guid id)
    {
        var device = DeviceManager.GetDevice<TDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"{DeviceName} device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!DeviceManager.RequestBootloader(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Bootloader Failed",
                Detail = $"{DeviceName} device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new { message = "Enter bootloader sent to device" });
    }

    /// <summary>
    /// Helper method to map DTO collections back to device collections
    /// </summary>
    protected void MapCollectionToDevice<TDtoItem, TDomainItem>(IReadOnlyList<TDomainItem> deviceCollection, List<TDtoItem>? dtoList)
    {
        if (dtoList == null) return;
        for (var i = 0; i < Math.Min(deviceCollection.Count, dtoList.Count); i++)
            Mapper.Map(dtoList[i], deviceCollection[i]);
    }
}
