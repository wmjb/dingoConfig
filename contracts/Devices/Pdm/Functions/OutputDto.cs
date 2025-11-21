using domain.Devices.dingoPdm.Enums;

namespace contracts.Devices.Pdm.Functions;

public class OutputDto
{
    // Config properties
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CurrentLimit { get; set; }
    public int ResetCountLimit { get; set; }
    public ResetMode ResetMode { get; set; }
    public double ResetTime { get; set; }
    public int InrushCurrentLimit { get; set; }
    public double InrushTime { get; set; }
    public VarMap Input { get; set; }
    public bool SoftStartEnabled { get; set; }
    public bool VariableDutyCycle { get; set; }
    public VarMap DutyCycleInput { get; set; }
    public int FixedDutyCycle { get; set; }
    public int Frequency { get; set; }
    public int SoftStartRampTime { get; set; }
    public int DutyCycleDenominator { get; set; }

    // State properties
    public double Current { get; set; }
    public OutState State { get; set; }
    public int ResetCount { get; set; }
    public double CurrentDutyCycle { get; set; }
    public double CalculatedPower { get; set; }
}
