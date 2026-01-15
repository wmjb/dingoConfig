using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;
using domain.Enums;
using domain.Models;
using domain.Interfaces;

namespace infrastructure.Adapters;

public class SocketCanAdapter : ICommsAdapter
{
    public string Name => "SocketCAN";

    private Socket? _socket;
    private Thread? _rxThread;
    private bool _running;

    private Stopwatch? _rxStopwatch;
    public TimeSpan RxTimeDelta { get; private set; }

    private string _ifName = "can0";

    public event DataReceivedHandler? DataReceived;

    public bool IsConnected => RxTimeDelta < TimeSpan.FromMilliseconds(500);

    public Task<bool> InitAsync(string iface, CanBitRate bitRate, CancellationToken ct)
    {
        try
        {
            _ifName = iface;
            _rxStopwatch = Stopwatch.StartNew();
            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(false);
        }
    }

    public Task<bool> StartAsync(CancellationToken ct)
    {
        try
        {
            _socket = new Socket(AddressFamily.Can, SocketType.Raw, (ProtocolType)SocketCANProtocols.CAN_RAW);

            int ifIndex = GetInterfaceIndex(_ifName);
            if (ifIndex < 0)
                return Task.FromResult(false);

            var addr = new SockAddrCan
            {
                can_family = (ushort)AddressFamily.Can,
                can_ifindex = ifIndex
            };

            var addrBytes = StructureToBytes(addr);

            _socket.Bind(new SockAddr(addrBytes));

            _running = true;
            _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
            _rxThread.Start();

            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(false);
        }
    }

    public Task<bool> StopAsync()
    {
        try
        {
            _running = false;
            _socket?.Close();
            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(false);
        }
    }

    public Task<bool> WriteAsync(CanFrame frame, CancellationToken ct)
    {
        if (_socket == null) return Task.FromResult(false);

        try
        {
            var can = new CanFrameRaw
            {
                can_id = (uint)frame.Id,
                can_dlc = (byte)frame.Len
            };

            Array.Copy(frame.Payload, can.data, frame.Len);

            var bytes = StructureToBytes(can);
            _socket.Send(bytes);

            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(false);
        }
    }

    private void ReceiveLoop()
    {
        var buf = new byte[Marshal.SizeOf<CanFrameRaw>()];

        while (_running && _socket != null)
        {
            try
            {
                int read = _socket.Receive(buf);
                if (read < Marshal.SizeOf<CanFrameRaw>())
                    continue;

                var raw = BytesToStructure<CanFrameRaw>(buf);

                if (_rxStopwatch != null)
                {
                    RxTimeDelta = TimeSpan.FromMilliseconds(_rxStopwatch.ElapsedMilliseconds);
                    _rxStopwatch.Restart();
                }

                var payload = new byte[raw.can_dlc];
                Array.Copy(raw.data, payload, raw.can_dlc);

                var frame = new CanFrame((int)raw.can_id, raw.can_dlc, payload);
                DataReceived?.Invoke(this, new CanFrameEventArgs(frame));
            }
            catch { }
        }
    }

    // -----------------------------
    // Linux SocketCAN Interop
    // -----------------------------

    private static int GetInterfaceIndex(string ifName)
    {
        var ifreq = new Ifreq();
        Encoding.ASCII.GetBytes(ifName).CopyTo(ifreq.ifr_name, 0);

        int fd = Libc.socket((int)AddressFamily.Unix, (int)SocketType.Dgram, 0);
        if (fd < 0) return -1;

        int result = Libc.ioctl(fd, Libc.SIOCGIFINDEX, ref ifreq);
        Libc.close(fd);

        return result < 0 ? -1 : ifreq.ifr_ifindex;
    }

    private static byte[] StructureToBytes<T>(T str)
    {
        int size = Marshal.SizeOf<T>();
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    private static T BytesToStructure<T>(byte[] arr)
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(arr, 0, ptr, size);
        T str = Marshal.PtrToStructure<T>(ptr)!;
        Marshal.FreeHGlobal(ptr);
        return str;
    }
}

// -----------------------------
// Native structs
// -----------------------------

[StructLayout(LayoutKind.Sequential)]
public struct SockAddrCan
{
    public ushort can_family;
    public int can_ifindex;
    public uint rx_id;
    public uint tx_id;
}

[StructLayout(LayoutKind.Sequential)]
public struct CanFrameRaw
{
    public uint can_id;
    public byte can_dlc;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] data;
}

[StructLayout(LayoutKind.Sequential)]
public struct Ifreq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] ifr_name;

    public int ifr_ifindex;
}

public static class SocketCANProtocols
{
    public const int CAN_RAW = 1;
}

public static class Libc
{
    public const int SIOCGIFINDEX = 0x8933;

    [DllImport("libc", SetLastError = true)]
    public static extern int socket(int domain, int type, int protocol);

    [DllImport("libc", SetLastError = true)]
    public static extern int ioctl(int fd, int request, ref Ifreq ifreq);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);
}
