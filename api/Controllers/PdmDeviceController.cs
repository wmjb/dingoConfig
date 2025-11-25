using application.Services;
using AutoMapper;
using contracts.Devices.Pdm;
using domain.Devices.dingoPdm;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/pdm-device")]
[ApiController]
public class PdmDeviceController(DeviceManager deviceManager, IMapper mapper)
    : BasePdmController<PdmDevice, PdmDto>(deviceManager, mapper)
{
    protected override string DeviceName => "PDM";
}
