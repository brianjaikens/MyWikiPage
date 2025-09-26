using System.Collections.Concurrent;

namespace WebGrabber.Services;

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly ConcurrentQueue<WebGrabConfig> _items = new();

    public void Enqueue(WebGrabConfig config)
    {
        _items.Enqueue(config);
    }

    public bool TryDequeue(out WebGrabConfig? config)
    {
        return _items.TryDequeue(out config);
    }
}
