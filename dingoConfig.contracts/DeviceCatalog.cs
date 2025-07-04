namespace dingoConfig.Contracts;

public class DeviceCatalog
{
    public string DeviceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SettingsMessage> Settings { get; set; } = new();
    public List<CyclicMessage> CyclicData { get; set; } = new();
}

public class SettingsMessage
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IdOffset { get; set; } = string.Empty;
    public SignalDefinition Prefix { get; set; } = new();
    public string Type { get; set; } = string.Empty; // "uint16", "int32", "bool", etc.
    public SignalDefinition Signal { get; set; } = new();
}

public class CyclicMessage
{
    public string Name { get; set; } = string.Empty;
    public string IdOffset { get; set; } = string.Empty;
    public List<SignalDefinition> Signals { get; set; } = new();
}

public class SignalDefinition
{
    public string Name { get; set; } = string.Empty;
    public int StartBit { get; set; }
    public int Length { get; set; }
    public bool IsSigned { get; set; } = false;
    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public string Unit { get; set; } = string.Empty;
    public string ByteOrder { get; set; } = "littleEndian"; // "littleEndian", "bigEndian"
    
    //Optional
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
}