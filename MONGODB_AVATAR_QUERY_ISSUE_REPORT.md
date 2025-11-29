# MongoDB Avatar Query Issue - Error Report

**Date:** November 28, 2025  
**Reporter:** Max Gershfield  
**Component:** OASIS API ONODE WebAPI  
**Severity:** High - Prevents wallet API from working

---

## Executive Summary

The OASIS API successfully connects to MongoDB Atlas and the MongoDBOASIS provider activates correctly. However, when querying for avatars by GUID, the API returns "Avatar Not Found" even though the avatar exists in the database. This prevents the wallet API from loading wallets.

---

## Symptoms

1. ✅ **MongoDB Atlas Connection:** Successfully connected
2. ✅ **Provider Activation:** `MongoDBOASIS Provider Activated Successfully (Async)`
3. ✅ **Avatar Authentication:** Works correctly - can authenticate and get JWT token
4. ❌ **Avatar Query by GUID:** Returns "Avatar Not Found" when loading wallets
5. ❌ **Wallet API:** Fails with "Avatar Not Found" error

---

## Function Call Chain

**Frontend Call:**
```typescript
// zypherpunk-wallet-ui/lib/store.ts:39
await oasisWalletAPI.loadWalletsById(targetId);
```

**API Endpoint:**
```
GET /api/wallet/avatar/{id}/wallets
```

**Controller Method:**
```csharp
// ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/WalletController.cs:87
[HttpGet("avatar/{id}/wallets")]
public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> 
LoadProviderWalletsForAvatarByIdAsync(Guid id, ProviderType providerType = ProviderType.Default)
{
    return await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, providerTypeToLoadFrom: providerType);
}
```

**Manager Method:**
```csharp
// NextGenSoftware.OASIS.API.Core/Managers/WalletManager.cs:1193
public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> 
LoadProviderWalletsForAvatarByIdAsync(Guid id, ...)
{
    // Line 1201: Hardcoded to LocalFileOASIS (TODO: Temp!)
    providerTypeToLoadFrom = ProviderType.LocalFileOASIS;
    
    // Line 1209: Calls storage provider
    result = ((IOASISLocalStorageProvider)providerResult.Result).LoadProviderWalletsForAvatarById(id);
}
```

**Error Location:**
The error occurs when the wallet loading process internally calls `AvatarManager.LoadAvatarForProviderAsync()` to load the avatar first, and that's where the MongoDB query fails.

**Note:** There's a hardcoded override in `WalletManager.LoadProviderWalletsForAvatarByIdAsync` (line 1201) that forces `ProviderType.LocalFileOASIS`, but the avatar loading uses `MongoDBOASIS` (as configured in `AutoFailOverProvidersForAvatarLogin`). The avatar lookup happens before wallet loading, which is why the error references `MongoDBOASIS`.

**Error Message:**
```
Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with id 89d907a8-5859-4171-b6c5-621bfe96930d for provider MongoDBOASIS. Reason: Avatar Not Found.
```

---

## Technical Details

### Avatar Confirmed in MongoDB Atlas

**Database:** `OASISAPI_DEV`  
**Collection:** `Avatar`  
**Query:** `db.Avatar.findOne({HolonId: '89d907a8-5859-4171-b6c5-621bfe96930d'})`

**Result:** Avatar exists with:
- `HolonId`: `89d907a8-5859-4171-b6c5-621bfe96930d` (stored as **String**)
- `Username`: `metabricks_admin`
- `Email`: `max.gershfield1@gmail.com`
- `_id`: `ObjectId('68cfe7a42422a0055a84acee')`

### API Error Logs

```
Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with id 89d907a8-5859-4171-b6c5-621bfe96930d for provider MongoDBOASIS. Reason: Avatar Not Found.
```

### Code Analysis

**Repository Query (AvatarRepository.cs:134):**
```csharp
FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
```

**Entity Definition (HolonBase.cs:43):**
```csharp
public Guid HolonId { get; set; } // C# type is Guid
```

**Serializer Registration (SerializerRegister.cs:43):**
```csharp
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
```

**MongoDB Storage:**
- `HolonId` is stored as **String** in MongoDB
- Serializer is configured to serialize GUIDs as strings
- Serializer is registered in `AvatarRepository` constructor

---

## Root Cause Hypothesis

The GUID serializer is registered in the `AvatarRepository` constructor, but there may be a timing issue where:
1. MongoDB context is created before serializer registration
2. Query filter builder doesn't use the serializer correctly
3. GUID-to-string conversion isn't happening during query execution

**Key Difference from Previous Working Setup:**
- Previously used local MongoDB (`mongodb://localhost:27017`)
- Now using MongoDB Atlas (`mongodb+srv://...`)
- Avatar exists in Atlas but queries fail

---

## Configuration

**OASIS_DNA.json:**
```json
"MongoDBOASIS": {
    "DBName": "OASISAPI_DEV",
    "ConnectionString": "mongodb+srv://OASISWEB4:Uppermall1%21@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4"
}
```

**AutoFailOver Configuration:**
```json
"AutoFailOverProvidersForAvatarLogin": "MongoDBOASIS"
```

---

## What Works

- ✅ MongoDB Atlas connection
- ✅ Provider activation
- ✅ Avatar authentication (can login and get JWT token)
- ✅ Avatar exists in database (confirmed via direct MongoDB query)

## What Doesn't Work

- ❌ Loading avatar by GUID when called from wallet API
- ❌ Wallet API endpoints (blocked by avatar lookup failure)
- ❌ Query: `x.HolonId == id` where `id` is `Guid` and `HolonId` is stored as `String` in MongoDB

---

## Questions for David

1. **GUID Serialization:** Is the serializer registration timing correct? Should it be registered before MongoDB context creation?

2. **Query Behavior:** Why does the query `x.HolonId == id` fail when:
   - `HolonId` is stored as String in MongoDB
   - Serializer is configured for `BsonType.String`
   - Serializer is registered in repository constructor

3. **MongoDB Atlas:** Are there any known issues with MongoDB Atlas connections and GUID serialization in the current version?

4. **Workaround:** Is there a way to query avatars by `HolonId` that works with string storage, or should we migrate the data format?

---

## Impact

- **Blocked Functionality:** Wallet API cannot load wallets for authenticated users
- **User Experience:** Users can authenticate but cannot access their wallets
- **Workaround:** Wallet UI handles missing avatars gracefully (shows empty wallets), but users cannot load existing wallets

---

## Test Credentials

- **Username:** `metabricks_admin`
- **Password:** `Uppermall1!`
- **Avatar ID:** `89d907a8-5859-4171-b6c5-621bfe96930d`
- **MongoDB Atlas:** Avatar confirmed to exist in database

---

## Files Involved

- `/Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs` (line 134)
- `/Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Infrastructure/Singleton/SerializerRegister.cs` (line 43)
- `/Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Entities/HolonBase.cs` (line 43)

---

## Request

Please advise on:
1. Correct way to query GUIDs stored as strings in MongoDB
2. Whether serializer registration timing is the issue
3. If there's a configuration change needed for MongoDB Atlas
4. Recommended fix approach

