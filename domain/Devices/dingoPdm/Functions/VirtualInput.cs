using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class VirtualInput : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get; }
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("not0")] public bool Not0 {get; set;}
    [JsonPropertyName("var0")] public VarMap Var0 { get; set; }
    [JsonPropertyName("cond0")] public Conditional Cond0 { get; set; }
    [JsonPropertyName("not1")] public bool Not1 {get; set;}
    [JsonPropertyName("var1")] public VarMap Var1 { get; set; }
    [JsonPropertyName("cond1")] public Conditional Cond1 { get; set; }
    [JsonPropertyName("not2")] public bool Not2 {get; set;}
    [JsonPropertyName("var2")] public VarMap Var2 { get; set; }
    [JsonPropertyName("cond2")] public Conditional Cond2 { get; set; }
    [JsonPropertyName("mode")] public InputMode Mode {get; set;}

    [JsonIgnore][Plotable(displayName:"State")] public bool Value {get; set;}

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public VirtualInput(int number, string name)
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
            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                val => Enabled = val != 0),

            (new DbcSignal { Name = "Not0", StartBit = 9, Length = 1 },
                val => Not0 = val != 0),

            (new DbcSignal { Name = "Not1", StartBit = 10, Length = 1 },
                val => Not1 = val != 0),

            (new DbcSignal { Name = "Not2", StartBit = 11, Length = 1 },
                val => Not2 = val != 0),

            (new DbcSignal { Name = "Var0", StartBit = 24, Length = 8 },
                val => Var0 = (VarMap)val),

            (new DbcSignal { Name = "Var1", StartBit = 32, Length = 8 },
                val => Var1 = (VarMap)val),

            (new DbcSignal { Name = "Var2", StartBit = 40, Length = 8 },
                val => Var2 = (VarMap)val),

            (new DbcSignal { Name = "Cond0", StartBit = 48, Length = 2 },
                val => Cond0 = (Conditional)val),

            (new DbcSignal { Name = "Cond1", StartBit = 50, Length = 2 },
                val => Cond1 = (Conditional)val),

            (new DbcSignal { Name = "Mode", StartBit = 54, Length = 2 },
                val => Mode = (InputMode)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.VirtualInputs),

            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "Not0", StartBit = 9, Length = 1 },
                () => Not0 ? 1 : 0),

            (new DbcSignal { Name = "Not1", StartBit = 10, Length = 1 },
                () => Not1 ? 1 : 0),

            (new DbcSignal { Name = "Not2", StartBit = 11, Length = 1 },
                () => Not2 ? 1 : 0),

            (new DbcSignal { Name = "Index", StartBit = 16, Length = 8 },
                () => Number - 1),

            (new DbcSignal { Name = "Var0", StartBit = 24, Length = 8 },
                () => (int)Var0),

            (new DbcSignal { Name = "Var1", StartBit = 32, Length = 8 },
                () => (int)Var1),

            (new DbcSignal { Name = "Var2", StartBit = 40, Length = 8 },
                () => (int)Var2),

            (new DbcSignal { Name = "Cond0", StartBit = 48, Length = 2 },
                () => (int)Cond0),

            (new DbcSignal { Name = "Cond1", StartBit = 50, Length = 2 },
                () => (int)Cond1),

            (new DbcSignal { Name = "Mode", StartBit = 54, Length = 2 },
                () => (int)Mode)
        ];
    }
    
    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.VirtualInputs) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.VirtualInputs, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.VirtualInputs,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
            MsgDescription = $"VirtualInput{Number}"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.VirtualInputs) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.VirtualInputs,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 7, Payload: Write()),
            MsgDescription = $"VirtualInput{Number}"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.VirtualInputs) return false;
        if (data.Length != 7) return false;

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