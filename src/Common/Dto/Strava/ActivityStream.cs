using System.Collections.Generic;

namespace P2G.Common.Dto.Strava
{
    /// <summary>
    /// Activity Stream - поток данных активности (временные ряды метрик)
    /// </summary>
    public record ActivityStream
    {
        /// <summary>
        /// Тип потока: time, distance, latlng, altitude, heartrate, cadence, watts, temp, pace_grade, speed
        /// </summary>
        public string Type { get; init; }
        
        /// <summary>
        /// Данные потока
        /// </summary>
        public List<object> Data { get; init; }
        
        /// <summary>
        /// Размерность данных
        /// </summary>
        public string SeriesType { get; init; }
        
        /// <summary>
        /// Количество точек данных
        /// </summary>
        public int OriginalSize { get; init; }
        
        /// <summary>
        /// Разрешение данных
        /// </summary>
        public string Resolution { get; init; }
    }

    /// <summary>
    /// Типы потоков Strava
    /// </summary>
    public static class StreamTypes
    {
        public const string Time = "time";
        public const string Distance = "distance";
        public const string LatLng = "latlng";
        public const string Altitude = "altitude";
        public const string Heartrate = "heartrate";
        public const string Cadence = "cadence";
        public const string Watts = "watts";
        public const string Temp = "temp";
        public const string PaceGrade = "pace_grade";
        public const string Speed = "velocity_smooth";
        public const string Grade = "grade_smooth";
    }
}
