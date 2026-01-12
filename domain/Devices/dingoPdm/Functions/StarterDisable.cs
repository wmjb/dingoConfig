using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class StarterDisable : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;}
    [JsonIgnore] public int Number => 1;
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("input")] public VarMap Input {get; set;}
    [JsonPropertyName("outputsDisabled")] public List<bool> OutputsDisabled {get; set;}

    [JsonIgnore] private List<(DbcSignal Signal, Action<double> SetValue)> SettingsRxSignals { get; }
    [JsonIgnore] private List<(DbcSignal Signal, Func<double> GetValue)> SettingsTxSignals { get; }

    [JsonConstructor]
    public StarterDisable(string name, int outputCount)
    {
        Name = name;
        OutputsDisabled = [..new bool[outputCount]];
        SettingsRxSignals = InitializeRxSignals();
        SettingsTxSignals = InitializeTxSignals();
    }

    private List<(DbcSignal Signal, Action<double> SetValue)> InitializeRxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                val => Enabled = val != 0),

            (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                val => Input = (VarMap)val)
        ];
    }

    private List<(DbcSignal Signal, Func<double> GetValue)> InitializeTxSignals()
    {
        return
        [
            (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                () => (int)MessagePrefix.StarterDisable),

            (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                () => Enabled ? 1 : 0),

            (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                () => (int)Input)
        ];
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.StarterDisable, 0, 8);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.StarterDisable,
            Index = 0,
            Frame = new CanFrame(Id: baseId - 1, Len: 1, Payload: data),
            MsgDescription = "StarterDisable"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.StarterDisable,
            Index = 0,
            Frame = new CanFrame(Id: baseId - 1, Len: 4, Payload: Write()),
            MsgDescription = "StarterDisable"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return false;
        if (data.Length != 4) return false;

        foreach (var (signal, setValue) in SettingsRxSignals)
        {
            var value = ExtractSignal(data, signal);
            setValue(value);
        }

        // Handle OutputsDisabled array (variable length, not in signal definitions)
        for (var i = 0; i < OutputsDisabled.Count; i++)
        {
            OutputsDisabled[i] = ExtractSignalInt(data, 24 + i, 1) == 1;
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

        // Handle OutputsDisabled array (variable length, not in signal definitions)
        for (var i = 0; i < OutputsDisabled.Count; i++)
        {
            InsertBool(data, OutputsDisabled[i], 24 + i);
        }

        return data;
    }
}