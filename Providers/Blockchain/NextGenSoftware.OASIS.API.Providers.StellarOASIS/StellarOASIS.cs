using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS
{
    /// <summary>
    /// OASIS provider for the Stellar blockchain network.
    ///
    /// Key Stellar concepts mapped to OASIS:
    ///   Stellar account (public key / G-address)  = OASIS Avatar provider key
    ///   Account data entries (key/value pairs)     = OASIS Avatar metadata
    ///   Stellar transactions                       = OASIS Holons (read-only via Horizon)
    ///   Manage Data operations                     = write metadata to account
    ///
    /// Uses the public Stellar Horizon REST API — no SDK dependency required.
    /// Default Horizon endpoint: https://horizon.stellar.org (mainnet)
    /// For testnet use: https://horizon-testnet.stellar.org
    ///
    /// Writing transactions (SaveHolonAsync) requires a funded Stellar account and
    /// the account's secret key. Pass secretKey to the constructor to enable writes.
    /// The secret key is used only locally to sign transaction envelopes via the
    /// Stellar Horizon /transactions endpoint.
    /// </summary>
    public class StellarOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly string _horizonUrl;
        private readonly string? _secretKey;
        private readonly HttpClient _http;

        private const string MainnetHorizon = "https://horizon.stellar.org";

        public StellarOASIS(string horizonUrl = MainnetHorizon, string? secretKey = null)
        {
            _horizonUrl = horizonUrl.TrimEnd('/');
            _secretKey = secretKey;
            _http = new HttpClient { BaseAddress = new Uri(_horizonUrl), Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Add("Accept", "application/json");

            ProviderName = "StellarOASIS";
            ProviderDescription = "Stellar blockchain provider (Horizon REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.StellarOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── HTTP helper ──────────────────────────────────────────────────────────

        private async Task<JsonElement?> GetAsync(string path)
        {
            var response = await _http.GetAsync(path);
            if (!response.IsSuccessStatusCode) return null;
            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonDocument.Parse(json).RootElement;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var el = await GetAsync("/");
                if (el.HasValue && el.Value.TryGetProperty("network_passphrase", out var net))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"StellarOASIS activated. Network: {net.GetString()}. Horizon: {_horizonUrl}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: Could not reach Horizon at '{_horizonUrl}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "StellarOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        /// <summary>providerKey = Stellar public key (G-address), e.g. GABC...XYZ</summary>
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var el = await GetAsync($"/accounts/{providerKey}");
                if (!el.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: No Stellar account found for address '{providerKey}'.");
                    return result;
                }

                result.Result = MapAccountToAvatar(providerKey, el.Value);
                result.IsError = false;
                result.Message = $"StellarOASIS: Avatar loaded for Stellar address '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error loading avatar for '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            // Stellar doesn't have usernames; treat as Stellar address or federation address
            var result = new OASISResult<IAvatar>();
            try
            {
                string address = username;

                // If it looks like a federation address (user*domain.com), resolve it
                if (username.Contains('*'))
                {
                    var parts = username.Split('*', 2);
                    string federationUrl = $"https://{parts[1]}/.well-known/stellar.toml";
                    // Attempt federation lookup via Horizon's /federation endpoint
                    var fedEl = await GetAsync($"/federation?q={Uri.EscapeDataString(username)}&type=name");
                    if (fedEl.HasValue && fedEl.Value.TryGetProperty("account_id", out var acct))
                        address = acct.GetString() ?? username;
                }

                return await LoadAvatarByProviderKeyAsync(address, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error resolving username '{username}': {ex.Message}");
                return result;
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Avatars are keyed by Stellar G-address; GUID lookup is not supported. Use LoadAvatarByProviderKey.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // Fetch the most recent accounts from Horizon (paginated, up to 200)
                var el = await GetAsync("/accounts?limit=200&order=desc");
                var avatars = new List<IAvatar>();

                if (el.HasValue &&
                    el.Value.TryGetProperty("_embedded", out var embedded) &&
                    embedded.TryGetProperty("records", out var records))
                {
                    foreach (var account in records.EnumerateArray())
                    {
                        string id2 = account.TryGetProperty("account_id", out var aid) ? aid.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(id2))
                            avatars.Add(MapAccountToAvatar(id2, account));
                    }
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"StellarOASIS: Loaded {avatars.Count} account(s) from Horizon.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error loading all avatars: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            if (string.IsNullOrEmpty(_secretKey))
            {
                OASISErrorHandling.HandleError(ref result,
                    "StellarOASIS: A Stellar secret key is required to write account data entries. Pass secretKey to the constructor.");
                return result;
            }

            try
            {
                string address = avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.StellarOASIS)
                    ? avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.StellarOASIS]
                    : string.Empty;

                if (string.IsNullOrEmpty(address))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "StellarOASIS: Avatar has no StellarOASIS provider key (G-address). Cannot save.");
                    return result;
                }

                // Load current account for sequence number
                var accountEl = await GetAsync($"/accounts/{address}");
                if (!accountEl.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: Could not load account '{address}' to get sequence number.");
                    return result;
                }

                string sequence = accountEl.Value.TryGetProperty("sequence", out var seq)
                    ? seq.GetString() ?? "0" : "0";
                long nextSeq = long.Parse(sequence) + 1;

                // Build a Manage Data XDR transaction using Stellar base64 XDR construction.
                // We store avatar fields as account data entries (key = "oasis:field", value = base64-encoded UTF-8 string).
                // The XDR is submitted to Horizon as a signed transaction envelope.
                // For correctness we use the Horizon /transactions POST endpoint.

                // Encode avatar metadata as Stellar data entries
                var dataEntries = new Dictionary<string, string>
                {
                    ["oasis:username"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(avatar.Username ?? "")),
                    ["oasis:firstname"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(avatar.FirstName ?? "")),
                    ["oasis:lastname"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(avatar.LastName ?? "")),
                    ["oasis:email"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(avatar.Email ?? "")),
                    ["oasis:description"] = Convert.ToBase64String(Encoding.UTF8.GetBytes((avatar.Description ?? "")[..Math.Min(avatar.Description?.Length ?? 0, 64)]))
                };

                // Build and sign XDR transaction envelope using Stellar's transaction structure.
                // This uses the .NET stellar-dotnet-sdk approach but without the SDK dependency:
                // we construct the XDR manually using the known Stellar XDR encoding rules.
                string txEnvelope = BuildManageDataTxEnvelope(address, nextSeq, dataEntries, _secretKey);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("tx", txEnvelope)
                });

                var response = await _http.PostAsync("/transactions", content);
                string respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = $"StellarOASIS: Avatar data entries saved for account '{address}'.";
                }
                else
                {
                    var errEl = JsonDocument.Parse(respBody).RootElement;
                    string errDetail = errEl.TryGetProperty("detail", out var det) ? det.GetString() ?? respBody : respBody;
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: Transaction submission failed for '{address}': {errDetail}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Deleting Stellar accounts is not supported — accounts require a minimum XLM balance to exist on the ledger and cannot be removed via the API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Deleting Stellar accounts is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Deleting Stellar accounts is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => DeleteAvatarByEmailAsync(email, softDelete).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        /// <summary>providerKey = Stellar transaction hash (64-char hex)</summary>
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var el = await GetAsync($"/transactions/{providerKey}");
                if (!el.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: No transaction found with hash '{providerKey}'.");
                    return result;
                }

                result.Result = MapTransactionToHolon(providerKey, el.Value);
                result.IsError = false;
                result.Message = $"StellarOASIS: Holon loaded from Stellar transaction '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error loading holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Holons are keyed by Stellar transaction hash; GUID lookup is not supported. Use LoadHolon(string providerKey).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var el = await GetAsync("/transactions?limit=200&order=desc");
                var holons = new List<IHolon>();

                if (el.HasValue &&
                    el.Value.TryGetProperty("_embedded", out var emb) &&
                    emb.TryGetProperty("records", out var records))
                {
                    foreach (var tx in records.EnumerateArray())
                    {
                        string hash = tx.TryGetProperty("hash", out var h) ? h.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(hash))
                            holons.Add(MapTransactionToHolon(hash, tx));
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"StellarOASIS: Loaded {holons.Count} transaction(s) from Horizon.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: LoadHolonsForParent by GUID is not supported. Use the string overload with the Stellar G-address.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        /// <summary>providerKey = Stellar G-address — loads all transactions for that account</summary>
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var el = await GetAsync($"/accounts/{providerKey}/transactions?limit=200&order=desc");
                var holons = new List<IHolon>();

                if (el.HasValue &&
                    el.Value.TryGetProperty("_embedded", out var emb) &&
                    emb.TryGetProperty("records", out var records))
                {
                    foreach (var tx in records.EnumerateArray())
                    {
                        string hash = tx.TryGetProperty("hash", out var h) ? h.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(hash))
                            holons.Add(MapTransactionToHolon(hash, tx));
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"StellarOASIS: Loaded {holons.Count} transaction(s) for account '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error loading holons for account '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            if (string.IsNullOrEmpty(_secretKey))
            {
                OASISErrorHandling.HandleError(ref result,
                    "StellarOASIS: A Stellar secret key is required to submit transactions. Pass secretKey to the constructor.");
                return result;
            }

            try
            {
                // Store holon metadata in a Manage Data transaction on the source account.
                // Derive the source account address from the secret key.
                string sourceAddress = DerivePublicKey(_secretKey);

                var accountEl = await GetAsync($"/accounts/{sourceAddress}");
                if (!accountEl.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: Could not load source account '{sourceAddress}' for sequence number.");
                    return result;
                }

                string sequence = accountEl.Value.TryGetProperty("sequence", out var seq)
                    ? seq.GetString() ?? "0" : "0";
                long nextSeq = long.Parse(sequence) + 1;

                string nameClipped = (holon.Name ?? "")[..Math.Min(holon.Name?.Length ?? 0, 60)];
                string descClipped = (holon.Description ?? "")[..Math.Min(holon.Description?.Length ?? 0, 64)];

                var dataEntries = new Dictionary<string, string>
                {
                    ["oasis:holon:name"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(nameClipped)),
                    ["oasis:holon:desc"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(descClipped)),
                    ["oasis:holon:type"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(holon.HolonType.ToString()))
                };

                string txEnvelope = BuildManageDataTxEnvelope(sourceAddress, nextSeq, dataEntries, _secretKey);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("tx", txEnvelope)
                });

                var response = await _http.PostAsync("/transactions", content);
                string respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var respEl = JsonDocument.Parse(respBody).RootElement;
                    string txHash = respEl.TryGetProperty("hash", out var h) ? h.GetString() ?? "" : "";

                    if (holon.ProviderUniqueStorageKey == null)
                        holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.StellarOASIS] = txHash;

                    result.Result = holon;
                    result.IsError = false;
                    result.Message = $"StellarOASIS: Holon saved as Stellar transaction '{txHash}'.";
                }
                else
                {
                    var errEl = JsonDocument.Parse(respBody).RootElement;
                    string errDetail = errEl.TryGetProperty("detail", out var det) ? det.GetString() ?? respBody : respBody;
                    OASISErrorHandling.HandleError(ref result,
                        $"StellarOASIS: Transaction submission failed: {errDetail}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error saving holon: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var errors = new List<string>();

            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError) errors.Add(r.Message);
                else saved.Add(r.Result!);
            }

            result.Result = saved;
            result.IsError = errors.Count > 0;
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"StellarOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Stellar transactions are immutable — deletion is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
            => DeleteHolonAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "StellarOASIS: Stellar transactions are immutable — deletion is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
            => DeleteHolonAsync(providerKey, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string query = searchParams.SearchQuery?.ToLowerInvariant() ?? string.Empty;

                // Search transactions by memo text (Stellar memo is a transaction-level note)
                var el = await GetAsync($"/transactions?limit=200&order=desc");
                var matched = new List<IHolon>();

                if (el.HasValue &&
                    el.Value.TryGetProperty("_embedded", out var emb) &&
                    emb.TryGetProperty("records", out var records))
                {
                    foreach (var tx in records.EnumerateArray())
                    {
                        string memo = tx.TryGetProperty("memo", out var m) ? m.GetString() ?? "" : "";
                        string hash = tx.TryGetProperty("hash", out var h) ? h.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(hash) && memo.ToLowerInvariant().Contains(query))
                            matched.Add(MapTransactionToHolon(hash, tx));
                    }
                }

                result.Result = new SearchResults { Holons = matched };
                result.IsError = false;
                result.Message = $"StellarOASIS: Found {matched.Count} transaction(s) with memo matching '{searchParams.SearchQuery}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"StellarOASIS: Error during search: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── AvatarDetail (not applicable) ───────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "StellarOASIS: LoadAvatarDetail by GUID is not supported. Use LoadAvatarByProviderKey with the Stellar G-address.");
            return await Task.FromResult(result);
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "StellarOASIS: LoadAvatarDetailByUsername is not supported. Use LoadAvatarByProviderKey with the Stellar G-address.");
            return await Task.FromResult(result);
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "StellarOASIS: LoadAvatarDetailByEmail is not supported. Stellar accounts do not use email addresses.");
            return await Task.FromResult(result);
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "StellarOASIS: LoadAllAvatarDetails is not supported. Use LoadAllAvatars.");
            return await Task.FromResult(result);
        }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "StellarOASIS: SaveAvatarDetail is not supported. Use SaveAvatar to write Stellar account data entries.");
            return await Task.FromResult(result);
        }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        // ─── Mapping helpers ──────────────────────────────────────────────────────

        private static Avatar MapAccountToAvatar(string address, JsonElement account)
        {
            var avatar = new Avatar
            {
                Username = address,
                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>
                {
                    [Core.Enums.ProviderType.StellarOASIS] = address
                },
                MetaData = new Dictionary<string, object>
                {
                    ["StellarAddress"] = address
                }
            };

            if (account.TryGetProperty("sequence", out var seq))
                avatar.MetaData["StellarSequence"] = seq.GetString() ?? "";

            if (account.TryGetProperty("balances", out var balances))
            {
                foreach (var bal in balances.EnumerateArray())
                {
                    string assetType = bal.TryGetProperty("asset_type", out var at) ? at.GetString() ?? "" : "";
                    string balance = bal.TryGetProperty("balance", out var b) ? b.GetString() ?? "0" : "0";
                    if (assetType == "native")
                        avatar.MetaData["StellarXLMBalance"] = balance;
                }
            }

            // Read OASIS data entries written by SaveAvatarAsync
            if (account.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("oasis:username", out var un))
                    avatar.Username = Encoding.UTF8.GetString(Convert.FromBase64String(un.GetString() ?? ""));
                if (data.TryGetProperty("oasis:firstname", out var fn))
                    avatar.FirstName = Encoding.UTF8.GetString(Convert.FromBase64String(fn.GetString() ?? ""));
                if (data.TryGetProperty("oasis:lastname", out var ln))
                    avatar.LastName = Encoding.UTF8.GetString(Convert.FromBase64String(ln.GetString() ?? ""));
                if (data.TryGetProperty("oasis:email", out var em))
                    avatar.Email = Encoding.UTF8.GetString(Convert.FromBase64String(em.GetString() ?? ""));
                if (data.TryGetProperty("oasis:description", out var desc))
                    avatar.Description = Encoding.UTF8.GetString(Convert.FromBase64String(desc.GetString() ?? ""));
            }

            if (account.TryGetProperty("last_modified_time", out var lmt))
                avatar.ModifiedDate = DateTime.Parse(lmt.GetString() ?? DateTime.UtcNow.ToString("o"));

            return avatar;
        }

        private static Holon MapTransactionToHolon(string hash, JsonElement tx)
        {
            var holon = new Holon
            {
                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>
                {
                    [Core.Enums.ProviderType.StellarOASIS] = hash
                },
                MetaData = new Dictionary<string, object>
                {
                    ["StellarTxHash"] = hash
                }
            };

            string memo = tx.TryGetProperty("memo", out var m) ? m.GetString() ?? "" : "";
            holon.Name = string.IsNullOrEmpty(memo) ? hash[..Math.Min(hash.Length, 16)] + "…" : memo;
            holon.Description = memo;

            if (tx.TryGetProperty("source_account", out var src))
                holon.MetaData["StellarSource"] = src.GetString() ?? "";

            if (tx.TryGetProperty("created_at", out var ca))
                holon.CreatedDate = DateTime.Parse(ca.GetString() ?? DateTime.UtcNow.ToString("o"));

            if (tx.TryGetProperty("fee_charged", out var fee))
                holon.MetaData["StellarFeeCharged"] = fee.GetString() ?? "";

            if (tx.TryGetProperty("operation_count", out var ops))
                holon.MetaData["StellarOperationCount"] = ops.GetInt32();

            return holon;
        }

        // ─── XDR transaction building ─────────────────────────────────────────────

        /// <summary>
        /// Builds a base64-encoded signed Stellar transaction XDR envelope containing
        /// one Manage Data operation per entry in dataEntries.
        ///
        /// Stellar XDR encoding (BIG-endian, fixed/variable length fields):
        ///   TransactionEnvelope → v1 = {tx: Transaction, signatures: [DecoratedSignature]}
        ///   Transaction → {sourceAccount, fee, seqNum, timeBounds?, memo, operations, ext}
        ///   Operation → {sourceAccount?, body: ManageDataOp}
        ///   ManageDataOp → {dataName: string, dataValue: optional opaque}
        ///   DecoratedSignature → {hint: byte[4], signature: byte[64]}  (Ed25519 over SHA-256(networkId ++ txHash))
        /// </summary>
        private string BuildManageDataTxEnvelope(string sourceAddress, long sequence,
            Dictionary<string, string> dataEntries, string secretKeyBase32)
        {
            // Decode Stellar keys from base32 (Stellar uses a custom base32 with checksum)
            byte[] secretBytes = StellarBase32Decode(secretKeyBase32);  // 32-byte Ed25519 seed
            byte[] publicBytes = StellarBase32Decode(sourceAddress);     // 32-byte Ed25519 public key
            byte[] secretKeyBase32Bytes = secretBytes;

            // Network passphrase hash (mainnet)
            string networkPassphrase = _horizonUrl.Contains("testnet")
                ? "Test SDF Network ; September 2015"
                : "Public Global Stellar Network ; September 2015";
            byte[] networkId = Sha256(Encoding.UTF8.GetBytes(networkPassphrase));

            // Build transaction XDR body
            using var txStream = new System.IO.MemoryStream();
            using var txWriter = new System.IO.BinaryWriter(txStream);

            // sourceAccount: AccountID (PublicKey, type=ED25519=0)
            WriteUint32(txWriter, 0); // PUBLIC_KEY_TYPE_ED25519
            txWriter.Write(publicBytes);

            // fee: uint32 (100 stroops per operation)
            WriteUint32(txWriter, (uint)(100 * dataEntries.Count));

            // seqNum: int64
            WriteInt64(txWriter, sequence);

            // timeBounds: optional (none = 0)
            WriteUint32(txWriter, 0);

            // memo: MEMO_NONE = 0
            WriteUint32(txWriter, 0);

            // operations: array
            WriteUint32(txWriter, (uint)dataEntries.Count);
            foreach (var kv in dataEntries)
            {
                // sourceAccount: optional (none = 0)
                WriteUint32(txWriter, 0);

                // operationType: MANAGE_DATA = 10
                WriteUint32(txWriter, 10);

                // dataName: string (max 64 bytes)
                byte[] nameBytes = Encoding.UTF8.GetBytes(kv.Key);
                WriteUint32(txWriter, (uint)nameBytes.Length);
                txWriter.Write(nameBytes);
                WritePadding(txWriter, nameBytes.Length);

                // dataValue: optional (present = 1)
                if (string.IsNullOrEmpty(kv.Value))
                {
                    WriteUint32(txWriter, 0); // absent
                }
                else
                {
                    WriteUint32(txWriter, 1); // present
                    byte[] valBytes = Convert.FromBase64String(kv.Value);
                    WriteUint32(txWriter, (uint)valBytes.Length);
                    txWriter.Write(valBytes);
                    WritePadding(txWriter, valBytes.Length);
                }
            }

            // ext: union type 0 (no ext)
            WriteUint32(txWriter, 0);

            byte[] txBytes = txStream.ToArray();

            // Hash = SHA-256(networkId ++ ENVELOPE_TYPE_TX(2 as uint32 big-endian) ++ txBytes)
            byte[] envelopeType = new byte[] { 0, 0, 0, 2 }; // ENVELOPE_TYPE_TX = 2
            byte[] sigPayload = Sha256(Concat(networkId, envelopeType, txBytes));

            // Sign with Ed25519
            byte[] sig = StellarEd25519.Sign(sigPayload, secretKeyBase32Bytes);

            // Build TransactionEnvelope (v1)
            using var envStream = new System.IO.MemoryStream();
            using var envWriter = new System.IO.BinaryWriter(envStream);

            // ENVELOPE_TYPE_TX_V1 = 1 (newer v1 format)
            // Actually use legacy ENVELOPE_TYPE_TX = 2 format for maximum compatibility
            WriteUint32(envWriter, 2); // ENVELOPE_TYPE_TX
            envWriter.Write(txBytes);
            WriteUint32(envWriter, 1); // 1 signature
            envWriter.Write(publicBytes, 28, 4); // hint = last 4 bytes of public key
            WriteUint32(envWriter, 64);
            envWriter.Write(sig);

            return Convert.ToBase64String(envStream.ToArray());
        }

        // ─── Crypto helpers ───────────────────────────────────────────────────────

        private static byte[] Sha256(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(data);
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            int total = 0;
            foreach (var a in arrays) total += a.Length;
            byte[] result = new byte[total];
            int offset = 0;
            foreach (var a in arrays) { Buffer.BlockCopy(a, 0, result, offset, a.Length); offset += a.Length; }
            return result;
        }

        private static void WriteUint32(System.IO.BinaryWriter w, uint v)
        {
            w.Write((byte)(v >> 24)); w.Write((byte)(v >> 16));
            w.Write((byte)(v >> 8)); w.Write((byte)v);
        }

        private static void WriteInt64(System.IO.BinaryWriter w, long v)
        {
            ulong u = (ulong)v;
            w.Write((byte)(u >> 56)); w.Write((byte)(u >> 48)); w.Write((byte)(u >> 40)); w.Write((byte)(u >> 32));
            w.Write((byte)(u >> 24)); w.Write((byte)(u >> 16)); w.Write((byte)(u >> 8)); w.Write((byte)u);
        }

        private static void WritePadding(System.IO.BinaryWriter w, int len)
        {
            int pad = (4 - (len % 4)) % 4;
            for (int i = 0; i < pad; i++) w.Write((byte)0);
        }

        /// <summary>
        /// Decodes a Stellar strkey (G... or S... address) into the raw 32-byte payload.
        /// Stellar uses RFC 4648 base32 (alphabet A-Z 2-7) with a 2-byte CRC-16/XModem checksum.
        /// </summary>
        private static byte[] StellarBase32Decode(string strkey)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            // Strip padding and decode base32
            string s = strkey.TrimEnd('=').ToUpperInvariant();
            var bits = new System.Collections.Generic.List<int>();
            foreach (char c in s)
            {
                int val = alphabet.IndexOf(c);
                if (val < 0) throw new FormatException($"Invalid base32 character: {c}");
                for (int i = 4; i >= 0; i--) bits.Add((val >> i) & 1);
            }
            var bytes = new byte[bits.Count / 8];
            for (int i = 0; i < bytes.Length; i++)
                for (int b = 7; b >= 0; b--)
                    bytes[i] |= (byte)(bits[i * 8 + (7 - b)] << b);

            // bytes[0] = version byte, bytes[1..32] = payload, bytes[33..34] = checksum
            if (bytes.Length < 35) throw new FormatException("Stellar strkey too short.");
            byte[] payload = new byte[32];
            Buffer.BlockCopy(bytes, 1, payload, 0, 32);
            return payload;
        }

        /// <summary>
        /// Derives the Stellar G-address (public key strkey) from a secret key strkey.
        /// The Ed25519 public key is derived from the 32-byte seed, then strkey-encoded.
        /// </summary>
        private static string DerivePublicKey(string secretKeyBase32)
        {
            byte[] seed = StellarBase32Decode(secretKeyBase32);
            StellarEd25519.KeyPairFromSeed(out byte[] publicKey, out _, seed);
            return StellarBase32Encode(6, publicKey); // version byte 6 = G-address
        }

        private static string StellarBase32Encode(byte versionByte, byte[] payload)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var data = new byte[1 + payload.Length];
            data[0] = versionByte;
            Buffer.BlockCopy(payload, 0, data, 1, payload.Length);

            // CRC-16/XModem checksum
            ushort crc = 0;
            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                    crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
            var full = new byte[data.Length + 2];
            Buffer.BlockCopy(data, 0, full, 0, data.Length);
            full[data.Length] = (byte)(crc & 0xFF);
            full[data.Length + 1] = (byte)(crc >> 8);

            var sb = new System.Text.StringBuilder();
            int bits = 0, accumulator = 0;
            foreach (byte b in full)
            {
                accumulator = (accumulator << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    bits -= 5;
                    sb.Append(alphabet[(accumulator >> bits) & 0x1F]);
                }
            }
            if (bits > 0) sb.Append(alphabet[(accumulator << (5 - bits)) & 0x1F]);
            return sb.ToString();
        }

        /// <summary>
        /// Thin wrapper around .NET 9+ System.Security.Cryptography.Ed25519.
        /// </summary>
        private static class StellarEd25519
        {
            public static void KeyPairFromSeed(out byte[] publicKey, out byte[] _, byte[] seed)
            {
                using var key = System.Security.Cryptography.Ed25519.Create();
                key.ImportEd25519PrivateKey(seed, out _);
                publicKey = key.ExportEd25519PublicKey();
            }

            public static byte[] Sign(byte[] message, byte[] seed)
            {
                using var key = System.Security.Cryptography.Ed25519.Create();
                key.ImportEd25519PrivateKey(seed, out _);
                return key.SignData(message);
            }
        }
    }
}
