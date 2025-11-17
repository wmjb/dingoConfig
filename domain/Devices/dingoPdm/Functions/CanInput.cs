using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class CanInput(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;} = name;
    [JsonPropertyName("number")] public int Number {get; set;} = num;
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
    
    [JsonIgnore] public bool Output { get; set; }
    [JsonIgnore] public int Value {get; set;}
    
    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.CanInputs, 0, 8);
        InsertSignalInt(data, index, 8, 8);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 7) return false;

        Enabled = ExtractSignalInt(data, 16, 1) == 1;
        Mode = (InputMode)ExtractSignalInt(data, 17, 2);
        TimeoutEnabled = ExtractSignalInt(data, 19, 1) == 1;
        Operator = (Operator)ExtractSignalInt(data, 20, 4);
        StartingByte = (int)ExtractSignalInt(data, 24, 4);
        Dlc = (int)ExtractSignalInt(data, 28, 4);
        OnVal = (int)ExtractSignalInt(data, 32, 16, ByteOrder.BigEndian);
        Timeout = (int)ExtractSignal(data, 48, 8, factor: 0.1);
        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.CanInputs, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);
        InsertBool(data, Enabled, 16);
        InsertSignalInt(data, (long)Mode, 17, 2);
        InsertBool(data, TimeoutEnabled, 19);
        InsertSignalInt(data, (long)Operator, 20, 4);
        InsertSignalInt(data, StartingByte, 24, 4);
        InsertSignalInt(data, Dlc, 28, 4);
        InsertSignalInt(data, OnVal, 32, 16, ByteOrder.BigEndian);
        InsertSignal(data, Timeout, 48, 8, factor: 0.1);
        return data;
    }
    
    public static byte[] RequestId(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.CanInputsId, 0, 8);
        InsertSignalInt(data, index, 8, 8);
        return data;
    }

    public bool ReceiveId(byte[] data)
    {
        if (data.Length != 8) return false;

        Ide = ExtractSignalInt(data, 19, 1) == 1;

        if (Ide)
        {
            // Extended ID: bits 32-36 (5 bits) + bits 40-63 (24 bits) = 29 bits total
            int idUpper = (int)ExtractSignalInt(data, 32, 5);
            int idLower = (int)ExtractSignalInt(data, 40, 24, ByteOrder.BigEndian);
            Id = (idUpper << 24) | idLower;
        }
        else
        {
            // Standard ID: 11 bits at position 16-18 (3 bits) and 24-31 (8 bits)
            Id = (int)ExtractSignalInt(data, 16, 3) << 8 | (int)ExtractSignalInt(data, 24, 8);
        }

        return true;
    }

    public byte[] WriteId()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.CanInputsId, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);

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