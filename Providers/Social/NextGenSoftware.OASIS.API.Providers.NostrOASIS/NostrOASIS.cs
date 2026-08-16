using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
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

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS
{
    /// <summary>
    /// OASIS provider for the Nostr decentralised social protocol.
    /// Uses WebSocket relay connections (System.Net.WebSockets) — no external NuGet packages required.
    ///
    /// Key Nostr concepts mapped to OASIS:
    ///   npub (public key hex) = avatar's Nostr identity / provider key
    ///   Kind 0 event (profile metadata)  = OASIS Avatar
    ///   Kind 1 event (text note)          = OASIS Holon
    ///   Kind 3 event (follow list)        = relationship data (not yet mapped)
    ///
    /// NOTE: Publishing (writing) Nostr events requires ed25519 / secp256k1 signing.
    /// Nostr uses secp256k1, which is not available in the BCL without a third-party library
    /// such as NBitcoin. An optional nsecHex constructor parameter is accepted but publishing
    /// will return IsError=true with guidance until a signing library is wired in.
    /// </summary>
    public class NostrOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly string[] _relayUrls;
        private readonly string? _nsecHex;
        private const string DefaultRelay = "wss://relay.damus.io";

        private static readonly string[] DefaultRelays =
        {
            "wss://relay.damus.io",
            "wss://nos.lol",
            "wss://relay.nostr.band"
        };

        public NostrOASIS(string[]? relayUrls = null, string? nsecHex = null)
        {
            _relayUrls = (relayUrls != null && relayUrls.Length > 0) ? relayUrls : DefaultRelays;
            _nsecHex = nsecHex;

            ProviderName = "NostrOASIS";
            ProviderDescription = "Nostr decentralised social protocol provider (WebSocket relay connections)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NostrOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Relay helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Connects to a Nostr relay via WebSocket, sends a single message,
        /// waits for the first response frame, and returns it as a string.
        /// </summary>
        private async Task<string> SendToRelayAsync(string relayUrl, string message)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(relayUrl), CancellationToken.None);

            var msgBytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(msgBytes, WebSocketMessageType.Text, true, CancellationToken.None);

            var buf = new byte[65536];
            var result = await ws.ReceiveAsync(buf, CancellationToken.None);
            return Encoding.UTF8.GetString(buf, 0, result.Count);
        }

        /// <summary>
        /// Reads one or more messages from the relay until an EVENT or EOSE frame is received.
        /// Returns the first EVENT frame found (or empty string if only EOSE/NOTICE received).
        /// </summary>
        private async Task<string> QueryRelayAsync(string relayUrl, string reqMessage)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(relayUrl), CancellationToken.None);

            var msgBytes = Encoding.UTF8.GetBytes(reqMessage);
            await ws.SendAsync(msgBytes, WebSocketMessageType.Text, true, CancellationToken.None);

            var buf = new byte[65536];
            // Read up to 10 frames looking for an EVENT response
            for (int i = 0; i < 10; i++)
            {
                var res = await ws.ReceiveAsync(buf, CancellationToken.None);
                var frame = Encoding.UTF8.GetString(buf, 0, res.Count);

                if (frame.StartsWith("[\"EVENT\"", StringComparison.OrdinalIgnoreCase))
                    return frame;

                if (frame.StartsWith("[\"EOSE\"", StringComparison.OrdinalIgnoreCase))
                    break; // End of stored events — no result
            }
            return string.Empty;
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Send a minimal REQ for a kind-0 event to verify relay connectivity
                string testReq = "[\"REQ\",\"test\",{\"kinds\":[0],\"limit\":1}]";
                var response = await SendToRelayAsync(_relayUrls[0], testReq);

                // Any non-empty response (EVENT, EOSE, NOTICE) means the relay is reachable
                if (!string.IsNullOrEmpty(response))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"NostrOASIS provider activated successfully via relay '{_relayUrls[0]}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: No response from relay '{_relayUrls[0]}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"NostrOASIS: Error activating provider via relay '{_relayUrls[0]}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            return await Task.FromResult(new OASISResult<bool>
            {
                Result = true,
                Message = "NostrOASIS provider deactivated."
            });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // providerKey = npub hex (32-byte public key as hex string)
                string reqMessage = $"[\"REQ\",\"sub1\",{{\"authors\":[\"{providerKey}\"],\"kinds\":[0],\"limit\":1}}]";

                string frame = string.Empty;
                foreach (var relay in _relayUrls)
                {
                    try
                    {
                        frame = await QueryRelayAsync(relay, reqMessage);
                        if (!string.IsNullOrEmpty(frame))
                            break;
                    }
                    catch { /* try next relay */ }
                }

                if (string.IsNullOrEmpty(frame))
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: No kind-0 (profile) event found for pubkey '{providerKey}'.");
                    return result;
                }

                // frame format: ["EVENT","sub1",{...event...}]
                var doc = JsonDocument.Parse(frame);
                var arr = doc.RootElement;
                if (arr.GetArrayLength() >= 3)
                {
                    var eventEl = arr[2];
                    result.Result = MapKind0ToAvatar(providerKey, eventEl);
                    result.IsError = false;
                    result.Message = $"Avatar loaded from Nostr for pubkey '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: Unexpected response format for pubkey '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"NostrOASIS: Error loading avatar by provider key: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // Use nostr.band search API to resolve username → pubkey
            var result = new OASISResult<IAvatar>();
            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(
                    $"https://nostr.band/api/v0/profiles/search?q={Uri.EscapeDataString(avatarUsername)}");

                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: nostr.band search failed for username '{avatarUsername}' ({response.StatusCode}). " +
                        "NIP-05 lookup is required to map Nostr usernames to pubkeys.");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("profiles", out var profilesEl)
                    || profilesEl.GetArrayLength() == 0)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: No profile found for username '{avatarUsername}' on nostr.band.");
                    return result;
                }

                string? pubkey = null;
                if (profilesEl[0].TryGetProperty("pubkey", out var pkEl))
                    pubkey = pkEl.GetString();

                if (string.IsNullOrEmpty(pubkey))
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: Profile found but pubkey is missing for username '{avatarUsername}'.");
                    return result;
                }

                return await LoadAvatarByProviderKeyAsync(pubkey, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"NostrOASIS: Error loading avatar by username: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
            => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: GUID lookup is not natively supported. Use LoadAvatarByProviderKeyAsync(npubHex) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Email lookup is not supported by the Nostr protocol. " +
                "Use LoadAvatarByUsernameAsync or LoadAvatarByProviderKeyAsync(npubHex) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: LoadAllAvatars is not supported — Nostr has no global user index. " +
                "Use LoadAvatarByUsernameAsync or LoadAvatarByProviderKeyAsync(npubHex) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── AvatarDetail loading ─────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadAvatarDetail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadAvatarDetailByEmail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
            => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadAvatarDetailByUsername is not supported. Use LoadAvatarByUsernameAsync instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
            => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadAllAvatarDetails is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Save / Delete Avatar ─────────────────────────────────────────────────

        private OASISResult<T> SigningNotSupported<T>()
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Publishing events requires an ed25519 private key (nsec). " +
                "Supply via constructor overload NostrOASIS(string[] relays, string nsecHex) to enable publishing. " +
                "Note: Nostr uses secp256k1 (not P-256). Signing requires NBitcoin or a similar secp256k1 library " +
                "which is not bundled with this provider. Add NBitcoin and re-implement PublishNoteAsync signing.");
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
            => await Task.FromResult(SigningNotSupported<IAvatar>());

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => SaveAvatarAsync(Avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
            => await Task.FromResult(SigningNotSupported<IAvatarDetail>());

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => SaveAvatarDetailAsync(Avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Holon loading ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Use LoadHolonAsync(string providerKey) with a Nostr event ID (hex) to load a specific note.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // providerKey = Nostr event ID (hex)
                string reqMessage = $"[\"REQ\",\"sub2\",{{\"ids\":[\"{providerKey}\"],\"kinds\":[1],\"limit\":1}}]";

                string frame = string.Empty;
                foreach (var relay in _relayUrls)
                {
                    try
                    {
                        frame = await QueryRelayAsync(relay, reqMessage);
                        if (!string.IsNullOrEmpty(frame))
                            break;
                    }
                    catch { /* try next relay */ }
                }

                if (string.IsNullOrEmpty(frame))
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: No kind-1 (note) event found for event ID '{providerKey}'.");
                    return result;
                }

                var doc = JsonDocument.Parse(frame);
                var arr = doc.RootElement;
                if (arr.GetArrayLength() >= 3)
                {
                    result.Result = MapKind1ToHolon(arr[2]);
                    result.IsError = false;
                    result.Message = $"Holon (note) loaded from Nostr for event ID '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"NostrOASIS: Unexpected response format for event ID '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"NostrOASIS: Error loading holon by provider key: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Query global feed of kind-1 notes from the first relay
                string reqMessage = "[\"REQ\",\"sub3\",{\"kinds\":[1],\"limit\":50}]";
                var holons = new List<IHolon>();

                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(_relayUrls[0]), CancellationToken.None);
                var msgBytes = Encoding.UTF8.GetBytes(reqMessage);
                await ws.SendAsync(msgBytes, WebSocketMessageType.Text, true, CancellationToken.None);

                var buf = new byte[65536];
                for (int i = 0; i < 60; i++) // read up to 60 frames (50 events + overhead)
                {
                    var res = await ws.ReceiveAsync(buf, CancellationToken.None);
                    var frame = Encoding.UTF8.GetString(buf, 0, res.Count);

                    if (frame.StartsWith("[\"EOSE\"", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (frame.StartsWith("[\"EVENT\"", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var doc = JsonDocument.Parse(frame);
                            var arr = doc.RootElement;
                            if (arr.GetArrayLength() >= 3)
                                holons.Add(MapKind1ToHolon(arr[2]));
                        }
                        catch { /* skip malformed events */ }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Loaded {holons.Count} notes from Nostr relay '{_relayUrls[0]}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"NostrOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadHolonsForParent is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadHolonsForParent is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── MetaData queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Save / Delete Holon ──────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await Task.FromResult(SigningNotSupported<IHolon>());

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await Task.FromResult(SigningNotSupported<IEnumerable<IHolon>>());

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Deletion is not supported — Nostr relays retain events by design.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: Search is not yet implemented. Use LoadAvatarByUsernameAsync or LoadHolonAsync (event ID) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "NostrOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).Result;

        // ─── Publishing (outside interface) ──────────────────────────────────────

        /// <summary>
        /// Publishes a kind-1 (text note) Nostr event to all configured relays.
        /// IMPORTANT: Nostr uses secp256k1 for signing, which is not in the .NET BCL.
        /// This method returns IsError=true until a secp256k1 library (e.g. NBitcoin) is wired in.
        /// Add NBitcoin as a NuGet reference and replace the signing placeholder below.
        /// </summary>
        /// <param name="text">The note text to publish.</param>
        public async Task<OASISResult<string>> PublishNoteAsync(string text)
        {
            var result = new OASISResult<string>();

            if (string.IsNullOrEmpty(_nsecHex))
            {
                OASISErrorHandling.HandleError(ref result,
                    "NostrOASIS: No nsec private key provided. " +
                    "Construct NostrOASIS with NostrOASIS(relays, nsecHex) to enable publishing.");
                return await Task.FromResult(result);
            }

            // NOTE: secp256k1 signing (required by Nostr NIP-01) cannot be performed with the
            // built-in System.Security.Cryptography.ECDsa, which only supports NIST curves (P-256, P-384, P-521).
            // To implement signing, add NBitcoin (or another secp256k1 library) and replace this block:
            //
            //   var privateKey = new NBitcoin.Key(Convert.FromHexString(_nsecHex));
            //   // build NIP-01 canonical event JSON, SHA-256 hash it, sign with Schnorr
            //   // then broadcast ["EVENT", signedEvent] to each relay
            //
            OASISErrorHandling.HandleError(ref result,
                "NostrOASIS: secp256k1 signing requires NBitcoin or similar. " +
                "Add NBitcoin NuGet package and implement secp256k1 Schnorr signing in PublishNoteAsync.");
            return await Task.FromResult(result);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private static Avatar MapKind0ToAvatar(string npubHex, JsonElement eventEl)
        {
            var avatar = new Avatar();

            if (avatar.MetaData == null)
                avatar.MetaData = new Dictionary<string, object>();

            avatar.MetaData["NostrPubkey"] = npubHex;

            // Kind-0 content is a JSON string containing profile fields
            if (eventEl.TryGetProperty("content", out var contentEl))
            {
                try
                {
                    var contentStr = contentEl.GetString() ?? "{}";
                    var profile = JsonDocument.Parse(contentStr).RootElement;

                    if (profile.TryGetProperty("name", out var nameEl))
                        avatar.Username = nameEl.GetString() ?? string.Empty;

                    if (profile.TryGetProperty("about", out var aboutEl))
                        avatar.Description = aboutEl.GetString() ?? string.Empty;

                    if (profile.TryGetProperty("picture", out var picEl))
                        avatar.MetaData["Picture"] = picEl.GetString() ?? string.Empty;

                    if (profile.TryGetProperty("display_name", out var dnEl))
                        avatar.MetaData["DisplayName"] = dnEl.GetString() ?? string.Empty;
                }
                catch { /* malformed content — leave defaults */ }
            }

            return avatar;
        }

        private static Holon MapKind1ToHolon(JsonElement eventEl)
        {
            var holon = new Holon();

            if (holon.MetaData == null)
                holon.MetaData = new Dictionary<string, object>();

            string content = string.Empty;
            if (eventEl.TryGetProperty("content", out var contentEl))
                content = contentEl.GetString() ?? string.Empty;

            holon.Name = content.Length > 80 ? content[..80] : content;
            holon.Description = content;

            if (eventEl.TryGetProperty("id", out var idEl))
                holon.MetaData["NostrEventId"] = idEl.GetString() ?? string.Empty;

            if (eventEl.TryGetProperty("pubkey", out var pkEl))
                holon.MetaData["AuthorPubkey"] = pkEl.GetString() ?? string.Empty;

            if (eventEl.TryGetProperty("created_at", out var tsEl))
                holon.MetaData["CreatedAt"] = tsEl.ToString();

            return holon;
        }
    }
}
