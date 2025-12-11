# Architecture Decision: Shipex Pro as Separate API

## Decision

**Shipex Pro will run as a separate API service**, not integrated into the main OASIS API.

## Rationale

### ✅ **Why Separate?**

1. **Business Application vs Core Provider**
   - Shipex Pro is a **business application** (logistics/shipping)
   - OASIS providers are **infrastructure** (storage, blockchain, network)
   - Different concerns, different deployment needs

2. **Modularity**
   - Can be deployed independently
   - Can scale separately
   - Can be updated without affecting OASIS API

3. **Clean Architecture**
   - Follows microservices principles
   - Clear separation of concerns
   - Easier to maintain and test

4. **Flexibility**
   - Can use different tech stack if needed
   - Can be replaced/upgraded independently
   - Doesn't bloat main OASIS API

### ❌ **Why NOT in OASIS API?**

1. **Not a Core Provider**
   - Shipex Pro is application-specific
   - Not used by other OASIS applications
   - Would add unnecessary dependencies to OASIS API

2. **Deployment Complexity**
   - Forces OASIS API to include Shipex Pro dependencies
   - Makes OASIS API larger and more complex
   - Harder to version independently

3. **Provider Pattern Mismatch**
   - OASIS providers are infrastructure (storage, blockchain)
   - Shipex Pro is business logic (shipping, logistics)
   - Different abstraction levels

## Architecture

```
┌─────────────────────────────────────────┐
│         Shipex Pro Frontend            │
│      (shipex-pro-frontend/)            │
└──────────────┬──────────────────────────┘
               │
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌──────────────┐  ┌──────────────┐
│ Shipex Pro   │  │  OASIS API   │
│     API      │  │  (Auth Only) │
│  Port: 5005  │  │  Port: 5002  │
└──────┬───────┘  └──────────────┘
       │
       ▼
┌──────────────┐
│   MongoDB    │
│ (ShipexPro)  │
└──────────────┘
```

## Integration Points

### 1. **Authentication**
- Frontend calls OASIS API for avatar auth
- Gets JWT token
- Uses token for Shipex Pro API calls

### 2. **Merchant Linking**
- Shipex Pro API stores merchant profiles
- Links to OASIS avatar via `AvatarId`
- Merchant operations in Shipex Pro API

### 3. **Data Storage**
- Shipex Pro uses its own MongoDB database
- Separate from OASIS data
- Can use OASIS MongoDB connection string if desired

## Implementation

### Shipex Pro API (`ShipexPro.API/`)
- Standalone ASP.NET Core Web API
- All Shipex Pro controllers
- All Shipex Pro services
- Own configuration and startup

### OASIS API
- **No changes needed**
- Continues to provide avatar authentication
- Shipex Pro calls it for auth only

### Frontend
- Calls OASIS API for authentication
- Calls Shipex Pro API for business operations
- Configure both URLs in `shipex-api.js`

## Benefits

1. ✅ **Clean Separation** - Business logic separate from infrastructure
2. ✅ **Independent Deployment** - Deploy Shipex Pro without touching OASIS
3. ✅ **Easier Testing** - Test Shipex Pro in isolation
4. ✅ **Better Scalability** - Scale Shipex Pro independently
5. ✅ **Maintainability** - Clear boundaries and responsibilities

## Alternative (If Needed Later)

If you want to integrate into OASIS API later:
1. Add project reference to OASIS WebAPI
2. Register services in OASIS Startup.cs
3. Controllers auto-discover via `MapControllers()`

But **separate is recommended** for this use case.

---

**Decision Date:** January 2025  
**Status:** ✅ Separate API Service
