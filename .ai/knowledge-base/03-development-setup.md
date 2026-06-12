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
    "SaveLocalCopy": true
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

### Integration Tests
1. **Configure Test Credentials:**
   - Use separate test accounts for Peloton/Garmin
   - Set `Upload: false` in Garmin config for testing

2. **Test Data:**
   - Sample data in `src/UnitTests/Data/`
   - Mock responses for API testing

### Manual Testing
1. **API Testing:**
   - Use Postman collection (create one)
   - Test all endpoints with various parameters

2. **UI Testing:**
   - Test all major user flows
   - Verify responsive design
   - Test error handling

## Debugging Tips

### Common Issues
1. **MAUI Workload Issues:**
   ```bash
   dotnet workload repair
   dotnet workload update
   ```

2. **Authentication Failures:**
   - Check credentials in configuration
   - Verify network connectivity
   - Check API rate limits

3. **File Permission Issues:**
   - Ensure write permissions to output directories
   - Check antivirus software interference

### Logging Configuration
Enable debug logging in `configuration.local.json`:
```json
{
  "Observability": {
    "Serilog": {
      "MinimumLevel": {
        "Default": "Debug"
      }
    }
  }
}
```

### Performance Profiling
1. **Enable Metrics:**
   ```json
   {
     "Observability": {
       "Prometheus": {
         "Enabled": true,
         "Port": 4000
       }
     }
   }
   ```

2. **View Metrics:**
   - Navigate to `http://localhost:4000/metrics`
   - Import Grafana dashboard from `grafana/p2g_dashboard.json`

## Code Quality

### Code Style
- Follow `.editorconfig` settings
- Use consistent naming conventions
- Add XML documentation to public APIs

### Static Analysis
```bash
# Run code analysis
dotnet build --verbosity normal

# Format code
dotnet format
```

### Pre-commit Hooks
Consider setting up pre-commit hooks for:
- Code formatting
- Unit test execution
- Static analysis

## Contribution Workflow

### 1. Create Feature Branch
```bash
git checkout -b feature/your-feature-name
```

### 2. Make Changes
- Follow existing code patterns
- Add/update tests
- Update documentation

### 3. Test Changes
```bash
dotnet test
dotnet build --configuration Release
```

### 4. Create Pull Request
- Ensure all tests pass
- Update release notes
- Follow PR template

## Troubleshooting

### Build Issues
- Clear bin/obj folders: `dotnet clean`
- Restore packages: `dotnet restore`
- Check .NET version: `dotnet --version`

### Runtime Issues
- Check log files in output directory
- Verify configuration settings
- Check network connectivity

### Performance Issues
- Enable profiling
- Check memory usage
- Monitor API response times

## Additional Resources

### Documentation
- [Official .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)

### Community
- [GitHub Issues](https://github.com/philosowaffle/peloton-to-garmin/issues)
- [GitHub Discussions](https://github.com/philosowaffle/peloton-to-garmin/discussions)
- [Project Wiki](https://philosowaffle.github.io/peloton-to-garmin) 