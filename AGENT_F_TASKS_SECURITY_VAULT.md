# Agent F - Security & Vault Tasks

## Overview

You are responsible for **security infrastructure** - implementing the Secret Vault service that securely stores and manages all sensitive credentials using OASIS STAR ledger. This includes API keys, OAuth tokens, and webhook secrets.

## What You're Building

The **Secret Vault Service** provides:
- Secure credential storage using OASIS STAR ledger
- Encryption/decryption of sensitive data
- Credential retrieval for connectors and services
- Credential rotation capabilities
- Access control per merchant

## Architecture Context

```
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  • Secret Vault Service      ← You build                    │
│  • Encryption Service        ← You build                    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Stores/Retrieves
                        ↓
┌─────────────────────────────────────────────────────────────┐
│          OASIS STAR Ledger                                   │
│  • Immutable audit trail                                     │
│  • Encrypted credential storage                              │
└─────────────────────────────────────────────────────────────┘

All Services Use Your Vault:
• iShip Connector (Agent C) → Gets iShip API keys
• Shipox Connector (Agent D) → Gets Shipox credentials
• QuickBooks Service (Agent E) → Gets OAuth tokens
• Webhook Service (Agent D) → Gets webhook secrets
```

**Your Role**: Build secure credential storage that all other components use to retrieve their API keys and secrets.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **OASIS STAR Ledger**: Search codebase for STAR ledger implementation examples

---

## Your Tasks

### Task 9.1: Integrate OASIS STAR Ledger for Secrets

**Priority**: CRITICAL - Foundation for all credential storage  
**Dependencies**: Task 1.1 (OASIS provider setup)  
**Estimated Time**: 10 hours

#### What to Build

Integrate with OASIS STAR ledger to store encrypted secrets with immutable audit trail.

#### Components to Build

1. **SecretVaultService.cs**
   - Interface for storing/retrieving secrets
   - Integration with STAR ledger
   - Secret categorization (API keys, tokens, webhook secrets)

2. **EncryptionService.cs**
   - Encrypt secrets before storage
   - Decrypt secrets when retrieved
   - Key management

#### Implementation Details

- Research OASIS STAR ledger implementation in codebase
- Understand how to store encrypted data in STAR ledger
- Implement encryption/decryption using OASIS encryption patterns
- Design secret key structure (e.g., `iship:api-key:merchant-{id}`)

#### Files to Create

- `Services/SecretVaultService.cs`
- `Services/EncryptionService.cs`
- `Models/SecretRecord.cs`

#### Secret Record Structure

```csharp
public class SecretRecord
{
    public string Key { get; set; } // e.g., "iship:api-key:merchant-123"
    public string EncryptedValue { get; set; }
    public string SecretType { get; set; } // "api-key", "oauth-token", "webhook-secret"
    public Guid? MerchantId { get; set; } // Optional merchant association
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // For tokens
    public bool IsActive { get; set; }
}
```

#### Implementation Pattern

```csharp
public class SecretVaultService : ISecretVaultService
{
    private readonly IEncryptionService _encryption;
    private readonly IStarLedgerProvider _starLedger;
    
    public async Task<OASISResult<string>> StoreSecretAsync(
        string key, 
        string secretValue, 
        Guid? merchantId = null)
    {
        try
        {
            // 1. Encrypt the secret
            var encryptedValue = await _encryption.EncryptAsync(secretValue);
            
            // 2. Create secret record
            var secretRecord = new SecretRecord
            {
                Key = key,
                EncryptedValue = encryptedValue,
                MerchantId = merchantId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            // 3. Store in STAR ledger (immutable audit trail)
            var result = await _starLedger.SaveSecretAsync(secretRecord);
            
            return new OASISResult<string>(result);
        }
        catch (Exception ex)
        {
            return new OASISResult<string>
            {
                IsError = true,
                Message = $"Failed to store secret: {ex.Message}"
            };
        }
    }
    
    public async Task<OASISResult<string>> GetSecretAsync(string key)
    {
        try
        {
            // 1. Retrieve from STAR ledger
            var secretRecord = await _starLedger.GetSecretAsync(key);
            if (secretRecord == null || !secretRecord.IsActive)
            {
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = "Secret not found or inactive"
                };
            }
            
            // 2. Check expiration
            if (secretRecord.ExpiresAt.HasValue && 
                secretRecord.ExpiresAt.Value < DateTime.UtcNow)
            {
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = "Secret has expired"
                };
            }
            
            // 3. Decrypt and return
            var decryptedValue = await _encryption.DecryptAsync(secretRecord.EncryptedValue);
            return new OASISResult<string>(decryptedValue);
        }
        catch (Exception ex)
        {
            return new OASISResult<string>
            {
                IsError = true,
                Message = $"Failed to retrieve secret: {ex.Message}"
            };
        }
    }
}
```

