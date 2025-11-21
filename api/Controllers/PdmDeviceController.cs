using application.Services;
using AutoMapper;
using contracts.Devices.Pdm;
using contracts.Devices.Pdm.Functions;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdm.Functions;
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

        // Map collections using reflection + AutoMapper
        var deviceType = device.GetType();

        deviceDto.Inputs = MapCollection<Input, InputDto>(deviceType, device, "Inputs");
        deviceDto.Outputs = MapCollection<Output, OutputDto>(deviceType, device, "Outputs");
        deviceDto.CanInputs = MapCollection<CanInput, CanInputDto>(deviceType, device, "CanInputs");
        deviceDto.VirtualInputs = MapCollection<VirtualInput, VirtualInputDto>(deviceType, device, "VirtualInputs");
        deviceDto.Flashers = MapCollection<Flasher, FlasherDto>(deviceType, device, "Flashers");
        deviceDto.Counters = MapCollection<Counter, CounterDto>(deviceType, device, "Counters");
        deviceDto.Conditions = MapCollection<Condition, ConditionDto>(deviceType, device, "Conditions");

        // Map single objects
        var wiper = deviceType.GetProperty("Wipers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as Wiper;
        deviceDto.Wipers = wiper != null ? mapper.Map<WiperDto>(wiper) : null;

        var starterDisable = deviceType.GetProperty("StarterDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as StarterDisable;
        deviceDto.StarterDisable = starterDisable != null ? mapper.Map<StarterDisableDto>(starterDisable) : null;

        return Ok(deviceDto);
    }

    /// <summary>
    /// Update PDM device data (state and configuration) and download to device
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult UpdateDevice(Guid id, [FromBody] PdmDto deviceDto)
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

        // Map collections using reflection + AutoMapper
        var deviceType = device.GetType();

        MapCollectionToDevice<InputDto, Input>(deviceType, device, "Inputs", deviceDto.Inputs);
        MapCollectionToDevice<OutputDto, Output>(deviceType, device, "Outputs", deviceDto.Outputs);
        MapCollectionToDevice<CanInputDto, CanInput>(deviceType, device, "CanInputs", deviceDto.CanInputs);
        MapCollectionToDevice<VirtualInputDto, VirtualInput>(deviceType, device, "VirtualInputs", deviceDto.VirtualInputs);
        MapCollectionToDevice<FlasherDto, Flasher>(deviceType, device, "Flashers", deviceDto.Flashers);
        MapCollectionToDevice<CounterDto, Counter>(deviceType, device, "Counters", deviceDto.Counters);
        MapCollectionToDevice<ConditionDto, Condition>(deviceType, device, "Conditions", deviceDto.Conditions);

        // Map single objects
        if (deviceDto.Wipers != null)
        {
            var wiper = deviceType.GetProperty("Wipers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(device) as Wiper;
            if (wiper != null)
                mapper.Map(deviceDto.Wipers, wiper);
        }

        if (deviceDto.StarterDisable != null)
        {
            var starterDisable = deviceType.GetProperty("StarterDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(device) as StarterDisable;
            if (starterDisable != null)
                mapper.Map(deviceDto.StarterDisable, starterDisable);
        }

        // Download updated config to device
        deviceManager.DownloadUpdatedConfig(id);

        return Ok(new { message = "Device data updated and download initiated" });
    }

    // Helper method to map collections from device to DTO
    private List<TDto> MapCollection<TDomain, TDto>(Type deviceType, PdmDevice device, string propertyName)
    {
        var collection = deviceType.GetProperty(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as List<TDomain>;

        return collection != null ? mapper.Map<List<TDto>>(collection) : [];
    }

    // Helper method to map collections from DTO to device
    private void MapCollectionToDevice<TDto, TDomain>(Type deviceType, PdmDevice device, string propertyName, List<TDto> dtoList)
    {
        var collection = deviceType.GetProperty(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(device) as List<TDomain>;

        if (collection != null && dtoList != null)
        {
            for (int i = 0; i < Math.Min(collection.Count, dtoList.Count); i++)
            {
                mapper.Map(dtoList[i], collection[i]);
            }
        }
    }
}
