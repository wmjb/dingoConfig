using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Input : IDeviceFunction
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("number")] public int Number { get; }
    [JsonPropertyName("invert")] public bool Invert { get; set; }
    [JsonPropertyName("mode")] public InputMode Mode { get; set; }
    [JsonPropertyName("debounceTime")] public int DebounceTime { get; set; }
    [JsonPropertyName("pull")] public InputPull Pull { get; set; }

    [JsonIgnore][Plotable(displayName:"State")] public bool State { get; set; }

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public Input(int number, string name)
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

            (new DbcSignal { Name = "Mode", StartBit = 9, Length = 2 },
                val => Mode = (InputMode)val),

            (new DbcSignal { Name = "Invert", StartBit = 11, Length = 1 },
                val => Invert = val != 0),

            (new DbcSignal { Name = "DebounceTime", StartBit = 16, Length = 8, Factor = 10.0 },
                val => DebounceTime = (int)val),

            (new DbcSignal { Name = "Pull", StartBit = 24, Length = 2 },
                val => Pull = (InputPull)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.Inputs),

            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "Mode", StartBit = 9, Length = 2 },
                () => (int)Mode),

            (new DbcSignal { Name = "Invert", StartBit = 11, Length = 1 },
                () => Invert ? 1 : 0),

            (new DbcSignal { Name = "Index", StartBit = 12, Length = 4 },
                () => Number - 1),

            (new DbcSignal { Name = "DebounceTime", StartBit = 16, Length = 8, Factor = 10.0 },
                () => DebounceTime),

            (new DbcSignal { Name = "Pull", StartBit = 24, Length = 2 },
                () => (int)Pull)
        ];
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return (data & 0xF0) >> 4;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Inputs) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Inputs, 0, 8);
        InsertSignalInt(data, Number - 1, 12, 4);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Inputs,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
            MsgDescription = $"Input{Number}"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Inputs) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.Inputs,
            Index = Number - 1,
            Frame = new CanFrame(Id: baseId - 1, Len: 4, Payload: Write()),
            MsgDescription = $"Input{Number}"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.Inputs) return false;
        if (data.Length != 4) return false;

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