#### Acceptance Criteria

- [ ] STAR ledger integration works
- [ ] Secrets encrypted before storage
- [ ] Secrets decrypted correctly when retrieved
- [ ] Expiration checking works
- [ ] Error handling robust

---

### Task 9.2: Implement Credential Management

**Priority**: CRITICAL - All connectors need this  
**Dependencies**: Task 9.1  
**Estimated Time**: 12 hours

#### What to Build

Implement complete credential management including:
- Storing different types of credentials
- Credential rotation
- Access control per merchant
- Credential retrieval helpers

#### Credential Types to Support

1. **iShip API Keys**
   - Key format: `iship:api-key:merchant-{merchantId}` or `iship:api-key:global`
   - Storage: Encrypted API key string

2. **Shipox API Credentials**
   - Key format: `shipox:credentials:merchant-{merchantId}`
   - Storage: Encrypted JSON with API key and secret

3. **QuickBooks OAuth Tokens**
   - Key format: `quickbooks:tokens:merchant-{merchantId}`
   - Storage: Encrypted JSON with access_token, refresh_token, expires_at
   - Auto-refresh logic (Task 9.3)

4. **Webhook Secrets**
   - Key format: `webhook:secret:iship` or `webhook:secret:shipox`
   - Storage: Encrypted HMAC secret string

5. **Merchant API Keys**
   - Key format: `merchant:api-key:{merchantId}`
   - Storage: Encrypted API key string

#### Methods to Implement

```csharp
public interface ISecretVaultService
{
    // Generic methods
    Task<OASISResult<string>> StoreSecretAsync(string key, string value, Guid? merchantId = null);
    Task<OASISResult<string>> GetSecretAsync(string key);
    Task<OASISResult<bool>> DeleteSecretAsync(string key);
    Task<OASISResult<bool>> RotateSecretAsync(string key, string newValue);
    
    // Specific credential helpers
    Task<OASISResult<string>> GetIShipApiKeyAsync(Guid? merchantId = null);
    Task<OASISResult<ShipoxCredentials>> GetShipoxCredentialsAsync(Guid merchantId);
    Task<OASISResult<QuickBooksTokens>> GetQuickBooksTokensAsync(Guid merchantId);
    Task<OASISResult<string>> GetWebhookSecretAsync(string source); // "iship" or "shipox"
    Task<OASISResult<string>> GetMerchantApiKeyAsync(Guid merchantId);
    
    // Token management
    Task<OASISResult<bool>> StoreQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens);
    Task<OASISResult<bool>> UpdateQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens);
}
```

#### Credential Rotation

```csharp
public async Task<OASISResult<bool>> RotateSecretAsync(string key, string newValue)
{
    // 1. Mark old secret as inactive
    var oldSecret = await GetSecretRecordAsync(key);
    if (oldSecret != null)
    {
        oldSecret.IsActive = false;
        await _starLedger.UpdateSecretAsync(oldSecret);
    }
    
    // 2. Store new secret (creates new record in STAR ledger)
    var storeResult = await StoreSecretAsync(key, newValue);
    
    // 3. Return success
    return new OASISResult<bool>(!storeResult.IsError);
}
```

#### Access Control

- Merchants can only access their own credentials
- Global/system credentials accessible by system services
- Implement access checks in GetSecretAsync based on merchant context

#### Acceptance Criteria

- [ ] All credential types can be stored
- [ ] Helper methods work for each credential type
- [ ] Credential rotation works
- [ ] Access control enforced
- [ ] Expiration handling works

---

### Task 9.3: Implement Credential Retrieval Integration

**Priority**: HIGH - Connectors need this  
**Dependencies**: Task 9.2  
**Estimated Time**: 6 hours

#### What to Build

Update all connectors and services to use SecretVaultService instead of hardcoded credentials.

