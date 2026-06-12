# External API Integration Guide

## Overview
P2G integrates with two primary external APIs: Peloton API for workout data retrieval and Garmin Connect API for workout upload. This document provides detailed information about these integrations.

## Peloton API Integration

### Authentication
- **Method**: Session-based authentication
- **Base URL**: `https://api.onepeloton.com/api`
- **Auth URL**: `https://api.onepeloton.com/auth/login`

### Authentication Flow
1. **Initial Login**:
   ```http
   POST /auth/login
   Content-Type: application/json
   
   {
     "username_or_email": "user@example.com",
     "password": "password123"
   }
   ```

2. **Response**:
   ```json
   {
     "session_id": "abc123...",
     "user_id": "user123",
     "user_data": { ... }
   }
   ```

3. **Subsequent Requests**:
   ```http
   GET /api/user/{user_id}/workouts
   Cookie: peloton_session_id=abc123...
   ```

### Key Endpoints

#### Get User Workouts
```http
GET /api/user/{user_id}/workouts
Parameters:
- limit: Number of workouts to return (default: 10)
- sort_by: Sort order (default: "-created")
- page: Page number for pagination
- joins: Additional data to include ("ride,ride.instructor")
```

#### Get Workout Details
```http
GET /api/workout/{workout_id}
Parameters:
- joins: Additional data to include ("ride,ride.instructor")
```

#### Get Workout Performance Data
```http
GET /api/workout/{workout_id}/performance_graph
Parameters:
- every_n: Data point frequency (default: 1)
```

#### Get User Data
```http
GET /api/me
```

#### Get Class Segments
```http
GET /api/ride/{ride_id}/details
```

### Data Models

#### Workout Object
```json
{
  "id": "workout123",
  "created_at": 1640995200,
  "device_type": "home_bike_v1",
  "end_time": 1640997000,
  "fitness_discipline": "cycling",
  "has_pedaling_metrics": true,
  "has_leaderboard_metrics": true,
  "is_total_work_personal_record": false,
  "metrics_type": "cycling",
  "name": "30 Min HIIT Ride",
  "peloton_id": "ride123",
  "platform": "home_bike",
  "start_time": 1640995200,
  "status": "COMPLETE",
  "timezone": "America/New_York",
  "title": "30 Min HIIT Ride",
  "total_work": 450000,
  "user_id": "user123",
  "workout_type": "class"
}
```

#### Performance Graph Data
```json
{
  "duration": 1800,
  "is_class_plan_shown": true,
  "segment_list": [...],
  "summaries": [...],
  "metrics": [
    {
      "display_name": "Output",
      "display_unit": "watts",
      "max_value": 250,
      "average_value": 150,
      "values": [120, 125, 130, ...]
    }
  ]
}
```

### Rate Limiting
- **Limits**: Not officially documented
- **Observed**: ~100 requests per minute
- **Best Practice**: Implement exponential backoff
- **Headers**: No rate limit headers returned

### Error Handling
- **401 Unauthorized**: Invalid credentials or expired session
- **403 Forbidden**: Account locked or suspended
- **404 Not Found**: Workout or user not found
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Peloton API issues

### Implementation Notes
```csharp
// Authentication example
var response = await $"{AuthBaseUrl}"
    .WithHeader("Accept-Language", "en-US")
    .WithHeader("User-Agent", "PostmanRuntime/7.26.20")
    .WithTimeout(30)
    .PostJsonAsync(new AuthRequest()
    {
        username_or_email = email,
        password = password
    })
    .ReceiveJson<AuthResponse>();

// API request example
var workouts = await $"{BaseUrl}/user/{userId}/workouts"
    .WithCookie("peloton_session_id", sessionId)
    .WithHeader("User-Agent", userAgent)
    .SetQueryParams(new { limit = 10, sort_by = "-created" })
    .GetJsonAsync<PagedPelotonResponse<Workout>>();
```

## Garmin Connect API Integration

### Authentication
- **Method**: OAuth 1.0a + OAuth 2.0 hybrid
- **Base URL**: `https://connect.garmin.com`
- **SSO URL**: `https://sso.garmin.com`

### Authentication Flow
1. **Initial SSO Request**:
   ```http
   GET /sso/embed?service=https://connect.garmin.com/modern/
   ```

2. **Get CSRF Token**:
   ```http
   GET /sso/signin?service=https://connect.garmin.com/modern/
   ```

3. **Submit Credentials**:
   ```http
   POST /sso/signin
   Content-Type: application/x-www-form-urlencoded
   
   username=user@example.com&password=password123&embed=false&_csrf=token
   ```

4. **Handle MFA (if enabled)**:
   ```http
   POST /sso/verifyMFA
   Content-Type: application/x-www-form-urlencoded
   
   mfa-code=123456&_csrf=token
   ```

5. **Get OAuth1 Token**:
   ```http
   GET /oauth-service/oauth/preauthorized?ticket=ticket123
   Authorization: OAuth oauth_consumer_key="...", oauth_signature="..."
   ```

6. **Exchange for OAuth2 Token**:
   ```http
   POST /oauth-service/oauth/exchange/user/2.0
   Authorization: OAuth oauth_consumer_key="...", oauth_token="...", oauth_signature="..."
   ```

### Upload Endpoint
```http
POST /upload-service/upload/{format}
Authorization: Bearer {oauth2_token}
Content-Type: multipart/form-data

file: [binary file data]
```

### Supported Formats
- **FIT**: Preferred format, full feature support
- **TCX**: Good compatibility, some limitations
- **GPX**: Basic support, limited features

