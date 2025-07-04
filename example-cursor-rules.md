# Example .cursor-rules File for Peloton-to-Garmin

This file demonstrates the recommended cursor rules that should be implemented in the `.cursor-rules` file at the root of the repository.

```yaml
# Peloton-to-Garmin Cursor Rules
# This file provides context and guidelines for AI agents working on this codebase

## Project Overview
project_name: "Peloton-to-Garmin"
description: "A .NET 9.0 C# application that synchronizes workout data from Peloton to Garmin Connect"
primary_language: "C#"
framework: ".NET 9.0"
architecture: "Modular with dependency injection"

## Project Structure
project_structure:
  - "Solution file: PelotonToGarmin.sln contains all project references"
  - "Main projects located in src/ directory with clear separation of concerns"
  - "Api: REST API for WebUI communication"
  - "WebUI: Blazor-based web interface" 
  - "ConsoleClient: Command-line interface for headless operation"
  - "Common: Shared utilities, configuration, and DTOs"
  - "Garmin: Garmin Connect API integration and authentication"
  - "Peloton: Peloton API integration and workout retrieval"
  - "Sync: Core synchronization logic and orchestration"
  - "Conversion: Workout format conversion (FIT, TCX, JSON)"
  - "UnitTests: Unit test project using standard .NET testing patterns"
  - "Documentation: mkdocs/ directory contains MkDocs-based documentation"
  - "Docker: docker/ directory contains all Docker-related files"

## Configuration Management
configuration:
  - "Primary config file: configuration.local.json (local overrides)"
  - "Example config: configuration.example.json (template for users)"
  - "Environment variables: Use P2G_ prefix with __ for nesting (P2G_APP__ENABLEPOLLING)"
  - "Configuration precedence: Command Line > Environment Variables > Config File"
  - "WebUI and GitHub Actions support encrypted credential storage"
  - "Console and Docker headless store credentials in plain text"
  - "Configuration binding uses Microsoft.Extensions.Configuration patterns"

## Coding Standards
coding_standards:
  - "Follow Microsoft C# coding conventions and style guidelines"
  - "Use nullable reference types (enabled project-wide in all .csproj files)"
  - "Implement structured logging using Serilog with appropriate log levels"
  - "Use dependency injection for all service dependencies"
  - "Follow async/await patterns for all I/O operations"
  - "Use configuration binding for settings management"
  - "Implement proper error handling with custom exceptions where appropriate"
  - "Use XML documentation comments for public APIs"
  - "Follow REST API conventions in controller design"
  - "Use DTOs for data transfer and avoid exposing internal models"

## API Integration Patterns
api_integrations:
  - "Peloton API: Located in src/Peloton project"
  - "Peloton authentication: Uses email/password with session management"
  - "Peloton rate limiting: Implement exponential backoff and retry logic"
  - "Garmin API: Located in src/Garmin project"
  - "Garmin authentication: Supports standard login and 2FA"
  - "Garmin upload: Handles FIT file uploads with proper error handling"
  - "Conversion logic: Located in src/Conversion project"
  - "Supported formats: FIT (primary), TCX, JSON for workout data"
  - "All API calls must include proper error handling and retry mechanisms"
  - "Use HttpClient with dependency injection and proper disposal"

## Testing Guidelines
testing:
  - "Unit tests located in src/UnitTests project"
  - "Use xUnit framework for test organization"
  - "Mock external API dependencies (Peloton, Garmin) using interfaces"
  - "Test configuration loading and validation scenarios"
  - "Include integration tests for conversion logic"
  - "Test error handling and edge cases"
  - "Use test data builders for complex object creation"
  - "Verify logging behavior in critical paths"

## Documentation Standards
documentation:
  - "User documentation: mkdocs/docs/ using Material theme"
  - "API documentation: Generated from XML comments using Swagger/OpenAPI"
  - "Configuration examples: Keep in sync with actual configuration classes"
  - "Release notes: Update vNextReleaseNotes.md for all user-facing changes"
  - "README: Keep concise with links to detailed documentation"
  - "Code comments: Focus on why, not what, especially for complex algorithms"

## Deployment Considerations
deployment:
  - "Multiple deployment targets: Docker, Windows, source build"
  - "Docker files: Located in docker/ directory with separate files for each target"
  - "Support both headless and WebUI deployments"
  - "GitHub Actions: Automated builds and releases"
  - "Cross-platform: Supports Windows, Linux, and macOS"
  - "Single-file deployment: Configured for self-contained applications"
  - "Consider security implications of credential storage for each deployment type"

## Observability
observability:
  - "Logging: Serilog with Console and File sinks"
  - "Metrics: Prometheus metrics for monitoring"
  - "Tracing: Jaeger distributed tracing support"
  - "Health checks: Implement for API endpoints"
  - "Log structured data using Serilog's structured logging"
  - "Include correlation IDs for request tracing"
  - "Monitor API rate limits and failures"

## Security Considerations
security:
  - "Credentials: Never commit actual credentials to repository"
  - "Encryption: WebUI supports encrypted credential storage"
  - "API keys: Use secure configuration management"
  - "Rate limiting: Respect API rate limits to avoid blocking"
  - "Input validation: Validate all user inputs and configuration"
  - "Dependency management: Keep dependencies updated for security"

## Performance Guidelines
performance:
  - "Async operations: Use async/await for all I/O operations"
  - "Connection pooling: Use HttpClient properly with dependency injection"
  - "Memory management: Dispose resources properly"
  - "Rate limiting: Implement exponential backoff for API calls"
  - "Caching: Consider caching for frequently accessed data"
  - "Batch operations: Process multiple workouts efficiently"

## Common Patterns
common_patterns:
  - "Configuration: Use IOptions<T> pattern for strongly-typed configuration"
  - "HTTP clients: Inject HttpClient and use typed clients where beneficial"
  - "Error handling: Create custom exceptions for domain-specific errors"
  - "Validation: Use FluentValidation or built-in validation attributes"
  - "Logging: Use structured logging with semantic properties"
  - "Background services: Use IHostedService for background operations"

## File Naming Conventions
file_naming:
  - "Controllers: End with 'Controller' (e.g., SyncController.cs)"
  - "Services: End with 'Service' (e.g., GarminService.cs)"
  - "DTOs: Use descriptive names in Dto folder (e.g., WorkoutDto.cs)"
  - "Configurations: Use descriptive names matching sections (e.g., GarminSettings.cs)"
  - "Tests: Match class under test with 'Tests' suffix (e.g., SyncControllerTests.cs)"

## Common Issues and Solutions
troubleshooting:
  - "Peloton authentication: Check for rate limiting and session expiration"
  - "Garmin 2FA: Ensure proper token handling and refresh logic"
  - "File conversion: Verify FIT file format compatibility"
  - "Configuration: Check precedence order and environment variable naming"
  - "Docker deployment: Verify volume mounts for configuration files"
  - "Performance: Monitor API call frequency and implement caching"