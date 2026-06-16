using System.Text.Json.Serialization;

namespace PaperAngleMonitor.Models
{
    public class BaslerSettings
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 0;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 0;

        [JsonPropertyName("pixelFormat")]
        public string PixelFormat { get; set; } = "Mono12";

        [JsonPropertyName("exposureAuto")]
        public string ExposureAuto { get; set; } = "Once";

        [JsonPropertyName("exposureTime")]
        public double ExposureTime { get; set; } = 10000;

        [JsonPropertyName("gainAuto")]
        public string GainAuto { get; set; } = "Once";

        [JsonPropertyName("gain")]
        public int Gain { get; set; } = 0;

        [JsonPropertyName("acquisitionFrameRateEnable")]
        public bool AcquisitionFrameRateEnable { get; set; } = false;

        [JsonPropertyName("acquisitionFrameRate")]
        public double AcquisitionFrameRate { get; set; } = 30;

        public BaslerSettings Clone()
        {
            return new BaslerSettings
            {
                Width = this.Width,
                Height = this.Height,
                PixelFormat = this.PixelFormat,
                ExposureAuto = this.ExposureAuto,
                ExposureTime = this.ExposureTime,
                GainAuto = this.GainAuto,
                Gain = this.Gain,
                AcquisitionFrameRateEnable = this.AcquisitionFrameRateEnable,
                AcquisitionFrameRate = this.AcquisitionFrameRate
            };
        }

        public void CopyFrom(BaslerSettings source)
        {
            this.Width = source.Width;
            this.Height = source.Height;
            this.PixelFormat = source.PixelFormat;
            this.ExposureAuto = source.ExposureAuto;
            this.ExposureTime = source.ExposureTime;
            this.GainAuto = source.GainAuto;
            this.Gain = source.Gain;
            this.AcquisitionFrameRateEnable = source.AcquisitionFrameRateEnable;
            this.AcquisitionFrameRate = source.AcquisitionFrameRate;
        }
    }
}
