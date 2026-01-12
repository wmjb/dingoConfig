using System.Text.Json.Serialization;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdm.Enums;
using Microsoft.Extensions.Logging;

namespace domain.Devices.dingoPdmMax;

public class PdmMaxDevice(string name, int baseId) : PdmDevice(name, baseId)
{
    [JsonIgnore] protected override int MinMajorVersion => 0;
    [JsonIgnore] protected override int MinMinorVersion => 4;
    [JsonIgnore] protected override int MinBuildVersion => 7;

    [JsonIgnore] protected override int NumOutputs => 4;

    [JsonIgnore] protected override int PdmType => 1; //0=dingoPDM, 1=dingoPDM-Max

    [JsonIgnore] public override string Type => "dingoPDM-Max";
}