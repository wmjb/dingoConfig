using AutoMapper;
using contracts.Devices.Canboard;
using domain.Devices.CanboardDevice;

namespace application.Profiles;

public class CanboardDeviceProfile : Profile
{
    public CanboardDeviceProfile()
    {
        CreateMap<CanboardDevice, CanboardDto>();
        CreateMap<CanboardDto, CanboardDevice>();
    }
}
