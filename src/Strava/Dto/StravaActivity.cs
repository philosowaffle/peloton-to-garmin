using System;
using System.Text.Json.Serialization;

namespace P2G.Strava.Dto
{
    /// <summary>
    /// Базовая модель активности Strava (краткая информация из списка)
    /// </summary>
    public class StravaActivity
    {
        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("athlete")]
        public AthleteMeta? Athlete { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public double Distance { get; set; } // в метрах

        [JsonPropertyName("moving_time")]
        public int MovingTime { get; set; } // в секундах

        [JsonPropertyName("elapsed_time")]
        public int ElapsedTime { get; set; } // в секундах

        [JsonPropertyName("total_elevation_gain")]
        public double TotalElevationGain { get; set; } // в метрах

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("sport_type")]
        public string SportType { get; set; } = string.Empty;

        [JsonPropertyName("workout_type")]
        public int? WorkoutType { get; set; } // 0-10 для бега, 0-3 для велосипеда

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("start_date_local")]
        public DateTime StartDateLocal { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("utc_offset")]
        public double UtcOffset { get; set; }

        [JsonPropertyName("location_city")]
        public string? LocationCity { get; set; }

        [JsonPropertyName("location_state")]
        public string? LocationState { get; set; }

        [JsonPropertyName("location_country")]
        public string? LocationCountry { get; set; }

        [JsonPropertyName("achievement_count")]
        public int AchievementCount { get; set; }

        [JsonPropertyName("kudos_count")]
        public int KudosCount { get; set; }

        [JsonPropertyName("comment_count")]
        public int CommentCount { get; set; }

        [JsonPropertyName("athlete_count")]
        public int AthleteCount { get; set; }

        [JsonPropertyName("photo_count")]
        public int PhotoCount { get; set; }

        [JsonPropertyName("map")]
        public MapSummary? Map { get; set; }

        [JsonPropertyName("trainer")]
        public bool Trainer { get; set; } // true если тренировка на тренажёре

        [JsonPropertyName("commute")]
        public bool Commute { get; set; } // true если поездка на работу

        [JsonPropertyName("manual")]
        public bool Manual { get; set; } // true если создана вручную

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; } // "everyone", "followers_only", "only_me"

        [JsonPropertyName("flagged")]
        public bool Flagged { get; set; }

        [JsonPropertyName("gear_id")]
        public string? GearId { get; set; }

        [JsonPropertyName("start_latlng")]
        public double[]? StartLatLng { get; set; } // [широта, долгота]

        [JsonPropertyName("end_latlng")]
        public double[]? EndLatLng { get; set; } // [широта, долгота]

        [JsonPropertyName("average_speed")]
        public double AverageSpeed { get; set; } // м/с

        [JsonPropertyName("max_speed")]
        public double MaxSpeed { get; set; } // м/с

        [JsonPropertyName("has_heartrate")]
        public bool HasHeartrate { get; set; }

        [JsonPropertyName("average_heartrate")]
        public double? AverageHeartrate { get; set; }

        [JsonPropertyName("max_heartrate")]
        public double? MaxHeartrate { get; set; }

        [JsonPropertyName("heartrate_opt_out")]
        public bool HeartrateOptOut { get; set; }

        [JsonPropertyName("display_hide_heartrate_option")]
        public bool DisplayHideHeartrateOption { get; set; }

        [JsonPropertyName("elev_high")]
        public double? ElevHigh { get; set; }

        [JsonPropertyName("elev_low")]
        public double? ElevLow { get; set; }

        [JsonPropertyName("upload_id")]
        public long? UploadId { get; set; }

        [JsonPropertyName("upload_id_str")]
        public string? UploadIdStr { get; set; }

        [JsonPropertyName("external_id")]
        public string? ExternalId { get; set; }

        [JsonPropertyName("from_accepted_tag")]
        public bool FromAcceptedTag { get; set; }

        [JsonPropertyName("pr_count")]
        public int PrCount { get; set; }

        [JsonPropertyName("total_photo_count")]
        public int TotalPhotoCount { get; set; }

        [JsonPropertyName("has_kudoed")]
        public bool HasKudoed { get; set; }

        [JsonPropertyName("suffer_score")]
        public int? SufferScore { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("calories")]
        public double? Calories { get; set; }

        [JsonPropertyName("segment_efforts")]
        public SegmentEffort[]? SegmentEfforts { get; set; }

        [JsonPropertyName("best_efforts")]
        public BestEffort[]? BestEfforts { get; set; }

        /// <summary>
        /// Определяет тип активности как enum
        /// </summary>
        public StravaActivityType ActivityType => StravaActivityTypeExtensions.Parse(SportType);
    }

    /// <summary>
    /// Мета-данные атлета (вложенный объект)
    /// </summary>
    public class AthleteMeta
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }
    }

    /// <summary>
    /// Краткая информация о карте маршрута
    /// </summary>
    public class MapSummary
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("polyline")]
        public string? Polyline { get; set; } // Encoded polyline

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("summary_polyline")]
        public string? SummaryPolyline { get; set; } // Сжатая версия для списков
    }

    /// <summary>
    /// Усилия на сегментах
    /// </summary>
    public class SegmentEffort
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("activity")]
        public ActivityMeta? Activity { get; set; }

        [JsonPropertyName("athlete")]
        public AthleteMeta? Athlete { get; set; }

        [JsonPropertyName("elapsed_time")]
        public int ElapsedTime { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("start_date_local")]
        public DateTime StartDateLocal { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }

        [JsonPropertyName("end_index")]
        public int EndIndex { get; set; }

        [JsonPropertyName("average_cadence")]
        public double? AverageCadence { get; set; }

        [JsonPropertyName("average_watts")]
        public double? AverageWatts { get; set; }

        [JsonPropertyName("device_watts")]
        public bool DeviceWatts { get; set; }

        [JsonPropertyName("average_heartrate")]
        public double? AverageHeartrate { get; set; }

        [JsonPropertyName("max_heartrate")]
        public double? MaxHeartrate { get; set; }

        [JsonPropertyName("segment")]
        public Segment? Segment { get; set; }

        [JsonPropertyName("kom_rank")]
        public int? KomRank { get; set; }

        [JsonPropertyName("pr_rank")]
        public int? PrRank { get; set; }

        [JsonPropertyName("achievements")]
        public Achievement[]? Achievements { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }
    }

    /// <summary>
    /// Мета-данные активности (вложенный объект)
    /// </summary>
    public class ActivityMeta
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }
    }

    /// <summary>
    /// Информация о сегменте
    /// </summary>
    public class Segment
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("activity_type")]
        public string? ActivityType { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("average_grade")]
        public double AverageGrade { get; set; }

        [JsonPropertyName("maximum_grade")]
        public double MaximumGrade { get; set; }

        [JsonPropertyName("elevation_high")]
        public double? ElevationHigh { get; set; }

        [JsonPropertyName("elevation_low")]
        public double? ElevationLow { get; set; }

        [JsonPropertyName("start_latlng")]
        public double[]? StartLatLng { get; set; }

        [JsonPropertyName("end_latlng")]
        public double[]? EndLatLng { get; set; }

        [JsonPropertyName("climb_category")]
        public int ClimbCategory { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("hazardous")]
        public bool Hazardous { get; set; }

        [JsonPropertyName("starred")]
        public bool Starred { get; set; }
    }

    /// <summary>
    /// Достижения на сегментах
    /// </summary>
    public class Achievement
    {
        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }
    }

    /// <summary>
    /// Лучшие усилия на дистанциях
    /// </summary>
    public class BestEffort
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("activity")]
        public ActivityMeta? Activity { get; set; }

        [JsonPropertyName("athlete")]
        public AthleteMeta? Athlete { get; set; }

        [JsonPropertyName("elapsed_time")]
        public int ElapsedTime { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("start_date_local")]
        public DateTime StartDateLocal { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }

        [JsonPropertyName("end_index")]
        public int EndIndex { get; set; }

        [JsonPropertyName("pr_rank")]
        public int? PrRank { get; set; }

        [JsonPropertyName("achievements")]
        public Achievement[]? Achievements { get; set; }
    }
}
