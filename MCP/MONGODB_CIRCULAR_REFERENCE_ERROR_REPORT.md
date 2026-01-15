# MongoDB Circular Reference Error Report

## Problem Summary
Authentication fails with MongoDB circular reference error when trying to save/update avatars after authentication.

## Root Cause
The `CreatedByAvatar`, `ModifiedByAvatar`, and `DeletedByAvatar` properties in `AuditBase` have **lazy-loading getters** that automatically load avatar objects when accessed:

```csharp
public IAvatar CreatedByAvatar
{
    get
    {
        if (_createdByAvatar == null && CreatedByAvatarId != Guid.Empty)
        {
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(CreatedByAvatarId);
            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                _createdByAvatar = avatarResult.Result;
        }
        return _createdByAvatar;
    }
}
```

**The Problem:**
1. When MongoDB tries to serialize the Avatar object during `ReplaceOne()`, it accesses these properties
2. The getters trigger and load nested Avatar objects
3. Those nested avatars also have `CreatedByAvatar` properties
4. This creates an infinite circular reference chain
5. MongoDB serialization fails with "Maximum serialization depth exceeded"

## Why Property Clearing Doesn't Work
Setting `avatar.CreatedByAvatar = null` before saving doesn't help because:
- MongoDB's serializer accesses the property during serialization
- The getter is called, which loads the avatar again
- The circular reference is recreated

## Why BsonIgnore Should Work for Reading
- `[BsonIgnore]` prevents MongoDB from serializing these properties (fixes the write error)
- It also prevents MongoDB from deserializing the full avatar objects when reading
- **HOWEVER**, the `CreatedByAvatarId`, `ModifiedByAvatarId`, `DeletedByAvatarId` GUIDs are still stored and deserialized normally
- When code accesses `avatar.CreatedByAvatar`, the lazy-loading getter will:
  1. Check if `CreatedByAvatarId` is set
  2. Load the avatar using that ID
  3. Return the loaded avatar
- **Result:** Existing avatars can be read, and avatar objects are loaded on-demand via the getters

## Current State
- **Server:** Running and healthy
- **MongoDB Connection:** Working
- **Authentication:** Fails with circular reference error when trying to save avatar after authentication
- **Error Location:** `AvatarRepository.Update()` line 513 (ReplaceOne call)

## Attempted Fixes
1. ✅ Added property clearing in `Update()` and `UpdateAsync()` - **Didn't work** (getter reloads during serialization)
2. ✅ Added property clearing in `Add()` and `AddAsync()` - **Didn't work** (getter reloads during serialization)
3. ⚠️ Added `[BsonIgnore]` attributes - **Should work** (prevents serialization, getters can still load on-demand)
4. ❌ Removed `[BsonIgnore]` - **Back to circular reference error**

## Required Fix
The fix needs to prevent the getter from being called during MongoDB serialization. Options:

1. **Use BsonIgnore + Custom Serializer** - Ignore during serialization but handle deserialization manually
2. **Disable Lazy Loading During Save** - Add a flag to prevent getter from loading during serialization
3. **Use BsonIgnore + Store Only IDs** - Only store `CreatedByAvatarId`, never serialize the object
4. **Custom MongoDB Serialization** - Override serialization to skip these properties

## Recommendation
**Use `[BsonIgnore]` on the `CreatedByAvatar`, `ModifiedByAvatar`, and `DeletedByAvatar` properties.**

**This will:**
- ✅ Fix the circular reference error (properties won't be serialized)
- ✅ Allow reading existing avatars (IDs are still stored, getters load on-demand)
- ✅ Allow saving new avatars (only IDs are stored, not full objects)
- ✅ Maintain functionality (avatar objects loaded via getters when accessed)

**The `CreatedByAvatarId`, `ModifiedByAvatarId`, `DeletedByAvatarId` GUIDs are separate properties and will still be saved/loaded normally.**

**Why it should work:**
- MongoDB stores only the IDs (not the full avatar objects)
- When code accesses `avatar.CreatedByAvatar`, the getter checks `CreatedByAvatarId` and loads the avatar
- No circular reference because full objects aren't stored in MongoDB

