# Garmin Authentication

P2G needs to sign into your Garmin account to upload workouts. This page explains how that works and what to do if something goes wrong.

!!! note "Page generated with AI assistance"

    This page was generated with the help of Claude AI. Please open an issue if you run into problems.

---

## How P2G Signs In

P2G tries several sign-in methods automatically, in this order:

1. **Use a stored session** — if P2G already signed in recently, it reuses that session (valid for up to ~30 days)
2. **Refresh an expiring session** — if the session is close to expiring, P2G refreshes it automatically
3. **[Automatic sign-in](#option-1-automatic-sign-in-recommended)** — P2G uses a built-in browser to sign in on your behalf
4. **[Manual service ticket](#option-2-manual-service-ticket-fallback)** — you paste a one-time code that P2G exchanges for a session
5. **Legacy sign-in** — an older method that may no longer work on Garmin's current login page

For most users, P2G handles everything automatically and you never need to think about authentication.

---

## Option 1: Automatic Sign-In (Recommended)

P2G includes a built-in headless browser that signs into Garmin on your behalf — fully automatic, no DevTools required.

**What happens:** When P2G needs to authenticate, it opens an invisible browser, navigates to Garmin's login page, enters your credentials, and captures the session. The session lasts approximately 30 days, after which P2G re-authenticates automatically.

**Who it works for:**

| Setup | Works? |
|---|---|
| Docker on amd64 (x86_64) | Yes |
| Docker on arm64 | Yes |
| Docker on arm/v7 (e.g. Raspberry Pi 2/3 32-bit) | No — [see below](#arm-v7-limitation) |
| GitHub Actions | Yes |
| Windows App | Only if Playwright is separately installed |
| Console (from source) | Only if Playwright is separately installed |

**Requirements:** Just provide your Garmin email and password in P2G settings. No extra setup is needed for Docker or GitHub Actions.

**Does not work when:**

- Your Garmin account has **two-factor authentication (MFA/2FA)** enabled — use [Option 2](#option-2-manual-service-ticket-fallback) instead
- You are on an **arm/v7** device — use [Option 2](#option-2-manual-service-ticket-fallback) instead
- Garmin shows a **CAPTCHA** challenge — P2G falls through to Option 2 automatically

### ARM/v7 Limitation

Playwright (the browser automation library P2G uses) does not support `linux/arm/v7`. If you are running P2G on a 32-bit ARM device such as a Raspberry Pi 2 or Raspberry Pi 3 running a 32-bit OS, automatic sign-in will not be available. Use the [manual service ticket method](#option-2-manual-service-ticket-fallback) instead.

---

## Option 2: Manual Service Ticket (Fallback)

If automatic sign-in is not available or is not working, you can sign into Garmin yourself and paste a one-time code into P2G.

See the full step-by-step instructions: **[Garmin Service Ticket Guide](garmin-service-ticket.md)**

**Who needs this:**

- Users with MFA/2FA enabled on their Garmin account
- arm/v7 device users
- Anyone where automatic sign-in is failing

**How often:** Approximately every 30 days when the stored session expires.

---

## Option 3: Legacy Sign-In (Deprecated)

P2G's original sign-in method sends your email and password directly to Garmin's login endpoint. Garmin has been blocking this method for many users (Cloudflare protection). It is kept as a last-resort fallback.

If this is the only method that works for you, be aware it may stop working at any time.

---

## Which Option Should I Use?

| Your situation | Recommended |
|---|---|
| Standard Docker/GitHub Actions setup, no MFA | Option 1 (Automatic) — no action needed |
| MFA/2FA enabled on Garmin | Option 2 (Manual Service Ticket) |
| Raspberry Pi or 32-bit ARM device | Option 2 (Manual Service Ticket) |
| Automatic sign-in stopped working | Try Option 2 as a fallback |
| Windows App or console from source | Option 1 if Playwright is installed, otherwise Option 2 |

---

## Troubleshooting

**"Automatic sign-in failed" in the logs**
Check the P2G logs for details. Common causes: MFA challenge, CAPTCHA, or network issue. P2G should automatically fall through to the manual service ticket if it's set. If syncs are failing, use Option 2.

**"Service ticket expired" or "NeedServiceTicket" in the logs**
Your stored session has expired (~30 days). If you are on Docker/GitHub Actions, automatic sign-in should re-authenticate. If not, follow the [Service Ticket Guide](garmin-service-ticket.md) to provide a fresh ticket.

**I'm on arm/v7 and syncs fail after 30 days**
Automatic sign-in is not available on arm/v7. Provide a new service ticket every 30 days using the [Service Ticket Guide](garmin-service-ticket.md).

**The Playwright badge in Settings shows "Unavailable"**
Playwright or Chromium is not installed on the host running P2G. On Docker (amd64/arm64), this should not happen — check that you are using an up-to-date image. For Windows App or console-from-source, see the Playwright installation docs.
