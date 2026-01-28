# Files API

## Overview

The Files API provides file storage and management for the OASIS ecosystem. Files are stored per avatar. You can list files for the current avatar, upload (including to IPFS via Pinata), download, delete, and get/update metadata.

**Base URL:** `/api/files`

**Authentication:** Required (Bearer token). All endpoints use the current logged-in avatar (AvatarId). Unauthenticated requests often return **HTTP 200** with `isError: true`—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

**Key Features:**
- ✅ **List files** – All files stored for the current avatar
- ✅ **Upload** – Upload file (byte array or multipart); optional metadata
- ✅ **Upload to IPFS** – Multipart upload to Pinata (IPFS); returns IPFS URL for NFT metadata
- ✅ **Download** – Download file by ID
- ✅ **Delete** – Delete file by ID
- ✅ **Metadata** – Get and update file metadata

---

## Quick Start

### List files for current avatar

```http
GET http://api.oasisweb4.com/api/files/get-all-files-stored-for-current-logged-in-avatar
Authorization: Bearer YOUR_JWT_TOKEN
```

### Upload to IPFS (Pinata)

```http
POST http://api.oasisweb4.com/api/files/upload
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: multipart/form-data

file: <binary>
provider: PinataOASIS
```

**Response:** `result` contains the IPFS URL (e.g. `https://gateway.pinata.cloud/ipfs/{hash}`).

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `get-all-files-stored-for-current-logged-in-avatar` | List all files for current avatar |
| POST | `upload-file` | Upload file (fileName, fileData byte[], contentType, optional metadata) |
| POST | `upload` | Upload to IPFS via Pinata (multipart: file, provider=PinataOASIS) |
| GET | `download-file/{fileId}` | Download file by ID |
| DELETE | `delete-file/{fileId}` | Delete file by ID |
| GET | `file-metadata/{fileId}` | Get file metadata |
| PUT | `update-file-metadata/{fileId}` | Update file metadata (body: Dictionary<string, object>) |

---

## Upload file (OASIS storage)

**Endpoint:** `POST /api/files/upload-file`

**Parameters (typical):**

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| fileName | string | query/form | File name |
| fileData | byte[] | body | File bytes |
| contentType | string | query/form | MIME type (e.g. image/png) |
| metadata | Dictionary<string, object> | form | Optional metadata |

**Response:** `result` is a `StoredFile` (id, name, size, contentType, avatarId, etc.).

---

## Upload to IPFS (Pinata)

**Endpoint:** `POST /api/files/upload`

**Request:** `multipart/form-data` with:

- **file** – The file (IFormFile)
- **provider** – `PinataOASIS` (default) or leave empty

**Response:**
```json
{
  "result": "https://gateway.pinata.cloud/ipfs/Qm...",
  "isError": false,
  "message": "File uploaded to IPFS successfully"
}
```

Use the returned URL in NFT metadata (e.g. image field).

---

## Download file

**Endpoint:** `GET /api/files/download-file/{fileId}`

**Response:** `result` is a `FileDownload` (e.g. file data, name, contentType).

---

## Delete file

**Endpoint:** `DELETE /api/files/delete-file/{fileId}`

**Response:** `result: true` on success; check `isError` and `message` on failure.

---

## File metadata

**Get:** `GET /api/files/file-metadata/{fileId}`  
**Update:** `PUT /api/files/update-file-metadata/{fileId}` with body `Dictionary<string, object>`.

---

## Error Handling

- Always check **isError** and **message** in the response body.
- See [Error Code Reference](../../reference/error-codes.md).

---

## Related Documentation

- [Data API](data-api.md) – Data layer overview
- [Holons API](holons-api.md) – Holon CRUD
- [NFT API](../blockchain-wallets/nft-api.md) – Use IPFS URL in NFT metadata

---

*Last Updated: January 24, 2026*
