using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Flasher(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;} = name;
    [JsonPropertyName("number")] public int Number {get; set;} = num;
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("single")] public bool Single {get; set;}
    [JsonPropertyName("input")] public VarMap Input {get; set;}
    [JsonPropertyName("onTime")] public int OnTime {get; set;}
    [JsonPropertyName("offTime")] public int  OffTime {get; set;}
    
    [JsonIgnore] public bool Value {get; set;}
    
    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Flashers, 0, 8);
        InsertSignalInt(data, index, 12, 4);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 6) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        Single = ExtractSignalInt(data, 9, 1) == 1;
        Input = (VarMap)ExtractSignalInt(data, 16, 8);
        OnTime = (int)ExtractSignal(data, 32, 8, factor: 0.1);
        OffTime = (int)ExtractSignal(data, 40, 8, factor: 0.1);

        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Flashers, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertBool(data, Single, 9);
        InsertSignalInt(data, Number - 1, 12, 4);
        InsertSignalInt(data, (long)Input, 16, 8);
        InsertSignal(data, OnTime, 32, 8, factor: 0.1);
        InsertSignal(data, OffTime, 40, 8, factor: 0.1);
        return data;
    }
}