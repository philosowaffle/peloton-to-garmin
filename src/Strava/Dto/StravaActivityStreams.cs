using System;
using System.Text.Json.Serialization;

namespace P2G.Strava.Dto
{
    /// <summary>
    /// Типы потоков данных Strava
    /// </summary>
    public enum StreamType
    {
        Time,
        Distance,
        Latlng,
        Altitude,
        VelocitySmooth,
        Heartrate,
        Cadence,
        Watts,
        Temp,
        Moving,
        GradeSmooth
    }

    /// <summary>
    /// Базовый класс для потока данных
    /// </summary>
    public class StravaStream
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object[] Data { get; set; } = Array.Empty<object>();

        [JsonPropertyName("series_type")]
        public string? SeriesType { get; set; }

        [JsonPropertyName("original_size")]
        public int OriginalSize { get; set; }

        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; } // "low", "medium", "high"
    }

    /// <summary>
    /// Поток времени (секунды от начала активности)
    /// </summary>
    public class TimeStream : StravaStream
    {
        public int[] TimeData => Array.ConvertAll(Data, x => Convert.ToInt32(x));
    }

    /// <summary>
    /// Поток дистанции (метры от начала активности)
    /// </summary>
    public class DistanceStream : StravaStream
    {
        public double[] DistanceData => Array.ConvertAll(Data, x => Convert.ToDouble(x));
    }

    /// <summary>
    /// Поток координат GPS [широта, долгота]
    /// </summary>
    public class LatlngStream : StravaStream
    {
        public double[][] LatLngData
        {
            get
            {
                var result = new double[Data.Length][];
                for (int i = 0; i < Data.Length; i++)
                {
                    if (Data[i] is System.Text.Json.JsonElement element && element.TryGetArray(out var arr))
                    {
                        result[i] = new double[2];
                        int idx = 0;
                        foreach (var item in arr.Enumerate())
                        {
                            if (idx < 2)
                                result[i][idx++] = item.GetDouble();
                        }
                    }
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Поток высоты (метры над уровнем моря)
    /// </summary>
    public class AltitudeStream : StravaStream
    {
        public double[] AltitudeData => Array.ConvertAll(Data, x => Convert.ToDouble(x));
    }

    /// <summary>
    /// Поток скорости (м/с)
    /// </summary>
    public class VelocityStream : StravaStream
    {
        public double[] VelocityData => Array.ConvertAll(Data, x => Convert.ToDouble(x));
    }

    /// <summary>
    /// Поток пульса (уд/мин)
    /// </summary>
    public class HeartrateStream : StravaStream
    {
        public int[] HeartrateData => Array.ConvertAll(Data, x => Convert.ToInt32(x));
    }

    /// <summary>
    /// Поток каденса (об/мин)
    /// </summary>
    public class CadenceStream : StravaStream
    {
        public int[] CadenceData => Array.ConvertAll(Data, x => Convert.ToInt32(x));
    }

    /// <summary>
    /// Поток мощности (ватты)
    /// </summary>
    public class WattsStream : StravaStream
    {
        public int[] WattsData => Array.ConvertAll(Data, x => Convert.ToInt32(x));
    }

    /// <summary>
    /// Поток температуры (градусы Цельсия)
    /// </summary>
    public class TempStream : StravaStream
    {
        public int[] TempData => Array.ConvertAll(Data, x => Convert.ToInt32(x));
    }

    /// <summary>
    /// Поток движения (true если двигался)
    /// </summary>
    public class MovingStream : StravaStream
    {
        public bool[] MovingData => Array.ConvertAll(Data, x => Convert.ToBoolean(x));
    }

    /// <summary>
    /// Поток уклона (проценты)
    /// </summary>
    public class GradeStream : StravaStream
    {
        public double[] GradeData => Array.ConvertAll(Data, x => Convert.ToDouble(x));
    }

    /// <summary>
    /// Модель активности с потоками данных
    /// </summary>
    public class StravaActivityWithStreams : StravaActivity
    {
        [JsonPropertyName("streams")]
        public StravaStream[]? Streams { get; set; }

        /// <summary>
        /// Получить поток времени
        /// </summary>
        public TimeStream? GetTimeStream() => Streams?.OfType<TimeStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток дистанции
        /// </summary>
        public DistanceStream? GetDistanceStream() => Streams?.OfType<DistanceStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток GPS координат
        /// </summary>
        public LatlngStream? GetLatLngStream() => Streams?.OfType<LatlngStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток высоты
        /// </summary>
        public AltitudeStream? GetAltitudeStream() => Streams?.OfType<AltitudeStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток скорости
        /// </summary>
        public VelocityStream? GetVelocityStream() => Streams?.OfType<VelocityStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток пульса
        /// </summary>
        public HeartrateStream? GetHeartrateStream() => Streams?.OfType<HeartrateStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток каденса
        /// </summary>
        public CadenceStream? GetCadenceStream() => Streams?.OfType<CadenceStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток мощности
        /// </summary>
        public WattsStream? GetWattsStream() => Streams?.OfType<WattsStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток температуры
        /// </summary>
        public TempStream? GetTempStream() => Streams?.OfType<TempStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток движения
        /// </summary>
        public MovingStream? GetMovingStream() => Streams?.OfType<MovingStream>().FirstOrDefault();

        /// <summary>
        /// Получить поток уклона
        /// </summary>
        public GradeStream? GetGradeStream() => Streams?.OfType<GradeStream>().FirstOrDefault();
    }
}
