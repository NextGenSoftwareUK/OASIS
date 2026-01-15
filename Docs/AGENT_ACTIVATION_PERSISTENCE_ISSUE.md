# Agent Activation Persistence Issue - Analysis & Solution

**Last Updated:** January 2026  
**Status:** 🔍 Issue Identified - Fix Required

---

## Problem Summary

Agents are being created with `IsActive = true` in code, but the value is not persisting to MongoDB. Authentication fails with "This avatar is no longer active", indicating `IsActive` is `false` when loaded from MongoDB.

---

## Current Implementation Status

### ✅ What's Working

1. **Code Sets `IsActive = true`:**
   - Line 167 in `AvatarManager-Private.cs`: Sets `IsActive = true` for agents with owners during creation
   - Line 317 in `AvatarManager-Private.cs`: Sets `IsActive = true` during auto-verification
   - Line 1082 in `AvatarManager-Private.cs`: Preserves `IsActive = true` for new holons in `PrepareAvatarForSaving`

2. **MongoDB Mapping Exists:**
   - `ConvertOASISAvatarToMongoEntity()` (line 421): Maps `IsActive` to MongoDB entity
   - `ConvertMongoEntityToOASISAvatar()` (line 163): Maps `IsActive` back from MongoDB
   - `ConvertOASISAvatarDetailToMongoEntity()` (line 529): Maps `IsActive` for AvatarDetail

3. **MongoDB Entity Has Property:**
   - `HolonBase.cs` (line 72): `IsActive` property exists
   - `Avatar.cs` inherits from `HolonBase`, so has `IsActive`

### ❌ What's Not Working

**Issue:** `IsActive = true` is set in code but not persisting to MongoDB documents.

**Symptoms:**
- Agent created with `IsActive = true` in memory
- Save operation completes successfully
- When loaded from MongoDB, `IsActive = false`
- Authentication fails: "This avatar is no longer active"

---

## Root Cause Analysis

### Possible Causes

#### 1. **Default Value Issue**

**Problem:** MongoDB entity might default `IsActive` to `false` if not explicitly set.

**Check:**
```csharp
// In HolonBase.cs
public bool IsActive { get; set; }  // Defaults to false if not set
```

**Solution:** Ensure `IsActive` is explicitly set before conversion to MongoDB entity.

#### 2. **Timing Issue**

**Problem:** `IsActive` might be set after the avatar is converted to MongoDB entity.

**Flow:**
```
1. Create avatar → IsActive not set yet
2. ConvertOASISAvatarToMongoEntity() → IsActive = false (default)
3. Set IsActive = true on OASIS avatar
4. Save → MongoDB entity still has IsActive = false
```

**Solution:** Set `IsActive = true` BEFORE calling `ConvertOASISAvatarToMongoEntity()`.

#### 3. **PrepareAvatarForSaving Resets It**

**Problem:** `PrepareAvatarForSaving()` might be resetting `IsActive` after it's set.

**Current Code (line 1082):**
```csharp
if (!avatar.IsActive)
    avatar.IsActive = true;
```

**Issue:** This only sets it if it's `false`, but if it's already `true`, it should preserve it. However, if `PrepareAvatarForSaving` is called AFTER `IsActive` is set, and it's checking a different condition, it might reset it.

**Solution:** Ensure `PrepareAvatarForSaving` preserves `IsActive = true` if already set.

#### 4. **Save Operation Not Persisting**

**Problem:** The save operation might not be including `IsActive` in the update.

**Check:** MongoDB update operations might be using a partial update that excludes `IsActive`.

**Solution:** Verify MongoDB update includes all fields, or explicitly include `IsActive`.

#### 5. **IsNewHolon Flag Issue**

**Problem:** If `IsNewHolon = false` when saving, it might use `UpdateAsync` which could have different behavior.

**Current Code:**
```csharp
return DataHelper.ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon ?
   await _avatarRepository.AddAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)) :
   await _avatarRepository.UpdateAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)));
```

**Issue:** If `IsNewHolon` is incorrectly set to `false` during auto-verification save, it might use `UpdateAsync` which could have different field handling.

