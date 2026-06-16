using Microsoft.Extensions.Logging;
using PaperAngleMonitor.Services;
using System.ComponentModel;

namespace PaperAngleMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IVideoService _videoService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(ILogger<MainViewModel> logger, IVideoService videoService)
        {
            _logger = logger;
            _videoService = videoService;
        }

        public void Dispose()
        {
            if (_videoService != null)
            {
                _videoService.Dispose();
            }
        }
    }
}
