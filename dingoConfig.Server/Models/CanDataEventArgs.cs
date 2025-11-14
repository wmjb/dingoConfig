using dingoConfig.Server.Models;

namespace dingoConfig.Server.Models;

public class CanDataEventArgs(CanData data) : EventArgs
{
    public CanData Data { get; private set; } = data;
}