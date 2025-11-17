using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class OutputPwm(int num, string name) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("number")] public int Number { get; set; } = num;
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("softStartEnabled")] public bool SoftStartEnabled { get; set; }
    [JsonPropertyName("variableDutyCycle")] public bool VariableDutyCycle { get; set; }
    [JsonPropertyName("dutyCycleInput")] public VarMap DutyCycleInput { get; set; }
    [JsonPropertyName("fixedDutyCycle")] public int FixedDutyCycle { get; set; }
    [JsonPropertyName("frequency")] public int Frequency { get; set; }
    [JsonPropertyName("softStartRampTime")] public int SoftStartRampTime { get; set; }
    [JsonPropertyName("dutyCycleDenominator")] public int DutyCycleDenominator { get; set; }

    public static byte[] Request(int index)
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.OutputsPwm, 0, 8);
        InsertSignalInt(data, index, 12, 4);
        return data;
    }

    public bool Receive(byte[] data)
    {
        if (data.Length != 8) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        SoftStartEnabled = ExtractSignalInt(data, 9, 1) == 1;
        VariableDutyCycle = ExtractSignalInt(data, 10, 1) == 1;
        DutyCycleInput = (VarMap)ExtractSignalInt(data, 16, 8);
    
        // Frequency is 9 bits with unusual encoding: upper 8 bits at byte 3, LSB at byte 4 bit 0
        int freqUpper = (int)ExtractSignalInt(data, 24, 8);
        int freqLsb = (int)ExtractSignalInt(data, 32, 1);
        Frequency = (freqUpper << 1) | freqLsb;
    
        FixedDutyCycle = (byte)ExtractSignalInt(data, 33, 7);
        SoftStartRampTime = (ushort)ExtractSignalInt(data, 40, 16, ByteOrder.BigEndian);
        DutyCycleDenominator = (byte)ExtractSignalInt(data, 56, 8);

        return true;
    }

    public byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.OutputsPwm, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertBool(data, SoftStartEnabled, 9);
        InsertBool(data, VariableDutyCycle, 10);
        InsertSignalInt(data, Number - 1, 12, 4);
        InsertSignalInt(data, (long)DutyCycleInput, 16, 8);
    
        // Frequency is 9 bits with unusual encoding: upper 8 bits at byte 3, LSB at byte 4 bit 0
        InsertSignalInt(data, Frequency >> 1, 24, 8);
        InsertSignalInt(data, Frequency & 0x01, 32, 1);
    
        InsertSignalInt(data, FixedDutyCycle, 33, 7);
        InsertSignalInt(data, SoftStartRampTime, 40, 16, ByteOrder.BigEndian);
        InsertSignalInt(data, DutyCycleDenominator, 56, 8);
    
        return data;
    }
}