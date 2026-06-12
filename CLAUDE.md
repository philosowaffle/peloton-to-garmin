# Peloton to Garmin (P2G) — Claude Code Instructions

## Project Overview

.NET 9.0 application that syncs Peloton workout data to Garmin Connect, converting workouts to FIT/TCX/JSON formats.

### Key Components
- **API** (`src/Api/`) — REST API for programmatic access
- **WebUI** (`src/WebUI/`) — Blazor-based web interface
- **ClientUI** (`src/ClientUI/`) — MAUI-based desktop application
- **ConsoleClient** (`src/ConsoleClient/`) — Headless console application
- **Sync Service** (`src/Sync/`) — Core synchronization logic
- **Conversion** (`src/Conversion/`) — Format converters (FIT, TCX, JSON)
- **Peloton Integration** (`src/Peloton/`) — Peloton API client
- **Garmin Integration** (`src/Garmin/`) — Garmin Connect uploader
- **Common** (`src/Common/`) — Shared utilities and DTOs

### Main Entry Points
- `src/ConsoleClient/Program.cs` — Console application
- `src/WebUI/Program.cs` — Web UI
- `src/Api/Program.cs` — API
- `src/ClientUI/MauiProgram.cs` — MAUI app

### Configuration Files
- `configuration.example.json` — Template configuration
- `src/Common/Configuration.cs` — Configuration loading logic

### Core Sync Logic
- `src/Sync/SyncService.cs` — Main synchronization service
- `src/Conversion/IConverter.cs` — Format conversion interface

---

## Development Workflow

When working a GitHub issue end-to-end (branch → spec → implement → PR), use the `/working-issue` skill.

For all changes:

1. **Research** — Consult the knowledge base in `.ai/knowledge-base/` for relevant context
2. **Plan** — Document a clear, concise step-by-step plan before writing code
3. **TDD** — First establish interface changes, write tests asserting behavior, then implement until tests pass
4. **Verify** — Build and all tests must pass before committing (see `/working-issue` for the full validation sequence)
5. **Docs** — Update user documentation in `mkdocs/`
6. **Knowledge Base** — Update `.ai/knowledge-base/` to reflect changes
7. **Release Notes** — Update `vNextReleaseNotes.md` and version in `src/Common/Constants.cs` if needed

### Guiding Principles
- Follow Test Driven Development
- Follow SOLID, DRY, and KISS
- Changes must be backwards compatible
- Keep changes focused and isolated
- Minimize unnecessary additions and refactors

### Don'ts
- Never ignore compile errors
- Never ignore test failures
- Never ignore flaky tests

---

## Knowledge Base

### Before Making Changes
Always consult the knowledge base before implementing changes:

- **System Architecture**: `.ai/knowledge-base/01-system-architecture.md`
- **API Reference**: `.ai/knowledge-base/02-api-reference.md`
- **Development Setup**: `.ai/knowledge-base/03-development-setup.md`
- **Troubleshooting**: `.ai/knowledge-base/04-troubleshooting-guide.md`
- **External APIs**: `.ai/knowledge-base/05-external-api-integration.md`
- **Testing Strategy**: `.ai/knowledge-base/06-testing-strategy.md`

### When Planning Changes
1. **Check Architecture** — Review system architecture to understand component interactions
2. **Verify API Impact** — Check if changes affect REST API endpoints or external API integrations
3. **Follow Patterns** — Use established development patterns and conventions
4. **Consider Testing** — Plan appropriate test coverage using the testing strategy
5. **Check Dependencies** — Understand how changes might affect other components

### Key Project Context
- **.NET 9.0** application with multiple deployment models (Docker, Windows app, console, GitHub Actions)
- **External APIs**: Peloton API and Garmin Connect integration
- **Core Components**: ConsoleClient, WebUI, API, ClientUI, Sync Service
- **File Formats**: FIT, TCX, JSON conversion capabilities
- **Authentication**: Complex OAuth flows for both Peloton and Garmin

### After Making Changes
Update the relevant knowledge base documents after any code change:

| Change Type | Update |
|---|---|
| Architecture | `01-system-architecture.md` — verify component interactions and data flow diagrams |
| API endpoints | `02-api-reference.md` — add/modify endpoints, update request/response schemas |
| Config/setup | `03-development-setup.md` — verify setup instructions, update configuration examples |
| New issues/solutions | `04-troubleshooting-guide.md` — add troubleshooting scenarios, update diagnostic procedures |
| External API flows | `05-external-api-integration.md` — update auth flows, rate limiting, error handling |
| Test patterns | `06-testing-strategy.md` — add new patterns, update test data management strategies |

