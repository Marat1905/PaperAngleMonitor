using OpenCvSharp;
using PaperAngleMonitor.Models;

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

        void ApplySettings(BaslerSettings settings);
        (double Min, double Max) GetExposureRange();
        (double Min, double Max) GetGainRange();
        (double Min, double Max) GetFrameRateRange();
    }
}