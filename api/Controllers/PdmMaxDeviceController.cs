using application.Services;
using AutoMapper;
using contracts.Devices.PdmMax;
using contracts.Devices.Pdm.Functions;
using domain.Devices.dingoPdmMax;
using domain.Devices.dingoPdm.Functions;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/pdm-max-device")]
[ApiController]
public class PdmMaxDeviceController(DeviceManager deviceManager, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Get PDM-Max device data (state and configuration)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PdmMaxDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<PdmMaxDto> GetDevice(Guid id)
    {
        var device = deviceManager.GetDevice<PdmMaxDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM-Max device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Use AutoMapper for device-level properties
        var deviceDto = mapper.Map<PdmMaxDto>(device);

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
    /// Update PDM-Max device data (state and configuration) and download to device
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult UpdateDevice(Guid id, [FromBody] PdmMaxDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = deviceManager.GetDevice<PdmMaxDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"PDM-Max device with ID '{id}' was not found",
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

        // Download updated config to device
        deviceManager.DownloadUpdatedConfig(id);

        return Ok(new { message = "Device data updated and download initiated" });
    }

    // Helper method to map DTO collections back to device collections
    private void MapCollectionToDevice<TDto, TDomain>(IReadOnlyList<TDomain> deviceCollection, List<TDto>? dtoList)
    {
        if (dtoList == null) return;
        for (var i = 0; i < Math.Min(deviceCollection.Count, dtoList.Count); i++)
        {
            mapper.Map(dtoList[i], deviceCollection[i]);
        }
    }
}
