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

### After Making Changes
Update the relevant knowledge base documents after any code change:

| Change Type | Update |
|---|---|
| Architecture | `01-system-architecture.md` |
| API endpoints | `02-api-reference.md` |
| Config/setup | `03-development-setup.md` |
| New issues/solutions | `04-troubleshooting-guide.md` |
| External API flows | `05-external-api-integration.md` |
| Test patterns | `06-testing-strategy.md` |

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

### Running Tests
```bash
dotnet test
dotnet test --filter "FullyQualifiedName~SyncServiceTests"
dotnet test --collect:"XPlat Code Coverage"
```

### Do's
- Write tests before fixing bugs
- Test edge cases and error conditions
- Use descriptive test names
- Keep tests independent and isolated
- Mock external dependencies (Peloton/Garmin APIs)
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
- `SyncController` (`/api/sync`)
- `SettingsController` (`/api/settings`)
- `SystemInfoController` (`/api/systeminfo`)
- `GarminAuthenticationController` (`/api/garmin/auth`)
- `PelotonWorkoutsController` (`/api/peloton/workouts`)
- `PelotonAnnualChallengeController` (`/api/peloton/challenges`)

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

Ensure API changes are documented in `.ai/knowledge-base/02-api-reference.md`.

---

## Configuration

_Apply when working with configuration and settings._

- **Example**: `configuration.example.json`
- **Local**: `configuration.local.json` (gitignored)
- **Loading Logic**: `src/Common/Configuration.cs`

### Priority Order (highest to lowest)
1. Command line arguments
2. Environment variables (prefix: `P2G_`, double underscore for nesting: `P2G_Peloton__Email`)
3. `configuration.local.json`
4. Default values

### Configuration Sections
- **App** — Application behavior
- **Format** — Output format preferences (FIT, TCX, JSON)
- **Peloton** — Peloton API credentials and settings
- **Garmin** — Garmin Connect credentials and settings
- **Observability** — Logging, metrics, and tracing

### Security
- Never commit credentials to version control
- Use environment variables for sensitive data in production
- Use `ISettingsService` for accessing settings in code

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

### Performance
- Implement rate limiting for API calls
- Use exponential backoff for transient failures
- Process workouts in batches when possible
- Clean up temporary files after processing

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

### Shared UI Components
- Layout: `src/SharedUI/Shared/MainLayout.razor`
- Forms: `src/SharedUI/Shared/AppSettingsForm.razor`
- Logs: `src/SharedUI/Shared/ApiLogs.razor`

### Patterns
- Use dependency injection for services
- Bootstrap for styling and responsive design
- Real-time updates via API polling
- Error handling with user-friendly messages

---

## Documentation

_Apply when updating documentation to reflect application changes._

- **MkDocs config**: `mkdocs/mkdocs.yml`
- **Docs root**: `mkdocs/docs/`

### Workflow
1. Edit markdown files in `mkdocs/docs/`
2. Test locally with `mkdocs serve`
3. Build with `mkdocs build`

**NEVER deploy documentation.**
