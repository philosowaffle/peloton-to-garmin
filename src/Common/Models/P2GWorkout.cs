using System;
using System.Collections.Generic;

namespace P2G.Common.Models
{
    /// <summary>
    /// P2GWorkout - универсальная модель тренировки для конвертации в различные форматы
    /// </summary>
    public class P2GWorkout
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationInSeconds { get; set; }
        public WorkoutType WorkoutType { get; set; }
        public string Source { get; set; }
        public WorkoutMetrics Metrics { get; set; }
        public List<WorkoutPoint> Points { get; set; }
        public List<WorkoutSplit> Splits { get; set; }
        public List<WorkoutSegment> Segments { get; set; }
        public string Description { get; set; }
        public string LocationCity { get; set; }
        public string LocationState { get; set; }
        public string LocationCountry { get; set; }
        public string GearName { get; set; }
        public string DeviceName { get; set; }
    }

    /// <summary>
    /// WorkoutMetrics - сводные метрики тренировки
    /// </summary>
    public class WorkoutMetrics
    {
        public double? Distance { get; set; } // км
        public double? ElevationGain { get; set; } // метры
        public double? AverageSpeed { get; set; } // км/ч
        public double? MaxSpeed { get; set; } // км/ч
        public double? AveragePower { get; set; } // ватты
        public double? MaxPower { get; set; } // ватты
        public double? AverageHeartRate { get; set; } // уд/мин
        public double? MaxHeartRate { get; set; } // уд/мин
        public double? Calories { get; set; } // ккал
        public double? AverageCadence { get; set; } // об/мин
        public double? AverageTempo { get; set; } // темп
        public double? SufferScore { get; set; } // Strava suffer score
    }

    /// <summary>
    /// WorkoutPoint - точка данных временного ряда тренировки
    /// </summary>
    public class WorkoutPoint
    {
        public int TimeOffset { get; set; } // секунды от начала
        public double? Distance { get; set; } // метры от начала
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; } // метры
        public double? HeartRate { get; set; } // уд/мин
        public double? Power { get; set; } // ватты
        public double? Cadence { get; set; } // об/мин
        public double? Speed { get; set; } // км/ч
        public double? Temperature { get; set; } // градусы Цельсия
        public double? Grade { get; set; } // процент уклона
    }

    /// <summary>
    /// WorkoutSplit - сегмент тренировки по дистанции (сплит)
    /// </summary>
    public class WorkoutSplit
    {
        public int SplitNumber { get; set; }
        public double Distance { get; set; } // км
        public int DurationInSeconds { get; set; }
        public double? AverageSpeed { get; set; } // км/ч
        public double? AverageHeartRate { get; set; } // уд/мин
        public double? AveragePower { get; set; } // ватты
        public string Pace { get; set; } // темп (например "5:30" мин/км)
    }

    /// <summary>
    /// WorkoutSegment - сегмент из Strava (прохождение конкретного участка)
    /// </summary>
    public class WorkoutSegment
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; } // метры
        public int ElapsedTime { get; set; } // секунды
        public int MovingTime { get; set; } // секунды
        public double AverageSpeed { get; set; } // м/с
        public double? AveragePower { get; set; } // ватты
        public double? AverageHeartRate { get; set; } // уд/мин
        public int Rank { get; set; }
        public bool IsKom { get; set; }
        public bool IsPr { get; set; }
        public double AverageGrade { get; set; } // процент
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }
}