#### Files to Update

1. **iShip Connector** (Agent C's work)
   - `Connectors/IShip/IShipApiClient.cs`
   - Replace hardcoded API key with: `await _secretVault.GetIShipApiKeyAsync()`

2. **Shipox Connector** (Agent D's work)
   - `Connectors/Shipox/ShipoxApiClient.cs`
   - Replace hardcoded credentials with: `await _secretVault.GetShipoxCredentialsAsync(merchantId)`

3. **QuickBooks OAuth Service** (Agent E's work)
   - `Connectors/QuickBooks/QuickBooksOAuthService.cs`
   - Replace hardcoded tokens with: `await _secretVault.GetQuickBooksTokensAsync(merchantId)`
   - Store new tokens after refresh: `await _secretVault.StoreQuickBooksTokensAsync(merchantId, tokens)`

4. **Webhook Security Service** (Agent D's work)
   - `Services/WebhookSecurityService.cs`
   - Replace hardcoded webhook secrets with: `await _secretVault.GetWebhookSecretAsync("iship")`

#### Implementation Pattern

```csharp
// Before (hardcoded)
public class IShipApiClient
{
    private readonly string _apiKey = "hardcoded-key"; // BAD!
}

// After (using Secret Vault)
public class IShipApiClient
{
    private readonly ISecretVaultService _secretVault;
    private string _apiKey;
    
    public async Task InitializeAsync()
    {
        var apiKeyResult = await _secretVault.GetIShipApiKeyAsync();
        if (apiKeyResult.IsError)
            throw new Exception($"Failed to get iShip API key: {apiKeyResult.Message}");
        _apiKey = apiKeyResult.Result;
    }
}
```

#### Dependency Injection

Ensure SecretVaultService is injected into all connectors:
- Update DI configuration in startup
- Pass merchant context where needed

#### Acceptance Criteria

- [ ] No hardcoded credentials remain
- [ ] All connectors use Secret Vault
- [ ] Credential retrieval works
- [ ] Error handling when credentials not found

---

## Additional Considerations

### Security Best Practices

1. **Encryption**
   - Use strong encryption algorithms (AES-256)
   - Store encryption keys securely (not in code)
   - Use OASIS encryption utilities where possible

2. **Access Control**
   - Merchant isolation (merchants can't access other merchants' secrets)
   - System credentials only accessible by system services
   - Log all credential access

3. **Audit Trail**
   - STAR ledger provides immutable audit trail
   - Log all credential operations (store, retrieve, rotate, delete)

4. **Credential Rotation**
   - Support seamless rotation without downtime
   - Maintain old credentials during rotation period
   - Automate rotation where possible

### Token Management

For OAuth tokens (QuickBooks):
- Store access_token and refresh_token
- Track expiration time
- Auto-refresh before expiration (implement in Agent E's QuickBooks service)
- Handle token revocation gracefully

---

## Working with Other Agents

### Dependencies You Need

- **Agent A**: OASIS provider structure, STAR ledger integration points
- **STAR Ledger**: Need to understand how to integrate with OASIS STAR ledger

### Dependencies You Create

- **Agent C**: Needs iShip API keys from your vault
- **Agent D**: Needs Shipox credentials and webhook secrets
- **Agent E**: Needs QuickBooks OAuth tokens

### Communication Points

1. **After Task 9.1**: Share SecretVaultService interface with all agents
2. **After Task 9.2**: Provide credential helper methods documentation
3. **Coordinate with Agents C, D, E**: Help them integrate your vault

---

## Success Criteria

You will know you've succeeded when:

1. ✅ All credentials stored securely in STAR ledger
2. ✅ No hardcoded credentials in codebase
3. ✅ Credential retrieval works for all connectors
4. ✅ Credential rotation supported
5. ✅ Access control enforced
6. ✅ Audit trail complete

---

## Important Notes

- **Security First**: This is critical infrastructure - security is paramount
- **OASIS Patterns**: Use OASIS encryption and STAR ledger patterns from codebase
- **Testing**: Test with real credentials carefully (use test/sandbox accounts)
- **Documentation**: Document credential key formats and access patterns

---

**Questions?** Refer to OASIS STAR ledger documentation or coordinate with Agent A.

**Ready to start?** Begin with Task 9.1 to understand STAR ledger integration.
