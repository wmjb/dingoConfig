using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;
using static domain.Common.DbcSignalCodec;

namespace domain.Devices.dingoPdm.Functions;

public class StarterDisable(string name, int outputCount) : IDeviceFunction
{
    [JsonPropertyName("name")] public string Name {get; set;} = name;
    [JsonIgnore] public int Number => 1;
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("input")] public VarMap Input {get; set;}
    [JsonPropertyName("outputsDisabled")] public List<bool> OutputsDisabled {get; set;} = [..new bool[outputCount]];

    public static int ExtractIndex(byte data, MessagePrefix prefix)
    {
        return data;
    }
    
    public DeviceCanFrame? CreateUploadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return null;
        
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.StarterDisable, 0, 8);

        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.StarterDisable,
            Index = 0,
            Frame = new CanFrame(Id: baseId - 1, Len: 1, Payload: data),
            MsgDescription = "StarterDisable"
        };
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return null;
        
        return new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            Sent = false,
            Received = false,
            Prefix = (int)MessagePrefix.StarterDisable,
            Index = 0,
            Frame = new CanFrame(Id: baseId - 1, Len: 4, Payload: Write()),
            MsgDescription = "StarterDisable"
        };
    }

    public bool Receive(byte[] data, MessagePrefix prefix)
    {
        if (prefix != MessagePrefix.StarterDisable) return false;
        if (data.Length != 4) return false;

        Enabled = ExtractSignalInt(data, 8, 1) == 1;
        Input = (VarMap)ExtractSignalInt(data, 16, 8);

        for (int i = 0; i < OutputsDisabled.Count; i++)
        {
            OutputsDisabled[i] = ExtractSignalInt(data, 24 + i, 1) == 1;
        }

        return true;
    }

    private byte[] Write()
    {
        var data = new byte[8];
        InsertSignalInt(data, (long)MessagePrefix.StarterDisable, 0, 8);
        InsertBool(data, Enabled, 8);
        InsertSignalInt(data, (long)Input, 16, 8);

        for (int i = 0; i < OutputsDisabled.Count; i++)
        {
            InsertBool(data, OutputsDisabled[i], 24 + i);
        }

        return data;
    }
}