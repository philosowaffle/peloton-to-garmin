# Issue 839 — Phase 3: Playwright Headless Browser Auth for Garmin DI OAuth

## Goal

Insert a new step 3 in `GetGarminAuthenticationAsync()` that:
1. Launches a headless Chromium browser via Playwright
2. Navigates to Garmin's mobile SSO page
3. Fills email + password from settings
4. Intercepts the `/mobile/api/login` response to extract `serviceTicketId`
5. Feeds it to the existing `ExchangeServiceTicketAsync()` from Phase 1

If Playwright is not installed, the browser is not found, or the login fails (MFA, CAPTCHA, timeout) — it returns `null` and the auth flow falls through to legacy paths.

## Current auth flow (GarminAuthenticationService.GetGarminAuthenticationAsync)

1. Try stored DI native session (refresh if needed)
2. Try pending service ticket from settings → `ExchangeServiceTicketAsync()`
3. Try legacy OAuth2 token (unexpired)
4. Try OAuth1 → OAuth2 exchange
5. Fall back to `SignInAsync()` (legacy SSO — likely blocked by Cloudflare)

**New step 3 inserts between steps 2 and 3:**

> 3. NEW: Try `IPlaywrightGarminAuthService.GetServiceTicketViaHeadlessBrowserAsync()` → `ExchangeServiceTicketAsync()`

## Order of Operations

### Step 1 — New settings in `GarminDiApiSettings`
**File:** `src/Common/Dto/Settings.cs`

Add to `GarminDiApiSettings`:
```csharp
public bool EnablePlaywrightAuth { get; set; } = true;
public int PlaywrightTimeoutSeconds { get; set; } = 30;
public bool PlaywrightHeadless { get; set; } = true;
public string LoginClientId { get; set; } = "GCM_ANDROID_DARK";
public string BrowserUserAgent { get; set; } = "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Mobile Safari/537.36";
```

Update `configuration.example.json` to include these new fields under `Garmin.Di`.

### Step 2 — `IPlaywrightGarminAuthService` interface and implementation
**New file:** `src/Garmin/Auth/PlaywrightGarminAuthService.cs`
**Updated csproj:** `src/Garmin/Garmin.csproj` — add `Microsoft.Playwright`

Interface:
```csharp
public interface IPlaywrightGarminAuthService
{
    Task<string?> GetServiceTicketViaHeadlessBrowserAsync(string email, string password);
    bool IsAvailable();
}
```

Implementation key behaviors:
- `IsAvailable()`: checks `Playwright.CreateAsync()` doesn't throw + browser installed; returns false without throwing
- `GetServiceTicketViaHeadlessBrowserAsync()`:
  - Returns null on any failure (never throws) except `InvalidCredentials` → throw `GarminAuthenticationError`
  - Reads `GarminDiApiSettings` for `LoginClientId`, `ServiceUrl`, `PlaywrightTimeoutSeconds`, `PlaywrightHeadless`, `BrowserUserAgent`
  - Creates browser context with mobile UA, viewport 412x915, `IsMobile=true`, `HasTouch=true`, locale `en-US`
  - Registers `Page.Response` handler to capture `/mobile/api/login` response and extract `serviceTicketId`
  - Navigates to `https://sso.garmin.com/mobile/sso/en_US/sign-in?clientId={LoginClientId}&service={ServiceUrl}`
  - Fills `#username` / `input[name="username"]`, `#password` / `input[name="password"]`
  - Clicks submit or presses Enter
  - Detects MFA redirect: logs info, returns null (Phase 3a — fall through)
  - Waits for ticket via `TaskCompletionSource` with timeout
  - Disposes browser resources in `finally`

### Step 3 — Inject Playwright into `GarminAuthenticationService`
**File:** `src/Garmin/Auth/GarminAuthenticationService.cs`

- Add `IPlaywrightGarminAuthService` constructor parameter
- Add step between service-ticket check and legacy OAuth2 check:
```csharp
var settings = await _settingsService.GetSettingsAsync();
if (settings.Garmin.Api.Di.EnablePlaywrightAuth && _playwrightAuth.IsAvailable())
{
    try
    {
        settings.Garmin.EnsureGarminCredentialsAreProvided();
        var ticket = await _playwrightAuth.GetServiceTicketViaHeadlessBrowserAsync(settings.Garmin.Email, settings.Garmin.Password);
        if (ticket is not null)
        {
            _logger.Information("Playwright headless auth succeeded, exchanging service ticket.");
            return await ExchangeServiceTicketAsync(ticket);
        }
    }
    catch (GarminAuthenticationError gae) when (gae.Code == Code.InvalidCredentials)
    {
        throw; // Surface bad credentials immediately
    }
    catch (Exception ex)
    {
        _logger.Warning("Playwright headless auth failed, falling through to legacy auth.", ex);
    }
}
```

### Step 4 — DI Registration
**Files:** `src/ConsoleClient/Program.cs`, `src/Api.Service/ApiStartupServices.cs`

Add: `services.AddSingleton<IPlaywrightGarminAuthService, PlaywrightGarminAuthService>();`

Note: WebUI calls the API service over HTTP (no direct DI). MAUI has no Garmin service registration (not used directly).

### Step 5 — UI Updates
**File:** `src/SharedUI/Shared/GarminSettingsForm.razor`

