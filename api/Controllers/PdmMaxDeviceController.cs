using application.Services;
using AutoMapper;
using contracts.Devices.PdmMax;
using domain.Devices.dingoPdmMax;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/pdm-max-device")]
[ApiController]
public class PdmMaxDeviceController(DeviceManager deviceManager, IMapper mapper)
    : BasePdmController<PdmMaxDevice, PdmMaxDto>(deviceManager, mapper)
{
    protected override string DeviceName => "PDM-Max";
}