### Maintenance Checklist
1. **Identify Impact** — Determine which knowledge base sections are affected
2. **Update Content** — Modify relevant documentation sections
3. **Verify Examples** — Ensure code examples still work
4. **Check Cross-References** — Update links between documents
5. **Update Overview** — Modify `.ai/knowledge-base/README.md` if structure changes

### Common Update Scenarios
- **New Features** — Document in architecture and API reference
- **Bug Fixes** — Add to troubleshooting guide
- **Dependencies** — Update development setup
- **Performance** — Update optimization guides
- **Security** — Update security considerations

---

## Testing

_Apply when adding, modifying, or planning tests._

**Location**: `src/UnitTests/`

### Framework Stack
- **NUnit** — Primary testing framework
- **Moq** — Mocking framework
- **FluentAssertions** — Assertion library
- **Bogus** — Test data generator

### Example Tests
- AutoMock usage: `src/UnitTests/Sync/SyncServiceTests.cs`
- Bogus test data: `src/UnitTests/Sync/StackedWorkoutsCalculatorTests.cs`

### Mocking Guidelines
- Mock external dependencies (Peloton/Garmin APIs)
- Use interfaces for testability
- Verify mock interactions
- Test both success and failure scenarios

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~SyncServiceTests"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Maintenance
- Keep sample data current
- Maintain >80% code coverage
- Remove obsolete tests
- Keep test frameworks updated

### Do's
- Write tests before fixing bugs
- Test edge cases and error conditions
- Use descriptive test names
- Keep tests independent and isolated
- Mock external dependencies
- Use test data builders for complex objects

### Don'ts
- Don't test implementation details
- Don't write tests that depend on external services
- Don't ignore flaky tests
- Don't test multiple concerns in one test
- Don't use production data in tests
- Don't skip tests without good reason

---

## API Development

_Apply when working with API controllers._

- **Base URL**: `http://localhost:8080` (configurable via `Api.HostUrl`)
- **Content-Type**: `application/json`
- **No Authentication**: Local deployment only

### Existing Controllers
- `SyncController` (`/api/sync`) — Workout synchronization operations
- `SettingsController` (`/api/settings`) — Application configuration
- `SystemInfoController` (`/api/systeminfo`) — System information
- `GarminAuthenticationController` (`/api/garmin/auth`) — Garmin authentication
- `PelotonWorkoutsController` (`/api/peloton/workouts`) — Peloton workout data
- `PelotonAnnualChallengeController` (`/api/peloton/challenges`) — Challenge data

### Response Patterns
```csharp
// Success
return Ok(new { isSuccess = true, data = result });

// Error
return BadRequest(new { 
    error = new { 
        code = "VALIDATION_ERROR", 
        message = "Error description",
        details = new[] { "Specific error details" }
    }
});
```

### HTTP Status Codes
- **200** — Successful operation
- **400** — Invalid request parameters
- **401** — Authentication required
- **404** — Resource not found
- **500** — Server error

### Validation Patterns
- Use model validation attributes
- Return specific error messages
- Validate business rules in the service layer
- Use `ServiceResult<T>` pattern for service responses

### Dependency Injection
- Inject services through constructor
- Use interfaces for testability
- Follow established service registration patterns in `src/ConsoleClient/Program.cs`

### Testing Considerations
- Create integration tests for new endpoints
- Mock external dependencies
- Test error scenarios and edge cases
- Verify response schemas match documentation

Ensure API changes are documented in `.ai/knowledge-base/02-api-reference.md`.

---

## Configuration

_Apply when working with configuration and settings._

- **Example**: `configuration.example.json`
- **Local**: `configuration.local.json` (gitignored)
- **Loading Logic**: `src/Common/Configuration.cs`

### Priority Order (highest to lowest)
1. Command line arguments
2. Environment variables (prefix: `P2G_`, double underscore for nesting)
3. `configuration.local.json`
4. Default values

### Environment Variable Format
```bash
# Correct — double underscore for nested properties
P2G_Peloton__Email=user@example.com
P2G_Peloton__Password=password123
P2G_Garmin__Upload=true

# Incorrect — wrong separator
P2G_PELOTON_EMAIL=user@example.com
```

### Configuration Sections
- **App** — Application behavior
- **Format** — Output format preferences (FIT, TCX, JSON)
- **Peloton** — Peloton API credentials and settings
- **Garmin** — Garmin Connect credentials and settings
- **Observability** — Logging, metrics, and tracing

### Settings Service Pattern
```csharp
public class MyService
{
    private readonly ISettingsService _settingsService;

    public MyService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task DoWorkAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        // Use settings...
    }
}
```

