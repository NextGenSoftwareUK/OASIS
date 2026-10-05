# WEB4 OASIS API Documentation

## Overview

Welcome to the WEB4 OASIS API documentation. This API provides the foundational layer for the OASIS ecosystem, bridging Web2 and Web3 technologies.

## API Documentation

### Core APIs
- [Avatar API](Avatar-API.md) - User identity and profile management (`/api/avatar`)
- [Keys API](Keys-API.md) - Cryptographic key management (`/api/keys`)
- [Karma API](Karma-API.md) - Reputation and karma tracking (`/api/karma`)
- [Data API](Data-API.md) - Data storage and management (`/api/data`)
- [Wallet API](Wallet-API.md) - Multi-chain wallet operations (`/api/wallet`)
- [NFT API](NFT-API.md) - NFT minting and management (`/api/nft`)

### Network APIs
- [ONET API](ONET-API.md) - OASIS Network operations (`/api/v1/onet`)
- [ONODE API](ONODE-API.md) - OASIS Node operations (`/api/v1/onode`)
- [HyperDrive API](HyperDrive-API.md) - HyperDrive network operations (`/api/hyperdrive`)

### Service APIs
- [Provider API](Provider-API.md) - Provider management (`/api/provider`)
- [Search API](Search-API.md) - Universal search functionality (`/api/search`)
- [Stats API](Stats-API.md) - Statistics and analytics (`/api/stats`)
- [Settings API](Settings-API.md) - Configuration management (`/api/settings`)
- [Map API](Map-API.md) - Mapping and location services (`/api/map`)

### Communication APIs
- [Chat API](Chat-API.md) - Real-time chat functionality (`/api/chat`)
- [Messaging API](Messaging-API.md) - Messaging services (`/api/messaging`)
- [Files API](Files-API.md) - File storage and management (`/api/files`)
- [Competition API](Competition-API.md) - Competition management (`/api/competition`)
- [Gifts API](Gifts-API.md) - Gift and rewards system (`/api/gifts`)

### Blockchain APIs
- [Solana API](Solana-API.md) - Solana blockchain integration (`/api/solana`)
- [EOSIO API](EOSIO-API.md) - EOSIO blockchain integration (`/api/eosio`)
- [Holochain API](Holochain-API.md) - Holochain integration (`/api/holochain`)
- [Telos API](Telos-API.md) - Telos blockchain integration (`/api/telos`)
- [OLand API](OLand-API.md) - Virtual land management (`/api/oland`)
- [Cargo API](Cargo-API.md) - Logistics and cargo management (`/api/cargo`)
- [Subscription API](Subscription-API.md) - Subscription management (`/api/subscription`)

### Admin APIs
- [OLand Unit API](OLand-Unit-API.md) - OLand unit management (`/api/admin/olandunit`)

### Disabled APIs
- [OAPP API](OAPP-API.md) - OASIS Application management (`/api/oapp`) - *Currently commented out*
- [Core API](Core-API.md) - Core functionality (`/api/core`) - *Currently commented out*
- [Cargo API](Cargo-API.md) - Logistics and cargo management (`/api/cargo`) - *Currently commented out*

### SCMS (Supply Chain Management System) APIs - *Currently Disabled*
- [SCMS Contacts API](SCMS-Contacts-API.md) - Contact management (`/api/scms/contacts`) - *Currently commented out*
- [SCMS Contracts API](SCMS-Contracts-API.md) - Contract management (`/api/scms/contracts`) - *Currently commented out*
- [SCMS Deliveries API](SCMS-Deliveries-API.md) - Delivery tracking (`/api/scms/deliveries`) - *Currently commented out*
- [SCMS Delivery Items API](SCMS-DeliveryItems-API.md) - Delivery item management (`/api/scms/deliveryitems`) - *Currently commented out*
- [SCMS Drawings API](SCMS-Drawings-API.md) - Technical drawings (`/api/scms/drawings`) - *Currently commented out*
- [SCMS Files API](SCMS-Files-API.md) - File management (`/api/scms/files`) - *Currently commented out*
- [SCMS Handovers API](SCMS-Handovers-API.md) - Handover management (`/api/scms/handovers`) - *Currently commented out*
- [SCMS Links API](SCMS-Links-API.md) - Link management (`/api/scms/links`) - *Currently commented out*
- [SCMS Logs API](SCMS-Logs-API.md) - Logging system (`/api/scms/logs`) - *Currently commented out*
- [SCMS Materials API](SCMS-Materials-API.md) - Material management (`/api/scms/materials`) - *Currently commented out*
- [SCMS Notes API](SCMS-Notes-API.md) - Note management (`/api/scms/notes`) - *Currently commented out*
- [SCMS Phases API](SCMS-Phases-API.md) - Phase management (`/api/scms/phases`) - *Currently commented out*
- [SCMS Sequences API](SCMS-Sequences-API.md) - Sequence management (`/api/scms/sequences`) - *Currently commented out*
- [SCMS Triggers API](SCMS-Triggers-API.md) - Trigger management (`/api/scms/triggers`) - *Currently commented out*

### Additional APIs
- [Eggs API](Eggs-API.md) - Egg collection system (`/api/eggs`)
- [Social API](Social-API.md) - Social features (`/api/social`)
- [Video API](Video-API.md) - Video processing (`/api/video`)
- [Share API](Share-API.md) - Content sharing (`/api/share`)
- [Seeds API](Seeds-API.md) - SEEDS integration (`/api/seeds`)
- [Telos API](Telos-API.md) - Telos blockchain integration (`/api/telos`)

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