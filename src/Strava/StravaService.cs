using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2G.Strava.Auth;
using P2G.Strava.Dto;
using P2G.Common;

namespace P2G.Strava
{
    /// <summary>
    /// Сервис для работы с Strava API
    /// </summary>
    public class StravaService : IStravaService
    {
        private readonly IStravaApiClient _apiClient;
        private readonly IStravaAuthApiClient _authClient;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<StravaService> _logger;

        private StravaSettings _settings => _settingsService.Settings.Strava;

        public StravaService(
            IStravaApiClient apiClient,
            IStravaAuthApiClient authClient,
            ISettingsService settingsService,
            ILogger<StravaService> logger)
        {
            _apiClient = apiClient;
            _authClient = authClient;
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.AccessToken))
                    return false;

                // Проверяем, не истёк ли токен
                if (NeedsTokenRefresh())
                {
                    await RefreshAccessTokenAsync();
                }

                // Пробуем получить данные атлета для проверки токена
                _apiClient.SetAccessToken(_settings.AccessToken);
                await _apiClient.GetAthleteAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Проверка аутентификации не удалась");
                return false;
            }
        }

        public async Task<StravaAthlete> GetAthleteDataAsync()
        {
            await EnsureAuthenticatedAsync();
            _apiClient.SetAccessToken(_settings.AccessToken);
            
            return await _apiClient.GetAthleteAsync();
        }

        public async Task<List<StravaActivity>> GetRecentActivitiesAsync(int numActivities, DateTime? afterDate = null)
        {
            await EnsureAuthenticatedAsync();
            _apiClient.SetAccessToken(_settings.AccessToken);

            long? afterTimestamp = afterDate.HasValue 
                ? new DateTimeOffset(afterDate.Value).ToUnixTimeSeconds() 
                : null;

            _logger.LogInformation("Загрузка {Count} активностей после {Date}", 
                numActivities, afterDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "начала времён");

            var activities = new List<StravaActivity>();
            int page = 1;
            const int perPage = 200; // Максимум для Strava

            while (activities.Count < numActivities)
            {
                var batch = await _apiClient.GetActivitiesAsync(
                    perPage: perPage, 
                    page: page, 
                    after: afterTimestamp);

                if (batch == null || batch.Count == 0)
                    break;

                activities.AddRange(batch);

                // Если получили меньше чем запрашивали - значит это последняя страница
                if (batch.Count < perPage)
                    break;

                page++;
            }

            // Сортируем по дате (новые сначала) и ограничиваем количество
            var result = activities
                .OrderByDescending(a => a.StartDate)
                .Take(numActivities)
                .ToList();

            _logger.LogInformation("Загружено {Count} активностей", result.Count);

            return result;
        }

        public async Task<StravaActivityWithStreams> GetActivityDetailsAsync(long activityId)
        {
            await EnsureAuthenticatedAsync();
            _apiClient.SetAccessToken(_settings.AccessToken);

            _logger.LogInformation("Загрузка деталей активности {Id}", activityId);

            // Получаем детальную информацию об активности
            var activity = await _apiClient.GetActivityAsync(activityId);
            
            // Получаем потоки данных
            var streams = await _apiClient.GetActivityStreamsAsync(activityId);

            // Создаём объект с объединёнными данными
            var activityWithStreams = new StravaActivityWithStreams
            {
                ResourceState = activity.ResourceState,
                Athlete = activity.Athlete,
                Name = activity.Name,
                Distance = activity.Distance,
                MovingTime = activity.MovingTime,
                ElapsedTime = activity.ElapsedTime,
                TotalElevationGain = activity.TotalElevationGain,
                Type = activity.Type,
                SportType = activity.SportType,
                WorkoutType = activity.WorkoutType,
                Id = activity.Id,
                StartDate = activity.StartDate,
                StartDateLocal = activity.StartDateLocal,
                Timezone = activity.Timezone,
                UtcOffset = activity.UtcOffset,
                LocationCity = activity.LocationCity,
                LocationState = activity.LocationState,
                LocationCountry = activity.LocationCountry,
                AchievementCount = activity.AchievementCount,
                KudosCount = activity.KudosCount,
                CommentCount = activity.CommentCount,
                AthleteCount = activity.AthleteCount,
                PhotoCount = activity.PhotoCount,
                Map = activity.Map,
                Trainer = activity.Trainer,
                Commute = activity.Commute,
                Manual = activity.Manual,
                Private = activity.Private,
                Visibility = activity.Visibility,
                Flagged = activity.Flagged,
                GearId = activity.GearId,
                StartLatLng = activity.StartLatLng,
                EndLatLng = activity.EndLatLng,
                AverageSpeed = activity.AverageSpeed,
                MaxSpeed = activity.MaxSpeed,
                HasHeartrate = activity.HasHeartrate,
                AverageHeartrate = activity.AverageHeartrate,
                MaxHeartrate = activity.MaxHeartrate,
                HeartrateOptOut = activity.HeartrateOptOut,
                DisplayHideHeartrateOption = activity.DisplayHideHeartrateOption,
                ElevHigh = activity.ElevHigh,
                ElevLow = activity.ElevLow,
                UploadId = activity.UploadId,
                UploadIdStr = activity.UploadIdStr,
                ExternalId = activity.ExternalId,
                FromAcceptedTag = activity.FromAcceptedTag,
                PrCount = activity.PrCount,
                TotalPhotoCount = activity.TotalPhotoCount,
                HasKudoed = activity.HasKudoed,
                SufferScore = activity.SufferScore,
                Description = activity.Description,
                Calories = activity.Calories,
                SegmentEfforts = activity.SegmentEfforts,
                BestEfforts = activity.BestEfforts,
                Streams = streams
            };

            _logger.LogInformation("Успешно загружены детали активности {Id} с {StreamCount} потоками данных", 
                activityId, streams?.Length ?? 0);

            return activityWithStreams;
        }

        /// <summary>
        /// Обновляет токен доступа если необходимо
        /// </summary>
        public async Task RefreshAccessTokenIfNeededAsync()
        {
            if (NeedsTokenRefresh() && !string.IsNullOrEmpty(_settings.RefreshToken))
            {
                await RefreshAccessTokenAsync();
            }
        }

        private bool NeedsTokenRefresh()
        {
            if (string.IsNullOrEmpty(_settings.AccessToken))
                return true;

            if (string.IsNullOrEmpty(_settings.TokenExpiresAt))
                return true;

            if (DateTime.TryParse(_settings.TokenExpiresAt, out var expiresAt))
            {
                // Обновляем за 10 минут до истечения
                return DateTime.UtcNow >= expiresAt.AddMinutes(-10);
            }

            return true;
        }

        private async Task RefreshAccessTokenAsync()
        {
            try
            {
                _logger.LogInformation("Обновление токена доступа Strava");

                var newAuth = await _authClient.RefreshTokenAsync(_settings.RefreshToken);

                // Сохраняем новые токены в настройки
                _settings.AccessToken = newAuth.AccessToken;
                _settings.RefreshToken = newAuth.RefreshToken;
                _settings.TokenExpiresAt = newAuth.ExpiresAtDateTime.ToString("O");

                await _settingsService.SaveSettingsAsync();

                _logger.LogInformation("Токен успешно обновлён. Истекает: {ExpiresAt}", 
                    newAuth.ExpiresAtDateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления токена Strava");
                throw;
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            if (string.IsNullOrEmpty(_settings.AccessToken))
                throw new InvalidOperationException("Strava access token not configured. Please complete OAuth flow first.");

            if (NeedsTokenRefresh())
            {
                await RefreshAccessTokenAsync();
            }
        }
    }
}
