# OASIS API Modification Guide - Disable Email Verification for Agents

## Problem

Agents register with auto-generated emails (`agent_{username}@agents.local`) and cannot receive verification emails, blocking authentication and wallet operations.

## Solution

Modify the OASIS API to skip email verification for agent emails.

---

## Modification 1: AvatarController.cs - Authenticate Method

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs`

**Current Code (around line 235):**
```csharp
[HttpPost("authenticate")]
public async Task<OASISHttpResponseMessage<IAvatar>> Authenticate(AuthenticateRequest request)
{
    var result = await Program.AvatarManager.AuthenticateAsync(...);
    
    if (!result.IsError && result.Result != null)
    {
        // Check if avatar is verified
        if (!result.Result.IsVerified)
        {
            // Currently blocks authentication
            return HttpResponseHelper.FormatResponse(
                new OASISResult<IAvatar> 
                { 
                    IsError = true, 
                    Message = "Avatar has not been verified. Please check your email." 
                }, 
                HttpStatusCode.Unauthorized
            );
        }
        // ... rest of code
    }
}
```

**Modified Code:**
```csharp
[HttpPost("authenticate")]
public async Task<OASISHttpResponseMessage<IAvatar>> Authenticate(AuthenticateRequest request)
{
    var result = await Program.AvatarManager.AuthenticateAsync(...);
    
    if (!result.IsError && result.Result != null)
    {
        // Skip verification check for agent emails
        bool isAgentEmail = result.Result.Email?.EndsWith("@agents.local") == true;
        
        if (!result.Result.IsVerified && !isAgentEmail)
        {
            // Only block if not an agent
            return HttpResponseHelper.FormatResponse(
                new OASISResult<IAvatar> 
                { 
                    IsError = true, 
                    Message = "Avatar has not been verified. Please check your email." 
                }, 
                HttpStatusCode.Unauthorized
            );
        }
        
        // Allow agents to authenticate even if not verified
        // ... rest of code continues normally
    }
}
```

---

## Modification 2: AvatarManager.cs - AuthenticateAsync Method

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/AvatarManager.cs`

**Current Code:**
```csharp
public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, ...)
{
    // ... authentication logic ...
    
    // Check if verified
    if (avatar != null && !avatar.IsVerified)
    {
        result.IsError = true;
        result.Message = "Avatar has not been verified. Please check your email.";
        return result;
    }
    
    // ... rest of code
}
```

**Modified Code:**
```csharp
public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, ...)
{
    // ... authentication logic ...
    
    // Check if verified (skip for agents)
    if (avatar != null && !avatar.IsVerified)
    {
        bool isAgentEmail = avatar.Email?.EndsWith("@agents.local") == true;
        
        if (!isAgentEmail)
        {
            // Only block non-agents
            result.IsError = true;
            result.Message = "Avatar has not been verified. Please check your email.";
            return result;
        }
        // Agents can proceed without verification
    }
    
    // ... rest of code
}
```

---

## Alternative: Add Agent Flag to Registration

Instead of checking email domain, add an explicit `isAgent` flag:

**Registration Request:**
```json
{
  "username": "agent_001",
  "email": "agent_001@agents.local",
  "password": "...",
  "isAgent": true,  // New field
  ...
}
```

**Then check:**
```csharp
if (avatar != null && !avatar.IsVerified && !avatar.IsAgent)
{
    // Block authentication
}
```

---

## Testing After Modification

1. **Restart OASIS API**
2. **Run test:**
   ```bash
   python test_agent_registration.py
   ```

Expected result:
- ✅ Agent registers
- ✅ Agent authenticates (even without email verification)
- ✅ Agent generates wallet
- ✅ Ready for payments!

---

## Files to Modify

1. `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs`
   - Method: `Authenticate()` (around line 235)

2. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/AvatarManager.cs`
   - Method: `AuthenticateAsync()` (if verification check is there)

---

## Quick Fix Location

Search for:
- `"Avatar has not been verified"`
- `!avatar.IsVerified`
- `IsVerified == false`

And add the agent email check before blocking authentication.

---

**After this modification, agents will be able to:**
1. ✅ Register with auto-generated emails
2. ✅ Authenticate without email verification
3. ✅ Generate wallets
4. ✅ Process payments

