using application.Services;
using AutoMapper;
using contracts.Devices.Pdm;
using contracts.Devices.Pdm.Functions;
using domain.Devices.dingoPdm;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/pdm-device")]
[ApiController]
public class PdmDeviceController(DeviceManager deviceManager, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Get PDM device data (state and configuration)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PdmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<PdmDto> GetDevice(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Use AutoMapper for device-level properties
        var deviceDto = mapper.Map<PdmDto>(device);

        // Map collections using accessor methods
        deviceDto.Inputs = mapper.Map<List<InputDto>>(device.GetInputs());
        deviceDto.Outputs = mapper.Map<List<OutputDto>>(device.GetOutputs());
        deviceDto.CanInputs = mapper.Map<List<CanInputDto>>(device.GetCanInputs());
        deviceDto.VirtualInputs = mapper.Map<List<VirtualInputDto>>(device.GetVirtualInputs());
        deviceDto.Flashers = mapper.Map<List<FlasherDto>>(device.GetFlashers());
        deviceDto.Counters = mapper.Map<List<CounterDto>>(device.GetCounters());
        deviceDto.Conditions = mapper.Map<List<ConditionDto>>(device.GetConditions());

        // Map single objects
        deviceDto.Wipers = mapper.Map<WiperDto>(device.GetWipers());
        deviceDto.StarterDisable = mapper.Map<StarterDisableDto>(device.GetStarterDisable());

        return Ok(deviceDto);
    }
    
    /// <summary>
    /// Initiate Read PDM device data (configuration) from device
    /// </summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult ReadDevice(Guid id, [FromBody] PdmDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        
        deviceManager.ReadDeviceConfig(id);
        //Properties will be updated as read responses come in

        return Ok(new { message = "Device data updated and write initiated" });
    }

    /// <summary>
    /// Write PDM device data (configuration) and write to device
    /// </summary>
    [HttpPut("{id:guid}/write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult WriteDevice(Guid id, [FromBody] PdmDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Map device-level properties
        mapper.Map(deviceDto, device);

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
            mapper.Map(deviceDto.Wipers, device.GetWipers());

        if (deviceDto.StarterDisable != null)
            mapper.Map(deviceDto.StarterDisable, device.GetStarterDisable());
        
        deviceManager.WriteDeviceConfig(id);

        return Ok(new { message = "Device data updated and write initiated" });
    }

    private void MapCollectionToDevice<TDto, TDomain>(IReadOnlyList<TDomain> deviceCollection, List<TDto>? dtoList)
    {
        if (dtoList == null) return;
        for (var i = 0; i < Math.Min(deviceCollection.Count, dtoList.Count); i++)
            mapper.Map(dtoList[i], deviceCollection[i]);
    }
    
    /// <summary>
    /// Send burn settings request to PDM
    /// </summary>
    [HttpPut("{id:guid}/burn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult SendBurn(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!deviceManager.BurnSettings(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Burn Failed",
                Detail = $"PDM device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        
        return Ok(new { message = "Burn sent to device" });
    }
    
    /// <summary>
    /// Send request sleep to PDM
    /// </summary>
    [HttpPut("{id:guid}/sleep")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult SendSleep(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!deviceManager.RequestSleep(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Sleep Failed",
                Detail = $"PDM device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        
        return Ok(new { message = "Sleep sent to device" });
    }
    
    /// <summary>
    /// Request PDM version
    /// </summary>
    [HttpPut("{id:guid}/version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult RequestVersion(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!deviceManager.RequestVersion(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Version Failed",
                Detail = $"PDM device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        
        return Ok(new { message = "Request version sent to device" });
    }
    
    /// <summary>
    /// Send device wakeup request to PDM
    /// </summary>
    [HttpPut("{id:guid}/wakeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult SendWakeup(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!deviceManager.RequestWakeup(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Wakeup Failed",
                Detail = $"PDM device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        
        return Ok(new { message = "Wakeup sent to device" });
    }
    
    /// <summary>
    /// Send enter bootloader request to PDM
    /// </summary>
    [HttpPut("{id:guid}/bootloader")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult EnterBootloader(Guid id)
    {
        var device = deviceManager.GetDevice<PdmDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        if (!deviceManager.RequestBootloader(device.Guid))
            return NotFound(new ProblemDetails
            {
                Title = "Request Bootloader Failed",
                Detail = $"PDM device with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        
        return Ok(new { message = "Enter bootloader sent to device" });
    }
}
