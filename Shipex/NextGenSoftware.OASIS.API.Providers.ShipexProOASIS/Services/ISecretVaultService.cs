using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for securely storing and retrieving credentials using OASIS STAR ledger
/// </summary>
public interface ISecretVaultService
{
    // Generic methods
    Task<OASISResult<string>> StoreSecretAsync(string key, string value, Guid? merchantId = null, string secretType = "api-key", DateTime? expiresAt = null);
    Task<OASISResult<string>> GetSecretAsync(string key, Guid? merchantId = null);
    Task<OASISResult<bool>> DeleteSecretAsync(string key);
    Task<OASISResult<bool>> RotateSecretAsync(string key, string newValue);
    Task<OASISResult<SecretRecord>> GetSecretRecordAsync(string key);
    
    // Specific credential helpers
    Task<OASISResult<string>> GetIShipApiKeyAsync(Guid? merchantId = null);
    Task<OASISResult<ShipoxCredentials>> GetShipoxCredentialsAsync(Guid merchantId);
    Task<OASISResult<QuickBooksTokens>> GetQuickBooksTokensAsync(Guid merchantId);
    Task<OASISResult<string>> GetWebhookSecretAsync(string source); // "iship" or "shipox"
    Task<OASISResult<string>> GetMerchantApiKeyAsync(Guid merchantId);
    
    // Token management
    Task<OASISResult<bool>> StoreQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens);
    Task<OASISResult<bool>> UpdateQuickBooksTokensAsync(Guid merchantId, QuickBooksTokens tokens);
    Task<OASISResult<bool>> StoreIShipApiKeyAsync(string apiKey, Guid? merchantId = null);
    Task<OASISResult<bool>> StoreShipoxCredentialsAsync(Guid merchantId, ShipoxCredentials credentials);
    Task<OASISResult<bool>> StoreWebhookSecretAsync(string source, string secret);
    Task<OASISResult<bool>> StoreMerchantApiKeyAsync(Guid merchantId, string apiKey);
}




