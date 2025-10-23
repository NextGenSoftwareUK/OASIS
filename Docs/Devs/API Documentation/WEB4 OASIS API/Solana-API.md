# Solana API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Wallet Operations](#wallet-operations)
- [Transaction Operations](#transaction-operations)
- [Token Operations](#token-operations)
- [Error Responses](#error-responses)

## Overview

The Solana API provides integration with the Solana blockchain for the OASIS ecosystem. It handles wallet management, transactions, and token operations.

## Wallet Operations

### Get Wallet Balance
```http
GET /api/solana/wallet/{address}/balance
Authorization: Bearer YOUR_TOKEN
```

### Create Wallet
```http
POST /api/solana/wallet
Authorization: Bearer YOUR_TOKEN
```

### Get Wallet Transactions
```http
GET /api/solana/wallet/{address}/transactions
Authorization: Bearer YOUR_TOKEN
```

## Transaction Operations

### Send Transaction
```http
POST /api/solana/transaction/send
Authorization: Bearer YOUR_TOKEN
```

### Get Transaction Status
```http
GET /api/solana/transaction/{signature}
Authorization: Bearer YOUR_TOKEN
```

### Get Transaction History
```http
GET /api/solana/transactions
Authorization: Bearer YOUR_TOKEN
```

## Token Operations

### Get Token Balance
```http
GET /api/solana/token/{mint}/balance
Authorization: Bearer YOUR_TOKEN
```

### Transfer Token
```http
POST /api/solana/token/transfer
Authorization: Bearer YOUR_TOKEN
```

### Get Token Info
```http
GET /api/solana/token/{mint}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Wallet Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Wallet not found"
}
```

### Transaction Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Transaction failed"
}
```

---

## Navigation

**← Previous:** [Subscription API](Subscription-API.md) | **Next:** [OLand API](OLand-API.md) →
