using Microsoft.Extensions.Logging;
using PaperAngleMonitor.Commands;
using PaperAngleMonitor.Models;
using PaperAngleMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PaperAngleMonitor.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IVideoService _videoService;
        private readonly ILogger<SettingsViewModel> _logger;
        private Window? _window;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel(IVideoService videoService, ILogger<SettingsViewModel> logger)
        {
            _videoService = videoService;
            _logger = logger;

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        // Свойства для привязки
        private int _width;
        public int Width { get => _width; set { _width = value; OnPropertyChanged(); } }

        private int _height;
        public int Height { get => _height; set { _height = value; OnPropertyChanged(); } }

        private string _pixelFormat = string.Empty;
        public string PixelFormat { get => _pixelFormat; set { _pixelFormat = value; OnPropertyChanged(); } }

        private string _exposureAuto = string.Empty;
        public string ExposureAuto { get => _exposureAuto; set { _exposureAuto = value; OnPropertyChanged(); } }

        private double _exposureTime;
        public double ExposureTime { get => _exposureTime; set { _exposureTime = value; OnPropertyChanged(); } }

        private string _gainAuto = string.Empty;
        public string GainAuto { get => _gainAuto; set { _gainAuto = value; OnPropertyChanged(); } }

        private double _gain;
        public double Gain { get => _gain; set { _gain = value; OnPropertyChanged(); } }

        private bool _acquisitionFrameRateEnable;
        public bool AcquisitionFrameRateEnable { get => _acquisitionFrameRateEnable; set { _acquisitionFrameRateEnable = value; OnPropertyChanged(); } }

        private double _acquisitionFrameRate;
        public double AcquisitionFrameRate { get => _acquisitionFrameRate; set { _acquisitionFrameRate = value; OnPropertyChanged(); } }

        // Диапазоны и списки
        public int MinWidth { get; private set; }
        public int MaxWidth { get; private set; }
        public int MinHeight { get; private set; }
        public int MaxHeight { get; private set; }
        public double MinExposure { get; private set; }
        public double MaxExposure { get; private set; }
        public double MinGain { get; private set; }
        public double MaxGain { get; private set; }
        public double MinFrameRate { get; private set; }
        public double MaxFrameRate { get; private set; }

        public ObservableCollection<string> PixelFormats { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ExposureAutoModes { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> GainAutoModes { get; } = new ObservableCollection<string>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private bool CanSave() => true;

        private void Save()
        {
            try
            {
                var settings = new BaslerSettings
                {
                    Width = Width,
                    Height = Height,
                    PixelFormat = PixelFormat,
                    ExposureAuto = ExposureAuto,
                    ExposureTime = ExposureTime,
                    GainAuto = GainAuto,
                    Gain = (int)Gain,
                    AcquisitionFrameRateEnable = AcquisitionFrameRateEnable,
                    AcquisitionFrameRate = AcquisitionFrameRate
                };
                _videoService.ApplySettings(settings);
                _logger.LogInformation("Settings applied successfully");
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply settings");
                MessageBox.Show($"Ошибка применения настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() => CloseWindow(false);

        private void CloseWindow(bool dialogResult)
        {
            if (_window != null && _window.IsLoaded)
            {
                _window.DialogResult = dialogResult;
                _window.Close();
            }
        }

        public void SetWindow(Window window) => _window = window;

        public void LoadSettings()
        {
            try
            {
                var widthRange = _videoService.GetWidthRange();
                MinWidth = widthRange.Min;
                MaxWidth = widthRange.Max;
                Width = widthRange.Current;
                OnPropertyChanged(nameof(MinWidth));
                OnPropertyChanged(nameof(MaxWidth));
                OnPropertyChanged(nameof(Width));

                var heightRange = _videoService.GetHeightRange();
                MinHeight = heightRange.Min;
                MaxHeight = heightRange.Max;
                Height = heightRange.Current;
                OnPropertyChanged(nameof(MinHeight));
                OnPropertyChanged(nameof(MaxHeight));
                OnPropertyChanged(nameof(Height));

                var exposureRange = _videoService.GetExposureRange();
                MinExposure = exposureRange.Min;
                MaxExposure = exposureRange.Max;
                ExposureTime = exposureRange.Current;
                OnPropertyChanged(nameof(MinExposure));
                OnPropertyChanged(nameof(MaxExposure));
                OnPropertyChanged(nameof(ExposureTime));

                var gainRange = _videoService.GetGainRange();
                MinGain = gainRange.Min;
                MaxGain = gainRange.Max;
                Gain = gainRange.Current;
                OnPropertyChanged(nameof(MinGain));
                OnPropertyChanged(nameof(MaxGain));
                OnPropertyChanged(nameof(Gain));

                var fpsRange = _videoService.GetFrameRateRange();
                MinFrameRate = fpsRange.Min;
                MaxFrameRate = fpsRange.Max;
                AcquisitionFrameRate = fpsRange.Current;
                OnPropertyChanged(nameof(MinFrameRate));
                OnPropertyChanged(nameof(MaxFrameRate));
                OnPropertyChanged(nameof(AcquisitionFrameRate));

                var pixelFormats = _videoService.GetSupportedPixelFormats();
                PixelFormats.Clear();
                foreach (var fmt in pixelFormats.SupportedFormats)
                    PixelFormats.Add(fmt);
                PixelFormat = pixelFormats.CurrentFormat;
                OnPropertyChanged(nameof(PixelFormat));

                var expAuto = _videoService.GetExposureAutoModes();
                ExposureAutoModes.Clear();
                foreach (var mode in expAuto.SupportedModes)
                    ExposureAutoModes.Add(mode);
                ExposureAuto = expAuto.CurrentMode;
                OnPropertyChanged(nameof(ExposureAuto));

                var gainAuto = _videoService.GetGainAutoModes();
                GainAutoModes.Clear();
                foreach (var mode in gainAuto.SupportedModes)
                    GainAutoModes.Add(mode);
                GainAuto = gainAuto.CurrentMode;
                OnPropertyChanged(nameof(GainAuto));

                AcquisitionFrameRateEnable = _videoService.GetAcquisitionFrameRateEnable();
                OnPropertyChanged(nameof(AcquisitionFrameRateEnable));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}