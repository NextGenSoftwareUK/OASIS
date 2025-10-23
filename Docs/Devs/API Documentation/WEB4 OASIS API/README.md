# WEB4 OASIS API Documentation

## Overview

Welcome to the WEB4 OASIS API documentation. This API provides the foundational layer for the OASIS ecosystem, bridging Web2 and Web3 technologies.

## API Documentation

### Core APIs
- [Avatar API](Avatar-API.md) - User identity and profile management
- [Keys API](Keys-API.md) - Cryptographic key management
- [Karma API](Karma-API.md) - Reputation and karma tracking
- [Data API](Data-API.md) - Data storage and management
- [Wallet API](Wallet-API.md) - Multi-chain wallet operations
- [NFT API](NFT-API.md) - NFT minting and management
- [ONET API](ONET-API.md) - OASIS Network operations

### Extended APIs
- [Provider API](Provider-API.md) - Provider management
- [Search API](Search-API.md) - Universal search functionality
- [Stats API](Stats-API.md) - Statistics and analytics
- [Settings API](Settings-API.md) - Configuration management
- [Map API](Map-API.md) - Mapping and location services
- [Chat API](Chat-API.md) - Real-time chat functionality
- [Messaging API](Messaging-API.md) - Messaging services
- [Files API](Files-API.md) - File storage and management
- [Competition API](Competition-API.md) - Competition management
- [Gifts API](Gifts-API.md) - Gift and rewards system

### Blockchain APIs
- [HyperDrive API](HyperDrive-API.md) - HyperDrive network operations
- [Subscription API](Subscription-API.md) - Subscription management
- [Solana API](Solana-API.md) - Solana blockchain integration
- [OLand API](OLand-API.md) - Virtual land management
- [Cargo API](Cargo-API.md) - Logistics and cargo management

## Getting Started

All APIs require authentication using Bearer tokens. Include your token in the Authorization header:

```http
Authorization: Bearer YOUR_TOKEN
```

## Response Format

All API responses follow the standard OASIS response format:

```json
{
  "result": {
    "success": true,
    "data": { ... },
    "message": "Operation successful"
  },
  "isError": false,
  "message": "Success"
}
```

## Support

For support and questions, please visit the [OASIS Documentation](../../DEVELOPER_DOCUMENTATION_INDEX.md).

---

**[Back to Main Documentation](../README.md)**