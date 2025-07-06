# Development Environment Setup Guide

## Prerequisites

### Required Software
- **.NET 9.0 SDK** (version 9.0.101 or later)
- **Visual Studio 2022** (17.0+) or **VS Code** with C# extension
- **Git** for version control
- **Docker Desktop** (optional, for containerized development)

### Optional Tools
- **PowerShell 7+** for build scripts
- **Postman** or **Insomnia** for API testing
- **SQLite Browser** for database inspection
- **Garmin Express** for testing Garmin integration

## Initial Setup

### 1. Clone Repository
```bash
git clone https://github.com/philosowaffle/peloton-to-garmin.git
cd peloton-to-garmin
```

### 2. Verify .NET Installation
```bash
dotnet --version
# Should output: 9.0.101 or later

dotnet --list-sdks
# Should show .NET 9.0.101 SDK
```

### 3. Install MAUI Workloads
```bash
dotnet workload restore
dotnet workload list
# Should show MAUI workloads installed
```

### 4. Restore Dependencies
```bash
dotnet clean
dotnet restore
```

### 5. Build Solution
```bash
dotnet build --configuration Debug
```

### 6. Run Tests
```bash
dotnet test
```

## Configuration Setup

### 1. Create Local Configuration
```bash
# Copy example configuration
cp configuration.example.json configuration.local.json
```

### 2. Configure Test Credentials
Edit `configuration.local.json`:
```json
{
  "Peloton": {
    "Email": "your-peloton-email@example.com",
    "Password": "your-peloton-password",
    "NumWorkoutsToDownload": 5
  },
  "Garmin": {
    "Email": "your-garmin-email@example.com",
    "Password": "your-garmin-password",
    "Upload": false,
    "TwoStepVerificationEnabled": false
  },
  "Format": {
    "Fit": true,
    "Json": true,
    "Tcx": false,
    "SaveLocalCopy": true,
    "Cycling": {
      "ElevationGain": {
        "CalculateElevationGain": false,
        "FlatRoadResistance": 30,
        "MaxGradePercentage": 15
      }
    }
  }
}
```

### 3. Environment Variables (Optional)
Set environment variables for sensitive data:
```bash
# Windows
set P2G_Peloton__Email=your-email@example.com
set P2G_Peloton__Password=your-password

# Linux/Mac
export P2G_Peloton__Email=your-email@example.com
export P2G_Peloton__Password=your-password
```

## IDE Configuration

### Visual Studio 2022
1. **Install Extensions:**
   - .NET Multi-platform App UI development
   - ASP.NET and web development
   - Docker support

2. **Configure Startup Projects:**
   - Right-click solution → Properties
   - Set multiple startup projects:
     - `Api` (Start)
     - `WebUI` (Start)
     - `ConsoleClient` (Start without debugging)

3. **Debug Configuration:**
   - Set breakpoints in key files
   - Configure launch profiles in `Properties/launchSettings.json`

### VS Code
1. **Install Extensions:**
   - C# for Visual Studio Code
   - .NET Extension Pack
   - Docker
   - REST Client (for API testing)

2. **Configure Launch Settings:**
Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Console",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/ConsoleClient/bin/Debug/net9.0/ConsoleClient.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false
    },
    {
      "name": "Launch WebUI",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/WebUI/bin/Debug/net9.0/WebUI.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false
    }
  ]
}
```

## Running Different Components

### Console Client
```bash
cd src/ConsoleClient
dotnet run
```

### Web UI
```bash
cd src/WebUI
dotnet run
# Navigate to https://localhost:5001
```

### API
```bash
cd src/Api
dotnet run
# API available at https://localhost:5001
# Swagger UI at https://localhost:5001/swagger
```

### MAUI Client
```bash
cd src/ClientUI
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

## Docker Development

### Build Docker Images
```bash
# Console
docker build -f docker/Dockerfile.console -t p2g-console .

# API
docker build -f docker/Dockerfile.api -t p2g-api .

# WebUI
docker build -f docker/Dockerfile.webui -t p2g-webui .
```

### Run with Docker Compose
```bash
cd docker/webui
docker-compose -f docker-compose-ui.yaml up
```

## Database Setup

### SQLite Database Location
- **Default**: `%APPDATA%/P2G/` (Windows) or `~/.local/share/P2G/` (Linux)
- **Files**: `settings.db`, `garmin.db`, `syncstatus.db`

### Database Schema
The application automatically creates and migrates databases on startup.

### Manual Database Inspection
```bash
# Install SQLite CLI
# Windows: Download from https://sqlite.org/download.html
# Linux: sudo apt install sqlite3
# Mac: brew install sqlite

# Inspect database
sqlite3 ~/.local/share/P2G/settings.db
.tables
.schema
```

## Testing Setup

### Unit Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/UnitTests/UnitTests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```