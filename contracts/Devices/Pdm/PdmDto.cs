using contracts.Devices.Pdm.Functions;
using domain.Enums;

namespace contracts.Devices.Pdm;

/// <summary>
/// Combined DTO for PDM device - includes both state and configuration properties
/// </summary>
public class PdmDto : DeviceDto
{
    // Device-level state properties
    public DeviceState State { get; set; }
    public double TotalCurrent { get; set; }
    public double BatteryVoltage { get; set; }
    public double BoardTempC { get; set; }
    public double BoardTempF { get; set; }

    // Device-level config properties
    public bool SleepEnabled { get; set; }
    public bool CanFiltersEnabled { get; set; }

    // Function state and config (combined)
    public List<CanInputDto> CanInputs { get; set; } = [];
    public List<ConditionDto> Conditions { get; set; } = [];
    public List<CounterDto> Counters { get; set; } = [];
    public List<FlasherDto> Flashers { get; set; } = [];
    public List<InputDto> Inputs { get; set; } = [];
    public List<OutputDto> Outputs { get; set; } = [];
    public StarterDisableDto? StarterDisable { get; set; }
    public List<VirtualInputDto> VirtualInputs { get; set; } = [];
    public WiperDto? Wipers { get; set; }
}
