using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P2G.Strava.Dto;

namespace P2G.Strava.Auth
{
    /// <summary>
    /// Клиент для OAuth аутентификации Strava
    /// </summary>
    public interface IStravaAuthApiClient
    {
        /// <summary>
        /// Генерирует URL для авторизации пользователя
        /// </summary>
        /// <param name="redirectUri">URI для перенаправления после авторизации</param>
        /// <param name="scope">Запрашиваемые разрешения</param>
        /// <returns>URL для авторизации</returns>
        string GetAuthorizationUrl(string redirectUri, string scope = "read,activity:read_all");

        /// <summary>
        /// Обменивает код авторизации на токены доступа
        /// </summary>
        Task<StravaAuthentication> ExchangeCodeForTokenAsync(string code);

        /// <summary>
        /// Обновляет токен доступа используя refresh token
        /// </summary>
        Task<StravaAuthentication> RefreshTokenAsync(string refreshToken);
    }

    public class StravaAuthApiClient : IStravaAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly int _clientId;
        private readonly string _clientSecret;
        private readonly ILogger<StravaAuthApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string BaseUrl = "https://www.strava.com/oauth";
        private const string TokenEndpoint = "/token";

        public StravaAuthApiClient(
            HttpClient httpClient,
            int clientId,
            string clientSecret,
            ILogger<StravaAuthApiClient> logger)
        {
            _httpClient = httpClient;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        public string GetAuthorizationUrl(string redirectUri, string scope = "read,activity:read_all")
        {
            var sb = new StringBuilder();
            sb.Append($"{BaseUrl}/authorize?");
            sb.Append($"client_id={_clientId}");
            sb.Append($"&redirect_uri={Uri.EscapeDataString(redirectUri)}");
            sb.Append($"&response_type=code");
            sb.Append($"&scope={Uri.EscapeDataString(scope)}");
            sb.Append($"&approval_prompt=auto"); // или "force" для повторного запроса разрешений
            
            return sb.ToString();
        }

        public async Task<StravaAuthentication> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var request = new TokenExchangeRequest
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                    Code = code
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}{TokenEndpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка обмена кода на токен: {StatusCode}, {Content}", 
                        response.StatusCode, responseContent);
                    
                    var error = JsonSerializer.Deserialize<StravaAuthError>(responseContent, _jsonOptions);
                    throw new StravaAuthException(
                        $"Failed to exchange authorization code: {response.StatusCode}", 
                        error?.Errors);
                }

                var authentication = JsonSerializer.Deserialize<StravaAuthentication>(responseContent, _jsonOptions);
                
                if (authentication == null)
                    throw new StravaAuthException("Получен пустой ответ от Strava API");

                _logger.LogInformation("Успешный обмен кода на токен. Токен истекает: {ExpiresAt}", 
                    authentication.ExpiresAtDateTime);

                return authentication;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка десериализации ответа от Strava API");
                throw new StravaAuthException("Ошибка обработки ответа от Strava", ex);
            }
        }

        public async Task<StravaAuthentication> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var request = new RefreshTokenRequest
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                    RefreshToken = refreshToken
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}{TokenEndpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка обновления токена: {StatusCode}, {Content}", 
                        response.StatusCode, responseContent);
                    
                    var error = JsonSerializer.Deserialize<StravaAuthError>(responseContent, _jsonOptions);
                    throw new StravaAuthException(
                        $"Failed to refresh token: {response.StatusCode}", 
                        error?.Errors);
                }

                var authentication = JsonSerializer.Deserialize<StravaAuthentication>(responseContent, _jsonOptions);
                
                if (authentication == null)
                    throw new StravaAuthException("Получен пустой ответ от Strava API");

                _logger.LogInformation("Успешное обновление токена. Новый токен истекает: {ExpiresAt}", 
                    authentication.ExpiresAtDateTime);

                return authentication;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка десериализации ответа от Strava API");
                throw new StravaAuthException("Ошибка обработки ответа от Strava", ex);
            }
        }
    }

    /// <summary>
    /// Исключение при ошибке аутентификации Strava
    /// </summary>
    public class StravaAuthException : Exception
    {
        public StravaErrorDetail[]? Errors { get; }

        public StravaAuthException(string message, StravaErrorDetail[]? errors = null) 
            : base(message)
        {
            Errors = errors;
        }

        public StravaAuthException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