**Solution:** Ensure `IsNewHolon = true` for the first save, or ensure `UpdateAsync` includes `IsActive`.

---

## Investigation Steps

### Step 1: Check MongoDB Document Directly

**Query MongoDB:**
```javascript
db.Avatar.findOne({ "HolonId": "agent-guid-here" })
```

**Check:**
- Does `IsActive` field exist?
- What is its value?
- Is it `false` or missing?

### Step 2: Add Logging

**Add logging in `ConvertOASISAvatarToMongoEntity`:**
```csharp
public static Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar, bool mapChildren = true)
{
    // ... existing code ...
    
    mongoAvatar.IsActive = avatar.IsActive;
    
    // ADD LOGGING
    LoggingManager.Log($"ConvertOASISAvatarToMongoEntity: avatar.IsActive = {avatar.IsActive}, mongoAvatar.IsActive = {mongoAvatar.IsActive}", LogType.Info);
    
    return mongoAvatar;
}
```

**Add logging in `SaveAvatarAsync`:**
```csharp
public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
{
    // ADD LOGGING BEFORE
    LoggingManager.Log($"SaveAvatarAsync BEFORE: avatar.IsActive = {avatar.IsActive}, IsNewHolon = {avatar.IsNewHolon}", LogType.Info);
    
    var mongoEntity = DataHelper.ConvertOASISAvatarToMongoEntity(avatar);
    LoggingManager.Log($"SaveAvatarAsync: mongoEntity.IsActive = {mongoEntity.IsActive}", LogType.Info);
    
    var result = avatar.IsNewHolon ?
       await _avatarRepository.AddAsync(mongoEntity) :
       await _avatarRepository.UpdateAsync(mongoEntity);
    
    // ADD LOGGING AFTER
    if (result.Result != null)
    {
        LoggingManager.Log($"SaveAvatarAsync AFTER: savedEntity.IsActive = {result.Result.IsActive}", LogType.Info);
    }
    
    return DataHelper.ConvertMongoEntityToOASISAvatar(result);
}
```

### Step 3: Check UpdateAsync Implementation

**Check `AvatarRepository.UpdateAsync`:**
```csharp
// Does it update all fields or only changed fields?
// Does it include IsActive in the update?
```

---

## Recommended Fixes

### Fix 1: Ensure IsActive is Set Before Conversion

**In `AvatarRegistered()` method:**

```csharp
if (shouldAutoVerify)
{
    // Set IsActive FIRST, before any save operations
    result.Result.IsActive = true;
    result.Result.Verified = DateTime.UtcNow;
    result.Result.VerificationToken = null;
    
    // Ensure IsNewHolon is true for first save
    result.Result.IsNewHolon = true;
    
    // Save the verified agent
    var saveResult = SaveAvatarAsync(result.Result).Result;
    
    // Verify IsActive persisted
    if (!saveResult.IsError && saveResult.Result != null)
    {
        if (!saveResult.Result.IsActive)
        {
            // Force update IsActive
            saveResult.Result.IsActive = true;
            saveResult.Result.IsNewHolon = false; // Now it's an update
            var updateResult = SaveAvatarAsync(saveResult.Result).Result;
            if (!updateResult.IsError && updateResult.Result != null)
            {
                result.Result = updateResult.Result;
            }
        }
        else
        {
            result.Result = saveResult.Result;
        }
    }
}
```

### Fix 2: Explicitly Set IsActive in PrepareAvatarForSaving

**In `PrepareAvatarForSaving()` method:**

```csharp
else
{
    // For new holons, always set IsActive = true
    // For agents with owners, preserve IsActive = true if already set
    if (avatar.IsNewHolon)
    {
        // New holon - set IsActive = true
        avatar.IsActive = true;
    }
    else
    {
        // Existing holon - preserve IsActive if already true
        // Only set to true if not explicitly set to false
        if (!avatar.IsActive && avatar.MetaData != null && avatar.MetaData.ContainsKey("OwnerAvatarId"))
        {
            // Agent with owner - should be active
            avatar.IsActive = true;
        }
    }
    
    avatar.CreatedDate = DateTime.Now;
    // ... rest of code
}
```

