using OpenCvSharp;
using PaperAngleMonitor.Models;

namespace PaperAngleMonitor.Services
{
    /// <summary>
    /// Интерфейс для сервиса видеозахвата, предоставляющий методы управления камерой,
    /// получения кадров и настройки параметров.
    /// </summary>
    public interface IVideoService : IDisposable
    {
        /// <summary>
        /// Событие возникает при получении нового кадра.
        /// </summary>
        event Action<Mat> FrameReady;

        /// <summary>
        /// Указывает, подключена ли камера в данный момент.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Текущая частота кадров (FPS).
        /// </summary>
        double CurrentFps { get; }

        /// <summary>
        /// Асинхронно подключается к камере.
        /// </summary>
        /// <returns>True в случае успешного подключения, иначе False.</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Асинхронно отключает камеру и освобождает ресурсы.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Запускает захват видеопотока.
        /// </summary>
        Task StartCaptureAsync();

        /// <summary>
        /// Останавливает захват видеопотока.
        /// </summary>
        Task StopCaptureAsync();

        /// <summary>
        /// Применяет настройки камеры.
        /// </summary>
        /// <param name="settings">Объект с параметрами камеры.</param>
        void ApplySettings(BaslerSettings settings);

        /// <summary>
        /// Возвращает диапазон и текущее значение экспозиции (в микросекундах).
        /// </summary>
        (double Min, double Max, double Current) GetExposureRange();

        /// <summary>
        /// Возвращает диапазон и текущее значение усиления (в сырых единицах).
        /// </summary>
        (double Min, double Max, double Current) GetGainRange();

        /// <summary>
        /// Возвращает диапазон и текущее значение частоты кадров (кадров/сек).
        /// </summary>
        (double Min, double Max, double Current) GetFrameRateRange();

        /// <summary>
        /// Возвращает диапазон и текущее значение ширины изображения (в пикселях).
        /// </summary>
        (int Min, int Max, int Current) GetWidthRange();

        /// <summary>
        /// Возвращает диапазон и текущее значение высоты изображения (в пикселях).
        /// </summary>
        (int Min, int Max, int Current) GetHeightRange();

        /// <summary>
        /// Возвращает список поддерживаемых форматов пикселей и текущий формат.
        /// </summary>
        (List<string> SupportedFormats, string CurrentFormat) GetSupportedPixelFormats();

        /// <summary>
        /// Возвращает список поддерживаемых режимов автоматической экспозиции и текущий режим.
        /// </summary>
        /// <returns>Кортеж: список режимов (строки) и текущий режим.</returns>
        (List<string> SupportedModes, string CurrentMode) GetExposureAutoModes();

        /// <summary>
        /// Возвращает список поддерживаемых режимов автоматического усиления и текущий режим.
        /// </summary>
        /// <returns>Кортеж: список режимов (строки) и текущий режим.</returns>
        (List<string> SupportedModes, string CurrentMode) GetGainAutoModes();

        /// <summary>
        /// Возвращает текущее состояние флага включения управления частотой кадров.
        /// </summary>
        /// <returns>True – управление частотой кадров включено, False – выключено.</returns>
        bool GetAcquisitionFrameRateEnable();
    }
}