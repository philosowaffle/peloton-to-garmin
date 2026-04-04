# Garmin Authentication - Service Ticket

!!! warning "Only use this if the normal sign-in is not working"

    Garmin periodically changes how their login page works, which can prevent P2G from signing in automatically. This guide walks you through an alternative method that works even when the standard sign-in fails.

    **If your automatic syncs are working fine, you do not need to do anything here.**

## What is this?

Normally, P2G signs into Garmin automatically using your email and password. When Garmin blocks this (for example with a Cloudflare challenge), P2G can no longer authenticate on its own.

This guide walks you through a manual one-time process:

1. You log into Garmin in your browser and copy a short-lived code called a **service ticket**
2. You paste that code into P2G
3. P2G exchanges the code for a long-lived token and stores it securely

Once this is done, P2G will handle daily syncs automatically for about **30 days**, after which you'll need to repeat this process.

---

## Step 1: Get a Service Ticket from Garmin

You will retrieve the service ticket directly from your browser. This takes about 2 minutes.

!!! tip "The service ticket expires in seconds — move quickly through Step 2 once you have it."

### In Chrome or Edge

1. Open a **new browser tab**
2. Press **F12** (or right-click anywhere on the page and choose **Inspect**) to open DevTools
3. Click the **Network** tab at the top of the DevTools panel
4. In the **Filter** box, type `login` to narrow down the results

    ![DevTools Network Tab filtered for login](../img/devtools-network-login.png)

5. Navigate to `https://sso.garmin.com/sso/signin` and log in with your Garmin credentials
6. After logging in, look in the Network tab for a request that contains `mobile/api/login`
7. Click on that request, then click the **Response** tab (or **Preview** tab in some browsers)
8. Find the value of `"serviceTicketId"` in the JSON — it looks like `ST-XXXX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX-cas`
9. **Copy the full value** (do not include the surrounding quotes)

### In Firefox

1. Open a **new browser tab**
2. Press **F12** to open DevTools
3. Click the **Network** tab
4. In the filter box, type `login`
5. Navigate to `https://sso.garmin.com/sso/signin` and log in with your Garmin credentials
6. Click the request containing `mobile/api/login`
7. Click the **Response** tab and find `"serviceTicketId"`
8. **Copy the full value**

---

## Step 2: Submit the Service Ticket to P2G

How you submit the ticket depends on which version of P2G you are running. Find your install method below.

---

### Docker Web UI / Source - Web UI

The Swagger interface provides a built-in way to call the API directly from your browser — no extra tools required.

1. Open a browser and go to `http://localhost:8080/swagger`
2. Scroll down to the section labelled **GarminAuthentication**
3. Click on `POST /api/garminauthentication/serviceticket` to expand it
4. Click **Try it out** (top right of that section)
5. In the Request body, replace the placeholder with your ticket:

    ```json
    {
      "serviceTicket": "ST-XXXX-paste-your-ticket-here"
    }
    ```

6. Click **Execute**
7. A `201` response means P2G has successfully authenticated with Garmin. Your next sync will work automatically.

!!! tip "If you get a 500 error, the ticket may have expired. Go back to Step 1 and try again — move faster this time."

---

### Windows App

!!! info "The Windows App will have a built-in button for this in a future update."

    In the meantime, the easiest workaround is to temporarily use the [Docker Web UI](#docker-web-ui-source-web-ui) method on the same machine to bootstrap the token, then continue using the Windows App as normal. Both versions share the same `data/` folder, so the token will carry over.

    **Steps:**

    1. Install the Docker Web UI version of P2G following [these instructions](../install/docker-webui.md), pointing it at the same `data/` folder as your Windows App
    2. Follow the [Docker Web UI steps](#docker-web-ui-source-web-ui) above to submit the ticket
    3. Stop the Docker containers — the token is now saved in your `data/` folder
    4. Relaunch the Windows App and run a sync as normal

---

### Docker Headless

Docker Headless has no web interface, so you will need to temporarily use the Docker Web UI to bootstrap the token.

1. Follow the [Docker Web UI install guide](../install/docker-webui.md), making sure the `data/` volume path matches your existing headless setup
2. Follow the [Docker Web UI steps](#docker-web-ui-source-web-ui) above to submit the ticket
3. Stop the Web UI containers — the token is now stored in your shared `data/` folder
4. Restart your headless container — it will pick up the saved token automatically

---

### GitHub Actions

!!! warning "GitHub Actions has limited support for this flow"

    GitHub Actions runs in a fresh environment each time and does not persist data between runs by default. To use the service ticket method you will need to bootstrap the token locally and upload it as a repository secret.

    **Steps:**

    1. On your local machine, run the [Docker Web UI version](../install/docker-webui.md) of P2G
    2. Follow the [Docker Web UI steps](#docker-web-ui-source-web-ui) above to submit the ticket
    3. Locate the `GarminDb.json` file inside your P2G `data/` folder
    4. Encode it as a base64 string:
        - **Mac/Linux:** `base64 -i GarminDb.json`
        - **Windows (PowerShell):** `[Convert]::ToBase64String([IO.File]::ReadAllBytes("GarminDb.json"))`
    5. In your forked GitHub repository, go to **Settings → Secrets → Actions** and create a new secret named `P2G_GARMIN_DB` with the base64 value
    6. Update your sync workflow to restore the file before running P2G:

        ```yaml
        - name: Restore Garmin DB
          run: |
            mkdir -p data
            echo "${{ secrets.P2G_GARMIN_DB }}" | base64 --decode > data/GarminDb.json
        ```

    You will need to repeat this process after approximately 30 days when the token expires.

---

### Source - Console

Console users have no web interface available. Use the Web UI temporarily to bootstrap the token, then return to console.

1. Follow the [Source - WebUI instructions](../install/source.md#webui) to build and run the web version
2. Follow the [Docker Web UI steps](#docker-web-ui-source-web-ui) above to submit the service ticket via Swagger
3. Stop the Web UI and API processes — the token is saved in your local `data/` folder
4. Run the console version as normal — it will use the saved token

---

## Troubleshooting

**I submitted the ticket but sync still fails**

- Make sure you moved fast enough — the service ticket expires within seconds of it appearing in the browser. Try again from Step 1.
- Check the P2G logs for a more specific error message. See [Finding logs](../help.md#-finding-logs).

**It stopped working after a few weeks**

The refresh token (which allows P2G to silently renew access) lasts approximately 30 days. After that, you'll see an error like `NeedServiceTicket` in the logs. Simply repeat this process from the beginning.

**The `/swagger` page doesn't load**

Make sure the P2G API container or process is running. For Docker Web UI, verify with `docker compose ps`. For Source, confirm the `Api` project is running alongside the `WebUI` project.
