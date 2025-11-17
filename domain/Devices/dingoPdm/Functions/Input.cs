using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Input(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("number")] public int Number { get; set; } = num;
    [JsonPropertyName("state")] public bool State { get; set; }
    [JsonPropertyName("invert")] public bool Invert { get; set; }
    [JsonPropertyName("mode")] public InputMode Mode { get; set; }
    [JsonPropertyName("debounceTime")] public int DebounceTime { get; set; }
    [JsonPropertyName("pull")] public InputPull Pull { get; set; }

    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Inputs, 0, 8);
        InsertSignalInt(data, index, 12, 4);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 4) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        Mode = (InputMode)ExtractSignalInt(data, 9, 2);
        Invert = ExtractSignalInt(data, 11, 1) == 1;
        DebounceTime = (int)ExtractSignal(data, 16, 8, factor: 10.0);
        Pull = (InputPull)ExtractSignalInt(data, 24, 2);

        return true;
    }

    public byte[] Write()
    {
        byte[] data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Inputs, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertSignalInt(data, (long)Mode, 9, 2);
        InsertBool(data, Invert, 11);
        InsertSignalInt(data, Number - 1, 12, 4);
        InsertSignal(data, DebounceTime, 16, 8, factor: 10.0);
        InsertSignalInt(data, (long)Pull, 24, 2);
        return data;
    }
}