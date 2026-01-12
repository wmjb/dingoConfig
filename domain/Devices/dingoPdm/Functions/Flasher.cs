using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Flasher : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("single")] public bool Single {get; set;}
    [JsonPropertyName("input")] public VarMap Input {get; set;}
    [JsonPropertyName("onTime")] public int OnTime {get; set;}
    [JsonPropertyName("offTime")] public int  OffTime {get; set;}

    [JsonIgnore][Plotable(displayName:"State")] public bool Value {get; set;}

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public Flasher(int number, string name)
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

            (new DbcSignal { Name = "Single", StartBit = 9, Length = 1 },
                val => Single = val != 0),

            (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                val => Input = (VarMap)val),

            (new DbcSignal { Name = "OnTime", StartBit = 32, Length = 8, Factor = 0.1 },
                val => OnTime = (int)val),

            (new DbcSignal { Name = "OffTime", StartBit = 40, Length = 8, Factor = 0.1 },
                val => OffTime = (int)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.Flashers),

            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "Single", StartBit = 9, Length = 1 },
                () => Single ? 1 : 0),

            (new DbcSignal { Name = "Index", StartBit = 12, Length = 4 },
                () => Number - 1),

            (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                () => (int)Input),

            (new DbcSignal { Name = "OnTime", StartBit = 32, Length = 8, Factor = 0.1 },
                () => OnTime),

            (new DbcSignal { Name = "OffTime", StartBit = 40, Length = 8, Factor = 0.1 },
                () => OffTime)
        ];
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return (data & 0xF0) >> 4;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Flashers) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Flashers, 0, 8);
        InsertSignalInt(data, Number - 1, 12, 4);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Flashers,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
            MsgDescription = $"Flasher{Number}"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Flashers) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Flashers,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 6, Payload: Write()),
            MsgDescription = $"Flasher{Number}"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Flashers) return false;
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