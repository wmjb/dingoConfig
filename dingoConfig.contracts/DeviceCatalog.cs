namespace dingoConfig.Contracts;

public class DeviceCatalog
{
    public string DeviceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CommunicationInfo Communication { get; set; } = new();
    public List<DeviceParameter> Parameters { get; set; } = new();
    public List<CyclicDataDefinition> CyclicData { get; set; } = new();
}

public class CommunicationInfo
{
    public string Type { get; set; } = string.Empty; // "CAN", "USB_CDC", etc.
    public string BaseId { get; set; } = string.Empty;
    public bool ExtendedFrames { get; set; }
}

public class DeviceParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "uint16", "int32", "bool", etc.
    public string Address { get; set; } = string.Empty;
    public int SubIndex { get; set; }
    public string ByteOrder { get; set; } = "littleEndian";
    public ParameterScaling? Scaling { get; set; }
    public ParameterLimits? Limits { get; set; }
    public object? DefaultValue { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

public class ParameterScaling
{
    public double Factor { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public string Units { get; set; } = string.Empty;
}

public class ParameterLimits
{
    public object? Min { get; set; }
    public object? Max { get; set; }
}

public class CyclicDataDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int Interval { get; set; } // milliseconds
    public List<SignalDefinition> Signals { get; set; } = new();
}

public class SignalDefinition
{
    public string Name { get; set; } = string.Empty;
    public int StartBit { get; set; }
    public int Length { get; set; }
    public string ByteOrder { get; set; } = "littleEndian";
    public string Type { get; set; } = string.Empty;
    public ParameterScaling? Scaling { get; set; }
}