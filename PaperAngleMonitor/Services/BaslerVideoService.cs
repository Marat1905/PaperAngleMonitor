using Basler.Pylon;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Text;
using PaperAngleMonitor.Models;

namespace PaperAngleMonitor.Services
{
    /// <summary>
    /// Реализация IVideoService для камер Basler на базе Pylon .NET SDK.
    /// </summary>
    public class BaslerVideoService : IVideoService
    {
        private readonly ILogger<BaslerVideoService> _logger;
        private readonly BaslerSettings _settings;
        private Camera? _camera;
        private bool _isCapturing;
        private int _frameCount;
        private DateTime _lastFpsUpdate;
        private double _currentFps;

        public event Action<Mat>? FrameReady;
        public bool IsConnected => _camera?.IsConnected == true;
        public double CurrentFps => _currentFps;

        private Size _frameSize;

        public BaslerVideoService(ILogger<BaslerVideoService> logger, BaslerSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _frameCount = 0;
            _lastFpsUpdate = DateTime.Now;
            _currentFps = 0;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _logger.LogInformation("Searching for Basler cameras...");

                // Проверка доступности камер
                var cameras = CameraFinder.Enumerate().ToList();
                if (cameras.Count == 0)
                {
                    _logger.LogWarning("No Basler cameras found");
                    return false;
                }

                _logger.LogInformation($"Found {cameras.Count} camera(s)");

                // Выбор камеры
                var cameraInfo = cameras.First();
                _camera = new Camera(cameraInfo);

                // ОТЛАДКА: Вывод информации о камере
                _logger.LogInformation($"Camera Model: {cameraInfo[CameraInfoKey.ModelName]}");
                _logger.LogInformation($"Camera Serial: {cameraInfo[CameraInfoKey.SerialNumber]}");
                _logger.LogInformation($"Camera Vendor: {cameraInfo[CameraInfoKey.VendorName]}");

                // Открываем камеру
                _camera.Open();

                // ОТЛАДКА: Проверка состояния камеры
                _logger.LogInformation($"Camera connected: {_camera.IsConnected}");
                _logger.LogInformation($"Camera opened: {_camera.IsOpen}");

                // Настройка параметров камеры
                ConfigureCameraSettings();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Basler camera");
                return false;
            }
        }

        private void ConfigureCameraSettings()
        {
            if (_camera == null) return;

            try
            {
                // Сбрасываем настройки к default
                //_camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.Default);
                //_camera.Parameters[PLCamera.UserSetLoad].Execute();

                // Базовые настройки
                //_camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);


                // Автоматические настройки для начала
                //падает fps при больших значениях
                //_camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Once);
                //При таких настройках нормально работает
                _camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Off);
                _camera.Parameters[PLCamera.ExposureTimeAbs].SetValue(16000.0);

                _camera.Parameters[PLCamera.GainAuto].SetValue(PLCamera.GainAuto.Continuous);
                _camera.Parameters[PLCamera.GainSelector].SetValue(PLCamera.GainSelector.All);

                // Формат пикселей
                _camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);

                // Настройка размера изображения (максимальный)
                _camera.Parameters[PLCamera.Width].SetValue(_camera.Parameters[PLCamera.Width].GetMaximum());
                _camera.Parameters[PLCamera.Height].SetValue(_camera.Parameters[PLCamera.Height].GetMaximum());

                _logger.LogInformation("Camera configured successfully");

                // ОТЛАДКА: Вывод текущих параметров
                _logger.LogInformation($"Width: {_camera.Parameters[PLCamera.Width].GetValue()}");
                _logger.LogInformation($"Height: {_camera.Parameters[PLCamera.Height].GetValue()}");
                _logger.LogInformation($"PixelFormat: {_camera.Parameters[PLCamera.PixelFormat].GetValue()}");

