using application.Services;
using AutoMapper;
using contracts.Devices.Canboard;
using domain.Devices.CanboardDevice;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/canboard-device")]
[ApiController]
public class CanboardDeviceController(DeviceManager deviceManager, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Get CANBoard device data (state and configuration)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CanboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<CanboardDto> GetDevice(Guid id)
    {
        var device = deviceManager.GetDevice<CanboardDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"CANBoard device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Map to DTO
        var deviceDto = mapper.Map<CanboardDto>(device);

        // TODO: Map CANBoard-specific collections when CanboardDevice is implemented

        return Ok(deviceDto);
    }

    /// <summary>
    /// Update CANBoard device data (state and configuration) and download to device
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult UpdateDevice(Guid id, [FromBody] CanboardDto deviceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var device = deviceManager.GetDevice<CanboardDevice>(id);
        if (device == null)
            return NotFound(new ProblemDetails
            {
                Title = "Device Not Found",
                Detail = $"CANBoard device with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });

        // Map device data to device
        mapper.Map(deviceDto, device);

        // TODO: Map CANBoard-specific collections when CanboardDevice is implemented

        // Download updated config to device
        deviceManager.DownloadUpdatedConfig(id);

        return Ok(new { message = "Device data updated and download initiated" });
    }
}
