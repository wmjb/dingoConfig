using domain.Interfaces;

namespace domain.Devices;

public class Input : IDeviceFunction
{
    public static byte[] Request(int index)
    {
        throw new NotImplementedException();
    }

    public bool Receive(byte[] data)
    {
        throw new NotImplementedException();
    }

    public byte[] Write()
    {
        throw new NotImplementedException();
    }
}