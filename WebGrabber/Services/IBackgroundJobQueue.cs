namespace WebGrabber.Services;

public interface IBackgroundJobQueue
{
    void Enqueue(WebGrabConfig config);
    bool TryDequeue(out WebGrabConfig? config);
}
