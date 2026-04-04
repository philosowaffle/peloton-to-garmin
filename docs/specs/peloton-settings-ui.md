# Spec: Peloton Settings UI

**Status:** In Progress

## Overview
UI management of Peloton configuration options, including advanced API settings that are normally configured via config files or environment variables.

---

## Capability: Advanced Peloton API Settings UI

**Issue:** #833 | **Status:** In Progress

### Problem
Users have no way to manage the advanced `PelotonApiSettings` (API URL, auth domain, OAuth paths, bearer token TTL, etc.) from the Web UI or ClientUI. These settings exist in the domain model and config files but are not exposed through the API contract or any UI form. This makes temporary workarounds (e.g. overriding an API endpoint) require direct file/env-var edits.

### Acceptance Criteria
- [ ] Advanced Peloton API settings are exposed in the UI under a collapsible "Advanced" accordion section on the Peloton settings tab
- [ ] The advanced section includes a "danger zone" warning card matching the Garmin advanced settings pattern
- [ ] All `PelotonApiSettings` fields are editable: `ApiUrl`, `AuthDomain`, `AuthClientId`, `AuthAudience`, `AuthScope`, `AuthRedirectUri`, `Auth0ClientPayload`, `AuthAuthorizePath`, `AuthTokenPath`, `BearerTokenDefaultTtlSeconds`
- [ ] A "Restore Defaults" button resets all API settings to their default values
- [ ] A documentation popover links to the Peloton configuration docs
- [ ] Changes save and persist correctly via the existing `POST /api/settings/peloton` endpoint
- [ ] The `SettingsPelotonGetResponse` and `SettingsPelotonPostRequest` contracts include the `Api` field
- [ ] Documentation at `mkdocs/docs/configuration/peloton.md` is updated with a field table and the TODO warning is removed

### Notes
- Mirror the existing Garmin advanced settings pattern in `GarminSettingsForm.razor` for visual consistency
- The `PelotonApiSettings` class with defaults lives in `src/Common/Dto/Settings.cs`
- No new API endpoints needed — the existing Peloton settings POST handles persistence once the contract is updated
