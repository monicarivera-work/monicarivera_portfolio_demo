# Next Steps — LeetCode Analytics & Code Lab Features

This document outlines the manual Azure / Entra ID steps required to fully activate the LeetCode analytics page, the Code Lab visualizer, and session persistence.

---

## 1. Azure Entra ID (Azure AD) App Registration

The Code Lab "save session" feature requires users to sign in with your Microsoft/Azure account. To enable this:

### 1.1 Create an App Registration

1. Open the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**.
2. **Name:** `monicarivera-portfolio-codelab` (or any name you prefer)
3. **Supported account types:** *Accounts in this organizational directory only* (Single tenant) — or *Personal Microsoft accounts* if you want broader access.
4. **Redirect URI:** `Web` → `https://monicarivera-portfolio-demo.azurewebsites.net/signin-oidc`
5. Click **Register**.

### 1.2 Note the IDs

After registration, copy:
- **Application (client) ID** → used as `AzureAd:ClientId`
- **Directory (tenant) ID** → used as `AzureAd:TenantId`

### 1.3 Configure Token Logout URI

1. In the app registration → **Authentication**.
2. Set **Front-channel logout URL** to `https://monicarivera-portfolio-demo.azurewebsites.net/signout-callback-oidc`.
3. Under **Implicit grant and hybrid flows**, do **not** enable anything (PKCE/auth-code flow is used).
4. Click **Save**.

### 1.4 Create a Client Secret (optional but recommended for production)

1. In the app registration → **Certificates & secrets** → **New client secret**.
2. Set an expiry, click **Add**, and copy the **Value** immediately.
3. Store it as GitHub secret `AZURE_AD_CLIENT_SECRET` and add it to App Service settings as `AzureAd__ClientSecret`.

> **Tip:** For a public portfolio you can omit the client secret and rely on the authorization-code flow (PKCE) alone — Microsoft.Identity.Web will handle it. To do this, leave `ClientSecret` blank in configuration.

---

## 2. Configure App Service Application Settings

In the [Azure Portal](https://portal.azure.com) → **App Services** → `monicarivera-portfolio-demo` → **Configuration** → **Application settings**, add the following:

| Setting name | Value |
|---|---|
| `AzureAd__TenantId` | Your Tenant ID from step 1.2 |
| `AzureAd__ClientId` | Your Client ID from step 1.2 |
| `AzureAd__ClientSecret` | *(optional)* Client secret value from step 1.4 |
| `AZURE_STORAGE_CONNECTION_STRING` | Connection string from the storage deployment (see step 3) |

---

## 3. Deploy the Azure Storage Account

The Code Lab uses Azure Table Storage (cheapest option, ~$0.045/GB/month) to persist named sessions.

### 3.1 Set up GitHub Secrets

Add the following to your GitHub repository → **Settings** → **Secrets and variables** → **Actions**:

| Secret name | How to get it |
|---|---|
| `AZURE_CREDENTIALS` | Run: `az ad sp create-for-rbac --name "portfolio-deploy" --sdk-auth --role contributor --scopes /subscriptions/<sub-id>/resourceGroups/<rg-name>` |
| `AZURE_RESOURCE_GROUP` | The name of your Azure resource group (e.g., `monicarivera-rg`) |

### 3.2 Run the Infra Workflow

- Go to **Actions** → **Deploy CodeLab Infrastructure** → **Run workflow**.
- After it completes, the storage account name and connection string will be printed in the job logs.
- Copy the connection string and add it to App Service settings as `AZURE_STORAGE_CONNECTION_STRING`.

> Alternatively, deploy manually:
> ```bash
> az deployment group create \
>   --resource-group <your-rg> \
>   --template-file infra/storage.bicep
> ```

---

## 4. LeetCode Profile — Notes

The LeetCode analytics page (`/LeetCode`) fetches live data from LeetCode's public GraphQL API for profile `NickyRivers6543`. No authentication is needed for this — it works as long as:

- The profile is **public** on LeetCode.
- LeetCode's API is reachable from the Azure App Service (it is a standard HTTPS request).
- LeetCode doesn't rate-limit the server IP (unlikely for a low-traffic portfolio site).

If LeetCode changes their API schema or blocks the requests, the page will show a user-friendly error message and a link to the public profile page.

---

## 5. Future Enhancements (Optional)

### 5.1 Python Execution in Code Lab
Currently the Code Lab supports **JavaScript only** (runs sandboxed in the browser, zero server cost). To add Python support:
- Option A: Add a **Python Azure Function** (Consumption plan — free tier ~1M requests/month) that receives code, executes it in a restricted subprocess, and returns stdout + a JSON trace log.
- Option B: Use [Pyodide](https://pyodide.org) — WebAssembly Python in the browser (no server needed). Load `pyodide.js` from CDN and hook it into the existing trace framework.

### 5.2 Entra ID — External Identities (B2C)
If you want users **other than yourself** to log in and save their own sessions, consider migrating to **Azure AD B2C** (external identities). The Consumption tier is free for the first 50,000 MAU. This would require a separate B2C tenant setup.

### 5.3 Session Sharing / Public Links
Sessions could be made public-shareable by storing them with a UUID RowKey and exposing a `GET /api/sessions/share/{uuid}` endpoint (no auth required for read).

### 5.4 CodeLab — Multi-language
Add a language selector (already scaffolded in the UI) and route to different backend executors.

---

## 6. Local Development

After cloning, set the following in `PortfolioDemo/appsettings.Development.json` or via `dotnet user-secrets`:

```json
{
  "AzureAd": {
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>"
  },
  "AZURE_STORAGE_CONNECTION_STRING": "<your-storage-connection-string>"
}
```

For local storage testing, you can use [Azurite](https://github.com/Azure/Azurite) (local Azure Storage emulator):
```bash
npx azurite --silent --location ./tmp/azurite
# Connection string: DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;
```
