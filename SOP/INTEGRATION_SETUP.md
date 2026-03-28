# SOP Integration Setup Guide

This guide covers how to configure each of the 9 named connectors available in the OASIS SOP platform. Each connector can be added to a SOP step in the Workflow Builder and configured in the node's ConfigPanel.

---

## Contents
1. [Slack](#1-slack)
2. [Email (SendGrid / SES / Mailgun / SMTP)](#2-email)
3. [Salesforce](#3-salesforce)
4. [HubSpot](#4-hubspot)
5. [Zendesk](#5-zendesk)
6. [DocuSign](#6-docusign)
7. [Google Calendar](#7-google-calendar)
8. [Jira](#8-jira)
9. [Zapier (Universal)](#9-zapier-universal)

---

## 1. Slack

**Purpose in SOPs:** Send step-completion notifications, deviation alerts, and run summaries to Slack channels or DMs.

### Setup (Incoming Webhook — recommended)

1. Go to [api.slack.com/apps](https://api.slack.com/apps) → **Create New App** → "From Scratch"
2. Name it something like `OASIS SOP` and select your workspace
3. In the sidebar, go to **Incoming Webhooks** → toggle **On**
4. Click **Add New Webhook to Workspace** → choose the channel → **Allow**
5. Copy the webhook URL: `https://hooks.slack.com/services/T.../B.../...`
6. Paste into the Slack connector's **Webhook URL** field in the node ConfigPanel

### Message Template Variables
```
{{sop.name}}         — SOP display name
{{run.id}}           — Current run identifier
{{step.name}}        — Name of the step that triggered this node
{{avatar.name}}      — Name of the Avatar who completed the step
{{completedAt}}      — ISO timestamp of step completion
{{deviation.reason}} — Deviation reason (for deviation alert action)
```

### OAuth App (Future)
> **Status: Stub** — The connector UI shows an "OAuth App (coming soon)" option. The Slack OAuth2 scope required is `chat:write`. The server-side OAuth exchange endpoint will be `POST /api/integrations/slack/oauth/callback`. No server-side changes are needed now — the stub button is present in the UI for future activation.

---

## 2. Email

**Purpose in SOPs:** Send step assignment notifications, overdue reminders, welcome emails, and run summaries.

### Setup — SendGrid (recommended)
1. Create an account at [app.sendgrid.com](https://app.sendgrid.com)
2. Go to **Settings → API Keys → Create API Key** (Full Access or "Mail Send" restricted)
3. Copy the key (starts with `SG.`)
4. In the Email node: select **Provider: SendGrid**, paste the API Key, set From/To addresses

### Setup — Amazon SES
1. Verify your sending domain in SES Console → **Verified Identities**
2. Go to **IAM → Users → Create User** with `AmazonSESFullAccess` policy
3. Create access key — paste the access key and secret as the API Key in format: `ACCESS_KEY_ID:SECRET_ACCESS_KEY`
4. Select Provider: **Amazon SES**

### Setup — Mailgun
1. [mailgun.com](https://mailgun.com) → **API Keys → Create Private API Key**
2. Key format: `key-xxxxxxxxxxxxxxxx`
3. Select Provider: **Mailgun**

### Template Variables
```
{{avatar.firstName}}  {{avatar.lastName}}  {{avatar.email}}
{{sop.name}}          {{run.id}}           {{step.name}}
{{customer.name}}     {{customer.email}}   {{dueDate}}
```

---

## 3. Salesforce

**Purpose in SOPs:** Update opportunity stage, create activities/tasks, update contacts. Typically used for CS onboarding SOPs triggered from "Closed Won".

### Setup — Access Token (Username + Password)
1. In Salesforce: **Setup → Users → Your user → Reset Security Token** (token emailed to you)
2. Go to **Setup → Apps → App Manager → New Connected App**
   - Enable OAuth, add callback URL `http://localhost:5001/api/integrations/sf/callback`
   - Select scopes: `api`, `refresh_token`
3. For immediate use without OAuth: generate a session token via:
   ```bash
   curl -X POST https://login.salesforce.com/services/oauth2/token \
     -d "grant_type=password&client_id=CLIENT_ID&client_secret=CLIENT_SECRET\
         &username=YOUR_EMAIL&password=YOUR_PASSWORD+SECURITY_TOKEN"
   ```
4. Copy the `access_token` and paste into the connector's **Access Token** field

### OAuth2 (Future)
> **Status: Stub** — "Connected App (OAuth2)" option is present. The server endpoint `POST /api/integrations/salesforce/oauth/callback` will handle the token exchange. Required OAuth scopes: `api`, `refresh_token`, `offline_access`.

### Field Templates
```json
{
  "StageName": "Customer",
  "CloseDate": "{{run.completedAt}}",
  "Description": "OASIS SOP run {{run.id}} completed"
}
```

---

## 4. HubSpot

**Purpose in SOPs:** Update deal stages, log timeline events, create follow-up tasks. Used for sales process SOPs triggered from HubSpot deal stage webhooks.

### Setup — Private App Token (recommended)
1. In HubSpot: **Settings → Integrations → Private Apps → Create Private App**
2. Name: `OASIS SOP`; Scopes: `crm.objects.deals.write`, `crm.objects.contacts.write`, `timeline`, `tasks`
3. Click **Create** → copy the token (format: `pat-na1-xxxxxxxx-...`)
4. Paste into **Private App Token** field in the HubSpot node

### OAuth2 (Future)
> **Status: Stub** — HubSpot OAuth2 uses the standard Auth Code flow. Required scopes: `crm.objects.deals.write crm.objects.contacts.write`. Callback: `POST /api/integrations/hubspot/oauth/callback`.

### Common Field Updates
```json
{
  "dealstage": "closedwon",
  "hs_deal_stage_probability": "1.0",
  "closedate": "{{run.completedAt}}"
}
```

---

## 5. Zendesk

**Purpose in SOPs:** Create tickets when SOPs start, add step-completion comments, resolve tickets when SOPs complete. Ideal for customer support and IT service desk SOPs.

### Setup — API Token
1. In Zendesk Admin: **Channels → API → API Token → Add API token**
2. Note your subdomain (from `yourcompany.zendesk.com`)
3. In the Zendesk node: enter **Subdomain**, **Agent Email**, and the **API Token**
4. The API uses Basic Auth: `email@yourcompany.com/token:YOUR_API_TOKEN`

### OAuth2 (Future)
> **Status: Stub** — Zendesk OAuth2 uses Auth Code flow. Required scopes: `read`, `write`. App registration at `https://yoursubdomain.zendesk.com/oauth/applications`. Callback: `POST /api/integrations/zendesk/oauth/callback`.

### Useful Macros
```
{{run.id}}    → Use as Zendesk ticket external_id for easy linking back
{{sop.name}}  → Use as ticket subject prefix
```

---

## 6. DocuSign

**Purpose in SOPs:** Send legally-binding signature requests for customer go-live approvals, contracts, and compliance sign-offs. The SOP step waits for the envelope to be signed before advancing.

### Setup — Integration Key + Access Token
1. Log in to [apps-d.docusign.com](https://apps-d.docusign.com) (sandbox) or [apps.docusign.com](https://apps.docusign.com) (production)
2. Go to **Settings → Apps and Keys → Add App and Integration Key**
3. Name: `OASIS SOP`; add Redirect URI: `http://localhost:5001/api/integrations/docusign/callback`
4. To get an access token for immediate testing:
   ```
   POST https://account-d.docusign.com/oauth/token
   Body: grant_type=authorization_code&code=...
   ```
   Or use the DocuSign [Token Generator](https://developers.docusign.com/tools/oauth-token-generator) for quick tokens
5. Copy your **Account ID** from the API Keys page
6. Paste **Account ID** + **Access Token** + select base path (sandbox vs production)

### JWT Grant (Future — Service Account)
> **Status: Stub** — For server-to-server integrations without a user OAuth flow. Requires RSA key pair upload to DocuSign and administrator consent. Endpoint: `POST /api/integrations/docusign/jwt`. This is the production-ready approach for automated SOP sign-off sending.

---

## 7. Google Calendar

**Purpose in SOPs:** Create kickoff meetings, 30/60/90-day check-ins, deadline reminders, and on-site visits as steps in customer success, project management, or compliance SOPs.

### Setup — Service Account (recommended for automation)
1. Go to [console.cloud.google.com](https://console.cloud.google.com) → Create Project
2. Enable the **Google Calendar API** from API Library
3. Go to **IAM & Admin → Service Accounts → Create Service Account**
4. Create a JSON key: **Actions → Manage Keys → Add Key → JSON** — download the file
5. Paste the entire JSON key file contents into **Service Account Key (JSON)** field in the node
6. Share your Google Calendar with the service account email: `yourapp@yourproject.iam.gserviceaccount.com`

### OAuth2 User Auth (Future)
> **Status: Stub** — For creating events in the user's personal calendar (requires user OAuth flow). Scopes: `https://www.googleapis.com/auth/calendar`. Callback: `POST /api/integrations/google-calendar/oauth/callback`.

---

## 8. Jira

**Purpose in SOPs:** Create and track issues alongside SOP runs — useful for technical onboarding SOPs, DevOps processes, and IT service management flows where work must also be tracked in Jira.

### Setup — API Token
1. Log in to [id.atlassian.com](https://id.atlassian.com) → **Security → Create and manage API tokens**
2. Create a token with a descriptive name: `OASIS SOP`
3. In the Jira node: enter your **Jira Instance URL** (e.g. `https://yourcompany.atlassian.net`), **Email**, and the **API Token**
4. Authentication is Basic Auth: `email:token` — this is handled automatically by the connector

### OAuth2 (Future)
> **Status: Stub** — Atlassian uses OAuth2 3LO (3-Legged OAuth). Required scopes: `read:jira-work`, `write:jira-work`. App registration at [developer.atlassian.com](https://developer.atlassian.com). Callback: `POST /api/integrations/jira/oauth/callback`.

### Finding Transition IDs
To use the "Transition Issue" action, you need the numeric transition ID:
```bash
curl -u email@co.com:API_TOKEN \
  https://yourco.atlassian.net/rest/api/3/issue/CS-123/transitions
```
The response contains `{"transitions": [{"id": "31", "name": "In Progress"}, ...]}`.

---

## 9. Zapier (Universal)

**Purpose in SOPs:** The universal escape hatch — trigger any of 7,000+ Zapier-connected apps when a SOP step completes, a run starts, or a deviation is detected. No OAuth required — just a Zapier Webhook URL.

### Setup
1. Log in to [zapier.com](https://zapier.com) → **Create Zap**
2. **Trigger:** Select **Webhooks by Zapier → Catch Hook** → **Continue**
3. Copy the custom webhook URL (format: `https://hooks.zapier.com/hooks/catch/xxxxxxx/xxxxxxx/`)
4. Paste into the **Zapier Webhook URL** field in the node ConfigPanel
5. In Zapier, complete the Zap with any Action (e.g. create Airtable record, update Notion page, send SMS via Twilio)

### Payload sent by OASIS SOP
```json
{
  "sopId": "{{sop.id}}",
  "sopName": "{{sop.name}}",
  "runId": "{{run.id}}",
  "stepId": "{{step.id}}",
  "stepName": "{{step.name}}",
  "triggerEvent": "step_completed",
  "avatarId": "{{avatar.id}}",
  "avatarName": "{{avatar.name}}",
  "completedAt": "{{completedAt}}",
  "proofHolonId": "{{run.proofHolonId}}"
}
```

### Custom Payload Override
You can override the default payload in the **Custom Payload (JSON)** field to send exactly the data your downstream Zap needs:
```json
{
  "customer_id": "{{trigger.customerId}}",
  "deal_size": "{{trigger.dealSize}}",
  "stage": "completed",
  "timestamp": "{{completedAt}}"
}
```

---

## OAuth Architecture (Future — All Connectors)

When OAuth is fully implemented, the server-side flow will be:

```
SOP App → GET /api/integrations/{provider}/connect?orgId={id}
  → Redirect to provider's auth URL
  → Provider redirects to POST /api/integrations/{provider}/callback
  → Server exchanges code for access + refresh tokens
  → Tokens stored encrypted in OrgHolon.IntegrationSecrets (Vault-backed)
  → SOP nodes use stored tokens — no user action required at runtime
```

This is consistent with how OASIS handles multi-provider persistence — OAuth tokens are stored as encrypted holon fields in VaultController, not in environment variables.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Slack message not delivered | Webhook URL expired or channel deleted | Regenerate webhook in Slack Apps page |
| Salesforce 401 | Session token expired (24h TTL) | Generate fresh session token or switch to OAuth |
| DocuSign envelope not sent | Account ID mismatch | Check Account ID in DocuSign Apps & Keys page |
| Jira transition fails | Invalid transition ID | Run the transitions API to get current IDs |
| Zapier not triggering | Test the webhook manually with curl | `curl -X POST YOUR_ZAPIER_URL -d '{"test": true}'` |
| Google Calendar permission denied | Service account not shared with calendar | Share calendar with service account email in Google Calendar settings |
