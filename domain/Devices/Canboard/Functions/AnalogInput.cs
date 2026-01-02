using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.Canboard.Functions;

public class AnalogInput(int number, string name) : IDeviceFunction
{
    [JsonPropertyName("number")] public int Number { get; set; } = number;
    [JsonPropertyName("name")] public string Name { get; set; } = name;

    [JsonIgnore] public double Millivolts { get; set; }
    [JsonIgnore] public int RotarySwitchPos { get; set; }
    [JsonIgnore] public bool DigitalIn { get; set; }
    
    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        throw new NotImplementedException();
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        throw new NotImplementedException();
    }

    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        throw new NotImplementedException();
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        throw new NotImplementedException();
    }
}