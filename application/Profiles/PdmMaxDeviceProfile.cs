using AutoMapper;
using contracts.Devices.PdmMax;
using domain.Devices.dingoPdmMax;

namespace application.Profiles;

public class PdmMaxDeviceProfile : Profile
{
    public PdmMaxDeviceProfile()
    {
        CreateMap<PdmMaxDevice, PdmMaxDto>();
        CreateMap<PdmMaxDto, PdmMaxDevice>();
    }
}
