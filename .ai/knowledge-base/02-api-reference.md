# API Reference - P2G REST API

## Overview
The P2G REST API provides programmatic access to sync functionality, settings management, and system information. The API is built using ASP.NET Core and follows RESTful conventions.

## Base Configuration
- **Default URL**: `http://localhost:8080`
- **Configurable**: Via `Api.HostUrl` in configuration
- **Content-Type**: `application/json`
- **Authentication**: Currently no authentication required (local deployment)

## API Controllers

### 1. Sync Controller (`/api/sync`)
Manages workout synchronization operations.

#### `POST /api/sync`
Trigger a sync operation with specified parameters.

**Request Body:**
```json
{
  "numWorkouts": 10,
  "workoutIds": ["workout1", "workout2"],
  "forceStackWorkouts": false
}
```

**Response:**
```json
{
  "syncSuccess": true,
  "pelotonDownloadSuccess": true,
  "conversionSuccess": true,
  "uploadToGarminSuccess": true,
  "errors": []
}
```

#### `GET /api/sync/status`
Get current sync service status.

**Response:**
```json
{
  "isRunning": false,
  "lastSyncTime": "2024-01-01T00:00:00Z",
  "nextSyncTime": "2024-01-01T01:00:00Z"
}
```

### 2. Settings Controller (`/api/settings`)
Manages application configuration and settings.

#### `GET /api/settings`
Retrieve current application settings.

**Response:**
```json
{
  "app": {
    "enablePolling": true,
    "pollingIntervalSeconds": 86400,
    "checkForUpdates": true
  },
  "format": {
    "fit": true,
    "json": false,
    "tcx": false,
    "saveLocalCopy": true
  },
  "peloton": {
    "email": "user@example.com",
    "numWorkoutsToDownload": 10,
    "excludeWorkoutTypes": []
  },
  "garmin": {
    "email": "user@example.com",
    "twoStepVerificationEnabled": false,
    "upload": true,
    "formatToUpload": "fit"
  }
}
```

#### `POST /api/settings`
Update application settings.

**Request Body:** Same structure as GET response

**Response:**
```json
{
  "isSuccess": true,
  "message": "Settings updated successfully"
}
```

### 3. System Info Controller (`/api/systeminfo`)
Provides system and application information.

#### `GET /api/systeminfo`
Get system information and health status.

**Response:**
```json
{
  "version": "5.0.1",
  "operatingSystem": "Windows 10.0.26100",
  "dotNetVersion": "9.0.101",
  "availableFormats": ["fit", "tcx", "json"],
  "healthStatus": "Healthy"
}
```

### 4. Garmin Authentication Controller (`/api/garmin/auth`)
Manages Garmin Connect authentication.

#### `POST /api/garmin/auth/signin`
Initiate Garmin authentication flow.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "requiresMfa": false,
  "isSuccess": true,
  "message": "Authentication successful"
}
```

#### `POST /api/garmin/auth/mfa`
Complete MFA authentication.

**Request Body:**
```json
{
  "mfaCode": "123456"
}
```

### 5. Peloton Workouts Controller (`/api/peloton/workouts`)
Provides access to Peloton workout data.

#### `GET /api/peloton/workouts`
Retrieve Peloton workouts.

**Query Parameters:**
- `pageSize` (int): Number of workouts per page
- `page` (int): Page number (0-based)

**Response:**
```json
{
  "data": [
    {
      "id": "workout123",
      "title": "30 Min HIIT Cycling",
      "workoutType": "cycling",
      "startTime": "2024-01-01T10:00:00Z",
      "endTime": "2024-01-01T10:30:00Z",
      "status": "COMPLETE"
    }
  ],
  "page": 0,
  "pageSize": 10,
  "totalItems": 100
}
```

#### `GET /api/peloton/workouts/{id}`
Get specific workout details.

**Response:**
```json
{
  "id": "workout123",
  "title": "30 Min HIIT Cycling",
  "workoutType": "cycling",
  "instructor": "John Doe",
  "duration": 1800,
  "metrics": {
    "totalOutput": 450,
    "avgHeartRate": 145,
    "maxHeartRate": 165
  }
}
```

### 6. Peloton Annual Challenge Controller (`/api/peloton/challenges`)
Manages Peloton annual challenge data.

#### `GET /api/peloton/challenges`
Get annual challenge progress.

**Response:**
```json
{
  "challenges": [
    {
      "id": "challenge123",
      "name": "Annual Challenge 2024",
      "progress": 75,
      "target": 100,
      "isComplete": false
    }
  ]
}
```

## Error Handling

### Standard Error Response
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request parameters",
    "details": ["Field 'numWorkouts' must be greater than 0"]
  }
}
```

### HTTP Status Codes
- **200 OK** - Successful operation
- **400 Bad Request** - Invalid request parameters
- **401 Unauthorized** - Authentication required
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Server error

## Rate Limiting
Currently no rate limiting implemented. Consider implementing for production use.

## Authentication & Security

### Current State
- **No authentication** required for local deployment
- **HTTPS** recommended for production
- **CORS** configured for web UI integration

### Security Considerations
- Implement API key authentication for production
- Add request validation and sanitization
- Consider implementing rate limiting
- Add audit logging for sensitive operations

## Integration Examples

### JavaScript/Fetch
```javascript
// Trigger sync
const response = await fetch('/api/sync', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    numWorkouts: 10,
    forceStackWorkouts: false
  })
});

const result = await response.json();
```

### PowerShell
```powershell
# Get settings
$settings = Invoke-RestMethod -Uri "http://localhost:8080/api/settings" -Method GET

# Update settings
$settings.app.enablePolling = $false
Invoke-RestMethod -Uri "http://localhost:8080/api/settings" -Method POST -Body ($settings | ConvertTo-Json) -ContentType "application/json"
```

### Python
```python
import requests

# Trigger sync
response = requests.post('http://localhost:8080/api/sync', json={
    'numWorkouts': 10,
    'forceStackWorkouts': False
})

result = response.json()
```

## OpenAPI/Swagger
The API includes Swagger documentation available at `/swagger` when running in development mode.

## Monitoring Endpoints

### Health Check
- **Endpoint**: `/health`
- **Method**: GET
- **Response**: Health status information

### Metrics
- **Endpoint**: `/metrics` (if Prometheus enabled)
- **Method**: GET
- **Response**: Prometheus metrics format 