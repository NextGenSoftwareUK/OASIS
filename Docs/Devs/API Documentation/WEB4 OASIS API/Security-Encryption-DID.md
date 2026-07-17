# OASIS Security: 3-Layer Password Encryption & DID SSO

## Table of Contents

- [Overview](#overview)
- [3-Layer Password Encryption](#3-layer-password-encryption)
  - [How It Works](#how-it-works)
  - [Layer 1: BCrypt](#layer-1-bcrypt)
  - [Layer 2: Rijndael-256 (AES-256-CBC)](#layer-2-rijndael-256-aes-256-cbc)
  - [Layer 3: Quantum-Resistant AES-256-GCM](#layer-3-quantum-resistant-aes-256-gcm)
  - [Configuring Encryption in OASISDNA](#configuring-encryption-in-oasisdna)
  - [Stored Password Format](#stored-password-format)
  - [Backwards Compatibility](#backwards-compatibility)
- [DID (Decentralized Identifier) SSO](#did-decentralized-identifier-sso)
  - [Overview](#did-overview)
  - [Avatar DID Properties](#avatar-did-properties)
  - [Enabling DID Support](#enabling-did-support)
  - [DID in JWT Tokens](#did-in-jwt-tokens)
  - [DID Authentication Flow](#did-authentication-flow)
  - [Registering a DID Public Key](#registering-a-did-public-key)
  - [GET /did-challenge/{did}](#get-did-challengedid)
  - [POST /authenticate-did](#post-authenticate-did)
  - [Client-Side Example (C#)](#client-side-example-c)
  - [Client-Side Example (JavaScript)](#client-side-example-javascript)
- [Security Recommendations](#security-recommendations)

---

## Overview

OASIS supports a configurable triple-layer password encryption stack and W3C Decentralized Identifier (DID) based single sign-on (SSO). All settings are controlled through `OASISDNA.json` under `OASIS.Security`, so you can tune the security level to match your deployment without changing any code.

---

## 3-Layer Password Encryption

### How It Works

When a password is saved (on registration, password reset, or avatar save), it is passed through up to three encryption layers in order:

```
Plaintext password
    ↓  [Layer 1 — BCrypt]
BCrypt hash  ($2a$11$...)
    ↓  [Layer 2 — Rijndael-256 / AES-256-CBC]
r256:<base64(IV + ciphertext)>
    ↓  [Layer 3 — AES-256-GCM]
qpq:<base64(nonce + tag + ciphertext)>
```

Only the layers that are enabled in the DNA are applied. The stored value always carries a prefix that identifies which layers are present, so verification can always peel the correct layers regardless of which combination is active.

When verifying a login:
1. The outer `qpq:` layer (AES-256-GCM) is decrypted if present.
2. The `r256:` layer (AES-256-CBC) is decrypted if present.
3. The remaining BCrypt hash is verified against the submitted password using `BCrypt.Verify`.

### Layer 1: BCrypt

- **Algorithm:** BCrypt with work factor 11 (adaptive, slow by design)
- **Purpose:** Converts the plaintext password into a salted, one-way hash. This is the core password hashing layer and should always be enabled.
- **DNA setting:** `BCryptEncryptionEnabled: true`
- **Output prefix:** `$2a$11$...` (standard BCrypt format)

### Layer 2: Rijndael-256 (AES-256-CBC)

- **Algorithm:** AES-256 (Rijndael with 256-bit key) in CBC mode with PKCS7 padding
- **Purpose:** Symmetrically encrypts the BCrypt hash so that even if the storage medium is compromised, the BCrypt hashes are not directly exposed to offline cracking attempts.
- **DNA settings:** `Rijndael256EncryptionEnabled: true`, `Rijndael256Key: "<your-passphrase>"`
- **Key derivation:** SHA-256 of the passphrase string → 32-byte key
- **IV:** Randomly generated per encryption, prepended to the stored ciphertext
- **Output prefix:** `r256:`

> **Important:** The `Rijndael256Key` should be a long, randomly generated string. Store it securely (e.g., environment variable or secrets manager), not in the committed `OASIS_DNA.json`.

### Layer 3: Quantum-Resistant AES-256-GCM

- **Algorithm:** AES-256-GCM (Galois/Counter Mode — authenticated encryption)
- **Purpose:** Adds a second symmetric encryption layer using authenticated encryption (AEAD). AES-256 provides approximately 128-bit security against Grover's algorithm, making it resistant to quantum attacks. The GCM tag also provides integrity protection — any tampering with the stored value is detected at decryption time.
- **DNA settings:** `QuantumEncryptionEnabled: true`, `QuantumEncryptionKey: "<your-passphrase>"`
- **Key derivation:** SHA-256 of the passphrase string → 32-byte key
- **Nonce:** 12 bytes, randomly generated per encryption
- **Tag:** 16 bytes, stored alongside the ciphertext
- **Output prefix:** `qpq:`

> **Note:** The `QuantumEncryptionKey` must be different from `Rijndael256Key`. Use two independently generated random strings.

### Configuring Encryption in OASISDNA

In `OASIS_DNA.json` under `OASIS.Security.AvatarPassword`:

```json
"AvatarPassword": {
    "BCryptEncryptionEnabled": true,
    "Rijndael256EncryptionEnabled": true,
    "Rijndael256Key": "your-long-random-rijndael-passphrase",
    "QuantumEncryptionEnabled": true,
    "QuantumEncryptionKey": "your-long-random-quantum-passphrase"
}
```

The same structure exists under `OASISProviderPrivateKeys` to apply the same stack when encrypting provider private keys:

```json
"OASISProviderPrivateKeys": {
    "BCryptEncryptionEnabled": true,
    "Rijndael256EncryptionEnabled": true,
    "Rijndael256Key": "your-long-random-rijndael-passphrase",
    "QuantumEncryptionEnabled": true,
    "QuantumEncryptionKey": "your-long-random-quantum-passphrase"
}
```

**Minimum recommended configuration (production):**

```json
"AvatarPassword": {
    "BCryptEncryptionEnabled": true,
    "Rijndael256EncryptionEnabled": true,
    "Rijndael256Key": "<64+ character random string>",
    "QuantumEncryptionEnabled": true,
    "QuantumEncryptionKey": "<64+ character random string — different from Rijndael256Key>"
}
```

**Default (development/testing):**

```json
"AvatarPassword": {
    "BCryptEncryptionEnabled": true,
    "Rijndael256EncryptionEnabled": false,
    "Rijndael256Key": "",
    "QuantumEncryptionEnabled": false,
    "QuantumEncryptionKey": ""
}
```

### Stored Password Format

| Value starts with | Layers applied |
|---|---|
| `qpq:` | BCrypt + Rijndael-256 + AES-256-GCM |
| `r256:` | BCrypt + Rijndael-256 |
| `$2` | BCrypt only |

### Backwards Compatibility

Existing BCrypt-only hashes (`$2a$...`) continue to work even after enabling layers 2 and 3. The system detects whether a stored value is already hashed (via the prefix check) and never double-hashes. New or updated passwords will use the currently-configured stack.

---

## DID (Decentralized Identifier) SSO

### DID Overview

Every OASIS avatar has a W3C Decentralized Identifier (DID) of the form:

```
did:oasis:<avatarId>
```

For example:
```
did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

This DID can be used as an alternative authentication method (in place of username/password) via a cryptographic challenge-response flow. When DID auth is enabled, the DID is also embedded as a claim in all issued JWT tokens, making it available to downstream services for identity verification.

### Avatar DID Properties

Two new properties are available on every avatar:

| Property | Type | Description |
|---|---|---|
| `DID` | string | W3C DID. Auto-derived as `did:oasis:<Id>` if not explicitly set. |
| `DIDPublicKey` | string | Base64-encoded SubjectPublicKeyInfo (DER) of the avatar's ECDsa P-256 public key. Set this to enable DID authentication for the avatar. |

### Enabling DID Support

In `OASIS_DNA.json` under `OASIS.Security`:

```json
"Security": {
    "DIDEnabled": true,
    ...
}
```

When `DIDEnabled` is `false` (the default), the `did` claim is omitted from JWTs and `POST /authenticate-did` returns an error.

### DID in JWT Tokens

When `DIDEnabled: true`, every JWT issued by the OASIS API includes a `did` claim:

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "did": "did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "exp": 1750000000,
  "iss": "OASIS",
  "aud": "OASIS"
}
```

Downstream services can extract and verify the DID from the JWT to establish identity without querying the OASIS API.

### DID Authentication Flow

DID authentication uses an ECDsa P-256 (NIST P-256 / prime256v1) challenge-response with a server-issued nonce:

```
1. Client generates a P-256 keypair (one-time setup).
2. Client stores their public key on their avatar (via the avatar update endpoint).
3. To authenticate:
   a. Client calls GET /api/avatar/did-challenge/{did} to obtain a server-issued nonce.
   b. Client signs SHA-256(nonce) with their P-256 private key.
   c. Client POSTs {DID, Challenge (the nonce), Signature} to POST /api/avatar/authenticate-did.
   d. Server verifies the nonce was issued by itself and has not expired or been replayed.
   e. Server verifies the signature against the stored DIDPublicKey.
   f. On success, server issues a JWT + refresh token exactly like the password auth flow.
```

The nonce expires after **5 minutes** and is **single-use** — attempting to authenticate with the same nonce twice always fails.

### Registering a DID Public Key

Before a user can log in via DID, they must register their ECDsa P-256 public key on their avatar. Use the standard avatar update endpoint:

```http
PATCH /api/avatar/{id}
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "DIDPublicKey": "<base64-encoded SubjectPublicKeyInfo>"
}
```

The `DIDPublicKey` is the Base64-encoded DER encoding of the SubjectPublicKeyInfo structure, which is the standard portable format for public keys. See the client examples below for how to generate this.

### GET /did-challenge/{did}

Request a server-issued nonce before authenticating. This is the required first step — the nonce ties the challenge to the server and expires after 5 minutes, preventing replay attacks.

```http
GET /api/avatar/did-challenge/{did}
```

| Parameter | Type | Description |
|---|---|---|
| `did` | path string | The avatar's W3C DID (`did:oasis:<avatarId>`) |

**Success response (200):**

```json
{
  "isError": false,
  "result": "K7gX2mPqR1vN8sL0dFhT4wYcZjU5oIeA3bQnVxOy6pC=",
  "message": "Challenge issued. Sign SHA-256 of this value with your DID private key and submit to /authenticate-did within 300 seconds."
}
```

The `result` string is the nonce to sign. Pass it as `Challenge` in the subsequent `/authenticate-did` call.

**Error responses:**

| Status | Reason |
|---|---|
| 400 | DID missing or DID auth not enabled in DNA |

> **One nonce per DID at a time.** Calling `/did-challenge` again for the same DID invalidates the previous nonce. The nonce is single-use — it is consumed and deleted the moment `/authenticate-did` validates it.

### POST /authenticate-did

Authenticate using a DID challenge-response instead of a password.

```http
POST /api/avatar/authenticate-did
Content-Type: application/json
```

**Request body:**

```json
{
  "DID": "did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "Challenge": "some-challenge-string-the-client-signed",
  "Signature": "<base64-encoded IEEE P1363 signature>"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `DID` | string | Yes | The avatar's W3C DID (`did:oasis:<id>`) |
| `Challenge` | string | Yes | The challenge string that was signed |
| `Signature` | string | Yes | Base64-encoded ECDsa P-256 IEEE P1363 signature (64 bytes: R[32] \|\| S[32]) of SHA-256(Challenge) |

**Success response (200):**

Same structure as the standard `/authenticate` endpoint — returns the avatar with `JwtToken` and sets the `refreshToken` HTTP-only cookie.

```json
{
  "isError": false,
  "isSaved": true,
  "message": "Avatar Successfully Authenticated via DID.",
  "result": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "did": "did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "username": "johnsmith",
    "jwtToken": "eyJ...",
    ...
  }
}
```

**Error responses:**

| Status | Reason |
|---|---|
| 400 | DID, Challenge or Signature missing |
| 401 | DID not found, DIDPublicKey not set, avatar not verified/active, or signature invalid |
| 401 | DID authentication not enabled (`DIDEnabled: false` in DNA) |

### Client-Side Example (C#)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// ── One-time setup: Generate keypair and register the public key on the avatar ──
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

string publicKeyBase64  = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
byte[] privateKeyBytes  = ecdsa.ExportECPrivateKey(); // store securely, never send to server

// PATCH /api/avatar/{id}  with body: { "DIDPublicKey": publicKeyBase64 }

// ── Authentication (two-step) ──
var http = new HttpClient { BaseAddress = new Uri("https://api.web4.oasisomniverse.one") };
string did = "did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890";

// Step 1: Request a server-issued challenge nonce
var challengeResponse = await http.GetFromJsonAsync<OASISResponse<string>>(
    $"/api/avatar/did-challenge/{Uri.EscapeDataString(did)}");
string challenge = challengeResponse.Result;

// Step 2: Sign the challenge and authenticate
byte[] challengeHash   = SHA256.HashData(Encoding.UTF8.GetBytes(challenge));
byte[] signatureBytes  = ecdsa.SignHash(challengeHash); // IEEE P1363: 64 bytes (R||S)
string signatureBase64 = Convert.ToBase64String(signatureBytes);

var authResponse = await http.PostAsJsonAsync("/api/avatar/authenticate-did", new
{
    DID       = did,
    Challenge = challenge,
    Signature = signatureBase64
});
```

**Restoring from a saved private key:**

```csharp
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
ecdsa.ImportECPrivateKey(savedPrivateKeyBytes, out _);
```

### Client-Side Example (JavaScript)

```js
// ── One-time setup: Generate keypair and register the public key on the avatar ──
const keyPair = await crypto.subtle.generateKey(
    { name: 'ECDSA', namedCurve: 'P-256' },
    true,
    ['sign', 'verify']
);

const spki           = await crypto.subtle.exportKey('spki', keyPair.publicKey);
const publicKeyB64   = btoa(String.fromCharCode(...new Uint8Array(spki)));
// PATCH /api/avatar/{id}  with body: { "DIDPublicKey": publicKeyB64 }

// ── Authentication (two-step) ──
const did = 'did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890';

// Step 1: Request a server-issued challenge nonce
const challengeRes = await fetch(`/api/avatar/did-challenge/${encodeURIComponent(did)}`);
const { result: challenge } = await challengeRes.json();

// Step 2: Sign the challenge and authenticate
// WebCrypto ECDSA sign() hashes the message internally with SHA-256
const challengeBytes = new TextEncoder().encode(challenge);
const sigBytes = await crypto.subtle.sign(
    { name: 'ECDSA', hash: { name: 'SHA-256' } },
    keyPair.privateKey,
    challengeBytes
);
const signatureB64 = btoa(String.fromCharCode(...new Uint8Array(sigBytes)));

const authRes = await fetch('/api/avatar/authenticate-did', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ DID: did, Challenge: challenge, Signature: signatureB64 })
});
const auth = await authRes.json();
// auth.result.jwtToken contains the JWT
```

> **Note:** `crypto.subtle.sign` with `{ hash: 'SHA-256' }` hashes the message internally before signing. The server also hashes the `Challenge` string with SHA-256 before verifying — both sides operate on the same `Challenge` string.

---

## Security Recommendations

| Recommendation | Details |
|---|---|
| Use all 3 layers in production | BCrypt + Rijndael-256 + AES-256-GCM provides defence in depth against storage breaches and future quantum attacks |
| Separate keys | `Rijndael256Key` and `QuantumEncryptionKey` must be different strings |
| Key length | Use at least 64 randomly generated characters for each passphrase |
| Store keys in secrets | Never commit keys to source control. Use environment variables, Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, or similar |
| DID private key storage | Store DID private keys in the client's secure enclave, OS keychain, or hardware security key — never on the server |
| Challenge freshness | Validate that the DID challenge is within an acceptable time window (±60 seconds) or use server-issued single-use nonces to prevent replay attacks |
| Rotate keys periodically | When rotating `Rijndael256Key` or `QuantumEncryptionKey`, re-hash all passwords during the next login cycle |
