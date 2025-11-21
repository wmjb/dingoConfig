using domain.Devices.dingoPdm.Enums;

namespace contracts.Devices.Pdm.Functions;

public class WiperDto
{
    // Config properties
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; } = 1; // Always 1 for singleton function
    public bool Enabled { get; set; }
    public WiperMode Mode { get; set; }
    public VarMap SlowInput { get; set; }
    public VarMap FastInput { get; set; }
    public VarMap InterInput { get; set; }
    public VarMap OnInput { get; set; }
    public VarMap SpeedInput { get; set; }
    public VarMap ParkInput { get; set; }
    public bool ParkStopLevel { get; set; }
    public VarMap SwipeInput { get; set; }
    public VarMap WashInput { get; set; }
    public int WashWipeCycles { get; set; }
    public WiperSpeed[] SpeedMap { get; set; } = new WiperSpeed[8];
    public double[] IntermitTime { get; set; } = new double[6];

    // State properties
    public bool SlowState { get; set; }
    public bool FastState { get; set; }
    public WiperState State { get; set; }
    public WiperSpeed Speed { get; set; }
}
