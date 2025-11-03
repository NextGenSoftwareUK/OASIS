# EOSIO API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
- [Error Responses](#error-responses)

## Overview

The EOSIO API provides integration with the EOSIO blockchain platform, enabling EOSIO-specific operations and transactions.

## Authentication

All endpoints require authentication using Bearer tokens:

```http
Authorization: Bearer YOUR_TOKEN
```

## Endpoints

### Blockchain Operations

#### Get EOSIO Account Info
```http
GET /api/eosio/account/{accountName}
Authorization: Bearer YOUR_TOKEN
```

#### Get EOSIO Balance
```http
GET /api/eosio/balance/{accountName}
Authorization: Bearer YOUR_TOKEN
```

#### Send EOSIO Transaction
```http
POST /api/eosio/transaction
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "from": "account1",
  "to": "account2",
  "amount": "1.0000 EOS",
  "memo": "Transfer"
}
```

#### Get EOSIO Transaction History
```http
GET /api/eosio/transactions/{accountName}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Invalid Account
```json
{
  "result": null,
  "isError": true,
  "message": "EOSIO account not found"
}
```

### Transaction Failed
```json
{
  "result": null,
  "isError": true,
  "message": "EOSIO transaction failed"
}
```

---

## Navigation

**← Previous:** [Data API](Data-API.md) | **Next:** [Files API](Files-API.md) →
