using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    public partial class ArweaveOASIS
    {
        private void Init()
        {
            this.ProviderName = "ArweaveOASIS";
            this.ProviderDescription = "Arweave Permanent Storage Provider for OASIS";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArweaveOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                var arweaveConfig = _OASISDNA?.OASIS?.StorageProviders?.ArweaveOASIS;
                if (arweaveConfig != null)
                    ParseConnectionString(arweaveConfig.ConnectionString);

                _gatewayUrl ??= "https://arweave.net";
                Bind(new ArweaveService(_walletJson, _gatewayUrl, _nodeExecutable, _sdkBridgePath, _mineAfterPost));
            }
            catch (Exception e)
            {
                IsProviderActivated = false;
                OASISErrorHandling.HandleError(ref result, $"Error occurred in ArweaveOASIS Provider in ActivateProviderAsync. Reason: {e.Message}", e);
                return result;
            }

            // Proves the gateway, the SDK bridge and owner-pinned discovery for the configured wallet.
            return await base.ActivateProviderAsync();
        }

        // Connection string format: "wallet=/path/to/wallet.json&gateway=https://arweave.net"
        // Or with embedded JSON:    "walletjson=<base64>&gateway=https://arweave.net"
        private void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;

            var parts = connectionString.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLowerInvariant();
                var value = Uri.UnescapeDataString(kv[1].Trim());

                switch (key)
                {
                    case "wallet":
                        _walletPath = value;
                        if (File.Exists(_walletPath))
                            _walletJson = File.ReadAllText(_walletPath);
                        break;
                    case "walletjson":
                        _walletJson = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                        break;
                    case "gateway":
                        _gatewayUrl = value;
                        break;
                }
            }
        }

        // Upload data to Arweave permanently; returns the transaction ID
        public async Task<OASISResult<string>> UploadDataToArweaveAsync(byte[] data, string contentType = "application/octet-stream", Dictionary<string, string> tags = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txId = await _arweaveService.PostTransactionAsync(data, contentType, tags);

                if (string.IsNullOrEmpty(txId) || !System.Text.RegularExpressions.Regex.IsMatch(txId, "^[A-Za-z0-9_-]{43}$"))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to upload data to Arweave: empty transaction ID returned.");
                    return result;
                }

                result.Result = txId;
                result.Message = $"Data uploaded to Arweave permanently. TxId: {txId}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading data to Arweave. Reason: {e}");
            }

            return result;
        }

        // Upload JSON object to Arweave; returns transaction ID
        public async Task<OASISResult<string>> UploadJsonToArweaveAsync(object data, string name = null, Dictionary<string, string> extraTags = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);

                var tags = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "App-Name", "OASIS" },
                    { "App-Version", "2.0" },
                    { "Unix-Time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                };

                if (!string.IsNullOrEmpty(name))
                    tags["OASIS-Name"] = name;

                if (extraTags != null)
                    foreach (var kvp in extraTags)
                        tags[kvp.Key] = kvp.Value;

                var txId = await _arweaveService.PostTransactionAsync(bytes, "application/json", tags);

                if (string.IsNullOrEmpty(txId) || !System.Text.RegularExpressions.Regex.IsMatch(txId, "^[A-Za-z0-9_-]{43}$"))
                {
                    OASISErrorHandling.HandleError(ref result, "Arweave post did not acknowledge a valid transaction identity; preserve the write outcome for reconciliation.");
                    return result;
                }

                result.Result = txId;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = $"Arweave accepted the JSON transaction. TxId: {txId}; confirmation/finality is separate.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading JSON to Arweave. Reason: {e}");
            }

            return result;
        }

        // Download data from Arweave using transaction ID
        public async Task<OASISResult<byte[]>> DownloadDataFromArweaveAsync(string txId)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var data = await _arweaveService.GetTransactionDataAsync(txId);

                if (data == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to download data from Arweave for TxId: {txId}");
                    return result;
                }

                result.Result = data;
                result.Message = $"Data downloaded from Arweave successfully. TxId: {txId}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error downloading data from Arweave. Reason: {e}");
            }

            return result;
        }

        // Returns the permanent URL for an Arweave transaction
        public string GetTransactionUrl(string txId)
        {
            return $"{_gatewayUrl ?? "https://arweave.net"}/{txId}";
        }

        private async Task EnsureActivatedAsync<T>(OASISResult<T> result)
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate ArweaveOASIS Provider: {activateResult.Message}");
            }
        }
    }
}
