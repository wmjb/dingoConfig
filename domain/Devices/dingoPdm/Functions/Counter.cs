using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Counter : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("incInput")] public VarMap IncInput { get; set; }
    [JsonPropertyName("decInput")] public VarMap DecInput { get; set; }
    [JsonPropertyName("resetInput")] public  VarMap ResetInput { get; set; }
    [JsonPropertyName("minCount")] public int  MinCount {get; set;}
    [JsonPropertyName("maxCount")] public int  MaxCount {get; set;}
    [JsonPropertyName("incEdge")] public InputEdge IncEdge {get; set;}
    [JsonPropertyName("decEdge")] public InputEdge DecEdge {get; set;}
    [JsonPropertyName("resetEdge")] public InputEdge ResetEdge {get; set;}
    [JsonPropertyName("wrapAround")] public bool WrapAround {get; set;}

    [JsonIgnore][Plotable(displayName:"State")] public int Value {get; set;}

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public Counter(int number, string name)
    {
        Number = number;
        Name = name;
        SettingsRxSignals = InitializeRxSignals();
        SettingsTxSignals = InitializeTxSignals();
    }

    private List<(DbcSignal Signal, Action<double> SetValue)> InitializeRxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Enabled", StartBit = 16, Length = 1 },
                val => Enabled = val != 0),

            (new DbcSignal { Name = "WrapAround", StartBit = 17, Length = 1 },
                val => WrapAround = val != 0),

            (new DbcSignal { Name = "IncInput", StartBit = 24, Length = 8 },
                val => IncInput = (VarMap)val),

            (new DbcSignal { Name = "DecInput", StartBit = 32, Length = 8 },
                val => DecInput = (VarMap)val),

            (new DbcSignal { Name = "ResetInput", StartBit = 40, Length = 8 },
                val => ResetInput = (VarMap)val),

            (new DbcSignal { Name = "MinCount", StartBit = 48, Length = 4 },
                val => MinCount = (int)val),

            (new DbcSignal { Name = "MaxCount", StartBit = 52, Length = 4 },
                val => MaxCount = (int)val),

            (new DbcSignal { Name = "IncEdge", StartBit = 56, Length = 2 },
                val => IncEdge = (InputEdge)val),

            (new DbcSignal { Name = "DecEdge", StartBit = 58, Length = 2 },
                val => DecEdge = (InputEdge)val),

            (new DbcSignal { Name = "ResetEdge", StartBit = 60, Length = 2 },
                val => ResetEdge = (InputEdge)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.Counter),

            (new DbcSignal { Name = "Index", StartBit = 8, Length = 8 },
                () => Number - 1),

            (new DbcSignal { Name = "Enabled", StartBit = 16, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "WrapAround", StartBit = 17, Length = 1 },
                () => WrapAround ? 1 : 0),

            (new DbcSignal { Name = "IncInput", StartBit = 24, Length = 8 },
                () => (int)IncInput),

            (new DbcSignal { Name = "DecInput", StartBit = 32, Length = 8 },
                () => (int)DecInput),

            (new DbcSignal { Name = "ResetInput", StartBit = 40, Length = 8 },
                () => (int)ResetInput),

            (new DbcSignal { Name = "MinCount", StartBit = 48, Length = 4 },
                () => MinCount),

            (new DbcSignal { Name = "MaxCount", StartBit = 52, Length = 4 },
                () => MaxCount),

            (new DbcSignal { Name = "IncEdge", StartBit = 56, Length = 2 },
                () => (int)IncEdge),

            (new DbcSignal { Name = "DecEdge", StartBit = 58, Length = 2 },
                () => (int)DecEdge),

            (new DbcSignal { Name = "ResetEdge", StartBit = 60, Length = 2 },
                () => (int)ResetEdge)
        ];
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Counter) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Counter, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Counter,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
            MsgDescription = $"Counter{Number}"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Counter) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Counter,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 8, Payload: Write()),
            MsgDescription = $"Counter{Number}"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Counter) return false;
        if (data.Length != 8) return false;

        foreach (var (signal, setValue) in SettingsRxSignals)
        {
            var value = ExtractSignal(data, signal);
            setValue(value);
        }

        return true;
    }

    private byte[] Write()
    {
        var data = new byte[8];

        foreach (var (signal, getValue) in SettingsTxSignals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }
}