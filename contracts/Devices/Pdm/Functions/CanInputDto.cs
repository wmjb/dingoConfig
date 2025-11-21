using domain.Devices.dingoPdm.Enums;
using domain.Enums;

namespace contracts.Devices.Pdm.Functions;

public class CanInputDto
{
    // Config properties
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool TimeoutEnabled { get; set; }
    public int Timeout { get; set; }
    public bool Ide { get; set; }
    public int StartingByte { get; set; }
    public int Dlc { get; set; }
    public Operator Operator { get; set; }
    public int OnVal { get; set; }
    public InputMode Mode { get; set; }
    public int Id { get; set; }

    // State properties
    public bool Output { get; set; }
    public int Value { get; set; }
}