### Configuration Validation
- Validate required settings on startup
- Provide clear error messages for missing configuration
- Use data annotations for validation where appropriate
- Test configuration loading in unit tests

### Security
- Never commit credentials to version control
- Use environment variables for sensitive data in production
- Encrypt stored credentials using platform-specific secure storage
- Validate configuration on application startup

### Development vs Production
- **Development**: Use `configuration.local.json` with test accounts
- **Production**: Use environment variables or secure configuration providers
- **Docker**: Mount configuration files or use environment variables

Refer to `.ai/knowledge-base/03-development-setup.md` for setup instructions and `.ai/knowledge-base/04-troubleshooting-guide.md` for configuration issues.

---

## Sync Service

_Apply when modifying sync-related components._

### Key Files
- `src/Sync/SyncService.cs` — Main sync logic
- `src/Peloton/ApiClient.cs` — Peloton integration
- `src/Garmin/ApiClient.cs` — Garmin integration

### Sync Workflow
1. Authentication (Peloton + Garmin)
2. Fetch workouts from Peloton API
3. Filter workouts per user config
4. Stack back-to-back workouts (if enabled)
5. Convert to FIT/TCX/JSON
6. Upload to Garmin Connect

### Error Handling
- Use `ServiceResult<T>` for operation results
- Use `ConvertStatus` for conversion operations
- Handle Peloton/Garmin auth errors separately
- Log exceptions with context using Serilog

### Authentication
- **Peloton**: Session-based with automatic renewal
- **Garmin**: OAuth 1.0a + OAuth 2.0 hybrid with MFA support
- Store credentials encrypted using platform-specific secure storage

### Testing Requirements
- Mock external API dependencies
- Test authentication failure scenarios
- Verify workout filtering and stacking logic
- Test conversion error handling

### Performance
- Implement rate limiting for API calls
- Use exponential backoff for transient failures
- Process workouts in batches when possible
- Clean up temporary files after processing

Refer to `.ai/knowledge-base/05-external-api-integration.md` for detailed API integration patterns.

---

## UI Development

_Apply when making changes to the UI or UX._

### Blazor WebUI
- Entry: `src/WebUI/Program.cs`
- Shared components: `src/SharedUI/`
- Pages: `src/SharedUI/Pages/`
- API client: `src/WebUI/ApiClient.cs`

### MAUI ClientUI
- Entry: `src/ClientUI/MauiProgram.cs`
- Main app: `src/ClientUI/Main.razor`
- Platforms: Android, iOS, macOS, Windows, Tizen
- Platform-specific implementations: `src/ClientUI/Platforms/`

### Shared UI Components
- Layout: `src/SharedUI/Shared/MainLayout.razor`
- Forms: `src/SharedUI/Shared/AppSettingsForm.razor`
- Logs: `src/SharedUI/Shared/ApiLogs.razor`

### Patterns
- Use dependency injection for services
- Shared components between WebUI and ClientUI
- Bootstrap for styling and responsive design
- Real-time updates via API polling
- Error handling with user-friendly messages

### Key Features
- Configuration management
- Real-time sync status
- Log viewing and filtering
- Workout progress tracking
- Annual challenge progress display

---

## Documentation

_Apply when updating documentation to reflect application changes._

### Structure
- **MkDocs config**: `mkdocs/mkdocs.yml`
- **Homepage**: `mkdocs/docs/index.md` — Project overview and quick start
- **Features**: `mkdocs/docs/features.md` — Detailed feature descriptions
- **Installation**: `mkdocs/docs/install/` — Docker, Windows App, Source, GitHub Actions
- **Configuration**: `mkdocs/docs/configuration/` — Settings and options
- **Help**: `mkdocs/docs/help.md` — Troubleshooting and support

### Key Sections
- **Installation Options**: Docker, Windows App, Source, GitHub Actions
- **Configuration**: App settings, format options, Peloton/Garmin credentials
- **Migration Guides**: Version upgrade instructions
- **FAQ**: Common questions and solutions
- **Contributing**: Development guidelines and setup

### Features
- Material Theme — modern, responsive design
- Version management via Mike
- Full-text search
- Tabbed navigation and breadcrumbs
- Syntax highlighting and code copy

### Contributing to Docs
- Use Markdown format with MkDocs extensions
- Include screenshots and examples where helpful
- Keep installation steps clear and sequential
- Update configuration examples when adding new features
- Test changes locally with `mkdocs serve`

### Workflow
1. Edit markdown files in `mkdocs/docs/`
2. Test locally with `mkdocs serve`
3. Build with `mkdocs build`

**NEVER deploy documentation.**
