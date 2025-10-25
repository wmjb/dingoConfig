using System.Threading.Channels;
using domain.Models;

namespace infrastructure.BackgroundServices;

public class CommsDataPipeline
{
    private readonly Channel<CanData> _rxChannel;
    private readonly Channel<CanData> _txChannel;

    public CommsDataPipeline()
    {
        _rxChannel = Channel.CreateBounded<CanData>(
            new BoundedChannelOptions(50000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });

        _txChannel = Channel.CreateBounded<CanData>(
            new BoundedChannelOptions(50000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });
    }
}