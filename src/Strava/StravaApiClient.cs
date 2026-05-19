using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P2G.Strava.Auth;
using P2G.Strava.Dto;

namespace P2G.Strava
{
    /// <summary>
    /// HTTP клиент для Strava API
    /// </summary>
    public interface IStravaApiClient
    {
        /// <summary>
        /// Получает данные текущего атлета
        /// </summary>
        Task<StravaAthlete> GetAthleteAsync();

        /// <summary>
        /// Получает список активностей атлета
        /// </summary>
        /// <param name="perPage">Количество активностей на страницу (макс 200)</param>
        /// <param name="page">Номер страницы</param>
        /// <param name="after">Unix timestamp - получать активности после этой даты</param>
        /// <param name="before">Unix timestamp - получать активности до этой даты</param>
        Task<List<StravaActivity>> GetActivitiesAsync(int perPage = 30, int page = 1, long? after = null, long? before = null);

        /// <summary>
        /// Получает детальную информацию об активности
        /// </summary>
        Task<StravaActivity> GetActivityAsync(long activityId);

        /// <summary>
        /// Получает потоки данных активности
        /// </summary>
        /// <param name="activityId">ID активности</param>
        /// <param name="keys">Типы потоков для получения</param>
        /// <param name="keyByType">Группировать по типу потока</param>
        Task<StravaStream[]> GetActivityStreamsAsync(long activityId, string[]? keys = null, bool keyByType = true);
    }

    public class StravaApiClient : IStravaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StravaApiClient> _logger;
        private readonly IStravaAuthApiClient _authClient;
        
        private const string BaseUrl = "https://www.strava.com/api/v3";

        public StravaApiClient(
            HttpClient httpClient,
            IStravaAuthApiClient authClient,
            ILogger<StravaApiClient> logger)
        {
            _httpClient = httpClient;
            _authClient = authClient;
            _logger = logger;
            
            _httpClient.BaseAddress = new System.Uri(BaseUrl);
        }

        /// <summary>
        /// Устанавливает токен доступа для запросов
        /// </summary>
        public void SetAccessToken(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }

        public async Task<StravaAthlete> GetAthleteAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StravaAthlete>("/athlete");
                
                if (response == null)
                    throw new StravaApiException("Получен пустой ответ от Strava API");

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при получении данных атлета");
                throw new StravaApiException("Ошибка при получении данных атлета", ex);
            }
        }

        public async Task<List<StravaActivity>> GetActivitiesAsync(int perPage = 30, int page = 1, long? after = null, long? before = null)
        {
            try
            {
                // Strava ограничивает perPage максимум 200
                perPage = System.Math.Min(perPage, 200);
                
                var queryParams = new List<string>
                {
                    $"per_page={perPage}",
                    $"page={page}"
                };

                if (after.HasValue)
                    queryParams.Add($"after={after.Value}");

                if (before.HasValue)
                    queryParams.Add($"before={before.Value}");

                var queryString = string.Join("&", queryParams);
                var url = $"/athlete/activities?{queryString}";

                var response = await _httpClient.GetFromJsonAsync<List<StravaActivity>>(url);
                
                if (response == null)
                    return new List<StravaActivity>();

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка активностей");
                throw new StravaApiException("Ошибка при получении списка активностей", ex);
            }
        }

        public async Task<StravaActivity> GetActivityAsync(long activityId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StravaActivity>($"/activities/{activityId}");
                
                if (response == null)
                    throw new StravaApiException($"Активность с ID {activityId} не найдена");

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при получении активности {ActivityId}", activityId);
                throw new StravaApiException($"Ошибка при получении активности {activityId}", ex);
            }
        }

        public async Task<StravaStream[]> GetActivityStreamsAsync(long activityId, string[]? keys = null, bool keyByType = true)
        {
            try
            {
                // Доступные типы потоков: time, distance, latlng, altitude, velocity_smooth, heartrate, cadence, watts, temp, moving, grade_smooth
                var defaultKeys = new[] { "time", "distance", "latlng", "altitude", "velocity_smooth", "heartrate", "cadence", "watts", "temp", "moving", "grade_smooth" };
                var streamKeys = keys ?? defaultKeys;

                var queryParams = new List<string>
                {
                    $"keys={string.Join(",", streamKeys)}",
                    $"key_by_type={keyByType.ToString().ToLower()}"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"/activities/{activityId}/streams?{queryString}";

                var response = await _httpClient.GetFromJsonAsync<StravaStream[]>(url);
                
                if (response == null)
                    return new StravaStream[0];

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при получении потоков данных для активности {ActivityId}", activityId);
                throw new StravaApiException($"Ошибка при получении потоков данных для активности {activityId}", ex);
            }
        }
    }

    /// <summary>
    /// Исключение при ошибке Strava API
    /// </summary>
    public class StravaApiException : System.Exception
    {
        public StravaApiException(string message) : base(message) { }
        
        public StravaApiException(string message, System.Exception innerException) 
            : base(message, innerException) { }
    }
}
