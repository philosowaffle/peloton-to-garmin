using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace P2G.Common.Dto.Strava
{
    /// <summary>
    /// Strava Activity model - represents a single activity from Strava API
    /// </summary>
    public record Activity
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public string Type { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime StartDateLocal { get; init; }
        public int ElapsedTime { get; init; }
        public double? Distance { get; init; }
        public double? TotalElevationGain { get; init; }
        public double? AverageSpeed { get; init; }
        public double? MaxSpeed { get; init; }
        public double? AverageWatts { get; init; }
        public double? MaxWatts { get; init; }
        public double? AverageHeartrate { get; init; }
        public double? MaxHeartrate { get; init; }
        public double? Calories { get; init; }
        public bool Manual { get; init; }
        public string Description { get; init; }
        public string SportType { get; init; }
        
        // Splits (сегменты по дистанции)
        public List<Split> SplitsMetric { get; init; }
        public List<Split> SplitsStandard { get; init; }
        
        // Best efforts
        public List<BestEffort> BestEfforts { get; init; }
        
        // Segments (сегменты из Strava)
        public List<SegmentEffort> SegmentEfforts { get; init; }
        
        // Gear info
        public Gear Gear { get; init; }
        
        // Location
        public string LocationCity { get; init; }
        public string LocationState { get; init; }
        public string LocationCountry { get; init; }
        
        // Additional metrics
        public double? AverageCadence { get; init; }
        public double? AverageTempo { get; init; }
        public int? PrCount { get; init; }
        public int? KudosCount { get; init; }
        public int? CommentCount { get; init; }
        public int? AthleteCount { get; init; }
        public int? PhotoCount { get; init; }
        public Map Map { get; init; }
        public DeviceName DeviceName { get; init; }
        public double? AverageGradeAdjustedSpeed { get; init; }
        public double? ElevHigh { get; init; }
        public double? ElevLow { get; init; }
        public string UploadIdStr { get; init; }
        public long? UploadId { get; init; }
        public int? WorkoutType { get; init; }
        public bool? SufferScore { get; init; }
        public bool HasKudoed { get; init; }
        public string ExternalId { get; init; }
        public int? Trainer { get; init; }
        public int? Commute { get; init; }
        public Guid? AthleteId { get; init; }
        public int? AverageWattsWeighted { get; init; }
    }

    /// <summary>
    /// Split - сегмент тренировки по дистанции (обычно 1 км или 1 миля)
    /// </summary>
    public record Split
    {
        public int SplitNumber { get; init; }
        public double Distance { get; init; }
        public int ElapsedTime { get; init; }
        public int MovingTime { get; init; }
        public double AverageSpeed { get; init; }
        public double? PaceZone { get; init; }
        public double? AverageHeartrate { get; init; }
        public int? AverageGradeAdjustedSpeed { get; init; }
        public double? AverageWatts { get; init; }
        public double ElevationDifference { get; init; }
        public double MinAverageSpeed { get; init; }
        public double MaxAverageSpeed { get; init; }
        public string Pace { get; init; }
    }

    /// <summary>
    /// Best effort - лучшие усилия на определённых дистанциях
    /// </summary>
    public record BestEffort
    {
        public string Name { get; init; }
        public int ElapsedTime { get; init; }
        public int MovingTime { get; init; }
        public double Distance { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime StartDateLocal { get; init; }
        public int Rank { get; init; }
    }

    /// <summary>
    /// Segment effort - прохождение конкретного сегмента
    /// </summary>
    public record SegmentEffort
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public int ElapsedTime { get; init; }
        public int MovingTime { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime StartDateLocal { get; init; }
        public double Distance { get; init; }
        public int StartIndex { get; init; }
        public int EndIndex { get; init; }
        public int Rank { get; init; }
        public bool IsKom { get; init; }
        public bool IsPr { get; init; }
        public double? AverageWatts { get; init; }
        public double? AverageHeartrate { get; init; }
        public Segment Segment { get; init; }
    }

    /// <summary>
    /// Segment - определение сегмента
    /// </summary>
    public record Segment
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public string ActivityType { get; init; }
        public double Distance { get; init; }
        public double AverageGrade { get; init; }
        public double MaximumGrade { get; init; }
        public double ElevationHigh { get; init; }
        public double ElevationLow { get; init; }
        public string City { get; init; }
        public string State { get; init; }
        public string Country { get; init; }
        public bool Private { get; init; }
        public bool Hazardous { get; init; }
        public bool Starred { get; init; }
    }

    /// <summary>
    /// Gear - информация о снаряжении (велосипед, обувь и т.д.)
    /// </summary>
    public record Gear
    {
        public string Id { get; init; }
        public bool Primary { get; init; }
        public string Name { get; init; }
        public double Distance { get; init; }
    }

    /// <summary>
    /// Map - карта маршрута
    /// </summary>
    public record Map
    {
        public string Id { get; init; }
        public string Polyline { get; init; }
        public string SummaryPolyline { get; init; }
    }

    /// <summary>
    /// Device name - устройство записи
    /// </summary>
    public record DeviceName
    {
        public string Value { get; init; }
    }
}
