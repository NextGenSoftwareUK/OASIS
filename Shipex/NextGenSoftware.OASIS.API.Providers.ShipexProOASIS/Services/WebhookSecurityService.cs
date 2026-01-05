using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for verifying webhook signatures and security
/// Implements HMAC signature verification, IP whitelisting, and replay protection
/// </summary>
public class WebhookSecurityService
{
    private readonly ISecretVaultService _secretVault;
    private readonly List<string>? _allowedIPs;
    private readonly Dictionary<string, DateTime> _processedNonces = new();
    private readonly TimeSpan _nonceExpiry = TimeSpan.FromMinutes(5);

    public WebhookSecurityService(ISecretVaultService secretVault, List<string>? allowedIPs = null)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _allowedIPs = allowedIPs;
    }

    /// <summary>
    /// Verifies HMAC signature for iShip webhooks
    /// Retrieves webhook secret from Secret Vault
    /// </summary>
    public async Task<OASISResult<bool>> VerifyIShipSignatureAsync(string payload, string signature)
    {
        var result = new OASISResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
            {
                OASISErrorHandling.HandleError(ref result, "Payload and signature cannot be empty.");
                return result;
            }

            // Get webhook secret from vault
            var secretResult = await _secretVault.GetWebhookSecretAsync("iship");
            if (secretResult.IsError || string.IsNullOrWhiteSpace(secretResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get iShip webhook secret: {secretResult.Message}");
                return result;
            }

            var computedSignature = ComputeHMACSHA256(payload, secretResult.Result);
            result.Result = string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to verify iShip signature: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Verifies HMAC signature for Shipox webhooks
    /// Retrieves webhook secret from Secret Vault
    /// </summary>
    public async Task<OASISResult<bool>> VerifyShipoxSignatureAsync(string payload, string signature)
    {
        var result = new OASISResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
            {
                OASISErrorHandling.HandleError(ref result, "Payload and signature cannot be empty.");
                return result;
            }

            // Get webhook secret from vault
            var secretResult = await _secretVault.GetWebhookSecretAsync("shipox");
            if (secretResult.IsError || string.IsNullOrWhiteSpace(secretResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get Shipox webhook secret: {secretResult.Message}");
                return result;
            }

            var computedSignature = ComputeHMACSHA256(payload, secretResult.Result);
            result.Result = string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to verify Shipox signature: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Checks if the IP address is whitelisted (optional security feature)
    /// </summary>
    public bool IsIPWhitelisted(string ipAddress)
    {
        if (_allowedIPs == null || _allowedIPs.Count == 0)
        {
            // If no whitelist configured, allow all IPs
            return true;
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return _allowedIPs.Contains(ipAddress);
    }

    /// <summary>
    /// Checks if a nonce has been processed before (replay protection)
    /// </summary>
    public bool IsNonceReplay(string nonce)
    {
        if (string.IsNullOrWhiteSpace(nonce))
        {
            return false;
        }

        // Clean up expired nonces
        var expiredNonces = _processedNonces
            .Where(kvp => kvp.Value < DateTime.UtcNow - _nonceExpiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var expired in expiredNonces)
        {
            _processedNonces.Remove(expired);
        }

        // Check if nonce exists
        if (_processedNonces.ContainsKey(nonce))
        {
            return true; // Replay detected
        }

        // Store nonce
        _processedNonces[nonce] = DateTime.UtcNow;
        return false;
    }

    /// <summary>
    /// Validates timestamp to prevent replay attacks (checks if timestamp is within acceptable window)
    /// </summary>
    public bool IsTimestampValid(DateTime timestamp, TimeSpan maxAge)
    {
        var age = DateTime.UtcNow - timestamp;
        return age >= TimeSpan.Zero && age <= maxAge;
    }

    /// <summary>
    /// Computes HMAC-SHA256 signature
    /// </summary>
    private string ComputeHMACSHA256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

