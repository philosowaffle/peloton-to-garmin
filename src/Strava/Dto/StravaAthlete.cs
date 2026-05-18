using System;
using System.Text.Json.Serialization;

namespace P2G.Strava.Dto
{
    /// <summary>
    /// Модель атлета Strava
    /// </summary>
    public class StravaAthlete
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("firstname")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastname")]
        public string? LastName { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("sex")]
        public string? Sex { get; set; } // "M" или "F"

        [JsonPropertyName("premium")]
        public bool IsPremium { get; set; }

        [JsonPropertyName("summit")]
        public bool IsSummit { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("badge_type_id")]
        public int BadgeTypeId { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; } // в кг

        [JsonPropertyName("profile_medium")]
        public string? ProfileMedium { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("friend")]
        public string? Friend { get; set; }

        [JsonPropertyName("follower")]
        public string? Follower { get; set; }

        [JsonPropertyName("ftp")]
        public int? Ftp { get; set; } // Functional Threshold Power для велоспорта

        [JsonPropertyName("date_preference")]
        public string? DatePreference { get; set; }

        [JsonPropertyName("measurement_preference")]
        public string? MeasurementPreference { get; set; } // "feet" или "meters"

        [JsonPropertyName("clubs")]
        public Club[]? Clubs { get; set; }

        [JsonPropertyName("bikes")]
        public Bike[]? Bikes { get; set; }

        [JsonPropertyName("shoes")]
        public Shoe[]? Shoes { get; set; }
    }

    public class Club
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profile_medium")]
        public string? ProfileMedium { get; set; }

        [JsonPropertyName("cover_photo")]
        public string? CoverPhoto { get; set; }

        [JsonPropertyName("cover_photo_small")]
        public string? CoverPhotoSmall { get; set; }

        [JsonPropertyName("sport_type")]
        public string? SportType { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class Bike
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; } // в метрах
    }

    public class Shoe
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; } // в метрах

        [JsonPropertyName("brand_name")]
        public string? BrandName { get; set; }

        [JsonPropertyName("model_name")]
        public string? ModelName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
