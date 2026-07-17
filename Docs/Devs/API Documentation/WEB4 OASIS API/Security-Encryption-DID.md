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

DID authentication uses an ECDsa P-256 (NIST P-256 / prime256v1) challenge-response:

```
1. Client generates a P-256 keypair.
2. Client stores their public key on their avatar (via the avatar update endpoint).
3. To authenticate:
   a. Client constructs a challenge string (e.g. a timestamp or server-issued nonce).
   b. Client signs SHA-256(challenge) with their P-256 private key.
   c. Client POSTs {DID, Challenge, Signature} to POST /api/avatar/authenticate-did.
   d. Server looks up the avatar by DID, verifies the signature against the stored DIDPublicKey.
   e. On success, server issues a JWT + refresh token exactly like the password auth flow.
```

> **Challenge freshness:** The challenge string should be short-lived to prevent replay attacks. Use a server-issued nonce (future feature) or a timestamp that your server validates is within an acceptable window (e.g. ±60 seconds of server time).

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
using System.Security.Cryptography;
using System.Text;

// ── Step 1: Generate keypair (do this once, store the private key securely) ──
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

// Export public key in SubjectPublicKeyInfo (DER) format — store this on the avatar
string publicKeyBase64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

// Export private key — store this securely on the client, never send it to the server
byte[] privateKeyBytes = ecdsa.ExportECPrivateKey();

// ── Step 2: Register the public key on the avatar ──
// PATCH /api/avatar/{id}  with body: { "DIDPublicKey": publicKeyBase64 }

// ── Step 3: Authenticate with DID ──
string did = "did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890";
string challenge = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); // or server nonce

byte[] challengeHash = SHA256.HashData(Encoding.UTF8.GetBytes(challenge));
byte[] signature = ecdsa.SignHash(challengeHash); // IEEE P1363 format (64 bytes)
string signatureBase64 = Convert.ToBase64String(signature);

// POST /api/avatar/authenticate-did
// Body: { "DID": did, "Challenge": challenge, "Signature": signatureBase64 }
```

**Restoring from saved private key bytes:**

```csharp
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
ecdsa.ImportECPrivateKey(privateKeyBytes, out _);
```

### Client-Side Example (JavaScript)

```js
// ── Step 1: Generate keypair ──
const keyPair = await crypto.subtle.generateKey(
    { name: 'ECDSA', namedCurve: 'P-256' },
    true,   // extractable
    ['sign', 'verify']
);

// Export public key as SubjectPublicKeyInfo (spki) — store on the avatar
const spkiBytes = await crypto.subtle.exportKey('spki', keyPair.publicKey);
const publicKeyBase64 = btoa(String.fromCharCode(...new Uint8Array(spkiBytes)));

// ── Step 2: Authenticate ──
const did = 'did:oasis:a1b2c3d4-e5f6-7890-abcd-ef1234567890';
const challenge = String(Date.now()); // or server-issued nonce

const challengeBytes = new TextEncoder().encode(challenge);
const challengeHash  = await crypto.subtle.digest('SHA-256', challengeBytes);

const sigBytes = await crypto.subtle.sign(
    { name: 'ECDSA', hash: 'SHA-256' }, // WebCrypto hashes internally
    keyPair.privateKey,
    challengeHash   // pass the pre-computed hash
);

// Note: WebCrypto's ECDSA sign() hashes again internally when you pass the raw message.
// To sign a pre-hashed value, use SignHash equivalent — pass the raw hash bytes as data.
// Alternatively, pass the raw challenge bytes directly and let the API accept either form:
const sigBytes2 = await crypto.subtle.sign(
    { name: 'ECDSA', hash: { name: 'SHA-256' } },
    keyPair.privateKey,
    challengeBytes  // WebCrypto will SHA-256 this internally before signing
);

const signatureBase64 = btoa(String.fromCharCode(...new Uint8Array(sigBytes2)));

const response = await fetch('/api/avatar/authenticate-did', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ DID: did, Challenge: challenge, Signature: signatureBase64 })
});
```

> **Note for JavaScript clients:** `crypto.subtle.sign` with `{ name: 'ECDSA', hash: 'SHA-256' }` accepts the raw message and hashes it internally. The server also hashes the `Challenge` string with SHA-256 before verifying. This means both sides hash the same `Challenge` string — do not pre-hash on the client before passing to `subtle.sign`.

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