Add to the existing "Garmin DI Api Settings" accordion section (after existing fields):
- `EnablePlaywrightAuth` toggle (`HxInputSwitch`)
- `PlaywrightTimeoutSeconds` number input (`HxInputNumber`)
- `PlaywrightHeadless` toggle (label: "Headless Mode", tooltip: "Disable to show the browser window (useful for debugging)")
- `LoginClientId` text input
- `BrowserUserAgent` text input

Update `RestoreDefaultDiApiSettings()` — already resets the whole `Di` object so new defaults come for free.

Add a status badge: "Playwright Available" / "Playwright Not Available" — driven by a new API endpoint or system info field.

### Step 6 — Docker Image Updates
**Files:** `docker/Dockerfile.console`, `docker/Dockerfile.webui`, `docker/Dockerfile.api`

In each Dockerfile's `final` layer, after the existing `apt-get` block:
```dockerfile
# Playwright Chromium system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 \
    libcups2 libdrm2 libdbus-1-3 libxcb1 libxkbcommon0 libatspi2.0-0 \
    libx11-6 libxcomposite1 libxdamage1 libxext6 libxfixes3 libxrandr2 \
    libgbm1 libpango-1.0-0 libcairo2 libasound2 libwayland-client0 \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# Install Playwright CLI and Chromium browser (skipped on arm/v7)
ARG TARGETPLATFORM
RUN if [ "$TARGETPLATFORM" != "linux/arm/v7" ]; then \
    dotnet tool install --global Microsoft.Playwright.CLI \
    && /root/.dotnet/tools/playwright install chromium --with-deps ; \
    fi
```

Run the Playwright install as root (before switching to `p2g` user).

### Step 7 — Unit Tests
**Files:**
- `src/UnitTests/Garmin/PlaywrightGarminAuthServiceTests.cs` — new
- `src/UnitTests/Garmin/GarminAuthenticationServiceTests.cs` — updated

`GarminAuthenticationService` tests need `IPlaywrightGarminAuthService` mock added to constructor. New test cases for:
- Playwright not available → skips, continues to legacy path
- Playwright available + success → returns DI auth, does not call legacy
- Playwright available + returns null → falls through to legacy path
- Playwright throws `GarminAuthenticationError(InvalidCredentials)` → rethrows
- Playwright throws other exception → logs warning, falls through

`PlaywrightGarminAuthServiceTests` — unit tests for `IsAvailable()` and the service logic (mocked Playwright via interface abstraction if feasible, or integration tests flagged as manual).

### Step 8 — Documentation
- **New:** `mkdocs/docs/authentication/garmin.md` — unified auth guide (non-technical)
- **Update:** `mkdocs/mkdocs.yml` — add new page to nav
- **Update:** `mkdocs/docs/install/docker.md` (or equivalent) — note arm/v7 Playwright limitation
- **Update:** `vNextReleaseNotes.md`

### Step 9 — Knowledge Base Updates
- `src/.ai/knowledge-base/05-external-api-integration.md` — document Playwright auth flow
- `src/.ai/knowledge-base/01-system-architecture.md` — note Playwright dependency

## Key Decisions

1. **`IsAvailable()` implementation**: Use `Playwright.CreateAsync()` in a try/catch to check if Playwright assemblies are present, then `IBrowserType.ExecutablePath` to verify Chromium is installed. Cache the result.

2. **Response interception**: Use `page.Response` event (`+=` handler) with a `TaskCompletionSource<string?>` — cleaner than `RouteAsync()` since we only need to read, not intercept.

3. **MFA detection**: Check if URL contains `/mfa/` or `/verifyMFA/` or if the title contains "Verification". Log and return null (Phase 3a).

4. **`GetServiceTicketAsync` settings access**: Inject `ISettingsService` into `PlaywrightGarminAuthService` to read `GarminDiApiSettings` (consistent with existing pattern).

5. **`GarminAuthenticationService` constructor change**: Add `IPlaywrightGarminAuthService` as a new param. Tests that construct it directly need to pass a mock.

## Files to Change

| File | Type |
|---|---|
| `src/Common/Dto/Settings.cs` | Add 5 new settings to `GarminDiApiSettings` |
| `configuration.example.json` | Add new settings |
| `src/Garmin/Garmin.csproj` | Add `Microsoft.Playwright` NuGet |
| `src/Garmin/Auth/PlaywrightGarminAuthService.cs` | **New** |
| `src/Garmin/Auth/GarminAuthenticationService.cs` | Add Playwright step + constructor param |
| `src/ConsoleClient/Program.cs` | Register new service |
| `src/Api.Service/ApiStartupServices.cs` | Register new service |
| `src/SharedUI/Shared/GarminSettingsForm.razor` | Add 5 new UI fields |
| `docker/Dockerfile.console` | Add Playwright deps + install |
| `docker/Dockerfile.webui` | Add Playwright deps + install |
| `docker/Dockerfile.api` | Add Playwright deps + install |
| `src/UnitTests/Garmin/PlaywrightGarminAuthServiceTests.cs` | **New** |
| `src/UnitTests/Garmin/GarminAuthenticationServiceTests.cs` | Update with mock + new test cases |
| `mkdocs/docs/authentication/garmin.md` | **New** unified auth guide |
| `mkdocs/mkdocs.yml` | Update nav |
| `vNextReleaseNotes.md` | Release notes |
| `.ai/knowledge-base/01-system-architecture.md` | Update |
| `.ai/knowledge-base/05-external-api-integration.md` | Update |
