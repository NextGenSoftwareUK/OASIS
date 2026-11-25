using System;
using System.Collections.Generic;
using System.Linq;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS
{
    /// <summary>
    /// OASIS provider for Secure Scuttlebutt (SSB).
    ///
    /// SSB has no network storage API: data is a signed, append-only feed owned by one key pair. Storage goes through the
    /// repository's Scuttlebutt storage service (<c>StorageService/storage-service.cjs</c>: ssb-db2 on secret-stack), which
    /// owns the feed. Every OASIS record key is the newest <c>oasis-record</c> message for that key in the feed; a delete
    /// appends a null value (feed messages cannot be removed). Values are compressed and encrypted with the configured key
    /// because feed messages replicate in plaintext to every peer that follows the feed. SSB caps messages at 8 KiB, so
    /// larger records are rejected explicitly.
    ///
    /// Setup: <c>npm ci</c> in StorageService, then
    /// <c>OASIS_SSB_STORAGE_TOKEN=... node storage-service.cjs &lt;data dir&gt; &lt;port&gt;</c>.
    /// </summary>
    public class ScuttlebuttOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        /// <param name="serviceUrl">Scuttlebutt storage service URL, e.g. <c>http://127.0.0.1:18904</c>.</param>
        /// <param name="serviceToken">The service's bearer token.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public ScuttlebuttOASIS(string serviceUrl, string serviceToken, byte[] encryptionKey)
            : base(CreateBackend(serviceUrl, serviceToken, encryptionKey))
        {
            ProviderName = "ScuttlebuttOASIS";
            ProviderDescription = "Scuttlebutt provider: encrypted records as messages in a signed append-only SSB feed.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ScuttlebuttOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(string serviceUrl, string serviceToken, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Scuttlebutt storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(
                new StorageServiceKeyValueBackend(serviceUrl, serviceToken, "Scuttlebutt storage service", "@"), encryptionKey, compress: true);
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "ScuttlebuttOASIS: Geolocation is not supported.");
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ScuttlebuttOASIS: Geolocation is not supported.");
            return result;
        }

        #region IOASISSuperStar

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                string folder = System.IO.Path.Combine(outputFolder, "Scuttlebutt");
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"@context\": \"https://www.w3.org/ns/activitystreams\",");
                sb.AppendLine($"  \"name\": \"{celestialBody.Name ?? "OAPP"}\",");
                sb.AppendLine("  \"type\": \"Application\",");
                sb.AppendLine("  \"items\": [");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                bool first = true;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;
                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;
                            if (!first) sb.AppendLine(",");
                            first = false;
                            sb.AppendLine("    {");
                            sb.AppendLine($"      \"type\": \"{holon.Name}\",");
                            sb.AppendLine($"      \"id\": \"{holon.Id}\",");
                            sb.Append($"      \"name\": \"{holon.Name}\"");
                            if (!string.IsNullOrWhiteSpace(holon.Description))
                            {
                                sb.AppendLine(",");
                                sb.Append($"      \"summary\": \"{holon.Description}\"");
                            }
                            sb.AppendLine();
                            sb.Append("    }");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
