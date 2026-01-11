# Agent Avatar Activation Guide

**Last Updated:** January 2026  
**Status:** ✅ Implementation Complete

---

## Overview

Agent avatars need to have `IsActive = true` to be fully functional. This document explains how agent activation works and how to ensure agents are properly activated.

---

## How Agent Activation Works

### The `IsActive` Property

The `IsActive` property on avatars determines whether an avatar is active and can be used. For agents, this is critical because:

1. **Login**: Agents may need `IsActive = true` to log in
2. **Operations**: Some operations may check `IsActive` before allowing actions
3. **Service Registration**: Agents may need to be active to register with SERV

### When Is `IsActive` Set to `true`?

#### 1. **Email Verification** (Standard Users)

**Location:** `AvatarManager.cs` - `VerifyEmail()` method (line ~409)

```csharp
avatar.IsActive = true;
avatar.Verified = DateTime.UtcNow;
avatar.VerificationToken = null;
```

**When:** User clicks verification link in email

#### 2. **Auto-Verification** (All Agents - Simplified Approach)

**Location:** `AvatarManager-Private.cs` - `AvatarRegistered()` method (line ~413)

**Current Implementation:** All Agent-type avatars are automatically verified and activated immediately on creation, regardless of owner status.

```csharp
// SIMPLE APPROACH: Auto-verify and activate ALL Agent-type avatars
if (isAgent && autoVerifyAgents)
{
    // Preserve password before second save (critical for authentication)
    if (string.IsNullOrEmpty(result.Result.Password))
    {
        var reloadResult = LoadAvatarAsync(result.Result.Id, false, false).Result;
        if (!reloadResult.IsError && reloadResult.Result != null)
        {
            result.Result.Password = reloadResult.Result.Password;
        }
    }
    
    // Auto-verify and activate the agent immediately
    result.Result.IsActive = true;
    result.Result.Verified = DateTime.UtcNow;
    result.Result.VerificationToken = null;
    
    // Save the verified agent
    var saveResult = SaveAvatarAsync(result.Result).Result;
}
```

**When:** Agent is created with `avatarType: AvatarType.Agent`

**Note:** This simplified approach auto-verifies ALL agents. Owner linking is still supported via `ownerAvatarId` metadata, but is not required for verification.

**See:** [`AGENT_VERIFICATION_SIMPLIFIED.md`](./AGENT_VERIFICATION_SIMPLIFIED.md) for details on the simplified approach.  
**See:** [`AGENT_PASSWORD_PRESERVATION.md`](./AGENT_PASSWORD_PRESERVATION.md) for password preservation details.
                    // ... set IsActive = true
                }
            }
        }
    }
}
```

#### 3. **Manual Activation** (If Needed)

**Location:** `AvatarManager-Private.cs` - `ActivateAvatarAsync()` method (line ~1059)

```csharp
avatar.IsActive = true;
```

**When:** Called manually to activate an avatar

---

## The Problem

### Issue: Agents Not Being Activated

**Symptoms:**
- Agent avatars created but `IsActive = false`
- Agents cannot log in
- Agents cannot register with SERV
- Operations fail with "agent not active" errors

**Root Causes:**

1. **Agent Created Without Owner:**
   - Agent created without `ownerAvatarId`
   - No auto-verification triggered
   - `IsActive` remains `false` (default)
   - Agent needs email verification (but agents typically don't have email access)

2. **Owner Not Verified:**
   - Agent created with `ownerAvatarId`
   - But owner is not verified (`IsVerified = false`)
   - Auto-verification doesn't trigger
   - `IsActive` remains `false`

3. **Owner Over Agent Limit:**
   - Agent created with `ownerAvatarId`
   - Owner is verified
   - But owner already has 10 agents (limit reached)
   - Auto-verification doesn't trigger
   - `IsActive` remains `false`

4. **Missing `IsActive` Set During Creation:**
   - `IsActive` is not set during avatar creation
   - Only set during verification or auto-verification
   - If verification doesn't happen, `IsActive` stays `false`

---

## The Solution

### Fix Applied: Set `IsActive = true` for Auto-Verified Agents

**File:** `AvatarManager-Private.cs`  
**Method:** `AvatarRegistered()`  
**Line:** ~317

**Change:**
```csharp
if (shouldAutoVerify)
{
    // Auto-verify the agent
    result.Result.Verified = DateTime.UtcNow;
    result.Result.VerificationToken = null;
    result.Result.IsActive = true; // ✅ ADDED: Set IsActive = true for auto-verified agents
    
    // Save the verified agent
    var saveResult = SaveAvatarAsync(result.Result).Result;
}
```

**What This Does:**
- When an agent is auto-verified (owner is verified and under limit), `IsActive` is now explicitly set to `true`
- Agent is immediately active and can be used
- No email verification needed

---

## How to Ensure Agents Are Activated

### Method 1: Create Agent with Verified Owner (Recommended)

**Best Practice:** Always create agents with a verified owner

```csharp
// 1. Ensure owner is verified first
var ownerResult = await AvatarManager.Instance.LoadAvatarAsync(ownerAvatarId, false, true);
if (ownerResult.Result.IsVerified)
{
    // 2. Create agent with ownerAvatarId
    var agentResult = await AvatarManager.Instance.RegisterAsync(
        avatarTitle: "Agent",
        firstName: "Agent",
        lastName: "One",
        email: "agent@example.com",
        password: "password123",
        username: "agent_one",
        avatarType: AvatarType.Agent,
        createdOASISType: OASISType.OASISAPIREST,
        ownerAvatarId: ownerAvatarId  // ✅ Include owner
    );
    
    // 3. Agent will be auto-verified and IsActive = true
}
```

**API Endpoint:**
```http
POST /api/avatar/register
Content-Type: application/json

