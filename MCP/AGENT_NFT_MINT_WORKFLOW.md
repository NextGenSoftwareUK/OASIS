# Agent Guide: Authenticate → Generate Image (Glif) → Mint NFT

**For AI agents:** Use **one MCP command** to create an NFT (authenticate → Glif image → mint), or use **curl** for OASIS/Glif APIs.

**Related docs:** [GLIF_IMAGE_GENERATION.md](./GLIF_IMAGE_GENERATION.md), [NATURAL_LANGUAGE_NFT_MINTING.md](./NATURAL_LANGUAGE_NFT_MINTING.md), [NFT_MINTING_FIELDS_REFERENCE.md](./NFT_MINTING_FIELDS_REFERENCE.md), [APIS_RUNNING.md](./APIS_RUNNING.md).

---

## Single MCP command (recommended)

**Tool:** `oasis_create_nft`

**Required:** `username`, `password`, `imagePrompt`, `symbol`  
**Optional:** `title`, `description`, `numberToMint`, `price`, `workflowId`

One call runs: authenticate → generate image with Glif → mint NFT.

**Example:**
```json
{
  "username": "OASIS_ADMIN",
  "password": "YOUR_PASSWORD",
  "imagePrompt": "A futuristic OASIS digital art, holographic, cyberpunk, 4k",
  "symbol": "OASISART",
  "title": "OASIS Glif Art",
  "description": "AI-generated via Glif",
  "numberToMint": 1
}
```

Use this instead of calling `oasis_authenticate_avatar`, `glif_generate_image`, and `oasis_mint_nft` separately.

---

## Prerequisites

1. **OASIS API** running locally (default: `http://localhost:5003`). See [APIS_RUNNING.md](./APIS_RUNNING.md) to build and start.
2. **Avatar credentials** (username + password).
3. **Glif (optional):** For AI-generated images, set `GLIF_API_TOKEN` (e.g. in `MCP/.env`). Get token at [glif.app/settings/api-tokens](https://glif.app/settings/api-tokens).

---

## Option C: Using curl (no MCP)

Base URL used below: `http://localhost:5003` (override with `OASIS_API_URL` if different).

### Step 1: Authenticate and get JWT

```bash
curl -s -X POST http://localhost:5003/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_USERNAME","password":"YOUR_PASSWORD"}'
```

- **JWT location in JSON:** `.result.result.jwtToken`
- **Example (with jq):**  
  `JWT=$(curl -s ... | jq -r '.result.result.jwtToken')`

### Step 2: Generate image with Glif (optional)

If you have `GLIF_API_TOKEN` (e.g. from `MCP/.env`):

```bash
# Load token (e.g. from MCP/.env)
export $(grep -v '^#' /Users/maxgershfield/OASIS_CLEAN/MCP/.env | xargs) 2>/dev/null

curl -s -X POST https://simple-api.glif.app \
  -H "Authorization: Bearer $GLIF_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"id":"cmigcvfwm0000k004u9shifki","inputs":{"input1":"YOUR_IMAGE_PROMPT"}}'
```

- **Image URL in response:** `.output` (string URL, e.g. Cloudinary).
- **Example (with jq):**  
  `IMAGE_URL=$(curl -s ... | jq -r '.output')`

### Step 3: Mint NFT

Use the JWT from Step 1 and (if used) the image URL from Step 2:

```bash
JWT="<paste or substitute from Step 1>"
IMAGE_URL="<paste from Step 2, or use any public image URL>"

curl -s -X POST http://localhost:5003/api/nft/mint-nft \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT" \
  -d "{
    \"Symbol\": \"MYSYMBOL\",
    \"JSONMetaDataURL\": \"https://jsonplaceholder.typicode.com/posts/1\",
    \"Title\": \"My NFT Title\",
    \"Description\": \"Optional description\",
    \"ImageUrl\": \"$IMAGE_URL\",
    \"NumberToMint\": 1,
    \"Price\": 0
  }"
```

- **Required:** `Symbol`, `JSONMetaDataURL`.
- **Recommended:** `Title`, `ImageUrl`, `NumberToMint` (default 1). See [NFT_MINTING_FIELDS_REFERENCE.md](./NFT_MINTING_FIELDS_REFERENCE.md) for full fields.

---

## Option B: Legacy MCP (multiple tools)

If you need to run steps separately (e.g. reuse an image), use these tools in order:

1. **Authenticate** – `oasis_authenticate_avatar` with `username`, `password`.
2. **Generate image (optional)** – `glif_generate_image` with `prompt`; returns `imageUrl` and `imagePath`.
3. **Mint NFT** – `oasis_mint_nft` with `Symbol`, `JSONMetaDataURL`, and optionally `Title`, `ImageUrl`, etc.

Otherwise prefer the single command **`oasis_create_nft`** (see top of this doc).

---

## One-shot curl example (auth + Glif + mint)

Replace `YOUR_USERNAME`, `YOUR_PASSWORD`, and optionally the Glif prompt, then run from a shell that has `GLIF_API_TOKEN` and `jq` available:

```bash
# 1) Auth
JWT=$(curl -s -X POST http://localhost:5003/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_USERNAME","password":"YOUR_PASSWORD"}' | jq -r '.result.result.jwtToken')

# 2) Glif (ensure GLIF_API_TOKEN is set, e.g. from MCP/.env)
export $(grep -v '^#' /Users/maxgershfield/OASIS_CLEAN/MCP/.env | xargs) 2>/dev/null
IMAGE_URL=$(curl -s -X POST https://simple-api.glif.app \
  -H "Authorization: Bearer $GLIF_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"id":"cmigcvfwm0000k004u9shifki","inputs":{"input1":"A futuristic OASIS digital art, holographic, cyberpunk, 4k"}}' | jq -r '.output')

# 3) Mint
curl -s -X POST http://localhost:5003/api/nft/mint-nft \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT" \
  -d "{
    \"Symbol\": \"OASISART\",
    \"JSONMetaDataURL\": \"https://jsonplaceholder.typicode.com/posts/1\",
    \"Title\": \"OASIS Glif Art\",
    \"Description\": \"AI-generated via Glif\",
    \"ImageUrl\": \"$IMAGE_URL\",
    \"NumberToMint\": 1,
    \"Price\": 0
  }"
```

---

## API reference (for agents)

| Step        | Method | URL (local) | JWT |
|------------|--------|-------------|-----|
| Authenticate | POST | `http://localhost:5003/api/avatar/authenticate` | No |
| Mint NFT     | POST | `http://localhost:5003/api/nft/mint-nft`       | Yes (Bearer) |
| Glif image   | POST | `https://simple-api.glif.app`                  | Yes (Bearer `GLIF_API_TOKEN`) |

- **Auth body:** `{"username":"...","password":"..."}`  
- **Auth response JWT path:** `.result.result.jwtToken`  
- **Glif body:** `{"id":"cmigcvfwm0000k004u9shifki","inputs":{"input1":"<prompt>"}}`  
- **Glif image URL path:** `.output`  
- **Mint required fields:** `Symbol`, `JSONMetaDataURL`; recommended: `Title`, `ImageUrl`, `NumberToMint`.

---

## Troubleshooting

- **401 on mint:** Re-run authenticate and use the latest JWT (tokens expire).
- **403 on auth:** Check username/password and that OASIS API is running.
- **Glif error / no `.output`:** Check `GLIF_API_TOKEN` and response body (e.g. `.error`).
- **OASIS API not responding:** Ensure API is running on the correct port (see [APIS_RUNNING.md](./APIS_RUNNING.md)); default is 5003.
