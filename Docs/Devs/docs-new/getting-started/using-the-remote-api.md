# Using the Remote OASIS API

You can use OASIS **without cloning the repo or running anything locally**. The hosted API is available 24/7 at a public base URL.

## Why use the remote API?

- **No setup** — No .NET SDK, no database, no clone or build.
- **Same API** — Same endpoints and behavior as a local ONODE.
- **Quick integration** — Register, authenticate, and call endpoints from any app or script.

---

## Base URL and docs

| Resource | URL |
|----------|-----|
| **API base** | `https://api.oasisweb4.one` |
| **API prefix** | `https://api.oasisweb4.one/api` |
| **Swagger UI** | [https://api.oasisweb4.one/swagger/index.html](https://api.oasisweb4.one/swagger/index.html) |
| **Postman collection** | [OASIS API Postman Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json) |

Use the **API prefix** for all endpoints below (e.g. `https://api.oasisweb4.one/api/avatar/register`).

---

## Quick flow: create an avatar and get a JWT

### 1. Register a new avatar

```bash
curl -s -X POST "https://api.oasisweb4.one/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "myusername",
    "email": "myemail@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Expected:** `isError: false` and a message to verify your email. Check your inbox for the verification link.

### 2. Verify your email

Open the link from the email, or call the verify endpoint with the token from the link:

```bash
# Replace YOUR_VERIFICATION_TOKEN with the token from the email link
curl -s "https://api.oasisweb4.one/api/avatar/verify-email?token=YOUR_VERIFICATION_TOKEN"
```

**Expected:** `isError: false`, `"Email verified successfully"` (or similar).

### 3. Authenticate and get a JWT

```bash
curl -s -X POST "https://api.oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "myusername",
    "password": "SecurePassword123!"
  }'
```

**Response:** JSON with `result` containing your avatar info and a JWT. The token is usually in:

- `result.jwtToken` or  
- `result.token`

Use that value in the `Authorization` header for all authenticated requests.

### 4. Call an authenticated endpoint

```bash
# Set your JWT and avatar ID from the authenticate response
JWT="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
AVATAR_ID="123e4567-e89b-12d3-a456-426614174000"

curl -s "https://api.oasisweb4.one/api/avatar/get-by-id/${AVATAR_ID}" \
  -H "Authorization: Bearer ${JWT}"
```

---

## Response format

Success:

```json
{
  "result": { ... },
  "isError": false,
  "message": "Success"
}
```

Error (still often HTTP 200):

```json
{
  "result": null,
  "isError": true,
  "message": "Error description"
}
```

Always check `isError` in the body; unauthenticated or invalid requests may return 200 with `isError: true`.

---

## Next steps

- [Getting started overview](overview.md) — Same flow with more detail and HTTP snippets.
- [Authentication](authentication.md) — Refresh tokens, revoke, password reset.
- [WEB4 OASIS API overview](../web4-oasis-api/overview.md) — Data, wallets, NFTs, and more.

---

## Running the API locally instead

If you need to run the API yourself (e.g. for development or offline use), see:

- **This repo:** [OASIS Quick Start Guide](../OASIS_Quick_Start_Guide.md) — Clone, build, and run the ONODE WebAPI.
- **Upstream:** [NextGenSoftwareUK/OASIS](https://github.com/NextGenSoftwareUK/OASIS) and the [DeepWiki Getting Started](https://deepwiki.com/NextGenSoftwareUK/OASIS/11.1-getting-started) (clone, restore, run WebAPI).

Once running, use `http://localhost:5003` (or your configured port) as the base URL instead of `https://api.oasisweb4.one`.

---

*Last updated: March 2026*
