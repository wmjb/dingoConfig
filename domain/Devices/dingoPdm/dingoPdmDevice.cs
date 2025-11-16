using System.Collections.Concurrent;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices;

public class dingoPdmDevice : IDevice
{
    protected virtual int _minMajorVersion { get; } = 0;
    protected virtual int _minMinorVersion { get; } = 4;
    protected virtual int _minBuildVersion { get; } = 18;

    protected virtual int _numDigitalInputs { get; } = 2;
    protected virtual int _numOutputs { get; } = 8;
    protected virtual int _numCanInputs { get; } = 32;
    protected virtual int _numVirtualInputs { get; } = 16;
    protected virtual int _numFlashers { get; } = 4;
    protected virtual int _numCounters { get; } = 4;
    protected virtual int _numConditions { get; } = 32;
    
    public Guid Id { get; }
    public string Name { get; set; }
    public int BaseId { get; set; }
    public bool Connected { get; set; }
    public TimeSpan LastRxTime { get; set; }

    public dingoPdmDevice(string name, int baseId)
    {
        Name = name;
        BaseId = baseId;
        
        DigitalInputs = new ObservableCollection<Input>();
        for (int i = 0; i < _numDigitalInputs; i++)
        {
            DigitalInputs.Add(new Input());
            DigitalInputs[i].Number = i + 1;
        }

        TotalCurrent = 0;
        BatteryVoltage = 0;
        BoardTempC = 0;

        Outputs = new ObservableCollection<Output>();
        for (int i = 0; i < _numOutputs; i++)
        {
            Outputs.Add(new Output());
            Outputs[i].Number = i + 1;
        }

        CanInputs = new ObservableCollection<CanInput>();
        for (int i = 0; i < _numCanInputs; i++)
        {
            CanInputs.Add(new CanInput());
            CanInputs[i].Number = i + 1;
        }

        VirtualInputs = new ObservableCollection<VirtualInput>();
        for (int i = 0; i < _numVirtualInputs; i++)
        {
            VirtualInputs.Add(new VirtualInput());
            VirtualInputs[i].Number = i + 1;
        }

        Wipers = new ObservableCollection<Wiper>
        {
            new Wiper()
        };

        Flashers = new ObservableCollection<Flasher>();
        for (int i = 0; i < _numFlashers; i++)
        {
            Flashers.Add(new Flasher());
            Flashers[i].Number = i + 1;
        }

        StarterDisable = new ObservableCollection<StarterDisable>
        {
            new StarterDisable()
        };

        Counters = new ObservableCollection<Counter>();
        for (int i = 0; i < _numCounters; i++)
        {
            Counters.Add(new Counter());
            Counters[i].Number = i + 1;
        }

        Conditions = new ObservableCollection<Condition>();
        for (int i = 0; i < _numConditions; i++)
        {
            Conditions.Add(new Condition());
            Conditions[i].Number = i + 1;
        }
    }
    public void UpdateConnected()
    {
        throw new NotImplementedException();
    }

    public void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceResponse> queue)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool InIdRange(int id)
    {
        return (id >= BaseId - 1) && (id <= BaseId + 30);
    }

    public List<DeviceResponse> GetUploadMsgs()
    {
        throw new NotImplementedException();
    }

    public List<DeviceResponse> GetDownloadMsgs()
    {
        throw new NotImplementedException();
    }

    public List<DeviceResponse> GetUpdateMsgs(int newId)
    {
        throw new NotImplementedException();
    }

    public DeviceResponse GetBurnMsg()
    {
        throw new NotImplementedException();
    }

    public DeviceResponse GetSleepMsg()
    {
        throw new NotImplementedException();
    }

    public DeviceResponse GetVersionMsg()
    {
        throw new NotImplementedException();
    }
}