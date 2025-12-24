# Email Verification Workaround for Agents

## Issue

OASIS API requires email verification before authentication. Since agents use auto-generated fake emails (`agent_{username}@agents.local`), they cannot receive verification emails, which blocks authentication and wallet generation.

## Current Status

✅ **Registration works** - Agents can register successfully  
❌ **Authentication fails** - Requires email verification  
❌ **Wallet generation blocked** - Requires authentication token

## Solutions

### Option 1: Disable Email Verification for Agents (Recommended)

Modify the OASIS API to skip email verification for agents. This can be done by:

1. **Check avatar type or email domain** - If email ends with `@agents.local`, skip verification
2. **Add agent flag** - Add an `isAgent` flag to registration that bypasses verification
3. **Modify authentication** - Allow authentication without verification for agent emails

**Location in OASIS API:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/`

**Suggested Change:**
```csharp
// In Authenticate method, check if email is agent email
if (model.Email.EndsWith("@agents.local"))
{
    // Skip verification check for agents
    // Allow authentication even if not verified
}
```

### Option 2: Manual Verification (For Testing)

If you have database access, you can manually verify agents:

1. **Find avatar in MongoDB** (or your database)
2. **Set `isVerified: true`** and `isActive: true`
3. **Clear `verificationToken`**

### Option 3: Use Admin Endpoint (If Available)

Check if there's an admin endpoint to verify avatars programmatically.

### Option 4: Temporary Workaround - Use Existing Verified Account

For testing, you can use an existing verified OASIS account:

```python
# In config.py or environment
OASIS_ADMIN_USERNAME = "OASIS_ADMIN"  # Or any verified account
OASIS_ADMIN_PASSWORD = "your_password"

# Use this for wallet operations
oasis.authenticate(OASIS_ADMIN_USERNAME, OASIS_ADMIN_PASSWORD)
```

## Testing Without Verification

For now, you can test the registration flow:

```bash
python test_agent_registration.py
```

This will:
- ✅ Register agent successfully
- ❌ Fail at authentication (email not verified)
- ❌ Fail at wallet generation (no token)

## Next Steps

1. **Modify OASIS API** to skip email verification for `@agents.local` emails
2. **Or** implement manual verification process
3. **Or** use admin account for testing wallet operations

---

**Note:** Once email verification is bypassed for agents, the full flow will work:
1. Register agent → Get avatar ID
2. Authenticate → Get token (will work after fix)
3. Generate wallet → Create Solana wallet
4. Ready for payments!

