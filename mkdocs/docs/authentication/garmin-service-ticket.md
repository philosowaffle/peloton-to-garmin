# Garmin Authentication - Service Ticket

!!! warning "Only use this if the normal sign-in is not working"

    Garmin periodically changes how their login page works, which can prevent P2G from signing in automatically. This page walks you through an alternative method that works even when the standard sign-in fails.

    **If your automatic syncs are working fine, you do not need to do anything here.**

## What is this?

Normally P2G signs into Garmin automatically using your email and password. When Garmin blocks this (for example with a Cloudflare challenge), P2G can no longer authenticate on its own.

This guide walks you through a manual one-time process:

1. You log into Garmin in your browser and copy a short-lived code called a **service ticket**
2. You give that code to P2G (via the Settings UI, a config file, or an environment variable)
3. P2G immediately exchanges it for a long-lived token and stores it securely

Once this is done, P2G handles daily syncs automatically for approximately **30 days**, after which you will need to repeat this process.

---

## Step 1: Get a Service Ticket from Garmin

You will retrieve the service ticket directly from your browser. This takes about 2 minutes.

!!! tip "The service ticket will expire, possibly within 1 minute — move quickly once you have it."

1. Open a **new browser tab**
2. Press **F12** (Windows/Linux) or **Cmd+Option+I** (Mac) to open Developer Tools
3. Click the **Network** tab at the top of the Developer Tools panel
4. In the filter/search box, type `mobile/api/login` to narrow down the results
5. Navigate to this exact URL and log in with your Garmin credentials:
   ```
   https://sso.garmin.com/mobile/sso/en_US/sign-in?clientId=GCM_ANDROID_DARK&service=https://mobile.integration.garmin.com/gcm/android
   ```
   !!! note "The page may not look like it loaded correctly — that is normal. As long as you were able to enter your credentials and submit the form, the ticket will appear in the Network tab."
6. After logging in, look in the Network tab for a request that includes `mobile/api/login`
7. Click that request, then open the **Response** or **Preview** tab
8. Find the value next to `"serviceTicketId"` — it looks like `ST-XXXX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX-sso`
9. **Copy the full value** (do not include the surrounding quotes)

---

## Step 2: Give the Service Ticket to P2G

### Via the Settings UI (recommended for most users)

This works for the **Windows App**, **Docker Web UI**, and **Source Web UI**.

1. Open P2G and go to **Settings → Garmin**
2. Scroll to the **Alternative Authentication** card (below the Email/Password fields)
3. Paste your service ticket into the **Service Ticket** field
4. Click **Save**

P2G will immediately exchange the ticket for a long-lived token. If it succeeds, the "Service Ticket is set" badge will disappear (the ticket has been consumed) and your next sync will work automatically.

!!! tip "If you see an error after saving, the ticket may have expired. Go back to Step 1 and try again — move faster this time."

---

### Via config file (Docker Headless, GitHub Actions, Console)

Add the `ServiceTicket` field to the `Garmin` section of your `configuration.local.json`:

```json
"Garmin": {
  "Email": "your@email.com",
  "Password": "yourpassword",
  "ServiceTicket": "ST-XXXX-paste-your-ticket-here",
  "Upload": true
}
```

P2G will exchange the ticket the next time it runs and then automatically remove it from your settings. You do not need to remove it manually.

---

### Via environment variable (Docker, GitHub Actions)

Set the following environment variable before starting P2G:

```
P2G_Garmin__ServiceTicket=ST-XXXX-paste-your-ticket-here
```

P2G will exchange the ticket on its next run and clear it from settings automatically.

For Docker Compose, add it to your service environment:

```yaml
environment:
  - P2G_Garmin__ServiceTicket=ST-XXXX-paste-your-ticket-here
```

Remove the variable after P2G has run successfully — you only need it once to bootstrap.

---

## After 30 days

The long-lived token P2G stores lasts approximately 30 days. After that, syncs will fail with an authentication error. When that happens, simply repeat this process from the beginning.

P2G will log a message like `NeedServiceTicket` to let you know it is time to re-authenticate.

---

## Troubleshooting

**P2G shows an error after I paste the ticket and save**

The ticket expired before P2G could use it — they are only valid for a few seconds. Go back to Step 1, copy the ticket, and paste it into P2G immediately without switching windows in between.

**It stopped working after a few weeks**

The token has expired. Repeat the process from Step 1.

**I don't see a `mobile/api/login` request in the Network tab**

Make sure you filtered the Network tab for `mobile/api/login` *before* you logged in, so the request is captured. Try again with the filter already in place before navigating to the sign-in URL.
