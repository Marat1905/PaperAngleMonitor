using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PaperAngleMonitor.Commands;
using PaperAngleMonitor.Converters;
using PaperAngleMonitor.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PaperAngleMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IVideoService _videoService;
        private readonly Dispatcher _dispatcher;

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                _isMonitoring = value;
                OnPropertyChanged();
            }
        }

        private string _statusBarText = "Ready";
        public string StatusBarText
        {
            get => _statusBarText;
            set
            {
                _statusBarText = value;
                OnPropertyChanged();
            }
        }

        private BitmapSource? _currentFrame;
        public BitmapSource? CurrentFrame
        {
            get => _currentFrame;
            set
            {
                _currentFrame = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public MainViewModel(ILogger<MainViewModel> logger, IVideoService videoService, Dispatcher dispatcher)
        {
            _logger = logger;
            _videoService = videoService;
            _dispatcher = dispatcher;

            StartMonitoringCommand = new RelayCommand(async _ => await StartMonitoringAsync(),
               _ => !IsMonitoring);

            StopMonitoringCommand = new RelayCommand(async _ => await StopMonitoringAsync(),
                _ => IsMonitoring);

            // Subscribe to events
            _videoService.FrameReady += OnFrameReady;
        }

        private async void OnFrameReady(Mat frame)
        {
            try
            {

                using (var frameClone = frame.Clone())
                {
                    await _dispatcher.InvokeAsync(() =>
                    {
                        BitmapSource bitmapSource;
                        try
                        {
                            bitmapSource = frameClone.ToBitmapSourceUnsafe();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Primary conversion failed, using alternative method");
                            bitmapSource = Converters.BitmapSourceConverter.ToBitmapSourceAlternative(frameClone);
                        }

                        CurrentFrame = bitmapSource;
                        //_logger.LogInformation($"Видео передается в потоке: {Thread.CurrentThread.ManagedThreadId}");
                    });

                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Frame already disposed, skipping processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing frame");
            }
        }

        private async Task StartMonitoringAsync()
        {

            try
            {

                // Подключаемся к видео источнику
                bool connected = await _videoService.ConnectAsync();
                if (!connected)
                {
                    StatusBarText = $"Failed to connect to Basler";
                    return;
                }

                await _videoService.StartCaptureAsync();
                IsMonitoring = true;

                StatusBarText = $"Monitoring started using Basler";
                _logger.LogInformation("Monitoring started using Basler");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start monitoring with Basler");
                StatusBarText = $"Error starting monitoring: {ex.Message}";
            }
        }

        private async Task StopMonitoringAsync()
        {
            try
            {

                await _videoService.StopCaptureAsync();
                await _videoService.DisconnectAsync();
                IsMonitoring = false;
                StatusBarText = "Monitoring stopped";
                _logger.LogInformation("Monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop monitoring");
                StatusBarText = "Error stopping monitoring";
            }
        }



        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_videoService != null)
            {
                _videoService.FrameReady -= OnFrameReady;
                _videoService.Dispose();
            }
        }
    }
}
