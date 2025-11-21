using AutoMapper;
using contracts.Devices.Pdm;
using contracts.Devices.Pdm.Functions;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdm.Enums;
using domain.Devices.dingoPdm.Functions;
using domain.Enums;

namespace application.Profiles;

public class PdmDeviceProfile : Profile
{
    public PdmDeviceProfile()
    {
        CreateMap<PdmDevice, PdmDto>();
        CreateMap<PdmDto, PdmDevice>();

        //Functions
        CreateMap<Input, InputDto>();
        CreateMap<InputDto, Input>();

        CreateMap<Output, OutputDto>();
        CreateMap<OutputDto, Output>();

        CreateMap<CanInput, CanInputDto>();
        CreateMap<CanInputDto, CanInput>();

        CreateMap<VirtualInput, VirtualInputDto>();
        CreateMap<VirtualInputDto, VirtualInput>();

        CreateMap<Flasher, FlasherDto>();
        CreateMap<FlasherDto, Flasher>();

        CreateMap<Counter, CounterDto>();
        CreateMap<CounterDto, Counter>();

        CreateMap<Condition, ConditionDto>();
        CreateMap<ConditionDto, Condition>();

        CreateMap<Wiper, WiperDto>();
        CreateMap<WiperDto, Wiper>();

        CreateMap<StarterDisable, StarterDisableDto>();
        CreateMap<StarterDisableDto, StarterDisable>();
    }
}
