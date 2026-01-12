using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Wiper : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;}
    [JsonIgnore] public int Number => 1; // Singleton function
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("mode")] public WiperMode Mode { get; set; }
    [JsonPropertyName("slowInput")] public VarMap SlowInput { get; set; }
    [JsonPropertyName("fastInput")] public VarMap FastInput { get; set; }
    [JsonPropertyName("interInput")] public VarMap InterInput { get; set; }
    [JsonPropertyName("onInput")] public VarMap OnInput { get; set; }
    [JsonPropertyName("speedInput")] public VarMap SpeedInput { get; set; }
    [JsonPropertyName("parkInput")] public VarMap ParkInput { get; set; }
    [JsonPropertyName("parkStopLevel")] public bool ParkStopLevel { get; set; }
    [JsonPropertyName("swipeInput")] public VarMap SwipeInput { get; set; }
    [JsonPropertyName("washInput")] public VarMap WashInput { get; set; }
    [JsonPropertyName("washWipeCycles")] public int WashWipeCycles { get; set; }
    [JsonPropertyName("speedMap")] public WiperSpeed[] SpeedMap { get; set; } = new WiperSpeed[8];
    [JsonPropertyName("intermitTime")] public double[] IntermitTime { get; set; } = new double[6];

    [JsonIgnore][Plotable(displayName:"SlowState")] public bool SlowState { get; set; }
    [JsonIgnore][Plotable(displayName:"FastState")] public bool FastState { get; set; }
    [JsonIgnore][Plotable(displayName:"State")] public WiperState State { get; set; }
    [JsonIgnore][Plotable(displayName:"Speed")] public WiperSpeed Speed { get; set; }

    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>> SettingsRxSignals { get; }
    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> SettingsTxSignals { get; }

    [JsonConstructor]
    public Wiper(string name)
    {
        Name = name;
        SettingsRxSignals = InitializeRxSignals();
        SettingsTxSignals = InitializeTxSignals();
    }

    private Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>> InitializeRxSignals()
    {
        return new Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>>
        {
            [MessagePrefix.Wiper] =
            [
                (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                    val => Enabled = val != 0),

                (new DbcSignal { Name = "Mode", StartBit = 9, Length = 2 },
                    val => Mode = (WiperMode)val),

                (new DbcSignal { Name = "ParkStopLevel", StartBit = 11, Length = 1 },
                    val => ParkStopLevel = val != 0),

                (new DbcSignal { Name = "WashWipeCycles", StartBit = 12, Length = 4 },
                    val => WashWipeCycles = (int)val),

                (new DbcSignal { Name = "SlowInput", StartBit = 16, Length = 8 },
                    val => SlowInput = (VarMap)val),

                (new DbcSignal { Name = "FastInput", StartBit = 24, Length = 8 },
                    val => FastInput = (VarMap)val),

                (new DbcSignal { Name = "InterInput", StartBit = 32, Length = 8 },
                    val => InterInput = (VarMap)val),

                (new DbcSignal { Name = "OnInput", StartBit = 40, Length = 8 },
                    val => OnInput = (VarMap)val),

                (new DbcSignal { Name = "ParkInput", StartBit = 48, Length = 8 },
                    val => ParkInput = (VarMap)val),

                (new DbcSignal { Name = "WashInput", StartBit = 56, Length = 8 },
                    val => WashInput = (VarMap)val)
            ],
            [MessagePrefix.WiperSpeed] =
            [
                (new DbcSignal { Name = "SwipeInput", StartBit = 8, Length = 8 },
                    val => SwipeInput = (VarMap)val),

                (new DbcSignal { Name = "SpeedInput", StartBit = 16, Length = 8 },
                    val => SpeedInput = (VarMap)val),

                (new DbcSignal { Name = "SpeedMap0", StartBit = 24, Length = 4 },
                    val => SpeedMap[0] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap1", StartBit = 28, Length = 4 },
                    val => SpeedMap[1] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap2", StartBit = 32, Length = 4 },
                    val => SpeedMap[2] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap3", StartBit = 36, Length = 4 },
                    val => SpeedMap[3] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap4", StartBit = 40, Length = 4 },
                    val => SpeedMap[4] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap5", StartBit = 44, Length = 4 },
                    val => SpeedMap[5] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap6", StartBit = 48, Length = 4 },
                    val => SpeedMap[6] = (WiperSpeed)val),

                (new DbcSignal { Name = "SpeedMap7", StartBit = 52, Length = 4 },
                    val => SpeedMap[7] = (WiperSpeed)val)
            ],
            [MessagePrefix.WiperDelays] =
            [
                (new DbcSignal { Name = "IntermitTime0", StartBit = 8, Length = 8, Factor = 0.1 },
                    val => IntermitTime[0] = val),

                (new DbcSignal { Name = "IntermitTime1", StartBit = 16, Length = 8, Factor = 0.1 },
                    val => IntermitTime[1] = val),

                (new DbcSignal { Name = "IntermitTime2", StartBit = 24, Length = 8, Factor = 0.1 },
                    val => IntermitTime[2] = val),

                (new DbcSignal { Name = "IntermitTime3", StartBit = 32, Length = 8, Factor = 0.1 },
                    val => IntermitTime[3] = val),

                (new DbcSignal { Name = "IntermitTime4", StartBit = 40, Length = 8, Factor = 0.1 },
                    val => IntermitTime[4] = val),

                (new DbcSignal { Name = "IntermitTime5", StartBit = 48, Length = 8, Factor = 0.1 },
                    val => IntermitTime[5] = val)
            ]
        };
    }

    private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> InitializeTxSignals()
    {
        return new Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>>
        {
            [MessagePrefix.Wiper] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.Wiper),

                (new DbcSignal { Name = "Enabled", StartBit = 8, Length = 1 },
                    () => Enabled ? 1 : 0),

                (new DbcSignal { Name = "Mode", StartBit = 9, Length = 2 },
                    () => (int)Mode),

                (new DbcSignal { Name = "ParkStopLevel", StartBit = 11, Length = 1 },
                    () => ParkStopLevel ? 1 : 0),

                (new DbcSignal { Name = "WashWipeCycles", StartBit = 12, Length = 4 },
                    () => WashWipeCycles),

                (new DbcSignal { Name = "SlowInput", StartBit = 16, Length = 8 },
                    () => (int)SlowInput),

                (new DbcSignal { Name = "FastInput", StartBit = 24, Length = 8 },
                    () => (int)FastInput),

                (new DbcSignal { Name = "InterInput", StartBit = 32, Length = 8 },
                    () => (int)InterInput),

                (new DbcSignal { Name = "OnInput", StartBit = 40, Length = 8 },
                    () => (int)OnInput),

                (new DbcSignal { Name = "ParkInput", StartBit = 48, Length = 8 },
                    () => (int)ParkInput),

                (new DbcSignal { Name = "WashInput", StartBit = 56, Length = 8 },
                    () => (int)WashInput)
            ],
            [MessagePrefix.WiperSpeed] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.WiperSpeed),

                (new DbcSignal { Name = "SwipeInput", StartBit = 8, Length = 8 },
                    () => (int)SwipeInput),

                (new DbcSignal { Name = "SpeedInput", StartBit = 16, Length = 8 },
                    () => (int)SpeedInput),

                (new DbcSignal { Name = "SpeedMap0", StartBit = 24, Length = 4 },
                    () => (int)SpeedMap[0]),

                (new DbcSignal { Name = "SpeedMap1", StartBit = 28, Length = 4 },
                    () => (int)SpeedMap[1]),

                (new DbcSignal { Name = "SpeedMap2", StartBit = 32, Length = 4 },
                    () => (int)SpeedMap[2]),

                (new DbcSignal { Name = "SpeedMap3", StartBit = 36, Length = 4 },
                    () => (int)SpeedMap[3]),

                (new DbcSignal { Name = "SpeedMap4", StartBit = 40, Length = 4 },
                    () => (int)SpeedMap[4]),

                (new DbcSignal { Name = "SpeedMap5", StartBit = 44, Length = 4 },
                    () => (int)SpeedMap[5]),

                (new DbcSignal { Name = "SpeedMap6", StartBit = 48, Length = 4 },
                    () => (int)SpeedMap[6]),

                (new DbcSignal { Name = "SpeedMap7", StartBit = 52, Length = 4 },
                    () => (int)SpeedMap[7])
            ],
            [MessagePrefix.WiperDelays] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.WiperDelays),

                (new DbcSignal { Name = "IntermitTime0", StartBit = 8, Length = 8, Factor = 0.1 },
                    () => IntermitTime[0]),

                (new DbcSignal { Name = "IntermitTime1", StartBit = 16, Length = 8, Factor = 0.1 },
                    () => IntermitTime[1]),

                (new DbcSignal { Name = "IntermitTime2", StartBit = 24, Length = 8, Factor = 0.1 },
                    () => IntermitTime[2]),

                (new DbcSignal { Name = "IntermitTime3", StartBit = 32, Length = 8, Factor = 0.1 },
                    () => IntermitTime[3]),

                (new DbcSignal { Name = "IntermitTime4", StartBit = 40, Length = 8, Factor = 0.1 },
                    () => IntermitTime[4]),

                (new DbcSignal { Name = "IntermitTime5", StartBit = 48, Length = 8, Factor = 0.1 },
                    () => IntermitTime[5])
            ]
        };
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        var data = new byte[8];
        switch (prefix)
        {
            case MessagePrefix.Wiper:
                InsertSignalInt(data, (long)MessagePrefix.Wiper, 0, 8);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.Wiper,
                    Index = 0,
                    Frame = new CanFrame(Id: baseId - 1, Len: 1, Payload: data),
                    MsgDescription = "Wiper"
                };
            
            case MessagePrefix.WiperSpeed:
                InsertSignalInt(data, (long)MessagePrefix.WiperSpeed, 0, 8);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.WiperSpeed,
                    Index = 0,
                    Frame = new CanFrame(Id: baseId - 1, Len: 1, Payload: data),
                    MsgDescription = "WiperSpeed"
                };
            
            case MessagePrefix.WiperDelays:
                InsertSignalInt(data, (long)MessagePrefix.WiperDelays, 0, 8);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.WiperDelays,
                    Index = 0,
                    Frame = new CanFrame(Id: baseId - 1, Len: 1, Payload: data),
                    MsgDescription = "WiperDelay"
                };
            
            default:
                return null;
        }
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        return prefix switch
        {
            MessagePrefix.Wiper => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.Wiper,
                Index = 0,
                Frame = new CanFrame(Id: baseId - 1, Len: 8, Payload: Write()),
                MsgDescription = "Wiper"
            },
            MessagePrefix.WiperSpeed => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.WiperSpeed,
                Index = 0,
                Frame = new CanFrame(Id: baseId - 1, Len: 7, Payload: WriteSpeed()),
                MsgDescription = "WiperSpeed"
            },
            MessagePrefix.WiperDelays => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.WiperDelays,
                Index = 0,
                Frame = new CanFrame(Id: baseId - 1, Len: 7, Payload: WriteDelays()),
                MsgDescription = "WiperDelay"
            },
            _ => null
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        switch (prefix)
        {
            case MessagePrefix.Wiper:
                if (data.Length != 8) return false;
                break;

            case MessagePrefix.WiperSpeed:
                if (data.Length != 7) return false;
                break;

            case MessagePrefix.WiperDelays:
                if (data.Length != 7) return false;
                break;

            default:
                return false;
        }

        if (!SettingsRxSignals.TryGetValue(prefix, out var signals))
            return false;

        foreach (var (signal, setValue) in signals)
        {
            var value = ExtractSignal(data, signal);
            setValue(value);
        }

        return true;
    }

    private byte[] Write()
    {
        var data = new byte[8];
        var signals = SettingsTxSignals[MessagePrefix.Wiper];

        foreach (var (signal, getValue) in signals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }

    private byte[] WriteSpeed()
    {
        var data = new byte[8];
        var signals = SettingsTxSignals[MessagePrefix.WiperSpeed];

        foreach (var (signal, getValue) in signals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }

    private byte[] WriteDelays()
    {
        var data = new byte[8];
        var signals = SettingsTxSignals[MessagePrefix.WiperDelays];

        foreach (var (signal, getValue) in signals)
        {
            signal.Value = getValue();
            InsertSignal(data, signal);
        }

        return data;
    }
}