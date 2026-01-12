using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class CanInput : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("timeoutEnabled")] public bool TimeoutEnabled {get; set;}
    [JsonPropertyName("timeout")] public int Timeout {get; set;}
    [JsonPropertyName("ide")] public bool Ide {get; set;}
    [JsonPropertyName("startingByte")] public int StartingByte {get; set;}
    [JsonPropertyName("dlc")] public int Dlc {get; set;}
    [JsonPropertyName("operator")] public Operator Operator {get; set;}
    [JsonPropertyName("onVal")] public int OnVal {get; set;}
    [JsonPropertyName("mode")] public InputMode Mode {get; set;}

    [JsonPropertyName("id")]
    public int Id
    {
        get;
        set
        {
            field = value;
            Ide = (field > 2047);
        }
    }
    
    [JsonIgnore][Plotable(displayName:"State")] public bool Output { get; set; }
    [JsonIgnore][Plotable(displayName:"Value")] public int Value {get; set;}

    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Action<double> SetValue)>> SettingsRxSignals { get; }
    [JsonIgnore] private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> SettingsTxSignals { get; }

    [JsonConstructor]
    public CanInput(int number, string name)
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
            [MessagePrefix.CanInputs] =
            [
                (new DbcSignal { Name = "Enabled", StartBit = 16, Length = 1 },
                    val => Enabled = val != 0),

                (new DbcSignal { Name = "Mode", StartBit = 17, Length = 2 },
                    val => Mode = (InputMode)val),

                (new DbcSignal { Name = "TimeoutEnabled", StartBit = 19, Length = 1 },
                    val => TimeoutEnabled = val != 0),

                (new DbcSignal { Name = "Operator", StartBit = 20, Length = 4 },
                    val => Operator = (Operator)val),

                (new DbcSignal { Name = "StartingByte", StartBit = 24, Length = 4 },
                    val => StartingByte = (int)val),

                (new DbcSignal { Name = "Dlc", StartBit = 28, Length = 4 },
                    val => Dlc = (int)val),

                (new DbcSignal { Name = "OnVal", StartBit = 32, Length = 16, ByteOrder = ByteOrder.BigEndian },
                    val => OnVal = (int)val),

                (new DbcSignal { Name = "Timeout", StartBit = 48, Length = 8, Factor = 0.1 },
                    val => Timeout = (int)val)
            ]
            // Note: CanInputsId uses custom ID parsing logic in Receive()
        };
    }

    private Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>> InitializeTxSignals()
    {
        return new Dictionary<MessagePrefix, List<(DbcSignal Signal, Func<double> GetValue)>>
        {
            [MessagePrefix.CanInputs] =
            [
                (new DbcSignal { Name = "Prefix", StartBit = 0, Length = 8 },
                    () => (int)MessagePrefix.CanInputs),

                (new DbcSignal { Name = "Index", StartBit = 8, Length = 8 },
                    () => Number - 1),

                (new DbcSignal { Name = "Enabled", StartBit = 16, Length = 1 },
                    () => Enabled ? 1 : 0),

                (new DbcSignal { Name = "Mode", StartBit = 17, Length = 2 },
                    () => (int)Mode),

                (new DbcSignal { Name = "TimeoutEnabled", StartBit = 19, Length = 1 },
                    () => TimeoutEnabled ? 1 : 0),

                (new DbcSignal { Name = "Operator", StartBit = 20, Length = 4 },
                    () => (int)Operator),

                (new DbcSignal { Name = "StartingByte", StartBit = 24, Length = 4 },
                    () => StartingByte),

                (new DbcSignal { Name = "Dlc", StartBit = 28, Length = 4 },
                    () => Dlc),

                (new DbcSignal { Name = "OnVal", StartBit = 32, Length = 16, ByteOrder = ByteOrder.BigEndian },
                    () => OnVal),

                (new DbcSignal { Name = "Timeout", StartBit = 48, Length = 8, Factor = 0.1 },
                    () => Timeout)
            ]
            // Note: CanInputsId uses custom ID encoding logic in WriteId()
        };
    }

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }

    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        switch (prefix)
        {
            case MessagePrefix.CanInputs:
            {
                var data = new byte[8];
                InsertSignalInt(data, (long)MessagePrefix.CanInputs, 0, 8);
                InsertSignalInt(data, Number - 1, 8, 8);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.CanInputs,
                    Index = Number - 1,
                    Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
                    MsgDescription = $"CANInput{Number}"
                };
            }
            case MessagePrefix.CanInputsId:
            {
                var data = new byte[8];
                InsertSignalInt(data, (long)MessagePrefix.CanInputsId, 0, 8);
                InsertSignalInt(data, Number - 1, 8, 8);

                return new DeviceCanFrame
                {
                    DeviceBaseId = baseId,
                    Sent = false,
                    Received = false,
                    Prefix = (int)MessagePrefix.CanInputsId,
                    Index = Number - 1,
                    Frame = new CanFrame(Id: baseId - 1, Len: 2, Payload: data),
                    MsgDescription = $"CANInputId{Number}"
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
            MessagePrefix.CanInputs => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.CanInputs,
                Index = Number - 1,
                Frame = new CanFrame(Id: baseId - 1, Len: 7, Payload: Write()),
                MsgDescription = $"CANInput{Number}"
            },
            MessagePrefix.CanInputsId => new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                Sent = false,
                Received = false,
                Prefix = (int)MessagePrefix.CanInputsId,
                Index = Number - 1,
                Frame = new CanFrame(Id: baseId - 1, Len: 8, Payload: WriteId()),
                MsgDescription = $"CANInputId{Number}"
            },
            _ => null
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        switch (prefix)
        {
            case MessagePrefix.CanInputs when data.Length != 7:
                return false;
            case MessagePrefix.CanInputs:
            {
                if (SettingsRxSignals.TryGetValue(prefix, out var signals))
                {
                    foreach (var (signal, setValue) in signals)
                    {
                        var value = ExtractSignal(data, signal);
                        setValue(value);
                    }
                }
                return true;
            }
            case MessagePrefix.CanInputsId when data.Length != 8:
                return false;
            // Custom ID parsing logic
            case MessagePrefix.CanInputsId:
            {
                Ide = ExtractSignalInt(data, 19, 1) == 1;

                if (Ide)
                {
                    // Extended ID: bits 32-36 (5 bits) + bits 40-63 (24 bits) = 29 bits total
                    var idUpper = (int)ExtractSignalInt(data, 32, 5);
                    var idLower = (int)ExtractSignalInt(data, 40, 24, ByteOrder.BigEndian);
                    Id = (idUpper << 24) | idLower;
                }
                else
                {
                    // Standard ID: 11 bits at position 16-18 (3 bits) and 24-31 (8 bits)
                    Id = (int)ExtractSignalInt(data, 16, 3) << 8 | (int)ExtractSignalInt(data, 24, 8);
                }

                return true;
            }
            default:
                return false;
        }
    }

    private byte[] Write()
    {
        var data = new byte[8];

        if (SettingsTxSignals.TryGetValue(MessagePrefix.CanInputs, out var signals))
        {
            foreach (var (signal, getValue) in signals)
            {
                signal.Value = getValue();
                InsertSignal(data, signal);
            }
        }

        return data;
    }

    private byte[] WriteId()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.CanInputsId, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);

        // Custom ID encoding logic
        if (Ide)
        {
            // Extended ID: upper 5 bits and lower 24 bits
            InsertSignalInt(data, (Id >> 8) & 0x07, 16, 3);
            InsertBool(data, Ide, 19);
            InsertSignalInt(data, (Id >> 24) & 0x1F, 32, 5);
            InsertSignalInt(data, Id & 0xFFFFFF, 40, 24, ByteOrder.BigEndian);
        }
        else
        {
            // Standard ID: 11 bits split across byte 2 and 3
            InsertSignalInt(data, (Id >> 8) & 0x07, 16, 3);
            InsertBool(data, Ide, 19);
            InsertSignalInt(data, Id & 0xFF, 24, 8);
        }

        return data;
    }
}