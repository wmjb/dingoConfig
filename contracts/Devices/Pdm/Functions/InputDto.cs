namespace contracts.Devices.Pdm.Functions;

public class InputDto
{
    // Config properties
    public int Number { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Invert { get; set; }
    public int Mode { get; set; }
    public int DebounceTime { get; set; }
    public int Pull { get; set; }

    // State properties
    public bool State { get; set; }
}
