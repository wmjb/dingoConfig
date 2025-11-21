using domain.Devices.dingoPdm.Enums;

namespace contracts.Devices.Pdm.Functions;

public class CounterDto
{
    // Config properties
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public VarMap IncInput { get; set; }
    public VarMap DecInput { get; set; }
    public VarMap ResetInput { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public InputEdge IncEdge { get; set; }
    public InputEdge DecEdge { get; set; }
    public InputEdge ResetEdge { get; set; }
    public bool WrapAround { get; set; }

    // State properties
    public int Value { get; set; }
}
