using domain.Enums;

namespace domain.Interfaces;

public interface ICommsAdapter
{
    Task<(bool success, string? error)> InitAsync(string port, CanBitRate bitRate);
    Task<(bool success, string? error)> StartAsync();
    Task<(bool success, string? error)> StopAsync();
    Task<(bool success, string? error)> WriteAsync();

    TimeSpan RxTimeDelta();
}