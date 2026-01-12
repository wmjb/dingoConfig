using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Condition : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set; }
    [JsonPropertyName("input")] public VarMap Input { get; set; }
    [JsonPropertyName("operator")] public Operator Operator {get; set;}
    [JsonPropertyName("arg")] public int Arg {get; set;}

    [JsonIgnore][Plotable(displayName:"State")] public int Value {get; set;}

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public Condition(int number, string name)
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

            (new DbcSignal { Name = "Operator", StartBit = 20, Length = 4 },
                val => Operator = (Operator)val),

            (new DbcSignal { Name = "Input", StartBit = 24, Length = 8 },
                val => Input = (VarMap)val),

            (new DbcSignal { Name = "Arg", StartBit = 32, Length = 16, ByteOrder = ByteOrder.BigEndian },
                val => Arg = (int)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.Conditions),

            (new DbcSignal { Name = "Index", StartBit = 8, Length = 8 },
                () => Number - 1),

            (new DbcSignal { Name = "Enabled", StartBit = 16, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "Operator", StartBit = 20, Length = 4 },
                () => (int)Operator),

            (new DbcSignal { Name = "Input", StartBit = 24, Length = 8 },
                () => (int)Input),

            (new DbcSignal { Name = "Arg", StartBit = 32, Length = 16, ByteOrder = ByteOrder.BigEndian },
                () => Arg)
        ];
    }
    
    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Conditions) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Conditions, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Conditions,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
            MsgDescription = $"Condition{Number}"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Conditions) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Conditions,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 6, Payload: Write()),
            MsgDescription = $"Condition{Number}"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Conditions) return false;
        if (data.Length != 6) return false;

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