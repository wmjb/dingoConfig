using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Condition(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;} = name;
    [JsonPropertyName("number")] public int Number {get; set;} = num;
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("input")] public VarMap Input { get; set; }
    [JsonPropertyName("operator")] public Operator Operator {get; set;}
    [JsonPropertyName("arg")] public int Arg {get; set;}
    
    [JsonIgnore] public int Value {get; set;}
    
    
    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Conditions, 0, 8);
        InsertSignalInt(data, index, 8, 8);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 6) return false;

        Enabled = ExtractSignalInt(data, 16, 1) == 1;
        Operator = (Operator)ExtractSignalInt(data, 20, 4);
        Input = (VarMap)ExtractSignalInt(data, 24, 8);
        Arg = (int)ExtractSignalInt(data, 32, 16, ByteOrder.BigEndian);

        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Conditions, 0, 8);
        InsertSignalInt(data, Number - 1, 8, 8);
        InsertBool(data, Enabled, 16);
        InsertSignalInt(data, (long)Operator, 20, 4);
        InsertSignalInt(data, (long)Input, 24, 8);
        InsertSignalInt(data, Arg, 32, 16, ByteOrder.BigEndian);

        return data;
    }
}