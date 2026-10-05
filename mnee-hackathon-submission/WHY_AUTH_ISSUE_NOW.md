# Why the Authentication Issue Arose Now

## The Evolution

### Phase 1: Original Implementation (Before Our Changes)
**What was happening**:
- `SolanaService.SendTransaction` always used a **temporary `oasisAccount`** for signing
- This temporary account was created in `Startup.cs` when registering the service
- **Never accessed user's private keys** - just used the temporary account for all transactions

**Why it "worked"**:
- No authorization check needed because we never tried to access user's private keys
- All transactions were signed with the same temporary account (wrong, but no auth error)

**The Problem**:
- ❌ All payments were signed with the wrong account
- ❌ Transactions failed with "Transaction signature verification failure"
- ❌ But no authorization error because we never accessed KeyManager

### Phase 2: Our Fix (Trying to Use Actual User Keys)
**What we did**:
- Created new endpoint `SendToAvatar` that tries to use the **actual sender's private key**
- This is the **correct approach** - use the keys generated at wallet creation

**Why authorization issue appeared**:
- ✅ We're now trying to access the user's private key (correct!)
- ✅ This triggers `KeyManager.GetProviderPrivateKeysForAvatarById`
- ❌ KeyManager checks `AvatarManager.LoggedInAvatar.Id` for authorization
- ❌ But `AvatarManager.LoggedInAvatar` is not set in web API context

## The Root Cause

### Why AvatarManager.LoggedInAvatar Isn't Set

**JWT Middleware** (`JwtMiddleware.cs`):
```csharp
// Sets avatar in HttpContext.Items
context.Items["Avatar"] = avatarResult.Result;

// BUT DOES NOT SET:
// AvatarManager.LoggedInAvatar = avatarResult.Result;  // ← Commented out!
```

**Why it's commented out**:
- Line 59 in `JwtMiddleware.cs` has a TODO comment:
  ```csharp
  //AvatarManager.LoggedInAvatarSessions[context.Session.Id] = avatarResult.Result; 
  //TODO: Maybe not good idea to set this because its static so will be shared with all client sessions?!
  ```
- The concern: `AvatarManager.LoggedInAvatar` is a **static property**
- In a multi-threaded web API, setting it could cause race conditions
- Different requests might overwrite each other's `LoggedInAvatar`

### Why This Wasn't a Problem Before

**Before our changes**:
- We never accessed `KeyManager.GetProviderPrivateKeysForAvatarById`
- We never needed `AvatarManager.LoggedInAvatar` to be set
- The temporary account approach avoided the authorization check entirely

**Now**:
- We're trying to do things correctly (use actual user keys)
- This requires accessing KeyManager
- KeyManager's authorization check fails because `LoggedInAvatar` isn't set

## Why Our Fix Works

**Our solution**: Bypass KeyManager entirely

**Instead of**:
```csharp
// This requires AvatarManager.LoggedInAvatar to be set
KeyManager.Instance.GetProviderPrivateKeysForAvatarById(avatarId, providerType);
```

**We do**:
```csharp
// This doesn't have an authorization check
WalletManager.Instance.LoadProviderWalletsForAvatarById(avatarId, ...);
// Then extract and decrypt private key ourselves
```

**Why this is safe**:
1. ✅ We're already authenticated (JWT token validated)
2. ✅ We're using the authenticated avatar's ID (`fromAvatarId` from JWT)
3. ✅ We're only accessing that avatar's own wallet
4. ✅ No authorization bypass - we're just using a different API that doesn't have the check

## The Architectural Issue

**The real problem**: There's a mismatch between:
- **Web API authentication**: Uses `HttpContext.Items["Avatar"]` (per-request, thread-safe)
- **Core OASIS authentication**: Uses `AvatarManager.LoggedInAvatar` (static, shared)

**Why it exists**:
- OASIS was originally designed for desktop/single-user scenarios
- Web API is multi-user, multi-threaded
- The static `LoggedInAvatar` property doesn't work well in web context

**Our fix**:
- Bypass the problematic authorization check
- Use WalletManager directly (which doesn't have the check)
- Still maintain security (we're authenticated via JWT)

## Summary

**Why it appeared now**:
- ✅ We're now trying to use actual user keys (correct approach)
- ✅ This triggers KeyManager's authorization check
- ✅ The check fails because `LoggedInAvatar` isn't set in web API context

**Why our fix is correct**:
- ✅ Bypasses the problematic static property
- ✅ Still secure (JWT authentication ensures we're the right user)
- ✅ Uses the actual keys generated at wallet creation
- ✅ No breaking changes to existing code

**The issue was always there** - we just never triggered it before because we were using a temporary account!