{
  "username": "my_agent",
  "email": "agent@example.com",
  "password": "password123",
  "confirmPassword": "password123",
  "firstName": "Agent",
  "lastName": "One",
  "avatarType": "Agent",
  "ownerAvatarId": "uuid-of-verified-owner",  // ✅ Include owner
  "acceptTerms": true
}
```

### Method 2: Verify Owner First

**If owner is not verified:**

```csharp
// 1. Verify owner first (via email verification)
// OR manually set owner as verified if needed

// 2. Then create agent with ownerAvatarId
var agentResult = await AvatarManager.Instance.RegisterAsync(
    // ... agent details ...
    ownerAvatarId: ownerAvatarId
);
```

### Method 3: Manual Activation (If Needed)

**If agent was created without owner or owner wasn't verified:**

```csharp
// Load agent
var agentResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);

if (!agentResult.IsError && agentResult.Result != null)
{
    var agent = agentResult.Result;
    
    // Set IsActive = true
    agent.IsActive = true;
    
    // Optionally verify
    agent.Verified = DateTime.UtcNow;
    agent.VerificationToken = null;
    
    // Save
    var saveResult = await AvatarManager.Instance.SaveAvatarAsync(agent);
}
```

**API Endpoint (if exists):**
```http
POST /api/avatar/{avatarId}/activate
Authorization: Bearer {token}
```

---

## Verification Checklist

### After Creating an Agent

Check these properties:

1. **`IsActive`** - Should be `true`
   ```csharp
   var agent = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
   if (agent.Result.IsActive)
   {
       // ✅ Agent is active
   }
   ```

2. **`IsVerified`** - Should be `true` (or `Verified` date set)
   ```csharp
   if (agent.Result.IsVerified || agent.Result.Verified.HasValue)
   {
       // ✅ Agent is verified
   }
   ```

3. **`OwnerAvatarId`** - Should be set (if using owner-based activation)
   ```csharp
   if (agent.Result.MetaData != null && agent.Result.MetaData.ContainsKey("OwnerAvatarId"))
   {
       // ✅ Agent has owner
   }
   ```

### Testing Agent Activation

```csharp
// Test 1: Check IsActive
var agent = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
Assert.IsTrue(agent.Result.IsActive, "Agent should be active");

// Test 2: Try to use agent (e.g., register with SERV)
var servResult = await A2AManager.Instance.RegisterAgentAsServiceAsync(agentId, capabilities);
Assert.IsFalse(servResult.IsError, "Agent should be able to register with SERV");

// Test 3: Try to log in as agent
var loginResult = await AvatarManager.Instance.AuthenticateAsync(agentEmail, agentPassword);
Assert.IsFalse(loginResult.IsError, "Agent should be able to log in");
```

---

## Common Issues & Solutions

### Issue 1: Agent Created But Not Active

**Symptom:** `IsActive = false` after creation

**Causes:**
- Agent created without `ownerAvatarId`
- Owner not verified
- Owner over agent limit

**Solution:**
- Ensure owner is verified before creating agent
- Include `ownerAvatarId` when creating agent
- Check owner's agent count (must be < 10)

### Issue 2: Agent Can't Log In

**Symptom:** Login fails even with correct credentials

**Causes:**
- `IsActive = false`
- `IsVerified = false`
- Missing verification

**Solution:**
- Check `IsActive` property
- If `false`, manually set to `true` (see Method 3 above)
- Ensure agent is verified

### Issue 3: Agent Can't Register with SERV

**Symptom:** `RegisterAgentAsServiceAsync()` fails

**Causes:**
- `IsActive = false`
- Agent not properly initialized

**Solution:**
- Verify `IsActive = true`
- Check agent exists and is Agent type
- Ensure agent has capabilities registered

---

## Code Locations

### Key Files

1. **AvatarManager-Private.cs**
   - `PrepareToRegisterAvatarAsync()` - Creates avatar (line ~128)
   - `AvatarRegistered()` - Handles auto-verification (line ~292)
   - `ActivateAvatarAsync()` - Manual activation (line ~1059)

2. **AvatarManager.cs**
   - `VerifyEmail()` - Email verification sets `IsActive = true` (line ~409)

3. **AgentManager-Ownership.cs**
   - `LinkAgentToUserAsync()` - Links agent to owner (may trigger activation)

---

## Summary

### How Agent Activation Works

1. **Agent Created** → `IsActive` not set (defaults to `false`)
2. **If Owner Verified & Under Limit** → Auto-verification:
   - `Verified = DateTime.UtcNow`
   - `VerificationToken = null`
   - `IsActive = true` ✅ **This is the fix**
3. **If No Owner or Owner Not Verified** → Email verification required:
   - User clicks verification link
   - `IsActive = true` (in `VerifyEmail()`)

### Best Practice

**Always create agents with a verified owner:**
```csharp
// ✅ Good: Agent with verified owner
await AvatarManager.Instance.RegisterAsync(
    // ... details ...
    ownerAvatarId: verifiedOwnerId
);
// Agent will be auto-verified and IsActive = true

// ❌ Bad: Agent without owner
await AvatarManager.Instance.RegisterAsync(
    // ... details ...
    // No ownerAvatarId
);
// Agent needs email verification (but agents don't check email)
```

---

**Status:** ✅ Fix Applied - Agents with verified owners are now automatically activated  
**Last Updated:** January 2026
