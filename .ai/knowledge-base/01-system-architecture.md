# System Architecture - Peloton to Garmin (P2G)

## Overview
P2G is a .NET 9.0 application that synchronizes workout data from Peloton to Garmin Connect. The system supports multiple deployment models and provides various interfaces for users.

## High-Level Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Peloton API   │    │      P2G        │    │  Garmin Connect │
│                 │    │   Application   │    │                 │
│  - Workouts     │◄──►│                 │──►│  - Upload API   │
│  - User Data    │    │  - Sync Engine  │    │  - OAuth Auth   │
│  - Authentication│    │  - Converters   │    │  - File Upload  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Core Components

### 1. Application Entry Points
- **ConsoleClient** (`src/ConsoleClient/`) - Headless console application
- **WebUI** (`src/WebUI/`) - Blazor web interface
- **API** (`src/Api/`) - REST API for programmatic access  
- **ClientUI** (`src/ClientUI/`) - MAUI desktop/mobile application

### 2. Core Services
- **Sync Service** (`src/Sync/`) - Orchestrates the entire sync process
- **Peloton Service** (`src/Peloton/`) - Handles Peloton API integration
- **Garmin Service** (`src/Garmin/`) - Manages Garmin Connect integration
- **Conversion Service** (`src/Conversion/`) - Converts between file formats

### 3. Shared Components
- **Common** (`src/Common/`) - Shared utilities, DTOs, and configuration
- **SharedUI** (`src/SharedUI/`) - Shared Blazor components

## Data Flow

### Primary Sync Workflow
1. **Authentication** - Authenticate with both Peloton and Garmin APIs
2. **Fetch Workouts** - Retrieve recent workouts from Peloton API
3. **Filter Workouts** - Apply user-configured filters (workout types, etc.)
4. **Stack Workouts** - Optionally combine back-to-back workouts
5. **Convert Formats** - Convert to FIT, TCX, or JSON formats
6. **Upload to Garmin** - Upload converted files to Garmin Connect

### Detailed Data Flow
```
Peloton API
    ↓ (JSON)
P2G Workout DTOs
    ↓ (Filtering/Stacking)
Processed Workouts
    ↓ (Conversion)
FIT/TCX/JSON Files
    ↓ (Upload)
Garmin Connect
```

## Key Technologies

### Backend
- **.NET 9.0** - Primary runtime
- **Flurl.Http** - HTTP client for API calls
- **Serilog** - Structured logging
- **Prometheus** - Metrics collection
- **Dynastream.Fit** - FIT file format handling

### Frontend
- **Blazor** - Web UI framework
- **MAUI** - Cross-platform desktop/mobile UI
- **Bootstrap** - CSS framework

### Data Storage
- **SQLite** - Local database for settings and sync status
- **File System** - Temporary storage for converted files

## Configuration Management

### Configuration Sources (Priority Order)
1. Command line arguments
2. Environment variables (prefix: `P2G_`)
3. Configuration files (`configuration.local.json`)
4. Default values

### Key Configuration Sections
- **App** - Application behavior settings
- **Format** - Output format preferences
- **Peloton** - Peloton API credentials and settings
- **Garmin** - Garmin Connect credentials and settings
- **Observability** - Logging, metrics, and tracing

## Authentication & Security

### Peloton Authentication
- **Method**: Session-based authentication
- **Credentials**: Email/Password
- **Storage**: Encrypted local storage
- **Session Management**: Automatic session renewal

### Garmin Authentication
- **Method**: OAuth 1.0a + OAuth 2.0 hybrid
- **Flow**: Multi-step authentication with optional 2FA
- **Credentials**: Email/Password with optional MFA
- **Token Management**: Automatic token refresh

### Security Features
- **Credential Encryption**: AES encryption for stored credentials
- **Secure Storage**: Platform-specific secure storage APIs
- **Token Management**: Automatic token refresh and validation

## Deployment Models

### 1. Console Application
- **Use Case**: Headless automation, scheduled sync
- **Deployment**: Single executable
- **Configuration**: File-based configuration

### 2. Web UI
- **Use Case**: Web-based management interface
- **Deployment**: Web application (IIS, Docker, etc.)
- **Configuration**: Web-based settings management

### 3. REST API
- **Use Case**: Integration with other applications
- **Deployment**: Web API service
- **Configuration**: API-based configuration

### 4. Desktop Application
- **Use Case**: Local desktop management
- **Deployment**: MAUI application
- **Configuration**: GUI-based settings

### 5. Docker Containers
- **Console**: `console-stable`, `console-latest`
- **API**: `api-stable`, `api-latest`
- **WebUI**: `webui-stable`, `webui-latest`

## Error Handling & Resilience

### Error Handling Strategy
- **ServiceResult<T>** pattern for operation results
- **Structured logging** with context
- **Graceful degradation** for non-critical failures
- **Retry policies** for transient failures

### Monitoring & Observability
- **Prometheus Metrics** - Performance and health metrics
- **Jaeger Tracing** - Distributed tracing
- **Serilog Logging** - Structured logging with multiple sinks
- **Health Checks** - Application health monitoring

## Scalability Considerations

### Current Limitations
- **Single-user focus** - Designed for individual users
- **Sequential processing** - Workouts processed one at a time
- **Local storage** - SQLite database limitations

### Scaling Opportunities
- **Batch processing** - Process multiple workouts in parallel
- **Database scaling** - Move to distributed database
- **Caching layer** - Redis for session/token caching
- **Queue system** - Async processing with message queues

## File System Organization

### Working Directories
- **Working Directory** - Temporary files during conversion
- **Upload Directory** - Files ready for Garmin upload
- **Output Directory** - Local backup copies
- **Download Directory** - Temporary Peloton data storage

### File Cleanup
- **Automatic cleanup** after successful upload
- **Configurable retention** for local copies
- **Error file preservation** for debugging 