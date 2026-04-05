[![](https://img.shields.io/static/v1?label=Sponsor&message=%E2%9D%A4&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/philosowaffle) <span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

> [!TIP]
> You can find specific Upgrade Instructions by visitng the [Install Page](https://philosowaffle.github.io/peloton-to-garmin/latest/install/) for your particular flavor of P2G and looking for the section titled `⬆️ Updating`.

## Features

- [#833] Add UI for managing advanced Peloton API configuration options
- [#837] New Garmin authentication method via service ticket — works when Cloudflare blocks the standard SSO login. Provide a one-time service ticket via the Settings UI, config file, or environment variable. P2G exchanges it for a long-lived DI OAuth2 token (~30 days). See [documentation](https://philosowaffle.github.io/peloton-to-garmin/authentication/garmin-service-ticket) for setup instructions.
- [#839] Automatic Garmin sign-in via Playwright headless browser — P2G now signs into Garmin automatically using a built-in invisible browser. Works out of the box on Docker (amd64/arm64) and GitHub Actions. No DevTools or manual service ticket needed for most users. Sessions last ~30 days and are refreshed automatically. MFA/2FA users and arm/v7 device users should continue using the manual service ticket method. See [documentation](https://philosowaffle.github.io/peloton-to-garmin/authentication/garmin) for details.

## Docker Tags

- Console
    - `console-stable`
    - `console-latest`
    - `console-v6.1.0-rc`
    - `console-v6.1`

- Api
    - `api-stable`
    - `api-latest`
    - `api-v6.1.0-rc`
    - `api-v6.1`
- WebUI
    - `webui-stable`
    - `webui-latest`
    - `webui-v6.1.0-rc`
    - `webui-v6.1`

