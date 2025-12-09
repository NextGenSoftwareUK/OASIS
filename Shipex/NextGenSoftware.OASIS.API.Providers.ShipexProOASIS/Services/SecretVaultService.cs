using System;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Secret Vault Service - Securely stores and retrieves credentials using OASIS STAR ledger
/// All secrets are encrypted before storage and decrypted when retrieved
/// </summary>
public class SecretVaultService : ISecretVaultService
{
    private readonly IEncryptionService _encryption;
    private readonly IShipexProRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public SecretVaultService(IEncryptionService encryption, IShipexProRepository repository)
    {
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    #region Generic Methods

    public async Task<OASISResult<string>> StoreSecretAsync(string key, string value, Guid? merchantId = null, string secretType = "api-key", DateTime? expiresAt = null)
    {
        var result = new OASISResult<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                OASISErrorHandling.HandleError(ref result, "Secret key cannot be empty.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                OASISErrorHandling.HandleError(ref result, "Secret value cannot be empty.");
                return result;
            }

            // Encrypt the secret
            var encryptedValue = await _encryption.EncryptAsync(value);

            // Create secret record
            var secretRecord = new SecretRecord
            {
                Key = key,
                EncryptedValue = encryptedValue,
                SecretType = secretType,
                MerchantId = merchantId,
                ExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Store in repository (which uses MongoDB with STAR ledger compatibility)
            var saveResult = await _repository.SaveSecretRecordAsync(secretRecord);
            if (saveResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to store secret: {saveResult.Message}");
                return result;
            }

            result.Result = key;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to store secret: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<string>> GetSecretAsync(string key, Guid? merchantId = null)
    {
        var result = new OASISResult<string>();

        try
        {
            // Get secret record from repository
            var secretRecordResult = await _repository.GetSecretRecordAsync(key);
            if (secretRecordResult.IsError || secretRecordResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Secret not found: {key}");
                return result;
            }

            var secretRecord = secretRecordResult.Result;

            // Check if secret is active
            if (!secretRecord.IsActive)
            {
                OASISErrorHandling.HandleError(ref result, $"Secret {key} is not active.");
                return result;
            }

            // Check expiration
            if (secretRecord.ExpiresAt.HasValue && secretRecord.ExpiresAt.Value < DateTime.UtcNow)
            {
                OASISErrorHandling.HandleError(ref result, $"Secret {key} has expired.");
                return result;
            }

            // Access control: merchant can only access their own secrets
            if (merchantId.HasValue && secretRecord.MerchantId.HasValue && secretRecord.MerchantId.Value != merchantId.Value)
            {
                OASISErrorHandling.HandleError(ref result, $"Access denied: merchant does not have access to secret {key}.");
                return result;
            }

            // Decrypt and return
            var decryptedValue = await _encryption.DecryptAsync(secretRecord.EncryptedValue);
            result.Result = decryptedValue;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to retrieve secret: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<bool>> DeleteSecretAsync(string key)
    {
        var result = new OASISResult<bool>();

        try
        {
            var deleteResult = await _repository.DeleteSecretRecordAsync(key);
            if (deleteResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to delete secret: {deleteResult.Message}");
                return result;
            }

            result.Result = deleteResult.Result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to delete secret: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<bool>> RotateSecretAsync(string key, string newValue)
    {
        var result = new OASISResult<bool>();

        try
        {
            // Get existing secret to preserve metadata
            var existingSecretResult = await _repository.GetSecretRecordAsync(key);
            if (existingSecretResult.IsError || existingSecretResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Secret not found for rotation: {key}");
                return result;
            }

            var existingSecret = existingSecretResult.Result;

            // Deactivate old secret
            existingSecret.IsActive = false;
            await _repository.SaveSecretRecordAsync(existingSecret);

            // Store new secret with same key but new encrypted value
            var storeResult = await StoreSecretAsync(
                key,
                newValue,
                existingSecret.MerchantId,
                existingSecret.SecretType,
                existingSecret.ExpiresAt);

            if (storeResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to store rotated secret: {storeResult.Message}");
                return result;
            }

            result.Result = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to rotate secret: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<SecretRecord>> GetSecretRecordAsync(string key)
    {
        var result = new OASISResult<SecretRecord>();

        try
        {
            var secretRecordResult = await _repository.GetSecretRecordAsync(key);
            if (secretRecordResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, secretRecordResult.Message);
                return result;
            }

            result.Result = secretRecordResult.Result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get secret record: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region Credential Helper Methods

    public async Task<OASISResult<string>> GetIShipApiKeyAsync(Guid? merchantId = null)
    {
        var key = merchantId.HasValue 
            ? $"iship:api-key:merchant-{merchantId.Value}" 
            : "iship:api-key:global";
        
        return await GetSecretAsync(key, merchantId);
    }

    public async Task<OASISResult<ShipoxCredentials>> GetShipoxCredentialsAsync(Guid merchantId)
    {
        var result = new OASISResult<ShipoxCredentials>();

        try
        {
            var key = $"shipox:credentials:merchant-{merchantId}";
            var secretResult = await GetSecretAsync(key, merchantId);
            
            if (secretResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, secretResult.Message);
                return result;
            }

            // Deserialize JSON credentials
            var credentials = JsonSerializer.Deserialize<ShipoxCredentials>(secretResult.Result, _jsonOptions);
            if (credentials == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to deserialize Shipox credentials.");
                return result;
            }

            result.Result = credentials;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get Shipox credentials: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<QuickBooksTokens>> GetQuickBooksTokensAsync(Guid merchantId)
    {
        var result = new OASISResult<QuickBooksTokens>();

        try
        {
            var key = $"quickbooks:tokens:merchant-{merchantId}";
            var secretResult = await GetSecretAsync(key, merchantId);
            
            if (secretResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, secretResult.Message);
                return result;
            }

            // Deserialize JSON tokens
            var tokens = JsonSerializer.Deserialize<QuickBooksTokens>(secretResult.Result, _jsonOptions);
            if (tokens == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to deserialize QuickBooks tokens.");
                return result;
            }

            result.Result = tokens;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get QuickBooks tokens: {ex.Message}");
        }

        return result;
    }

    public async Task<OASISResult<string>> GetWebhookSecretAsync(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            var result = new OASISResult<string>();
            OASISErrorHandling.HandleError(ref result, "Webhook source cannot be empty.");
            return result;
        }

        var key = $"webhook:secret:{source.ToLowerInvariant()}";
        return await GetSecretAsync(key);
    }

    public async Task<OASISResult<string>> GetMerchantApiKeyAsync(Guid merchantId)
    {
        var key = $"merchant:api-key:{merchantId}";
        return await GetSecretAsync(key, merchantId);
    }

    #endregion

    #region Token Management Methods

    public async Task<OASISResult<bool>> StoreQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens)
    {
        if (tokens == null)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "QuickBooks tokens cannot be null.");
            return result;
        }

        var json = JsonSerializer.Serialize(tokens, _jsonOptions);
        var key = $"quickbooks:tokens:merchant-{merchantId}";
        
        var storeResult = await StoreSecretAsync(key, json, merchantId, "oauth-token", tokens.ExpiresAt);
        
        return new OASISResult<bool>
        {
            Result = !storeResult.IsError,
            IsError = storeResult.IsError,
            Message = storeResult.Message
        };
    }

    public async Task<OASISResult<bool>> UpdateQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens)
    {
        // Update is same as store for secrets (rotation pattern)
        return await StoreQuickBooksTokensAsync(merchantId, tokens);
    }

    public async Task<OASISResult<bool>> StoreIShipApiKeyAsync(string apiKey, Guid? merchantId = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "iShip API key cannot be empty.");
            return result;
        }

        var key = merchantId.HasValue 
            ? $"iship:api-key:merchant-{merchantId.Value}" 
            : "iship:api-key:global";
        
        var storeResult = await StoreSecretAsync(key, apiKey, merchantId, "api-key");
        
        return new OASISResult<bool>
        {
            Result = !storeResult.IsError,
            IsError = storeResult.IsError,
            Message = storeResult.Message
        };
    }

    public async Task<OASISResult<bool>> StoreShipoxCredentialsAsync(Guid merchantId, ShipoxCredentials credentials)
    {
        if (credentials == null)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Shipox credentials cannot be null.");
            return result;
        }

        var json = JsonSerializer.Serialize(credentials, _jsonOptions);
        var key = $"shipox:credentials:merchant-{merchantId}";
        
        var storeResult = await StoreSecretAsync(key, json, merchantId, "api-key");
        
        return new OASISResult<bool>
        {
            Result = !storeResult.IsError,
            IsError = storeResult.IsError,
            Message = storeResult.Message
        };
    }

    public async Task<OASISResult<bool>> StoreWebhookSecretAsync(string source, string secret)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(secret))
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Webhook source and secret cannot be empty.");
            return result;
        }

        var key = $"webhook:secret:{source.ToLowerInvariant()}";
        var storeResult = await StoreSecretAsync(key, secret, null, "webhook-secret");
        
        return new OASISResult<bool>
        {
            Result = !storeResult.IsError,
            IsError = storeResult.IsError,
            Message = storeResult.Message
        };
    }

    public async Task<OASISResult<bool>> StoreMerchantApiKeyAsync(Guid merchantId, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Merchant API key cannot be empty.");
            return result;
        }

        var key = $"merchant:api-key:{merchantId}";
        var storeResult = await StoreSecretAsync(key, apiKey, merchantId, "api-key");
        
        return new OASISResult<bool>
        {
            Result = !storeResult.IsError,
            IsError = storeResult.IsError,
            Message = storeResult.Message
        };
    }

    #endregion
}




