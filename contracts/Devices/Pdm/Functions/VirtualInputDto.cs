using domain.Devices.dingoPdm.Enums;
using domain.Enums;

namespace contracts.Devices.Pdm.Functions;

public class VirtualInputDto
{
    // Config properties
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Not0 { get; set; }
    public VarMap Var0 { get; set; }
    public Conditional Cond0 { get; set; }
    public bool Not1 { get; set; }
    public VarMap Var1 { get; set; }
    public Conditional Cond1 { get; set; }
    public bool Not2 { get; set; }
    public VarMap Var2 { get; set; }
    public Conditional Cond2 { get; set; }
    public InputMode Mode { get; set; }

    // State properties
    public bool Value { get; set; }
}
