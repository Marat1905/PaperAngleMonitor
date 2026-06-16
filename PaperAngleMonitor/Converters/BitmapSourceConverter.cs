using OpenCvSharp;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PaperAngleMonitor.Converters
{
    public static class BitmapSourceConverter
    {
        public static BitmapSource ToBitmapSource(Mat mat)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat.Empty())
                return null;

            try
            {
                PixelFormat format;
                var matType = mat.Type();

                // Используем if-else вместо switch, так как MatType не является enum
                if (matType == MatType.CV_8UC1)
                {
                    format = PixelFormats.Gray8;
                }
                else if (matType == MatType.CV_8UC3)
                {
                    format = PixelFormats.Bgr24;
                }
                else if (matType == MatType.CV_8UC4)
                {
                    format = PixelFormats.Bgra32;
                }
                else
                {
                    throw new NotSupportedException($"Unsupported Mat type: {matType}");
                }

                int width = mat.Width;
                int height = mat.Height;
                int step = (int)mat.Step();
                IntPtr data = mat.Data;

                // Создаем WriteableBitmap
                var bitmap = new WriteableBitmap(width, height, 96, 96, format, null);

                bitmap.Lock();

                try
                {
                    IntPtr buffer = bitmap.BackBuffer;
                    int bufferSize = height * step;

                    if (step == width * format.BitsPerPixel / 8)
                    {
                        // Копируем данные целиком
                        NativeMethods.memcpy(buffer, data, (uint)bufferSize);
                    }
                    else
                    {
                        // Копируем построчно
                        for (int y = 0; y < height; y++)
                        {
                            IntPtr src = new IntPtr(data.ToInt64() + y * step);
                            IntPtr dst = new IntPtr(buffer.ToInt64() + y * bitmap.BackBufferStride);
                            NativeMethods.memcpy(dst, src, (uint)(width * format.BitsPerPixel / 8));
                        }
                    }

                    bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                }
                finally
                {
                    bitmap.Unlock();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert Mat to BitmapSource", ex);
            }
        }

        /// <summary>
        /// Конвертирует OpenCV Mat в WPF BitmapSource с максимальной производительностью.
        /// </summary>
        /// <param name="mat">Исходная матрица OpenCV.</param>
        /// <returns>Готовый для отображения в WPF BitmapSource или null, если матрица пуста.</returns>
        public static BitmapSource ToBitmapSourceUnsafe(this Mat mat)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));

            if (mat.Empty())
                return null;

            // Определяем формат пикселей WPF на основе типа матрицы OpenCV
            PixelFormat format = mat.Type() switch
            {
                var t when t == MatType.CV_8UC1 => PixelFormats.Gray8,
                var t when t == MatType.CV_8UC3 => PixelFormats.Bgr24,
                var t when t == MatType.CV_8UC4 => PixelFormats.Bgra32,
                _ => throw new NotSupportedException($"Указанный тип Mat ({mat.Type()}) не поддерживается для конвертации в BitmapSource.")
            };

            int width = mat.Width;
            int height = mat.Height;
            int step = (int)mat.Step(); // Длина строки в байтах внутри OpenCV (с учетом выравнивания)

            int bytesPerPixel = format.BitsPerPixel / 8;
            int stride = width * bytesPerPixel; // Идеальная длина строки без выравнивания

            // Создаем WriteableBitmap. Параметры 96, 96 — стандартный DPI для WPF.
            var bitmap = new WriteableBitmap(width, height, 96, 96, format, null);
            bitmap.Lock();

            try
            {
                // Используем небезопасный код для прямого доступа к указателям памяти
                unsafe
                {
                    byte* srcPtr = (byte*)mat.Data.ToPointer();
                    byte* dstPtr = (byte*)bitmap.BackBuffer.ToPointer();

                    // Если шаг строк совпадает, копируем весь массив данных одним махом
                    if (step == stride && step == bitmap.BackBufferStride)
                    {
                        long totalBytes = (long)step * height;
                        Buffer.MemoryCopy(srcPtr, dstPtr, totalBytes, totalBytes);
                    }
                    else
                    {
                        // Если есть разница в выравнивании строк (padding), копируем построчно
                        for (int y = 0; y < height; y++)
                        {
                            long srcOffset = (long)y * step;
                            long dstOffset = (long)y * bitmap.BackBufferStride;

                            Buffer.MemoryCopy(
                                source: srcPtr + srcOffset,
                                destination: dstPtr + dstOffset,
                                destinationSizeInBytes: stride,
                                sourceBytesToCopy: stride
                            );
                        }
                    }
                }

                // Уведомляем WPF, что вся область изображения была обновлена
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Критическая ошибка при копировании памяти из Mat в WriteableBitmap.", ex);
            }
            finally
            {
                bitmap.Unlock();
            }

            // Замораживаем объект, если он был создан в фоновом потоке.
            // Это делает его потокобезопасным и позволяет передать в UI-поток без ошибок.
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }

        // Альтернативный метод через MemoryStream
        public static BitmapSource ToBitmapSourceAlternative(Mat mat)
        {
            if (mat == null || mat.Empty())
                return null;

            try
            {
                // Конвертируем Mat в массив байтов
                byte[] imageBytes = mat.ToBytes(".jpg");

                using (var memory = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch
            {
                // Fallback: используем BMP если JPEG не сработал
                try
                {
                    byte[] imageBytes = mat.ToBytes(".bmp");

                    using (var memory = new System.IO.MemoryStream(imageBytes))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        return bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to convert Mat to BitmapSource", ex);
                }
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
    }
}
