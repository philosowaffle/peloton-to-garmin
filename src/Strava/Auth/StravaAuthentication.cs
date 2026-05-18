using System;
using System.Text.Json.Serialization;

namespace P2G.Strava.Auth
{
    /// <summary>
    /// Модель токенов аутентификации Strava OAuth 2.0
    /// </summary>
    public class StravaAuthentication
    {
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public int ExpiresAt { get; set; } // Unix timestamp

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } // секунд до истечения

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("athlete")]
        public StravaAthleteMeta? Athlete { get; set; }

        /// <summary>
        /// Дата истечения токена в формате DateTime
        /// </summary>
        public DateTime ExpiresAtDateTime => DateTimeOffset.FromUnixTimeSeconds(ExpiresAt).DateTime;

        /// <summary>
        /// Проверяет, истёк ли токен
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtDateTime.AddMinutes(-5); // 5 минут буфера

        /// <summary>
        /// Проверяет, нужен ли refresh токена
        /// </summary>
        public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAtDateTime.AddMinutes(-10); // 10 минут буфера
    }

    /// <summary>
    /// Мета-данные атлета в ответе на авторизацию
    /// </summary>
    public class StravaAthleteMeta
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

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("sex")]
        public string? Sex { get; set; }

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
        public double? Weight { get; set; }

        [JsonPropertyName("profile_medium")]
        public string? ProfileMedium { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("friend")]
        public string? Friend { get; set; }

        [JsonPropertyName("follower")]
        public string? Follower { get; set; }

        [JsonPropertyName("ftp")]
        public int? Ftp { get; set; }
    }

    /// <summary>
    /// Запрос на обмен кода авторизации на токен
    /// </summary>
    public class TokenExchangeRequest
    {
        [JsonPropertyName("client_id")]
        public int ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("grant_type")]
        public string GrantType => "authorization_code";
    }

    /// <summary>
    /// Запрос на обновление токена
    /// </summary>
    public class RefreshTokenRequest
    {
        [JsonPropertyName("client_id")]
        public int ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("grant_type")]
        public string GrantType => "refresh_token";
    }

    /// <summary>
    /// Ответ с ошибкой от Strava API
    /// </summary>
    public class StravaAuthError
    {
        [JsonPropertyName("errors")]
        public StravaErrorDetail[]? Errors { get; set; }
    }

    public class StravaErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;
    }
}