### Fix 3: Verify MongoDB Update Includes IsActive

**Check `AvatarRepository.UpdateAsync`:**

```csharp
public async Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar)
{
    // Ensure IsActive is included in the update
    var update = Builders<Avatar>.Update
        .Set(x => x.IsActive, avatar.IsActive)
        // ... other fields
        .Set(x => x.ModifiedDate, DateTime.UtcNow);
    
    // Or use ReplaceOne to replace entire document
    var result = await _dbContext.Avatar.ReplaceOneAsync(
        x => x.HolonId == avatar.HolonId,
        avatar
    );
    
    return new OASISResult<Avatar> { Result = avatar };
}
```

### Fix 4: Add Default Value in MongoDB Entity

**In `HolonBase.cs`:**

```csharp
public bool IsActive { get; set; } = true;  // Default to true instead of false
```

**Note:** This might affect other holons, so use with caution.

---

## Testing Plan

### Test 1: Create Agent and Check MongoDB

```csharp
// 1. Create agent with verified owner
var agentResult = await AvatarManager.Instance.RegisterAsync(
    // ... agent details ...
    ownerAvatarId: verifiedOwnerId
);

// 2. Immediately check MongoDB
var mongoCheck = await CheckMongoDBDirectly(agentResult.Result.Id);
Assert.IsTrue(mongoCheck.IsActive, "IsActive should be true in MongoDB");

// 3. Reload from MongoDB
var reloaded = await AvatarManager.Instance.LoadAvatarAsync(agentResult.Result.Id, false, false);
Assert.IsTrue(reloaded.Result.IsActive, "IsActive should be true after reload");
```

### Test 2: Add Logging and Trace

```csharp
// Add logging at each step:
// 1. Before ConvertOASISAvatarToMongoEntity
// 2. After ConvertOASISAvatarToMongoEntity
// 3. Before SaveAvatarAsync
// 4. After SaveAvatarAsync
// 5. After ConvertMongoEntityToOASISAvatar
// 6. Check MongoDB directly
```

### Test 3: Verify Update vs Insert

```csharp
// Check if IsNewHolon is correct
// Check if AddAsync vs UpdateAsync is used
// Verify both paths handle IsActive correctly
```

---

## Immediate Action Items

1. ✅ **Add Logging** - Add logging to trace `IsActive` through save/load cycle
2. ✅ **Check MongoDB Directly** - Query MongoDB to see actual stored value
3. ✅ **Verify UpdateAsync** - Check if `UpdateAsync` includes `IsActive` in update
4. ✅ **Test Both Paths** - Test with `IsNewHolon = true` (AddAsync) and `IsNewHolon = false` (UpdateAsync)
5. ✅ **Add Admin Endpoint** - Create endpoint to manually set `IsActive = true` for testing

---

## Admin Activation Endpoint (Temporary Fix)

**Create endpoint for manual activation:**

```csharp
[HttpPost("avatar/{avatarId}/activate")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ActivateAvatar(Guid avatarId)
{
    var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, false);
    if (avatarResult.IsError || avatarResult.Result == null)
    {
        return NotFound(new { error = "Avatar not found" });
    }
    
    var avatar = avatarResult.Result;
    avatar.IsActive = true;
    avatar.Verified = DateTime.UtcNow;
    
    var saveResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);
    if (saveResult.IsError)
    {
        return BadRequest(new { error = saveResult.Message });
    }
    
    return Ok(new { success = true, message = "Avatar activated", avatar = saveResult.Result });
}
```

---

## Summary

**Issue:** `IsActive = true` is set in code but not persisting to MongoDB.

**Likely Causes:**
1. Timing issue (set after conversion)
2. `UpdateAsync` not including `IsActive`
3. `IsNewHolon` flag causing wrong save path
4. Default value issue in MongoDB entity

**Next Steps:**
1. Add logging to trace the issue
2. Check MongoDB directly
3. Verify `UpdateAsync` implementation
4. Apply fixes based on findings

---

**Status:** 🔍 Investigation Required  
**Last Updated:** January 2026
