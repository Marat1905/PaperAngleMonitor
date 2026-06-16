using OpenCvSharp;

namespace PaperAngleMonitor.Services
{
    public interface IVideoService : IDisposable
    {
        event Action<Mat> FrameReady;
        bool IsConnected { get; }
        double CurrentFps { get; }

        Task<bool> ConnectAsync();

        Task DisconnectAsync();
        Task StartCaptureAsync();
        Task StopCaptureAsync();

    }
}
