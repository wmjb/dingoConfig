using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Output(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("number")] public int Number { get; set; } = num;
    [JsonPropertyName("currentLimit")] public int CurrentLimit { get; set; }
    [JsonPropertyName("resetCountLimit")] public int ResetCountLimit { get; set; }
    [JsonPropertyName("resetMode")] public ResetMode ResetMode { get; set; }
    [JsonPropertyName("resetTime")] public double ResetTime { get; set; }
    [JsonPropertyName("inrushCurrentLimit")] public int InrushCurrentLimit { get; set; }
    [JsonPropertyName("inrushTime")] public double InrushTime { get; set; }
    [JsonPropertyName("input")] public VarMap Input { get; set; }
    
    [JsonIgnore] public double Current { get; set; }
    [JsonIgnore] public OutState State { get; set; }
    [JsonIgnore] public int ResetCount { get; set; }
    [JsonIgnore] public double CurrentDutyCycle { get; set; }
    [JsonIgnore] public double CalculatedPower { get; set; }
    
    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Outputs, 0, 8);
        InsertSignalInt(data, index, 12, 4);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 8) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        Input = (VarMap)ExtractSignalInt(data, 16, 8);
        CurrentLimit = (int)ExtractSignalInt(data, 24, 8);
        ResetMode = (ResetMode)ExtractSignalInt(data, 32, 4);
        ResetCountLimit = (int)ExtractSignalInt(data, 36, 4);
        ResetTime = ExtractSignal(data, 40, 8, factor: 0.1);
        InrushCurrentLimit = (int)ExtractSignalInt(data, 48, 8);
        InrushTime = ExtractSignal(data, 56, 8, factor: 0.1);

        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Outputs, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertSignalInt(data, Number - 1, 12, 4);
        InsertSignalInt(data, (long)Input, 16, 8);
        InsertSignalInt(data, CurrentLimit, 24, 8);
        InsertSignalInt(data, (long)ResetMode, 32, 4);
        InsertSignalInt(data, ResetCountLimit, 36, 4);
        InsertSignal(data, ResetTime, 40, 8, factor: 0.1);
        InsertSignalInt(data, InrushCurrentLimit, 48, 8);
        InsertSignal(data, InrushTime, 56, 8, factor: 0.1);
        return data;
    }
}