using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class Wiper(string name) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;} = name;
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

    [JsonIgnore] public bool SlowState { get; set; }
    [JsonIgnore] public bool FastState { get; set; }
    [JsonIgnore] public WiperState State { get; set; }
    [JsonIgnore] public WiperSpeed Speed { get; set; }

    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Wiper, 0, 8);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 8) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        Mode = (WiperMode)ExtractSignalInt(data, 9, 2);
        ParkStopLevel = ExtractSignalInt(data, 11, 1) == 1;
        WashWipeCycles = (byte)ExtractSignalInt(data, 12, 4);
        SlowInput = (VarMap)ExtractSignalInt(data, 16, 8);
        FastInput = (VarMap)ExtractSignalInt(data, 24, 8);
        InterInput = (VarMap)ExtractSignalInt(data, 32, 8);
        OnInput = (VarMap)ExtractSignalInt(data, 40, 8);
        ParkInput = (VarMap)ExtractSignalInt(data, 48, 8);
        WashInput = (VarMap)ExtractSignalInt(data, 56, 8);

        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.Wiper, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertSignalInt(data, (long)Mode, 9, 2);
        InsertBool(data, ParkStopLevel, 11);
        InsertSignalInt(data, WashWipeCycles, 12, 4);
        InsertSignalInt(data, (long)SlowInput, 16, 8);
        InsertSignalInt(data, (long)FastInput, 24, 8);
        InsertSignalInt(data, (long)InterInput, 32, 8);
        InsertSignalInt(data, (long)OnInput, 40, 8);
        InsertSignalInt(data, (long)ParkInput, 48, 8);
        InsertSignalInt(data, (long)WashInput, 56, 8);
        return data;
    }

    public static byte[] RequestSpeed()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.WiperSpeed, 0, 8);
        return data;
    }

    public bool ReceiveSpeed(byte[] data)
    {
        if (data.Length != 7) return false;

        SwipeInput = (VarMap)ExtractSignalInt(data, 8, 8);
        SpeedInput = (VarMap)ExtractSignalInt(data, 16, 8);
        SpeedMap[0] = (WiperSpeed)ExtractSignalInt(data, 24, 4);
        SpeedMap[1] = (WiperSpeed)ExtractSignalInt(data, 28, 4);
        SpeedMap[2] = (WiperSpeed)ExtractSignalInt(data, 32, 4);
        SpeedMap[3] = (WiperSpeed)ExtractSignalInt(data, 36, 4);
        SpeedMap[4] = (WiperSpeed)ExtractSignalInt(data, 40, 4);
        SpeedMap[5] = (WiperSpeed)ExtractSignalInt(data, 44, 4);
        SpeedMap[6] = (WiperSpeed)ExtractSignalInt(data, 48, 4);
        SpeedMap[7] = (WiperSpeed)ExtractSignalInt(data, 52, 4);

        return true;
    }

    public byte[] WriteSpeed()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.WiperSpeed, 0, 8);
        InsertSignalInt(data, (long)SwipeInput, 8, 8);
        InsertSignalInt(data, (long)SpeedInput, 16, 8);
        InsertSignalInt(data, (long)SpeedMap[0], 24, 4);
        InsertSignalInt(data, (long)SpeedMap[1], 28, 4);
        InsertSignalInt(data, (long)SpeedMap[2], 32, 4);
        InsertSignalInt(data, (long)SpeedMap[3], 36, 4);
        InsertSignalInt(data, (long)SpeedMap[4], 40, 4);
        InsertSignalInt(data, (long)SpeedMap[5], 44, 4);
        InsertSignalInt(data, (long)SpeedMap[6], 48, 4);
        InsertSignalInt(data, (long)SpeedMap[7], 52, 4);
        return data;
    }

    public static byte[] RequestDelays()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.WiperDelays, 0, 8);
        return data;
    }

    public bool ReceiveDelays(byte[] data)
    {
        if (data.Length != 7) return false;

        IntermitTime[0] = ExtractSignal(data, 8, 8, factor: 0.1);
        IntermitTime[1] = ExtractSignal(data, 16, 8, factor: 0.1);
        IntermitTime[2] = ExtractSignal(data, 24, 8, factor: 0.1);
        IntermitTime[3] = ExtractSignal(data, 32, 8, factor: 0.1);
        IntermitTime[4] = ExtractSignal(data, 40, 8, factor: 0.1);
        IntermitTime[5] = ExtractSignal(data, 48, 8, factor: 0.1);

        return true;
    }

    public byte[] WriteDelays()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.WiperDelays, 0, 8);
        InsertSignal(data, IntermitTime[0], 8, 8, factor: 0.1);
        InsertSignal(data, IntermitTime[1], 16, 8, factor: 0.1);
        InsertSignal(data, IntermitTime[2], 24, 8, factor: 0.1);
        InsertSignal(data, IntermitTime[3], 32, 8, factor: 0.1);
        InsertSignal(data, IntermitTime[4], 40, 8, factor: 0.1);
        InsertSignal(data, IntermitTime[5], 48, 8, factor: 0.1);
        return data;
    }
}