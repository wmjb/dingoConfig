using domain.Devices.dingoPdm.Enums;

namespace contracts.Devices.Pdm.Functions;

public class ConditionDto
{
    // Config properties
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public VarMap Input { get; set; }
    public Operator Operator { get; set; }
    public int Arg { get; set; }

    // State properties
    public int Value { get; set; }
}
