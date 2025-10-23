# WEB4 OASIS API Documentation

## 📋 **Overview**

The WEB4 OASIS API is the core data aggregation layer that unifies all Web2 and Web3 technologies into a single, intelligent, auto-failover system. It provides universal access to blockchain networks, cloud providers, storage systems, and network protocols through a unified interface.

## 🏗️ **Architecture**

### **Core Components**
- **AVATAR API**: Centralized user data and identity management
- **KARMA API**: Digital reputation and positive action tracking
- **DATA API**: Seamless Web2/Web3 data transfer
- **WALLET API**: High-security, cross-chain wallet with fiat integration
- **NFT API**: Cross-chain NFTs with geo-caching
- **KEYS API**: Secure key storage and backup
- **ONET API**: Integration with the ONET decentralized network
- **SEARCH API**: Universal search across all OASIS data
- **STATS API**: Comprehensive statistics and analytics
- **HOLOCHAIN API**: Integration for distributed applications
- **IPFS API**: Decentralized storage
- **ONODE API**: ONODE status, config, providers, stats
- **SETTINGS API**: Get/Update system settings
- **MAP API**: Load/Save/Search/Update/Delete maps
- **CHAT API**: Conversations, send message, messages, create conversation
- **MESSAGING API**: Inbox, send message, sent, mark read
- **SOCIAL API**: Friends, add/remove friend, followers, follow/unfollow, feed
- **FILES API**: Upload/Download/List/Delete/Share files

### **Key Features**
- **Universal Compatibility**: Works with 50+ blockchain networks
- **Auto-Failover**: Intelligent provider switching
- **Cross-Chain Support**: Native multi-blockchain operations
- **Real-Time Analytics**: Comprehensive monitoring and statistics
- **Secure Authentication**: Multi-layer security with key management
- **Provider Management**: Dynamic provider configuration and monitoring

## 🔗 **Base URL**
```
https://api.oasisplatform.world
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **API Documentation**

### **🔐 Authentication & Identity**
- [Avatar API](Avatar-API.md) - Complete user management system (80+ endpoints)
- [Keys API](Keys-API.md) - Cryptographic key management (40+ endpoints)

### **💎 Reputation & Social**
- [Karma API](Karma-API.md) - Digital reputation system (20+ endpoints)
- [Social API](Social-API.md) - Social networking features (4+ endpoints)
- [Chat API](Chat-API.md) - Real-time communication (3+ endpoints)
- [Messaging API](Messaging-API.md) - Advanced messaging (6+ endpoints)

### **💾 Data Management**
- [Data API](Data-API.md) - Comprehensive data operations (30+ endpoints)
- [Files API](Files-API.md) - File management system (6+ endpoints)
- [Search API](Search-API.md) - Universal search (2+ endpoints)

### **💰 Financial Services**
- [Wallet API](Wallet-API.md) - Multi-chain wallet management (25+ endpoints)
- [NFT API](NFT-API.md) - Cross-chain NFT operations (20+ endpoints)
- [Subscription API](Subscription-API.md) - Billing and quotas (8+ endpoints)

### **🌐 Network & Discovery**
- [ONET API](ONET-API.md) - Decentralized networking (10+ endpoints)
- [ONODE API](ONODE-API.md) - Node management (12+ endpoints)
- [Provider API](Provider-API.md) - Provider management (25+ endpoints)

### **🎮 Gamification**
- [Competition API](Competition-API.md) - Gaming system (9+ endpoints)
- [Gifts API](Gifts-API.md) - Gift system (6+ endpoints)
- [Eggs API](Eggs-API.md) - Discovery system (3+ endpoints)

### **🗺️ Location & Mapping**
- [Map API](Map-API.md) - Advanced mapping (15+ endpoints)

### **🔗 Blockchain Integration**
- [EOSIO API](EOSIO-API.md) - EOSIO integration (9+ endpoints)
- [Holochain API](Holochain-API.md) - Holochain integration (7+ endpoints)
- [Telos API](Telos-API.md) - Telos integration (9+ endpoints)
- [Seeds API](Seeds-API.md) - Seeds integration (15+ endpoints)
- [Solana API](Solana-API.md) - Solana integration (2+ endpoints)

### **⚙️ System Management**
- [Settings API](Settings-API.md) - Configuration (12+ endpoints)
- [Stats API](Stats-API.md) - Analytics (8+ endpoints)
- [Video API](Video-API.md) - Video calling (3+ endpoints)
- [Share API](Share-API.md) - Content sharing (2+ endpoints)

## 📊 **Response Format**

### **Success Response**
```json
{
  "result": {
    "success": true,
    "data": {},
    "message": "Operation completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Error Response**
```json
{
  "result": null,
  "isError": true,
  "message": "Error description",
  "exception": "Exception details"
}
```

## 🚀 **Getting Started**

### **1. Authentication**
```http
POST /api/avatar/authenticate
Content-Type: application/json

{
  "username": "your_username",
  "password": "your_password"
}
```

### **2. Create Your First Avatar**
```http
POST /api/avatar/register
Content-Type: application/json

{
  "username": "your_username",
  "email": "your_email@example.com",
  "password": "secure_password",
  "firstName": "Your",
  "lastName": "Name"
}
```

## 📈 **Rate Limits**

- **Standard**: 1000 requests per hour
- **Premium**: 10000 requests per hour
- **Enterprise**: Unlimited

## 🛡️ **Security**

### **HTTPS Only**
All API endpoints require HTTPS encryption.

### **Authentication Required**
Most endpoints require valid authentication tokens.

### **Rate Limiting**
API calls are rate-limited to prevent abuse.

## 📞 **Support**

For technical support and questions:
- **Documentation**: [OASIS API Docs](https://docs.oasisplatform.world)
- **Support**: [support@oasisplatform.world](mailto:support@oasisplatform.world)
- **Community**: [OASIS Discord](https://discord.gg/oasis)

## 🔄 **Versioning**

The API uses semantic versioning:
- **Current Version**: v1.0.0
- **Version Header**: `API-Version: v1.0.0`
- **Deprecation Policy**: 6 months notice for breaking changes

## 📝 **Changelog**

### **v1.0.0** (Current)
- Initial release of WEB4 OASIS API
- Complete avatar management system
- Multi-chain wallet support
- Comprehensive data operations
- Advanced provider management
- Real-time analytics and monitoring
