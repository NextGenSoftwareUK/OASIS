# Telos API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Telos Management](#telos-management)
- [Telos Operations](#telos-operations)
- [Error Responses](#error-responses)

## Overview

The Telos API provides Telos blockchain integration for the OASIS ecosystem. It handles Telos transactions, smart contracts, and account management.

## Telos Management

### Get Telos Account
```http
GET /api/telos/account/{accountName}
Authorization: Bearer YOUR_TOKEN
```

### Get Telos Balance
```http
GET /api/telos/balance/{accountName}
Authorization: Bearer YOUR_TOKEN
```

### Get Telos Transactions
```http
GET /api/telos/transactions/{accountName}
Authorization: Bearer YOUR_TOKEN
```

## Telos Operations

### Send Telos Transaction
```http
POST /api/telos/send
Authorization: Bearer YOUR_TOKEN
```

### Execute Smart Contract
```http
POST /api/telos/contract/execute
Authorization: Bearer YOUR_TOKEN
```

### Get Contract Info
```http
GET /api/telos/contract/{contractName}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Account Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Telos account not found"
}
```

---

## Navigation

**← Previous:** [Seeds API](Seeds-API.md) | **Next:** [Avatar API](Avatar-API.md) →
