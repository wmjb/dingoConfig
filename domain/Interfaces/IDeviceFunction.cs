namespace domain.Interfaces;

public interface IDeviceFunction
{
    public static abstract byte[] Request(int index);
    public bool Receive(byte[] data);
    public byte[] Write();
}