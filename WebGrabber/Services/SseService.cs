using System.Collections.Concurrent;
using System.Threading.Channels;

namespace WebGrabber.Services;

public class SseService
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _channels = new();

    public Channel<string> Subscribe()
    {
        var ch = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _channels.TryAdd(Guid.NewGuid(), ch);
        return ch;
    }

    public void Unsubscribe(Channel<string> channel)
    {
        var key = _channels.FirstOrDefault(kvp => kvp.Value == channel).Key;
        if (key != Guid.Empty)
        {
            _channels.TryRemove(key, out _);
        }
    }

    public void Broadcast(string message)
    {
        foreach (var kvp in _channels)
        {
            var writer = kvp.Value.Writer;
            // fire-and-forget; if write fails remove channel
            writer.TryWrite(message);
        }
    }
}