### Upload Response
```json
{
  "detailedImportResult": {
    "uploadId": 123456789,
    "uploadUuid": "abc-123-def",
    "owner": 12345,
    "fileSize": 45678,
    "processingTime": 1234,
    "creationDate": "2024-01-01T10:00:00.000Z",
    "ipAddress": "192.168.1.1",
    "fileName": "workout.fit",
    "report": null,
    "successes": [
      {
        "internalId": 987654321,
        "externalId": "abc-123-def-456"
      }
    ],
    "failures": []
  }
}
```

### Error Handling
- **401 Unauthorized**: Invalid or expired token
- **403 Forbidden**: Insufficient permissions
- **409 Conflict**: Duplicate activity
- **413 Request Entity Too Large**: File too large
- **500 Internal Server Error**: Garmin service issues

### Implementation Notes
```csharp
// OAuth1 token request
OAuthRequest oauthClient = OAuthRequest.ForRequestToken(
    consumerKey, consumerSecret);
oauthClient.RequestUrl = $"{oauthTokenUrl}?ticket={ticket}";

var oauth1Response = await oauthClient.RequestUrl
    .WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
    .GetStringAsync();

// OAuth2 token exchange
OAuthRequest oauthClient2 = OAuthRequest.ForProtectedResource(
    "POST", consumerKey, consumerSecret, oauth1Token, oauth1TokenSecret);

var oauth2Token = await oauthClient2.RequestUrl
    .WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
    .PostUrlEncodedAsync(new object())
    .ReceiveJson<OAuth2Token>();

// File upload
var response = await $"{uploadUrl}/{format}"
    .WithOAuthBearerToken(oauth2Token.Access_Token)
    .PostMultipartAsync((data) =>
    {
        data.AddFile("file", filePath, "application/octet-stream");
    })
    .ReceiveJson<UploadResponse>();
```

## API Monitoring and Reliability

### Health Checks
```csharp
// Peloton health check
public async Task<bool> IsPelotonHealthy()
{
    try
    {
        var response = await "https://api.onepeloton.com/api/me"
            .WithTimeout(10)
            .GetAsync();
        return response.StatusCode == 200;
    }
    catch
    {
        return false;
    }
}

// Garmin health check
public async Task<bool> IsGarminHealthy()
{
    try
    {
        var response = await "https://connect.garmin.com"
            .WithTimeout(10)
            .GetAsync();
        return response.StatusCode == 200;
    }
    catch
    {
        return false;
    }
}
```

### Retry Policies
```csharp
// Exponential backoff for transient failures
public static async Task<T> WithRetry<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan baseDelay = default)
{
    var delay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsTransientError(ex) && i < maxRetries - 1)
        {
            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        }
    }
    
    return await operation(); // Final attempt
}
```

### Rate Limiting
```csharp
// Simple rate limiter
public class RateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _timer;
    
    public RateLimiter(int requestsPerMinute)
    {
        _semaphore = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
        _timer = new Timer(ReleaseSemaphore, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public async Task WaitAsync()
    {
        await _semaphore.WaitAsync();
    }
    
    private void ReleaseSemaphore(object state)
    {
        _semaphore.Release();
    }
}
```

## API Change Management

### Monitoring API Changes
1. **Version Tracking**: Monitor API version headers
2. **Response Validation**: Validate response schemas
3. **Error Pattern Analysis**: Track new error types
4. **Performance Monitoring**: Monitor response times

### Handling Breaking Changes
1. **Graceful Degradation**: Continue with reduced functionality
2. **Feature Flags**: Enable/disable features based on API availability
3. **Backward Compatibility**: Support multiple API versions
4. **User Communication**: Notify users of service disruptions

### Testing Strategies
1. **Contract Testing**: Validate API contracts
2. **Integration Testing**: Test against live APIs
3. **Mock Testing**: Test with simulated responses
4. **Chaos Testing**: Test failure scenarios

## Security Considerations

### Credential Management
- **Encryption**: Encrypt stored credentials
- **Token Rotation**: Regularly refresh tokens
- **Secure Storage**: Use platform-specific secure storage
- **Audit Logging**: Log authentication events

### Network Security
- **TLS/SSL**: Always use HTTPS
- **Certificate Validation**: Validate SSL certificates
- **Proxy Support**: Handle corporate proxies
- **Firewall Rules**: Document required network access

### Data Privacy
- **Minimal Data**: Only collect necessary data
- **Data Retention**: Implement data retention policies
- **User Consent**: Obtain explicit user consent
- **Data Deletion**: Provide data deletion capabilities

## Performance Optimization

### Caching Strategies
```csharp
// Cache authentication tokens
public class TokenCache
{
    private readonly IMemoryCache _cache;
    
    public async Task<string> GetTokenAsync(string key)
    {
        if (_cache.TryGetValue(key, out string token))
        {
            return token;
        }
        
        // Fetch new token
        token = await FetchNewTokenAsync();
        _cache.Set(key, token, TimeSpan.FromHours(1));
        return token;
    }
}
```

### Batch Processing
```csharp
// Process workouts in batches
public async Task<IEnumerable<ConvertStatus>> ConvertWorkoutsAsync(
    IEnumerable<P2GWorkout> workouts)
{
    var batches = workouts.Chunk(10); // Process 10 at a time
    var results = new List<ConvertStatus>();
    
    foreach (var batch in batches)
    {
        var tasks = batch.Select(ConvertWorkoutAsync);
        var batchResults = await Task.WhenAll(tasks);
        results.AddRange(batchResults);
        
        // Rate limiting delay
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    return results;
}
```

### Connection Pooling
```csharp
// Configure HTTP client with connection pooling
services.AddHttpClient<IPelotonApi, PelotonApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.onepeloton.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});
``` 