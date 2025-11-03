# Holochain API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
- [Error Responses](#error-responses)

## Overview

The Holochain API provides integration with the Holochain distributed computing platform, enabling decentralized application operations.

## Authentication

All endpoints require authentication using Bearer tokens:

```http
Authorization: Bearer YOUR_TOKEN
```

## Endpoints

### Holochain Operations

#### Get Holochain DNA
```http
GET /api/holochain/dna
Authorization: Bearer YOUR_TOKEN
```

#### Get Holochain Agent Info
```http
GET /api/holochain/agent
Authorization: Bearer YOUR_TOKEN
```

#### Create Holochain Entry
```http
POST /api/holochain/entry
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "entryType": "holon",
  "content": {
    "name": "My Holon",
    "description": "A test holon"
  }
}
```

#### Get Holochain Entry
```http
GET /api/holochain/entry/{hash}
Authorization: Bearer YOUR_TOKEN
```

#### Link Holochain Entries
```http
POST /api/holochain/link
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "base": "entry_hash_1",
  "target": "entry_hash_2",
  "linkType": "parent"
}
```

## Error Responses

### Holochain Connection Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Failed to connect to Holochain"
}
```

### Entry Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Holochain entry not found"
}
```

---

## Navigation

**← Previous:** [EOSIO API](EOSIO-API.md) | **Next:** [HyperDrive API](HyperDrive-API.md) →