                // _camera.Parameters[PLCamera.BalanceWhiteAuto].SetValue(PLCamera.BalanceWhiteAuto.Once);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring camera settings");
            }
        }

        public void ApplySettings(BaslerSettings settings)
        {
            if (_camera == null || !_camera.IsConnected) return;
            try
            {
                // Настройка размера изображения
                if (settings.Width > 0 && settings.Height > 0)
                {
                    _camera.Parameters[PLCamera.Width].SetValue(settings.Width);
                    _camera.Parameters[PLCamera.Height].SetValue(settings.Height);
                }

                // Настройка формата пикселей
                _camera.Parameters[PLCamera.PixelFormat].SetValue(settings.PixelFormat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply camera settings (size/format)");
            }

            try
            {
                // Настройка экспозиции
                _camera.Parameters[PLCamera.ExposureAuto].SetValue(settings.ExposureAuto);
                if (settings.ExposureAuto == "Off")
                {
                    _camera.Parameters[PLCamera.ExposureTimeAbs].SetValue(settings.ExposureTime);
                }

                // Настройка усиления
                _camera.Parameters[PLCamera.GainAuto].SetValue(settings.GainAuto);
                if (settings.GainAuto == "Off")
                {
                    _camera.Parameters[PLCamera.GainRaw].SetValue(settings.Gain);
                }

                // Настройка частоты кадров
                _camera.Parameters[PLCamera.AcquisitionFrameRateEnable].SetValue(
                    settings.AcquisitionFrameRateEnable);

                if (settings.AcquisitionFrameRateEnable)
                {
                    _camera.Parameters[PLCamera.AcquisitionFrameRateAbs].SetValue(
                        settings.AcquisitionFrameRate);
                }

                _logger.LogInformation("Camera settings applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply camera settings (exposure/gain/fps)");
                throw;
            }
        }

        public (double Min, double Max, double Current) GetFrameRateRange()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.AcquisitionFrameRateAbs];
                return (param.GetMinimum(), param.GetMaximum(), param.GetValue());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get frame rate range");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            await StopCaptureAsync();

            try
            {
                if (_camera != null)
                {
                    if (_camera.StreamGrabber.IsGrabbing)
                    {
                        _camera.StreamGrabber.Stop();
                    }

                    _camera.Close();
                    _camera.Dispose();
                    _camera = null;
                }
                _logger.LogInformation("Basler camera disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during camera disconnection");
            }
        }

        public async Task StartCaptureAsync()
        {
            if (_camera == null || !_camera.IsConnected)
            {
                _logger.LogWarning("Camera is not connected");
                return;
            }

            if (_isCapturing) return;

            try
            {
                // Останавливаем grabber если уже запущен
                if (_camera.StreamGrabber.IsGrabbing)
                {
                    _camera.StreamGrabber.Stop();
                }

                // Сбрасываем счетчики FPS
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
                _currentFps = 0;

                // Подписываемся на события
                _camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;
                _camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                // Запускаем захват
                _camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                _isCapturing = true;

                _logger.LogInformation("Capture started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start capture");
            }
        }

        public async Task StopCaptureAsync()
        {
            if (_camera == null || !_isCapturing) return;

            try
            {
                _camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;
                _camera.StreamGrabber.Stop();
                _isCapturing = false;
                _logger.LogInformation("Stopped video capture");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop video capture");
            }
        }

        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                using (var grabResult = e.GrabResult)
                {
                    if (!grabResult.IsValid) return;

                    _logger.LogDebug($"Basler frame - Size: {grabResult.Width}x{grabResult.Height}, " +
                                   $"PixelFormat: {grabResult.PixelTypeValue}, Valid: {grabResult.IsValid}");

                    // Обновляем счетчик FPS
                    _frameCount++;
                    var now = DateTime.Now;
                    var elapsed = (now - _lastFpsUpdate).TotalSeconds;

                    if (elapsed >= 1.0)
                    {
                        _currentFps = _frameCount / elapsed;
                        _frameCount = 0;
                        _lastFpsUpdate = now;
                        _logger.LogDebug($"Current FPS: {_currentFps:F2}");
                    }

                    // Convert grab result to OpenCV Mat
                    using (var converter = new PixelDataConverter())
                    {
                        // Проверяем формат пикселей
                        Mat colorMat;
                        if (grabResult.PixelTypeValue == PixelType.Mono8)
                        {
                            // _logger.LogWarning("Basler camera is in Mono8 mode. Converting to BGR.");

                            // Для Mono8 создаем одноканальный Mat и конвертируем в BGR
                            int bufferSize = grabResult.Width * grabResult.Height;
                            var buffer = new byte[bufferSize];
                            converter.OutputPixelFormat = PixelType.Mono8;
                            converter.Convert(buffer, grabResult);

                            using (var monoMat = Mat.FromPixelData(grabResult.Height, grabResult.Width, MatType.CV_8UC1, buffer))
                            {
                                colorMat = new Mat();
                                Cv2.CvtColor(monoMat, colorMat, ColorConversionCodes.GRAY2BGR);
                            }
                        }
                        else
                        {
                            // Для цветных форматов используем стандартную обработку
                            int bufferSize = grabResult.Width * grabResult.Height * 3;
                            var buffer = new byte[bufferSize];
                            converter.OutputPixelFormat = PixelType.BGR8packed;
                            converter.Convert(buffer, grabResult);

                            colorMat = Mat.FromPixelData(grabResult.Height, grabResult.Width, MatType.CV_8UC3, buffer);
                        }

                        // Сохраняем размер кадра для записи
                        _frameSize = colorMat.Size();

                        // Отладочное сохранение кадра
                        //Cv2.ImWrite($"debug_basler_{DateTime.Now:HHmmss_fff}.jpg", colorMat);
                        //_logger.LogInformation($"Basler output - Channels: {colorMat.Channels()}, Type: {colorMat.Type()}");

                        FrameReady?.Invoke(colorMat);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Basler camera frame");
            }
        }

        #region Настройки с возвратом текущего значения

        public (int Min, int Max, int Current) GetWidthRange()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.Width];
                return ((int)param.GetMinimum(), (int)param.GetMaximum(), (int)param.GetValue());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get width range");
                throw;
            }
        }

        public (int Min, int Max, int Current) GetHeightRange()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.Height];
                return ((int)param.GetMinimum(), (int)param.GetMaximum(), (int)param.GetValue());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get height range");
                throw;
            }
        }

        public (List<string> SupportedFormats, string CurrentFormat) GetSupportedPixelFormats()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.PixelFormat];
                // Получаем все возможные значения (это может быть массив строк или объектов)
                var allValues = param.GetAllValues(); // предположим, возвращает object[]
                var supported = new List<string>();
                var currentValue = param.GetValue(); // запоминаем текущий формат

                foreach (var value in allValues)
                {
                    try
                    {
                        // Пытаемся установить значение
                        param.SetValue(value);
                        // Если исключения не было – формат поддерживается
                        supported.Add(value.ToString());
                    }
                    catch
                    {
                        // Если возникла ошибка – пропускаем
                    }
                }

                // Возвращаем исходный формат
                param.SetValue(currentValue);

                return (supported, currentValue.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get supported pixel formats");
                throw;
            }
        }

        public (double Min, double Max, double Current) GetExposureRange()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.ExposureTimeAbs];
                return (param.GetMinimum(), param.GetMaximum(), param.GetValue());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get exposure range");
                throw;
            }
        }

        public (double Min, double Max, double Current) GetGainRange()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.GainRaw];
                return (param.GetMinimum(), param.GetMaximum(), param.GetValue());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get gain range");
                throw;
            }
        }

        #endregion

        #region Новые методы для авторежимов и управления частотой кадров

        /// <summary>
        /// Возвращает список поддерживаемых режимов автоматической экспозиции и текущий режим.
        /// </summary>
        /// <returns>Кортеж: список режимов (строки) и текущий режим.</returns>
        /// <exception cref="InvalidOperationException">Если камера не подключена.</exception>
        public (List<string> SupportedModes, string CurrentMode) GetExposureAutoModes()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.ExposureAuto];
                // GetSymbolics() возвращает IList<string> всех допустимых символьных имён для перечисляемого параметра
                var supported = param.GetAllValues().ToList();
                var current = param.GetValue().ToString(); // или param.GetSymbolic()
                return (supported, current);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get exposure auto modes");
                throw;
            }
        }

        /// <summary>
        /// Возвращает список поддерживаемых режимов автоматического усиления и текущий режим.
        /// </summary>
        /// <returns>Кортеж: список режимов (строки) и текущий режим.</returns>
        /// <exception cref="InvalidOperationException">Если камера не подключена.</exception>
        public (List<string> SupportedModes, string CurrentMode) GetGainAutoModes()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                var param = _camera.Parameters[PLCamera.GainAuto];
                var supported = param.GetAllValues().ToList();
                var current = param.GetValue().ToString();
                return (supported, current);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get gain auto modes");
                throw;
            }
        }

        /// <summary>
        /// Возвращает текущее состояние флага включения управления частотой кадров.
        /// </summary>
        /// <returns>True – управление частотой кадров включено, False – выключено.</returns>
        /// <exception cref="InvalidOperationException">Если камера не подключена.</exception>
        public bool GetAcquisitionFrameRateEnable()
        {
            if (_camera == null || !_camera.IsConnected)
                throw new InvalidOperationException("Camera not connected");

            try
            {
                return _camera.Parameters[PLCamera.AcquisitionFrameRateEnable].GetValue();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get acquisition frame rate enable state");
                throw;
            }
        }

        #endregion

        public void Dispose()
        {
            try
            {
                DisconnectAsync().Wait();
                _camera?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during camera disposal");
            }
        }

        public static string GetPylonVersionInfo()
        {
            try
            {
                var assembly = typeof(Camera).Assembly;
                return $"Basler Pylon .NET SDK Version: {assembly.GetName().Version}";
            }
            catch
            {
                return "Unable to determine Pylon version";
            }
        }
    }
}