using domain.Enums;
using domain.Interfaces;
using domain.Models;

namespace infrastructure.Adapters;

public class SimAdapter : ICommsAdapter
{
    private TimeSpan _rxTimeDelta;
    public string? Name => "Sim";

    public Task<bool> InitAsync(string port, CanBitRate bitRate, CancellationToken ct)
    {
        IsConnected = false;
        return Task.FromResult(true);
    }

    public Task<bool> StartAsync(CancellationToken ct)
    {
        IsConnected = true;
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync()
    {
        IsConnected = false;
        return Task.FromResult(true);
    }

    public Task<bool> WriteAsync(CanFrame frame, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    public event DataReceivedHandler? DataReceived;
    public TimeSpan RxTimeDelta()
    {
        return TimeSpan.Zero;
    }

    TimeSpan ICommsAdapter.RxTimeDelta => _rxTimeDelta;

    public bool IsConnected { get; set; }
}