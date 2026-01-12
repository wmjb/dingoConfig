using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Output : IDeviceFunction
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("number")] public int Number { get; }
    
    [JsonPropertyName("currentLimit")]
    [Range(0.0, 18.0)]
    public double CurrentLimit { get; set; }
    
    [JsonPropertyName("resetCountLimit")] public int ResetCountLimit { get; set; }
    [JsonPropertyName("resetMode")] public ResetMode ResetMode { get; set; }
    [JsonPropertyName("resetTime")] public int ResetTime { get; set; }  //seconds
    [JsonPropertyName("inrushCurrentLimit")] public double InrushCurrentLimit { get; set; }
    [JsonPropertyName("inrushTime")] public int InrushTime { get; set; }
    [JsonPropertyName("input")] public VarMap Input { get; set; }
    [JsonPropertyName("pwmEnabled")] public bool PwmEnabled { get; set; }
    [JsonPropertyName("softStartEnabled")] public bool SoftStartEnabled { get; set; }
    [JsonPropertyName("variableDutyCycle")] public bool VariableDutyCycle { get; set; }
    [JsonPropertyName("dutyCycleInput")] public VarMap DutyCycleInput { get; set; }
    [JsonPropertyName("fixedDutyCycle")] public int FixedDutyCycle { get; set; }
    [JsonPropertyName("frequency")] public int Frequency { get; set; }
    [JsonPropertyName("softStartRampTime")] public int SoftStartRampTime { get; set; }
    [JsonPropertyName("dutyCycleDenominator")] public int DutyCycleDenominator { get; set; }
    
    [JsonIgnore][Plotable(displayName:"Current", unit:"A")] public double Current { get; set; }
    [JsonIgnore][Plotable(displayName:"State")] public OutState State { get; set; }
    [JsonIgnore][Plotable(displayName:"ResetCount")] public int ResetCount { get; set; }
    [JsonIgnore][Plotable(displayName:"DutyCycle", unit:"%")] public double CurrentDutyCycle
    {
        get;
        set
        {
            field = value;
            CalculatedCurrent = (field / 100.0) * Current;
        }
    }
    [JsonIgnore][Plotable(displayName:"CalcCurrent", unit:"A")] public double CalculatedCurrent { get; private set; }

    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>> SettingsRxSignals { get; }
    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> SettingsTxSignals { get; }

    [JsonConstructor]
    public Output(int number, string name)
    {
        Number = number;
        Name = name;
        SettingsRxSignals = InitializeRxSignals();
        SettingsTxSignals = InitializeTxSignals();
    }

    private Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>> InitializeRxSignals()
    {
        return new Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>>
        {
            [MessagePrefix.Outputs] =
            [
                (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                    val => Enabled = val != 0),

                (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                    val => Input = (VarMap)val),

                (new DbcSignal { Name = "CurrentLimit", StartBit = 24, Length = 8 },
                    val => CurrentLimit = val),

                (new DbcSignal { Name = "ResetMode", StartBit = 32, Length = 4 },
                    val => ResetMode = (ResetMode)val),

                (new DbcSignal { Name = "ResetCountLimit", StartBit = 36, Length = 4 },
                    val => ResetCountLimit = (int)val),

                (new DbcSignal { Name = "ResetTime", StartBit = 40, Length = 8, Factor = 0.1 },
                    val => ResetTime = (int)val),

                (new DbcSignal { Name = "InrushCurrentLimit", StartBit = 48, Length = 8 },
                    val => InrushCurrentLimit = val),

                (new DbcSignal { Name = "InrushTime", StartBit = 56, Length = 8, Factor = 0.1 },
                    val => InrushTime = (int)val)
            ],
            [MessagePrefix.OutputsPwm] =
            [
                (new DbcSignal { Name = "PwmEnabled", StartBit = 8, Length = 1 },
                    val => PwmEnabled = val != 0),

                (new DbcSignal { Name = "SoftStartEnabled", StartBit = 9, Length = 1 },
                    val => SoftStartEnabled = val != 0),

                (new DbcSignal { Name = "VariableDutyCycle", StartBit = 10, Length = 1 },
                    val => VariableDutyCycle = val != 0),

                (new DbcSignal { Name = "DutyCycleInput", StartBit = 16, Length = 8 },
                    val => DutyCycleInput = (VarMap)val),

                (new DbcSignal { Name = "FixedDutyCycle", StartBit = 33, Length = 7 },
                    val => FixedDutyCycle = (int)val),

                (new DbcSignal { Name = "SoftStartRampTime", StartBit = 40, Length = 16, ByteOrder = ByteOrder.BigEndian },
                    val => SoftStartRampTime = (int)val),

                (new DbcSignal { Name = "DutyCycleDenominator", StartBit = 56, Length = 8 },
                    val => DutyCycleDenominator = (int)val)
            ]
        };
    }

    private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> InitializeTxSignals()
    {
        return new Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>>
        {
            [MessagePrefix.Outputs] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.Outputs),

                (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                    () => Enabled ? 1 : 0),

                (new DbcSignal { Name = "Index", StartBit = 12, Length = 4 },
                    () => Number - 1),

                (new DbcSignal { Name = "Input", StartBit = 16, Length = 8 },
                    () => (int)Input),

                (new DbcSignal { Name = "CurrentLimit", StartBit = 24, Length = 8 },
                    () => CurrentLimit),

                (new DbcSignal { Name = "ResetMode", StartBit = 32, Length = 4 },
                    () => (int)ResetMode),

                (new DbcSignal { Name = "ResetCountLimit", StartBit = 36, Length = 4 },
                    () => ResetCountLimit),

                (new DbcSignal { Name = "ResetTime", StartBit = 40, Length = 8, Factor = 0.1 },
                    () => ResetTime),

                (new DbcSignal { Name = "InrushCurrentLimit", StartBit = 48, Length = 8 },
                    () => InrushCurrentLimit),

                (new DbcSignal { Name = "InrushTime", StartBit = 56, Length = 8, Factor = 0.1 },
                    () => InrushTime)
            ],
            [MessagePrefix.OutputsPwm] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.OutputsPwm),

                (new DbcSignal { Name = "PwmEnabled", StartBit = 8, Length = 1 },
                    () => PwmEnabled ? 1 : 0),

                (new DbcSignal { Name = "SoftStartEnabled", StartBit = 9, Length = 1 },
                    () => SoftStartEnabled ? 1 : 0),

                (new DbcSignal { Name = "VariableDutyCycle", StartBit = 10, Length = 1 },
                    () => VariableDutyCycle ? 1 : 0),

                (new DbcSignal { Name = "Index", StartBit = 12, Length = 4 },
                    () => Number - 1),

                (new DbcSignal { Name = "DutyCycleInput", StartBit = 16, Length = 8 },
                    () => (int)DutyCycleInput),

                (new DbcSignal { Name = "FrequencyUpper", StartBit = 24, Length = 8 },
                    () => Frequency >> 1),

                (new DbcSignal { Name = "FrequencyLsb", StartBit = 32, Length = 1 },
                    () => Frequency & 0x01),

                (new DbcSignal { Name = "FixedDutyCycle", StartBit = 33, Length = 7 },
                    () => FixedDutyCycle),

                (new DbcSignal { Name = "SoftStartRampTime", StartBit = 40, Length = 16, ByteOrder = ByteOrder.BigEndian },
                    () => SoftStartRampTime),

                (new DbcSignal { Name = "DutyCycleDenominator", StartBit = 56, Length = 8 },
                    () => DutyCycleDenominator)
            ]
        };
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return (data & 0xF0) >> 4;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        switch (prefix)
        {
            case MessagePrefix.Outputs:
            {
                var data = new byte[8];
                InsertSignalInt(data, (long)MessagePrefix.Outputs, 0, 8);
                InsertSignalInt(data, Number - 1, 12, 4);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.Outputs,
                    Index = Number - 1,
                    Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
                    MsgDescription = $"Output{Number}"
                };
            }
            case MessagePrefix.OutputsPwm:
            {
                var data = new byte[8];
                InsertSignalInt(data, (long)MessagePrefix.OutputsPwm, 0, 8);
                InsertSignalInt(data, Number - 1, 12, 4);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.OutputsPwm,
                    Index = Number - 1,
                    Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
                    MsgDescription = $"OutputPwm{Number}"
                };
            }
            default:
                return null;
        }
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        return prefix switch
        {
            MessagePrefix.Outputs => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.Outputs,
                Index = Number - 1,
                Frame = new CanFrame(Id: baseId - 1, Len: 8, Payload: Write()),
                MsgDescription = $"Output{Number}"
            },
            MessagePrefix.OutputsPwm => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.OutputsPwm,
                Index = Number - 1,
                Frame = new CanFrame(Id: baseId - 1, Len: 8, Payload: WritePwm()),
                MsgDescription = $"OutputPwm{Number}"
            },
            _ => null
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (data.Length != 8)
            return false;

        if (!SettingsRxSignals.TryGetValue(prefix, out var signals))
            return false;

        // Parse all signals using dictionary
        foreach (var (signal, setValue) in signals)
        {
            var value = ExtractSignal(data, signal);
            setValue(value);
        }

        // Special handling for OutputsPwm frequency (9-bit split encoding)
        if (prefix != MessagePrefix.OutputsPwm) return true;
        var freqUpper = (int)ExtractSignalInt(data, 24, 8);
        var freqLsb = (int)ExtractSignalInt(data, 32, 1);
        Frequency = (freqUpper << 1) | freqLsb;

        return true;
    }

    private byte[] Write()
    {
        var data = new byte[8];
        var signals = SettingsTxSignals[MessagePrefix.Outputs];

        foreach (var (signal, getValue) in signals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }

    private byte[] WritePwm()
    {
        var data = new byte[8];
        var signals = SettingsTxSignals[MessagePrefix.OutputsPwm];

        foreach (var (signal, getValue) in signals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }
}