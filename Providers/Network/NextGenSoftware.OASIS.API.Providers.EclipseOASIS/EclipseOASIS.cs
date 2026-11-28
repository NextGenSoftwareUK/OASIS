using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS
{
    /// <summary>
    /// OASIS provider for Eclipse, a Solana VM (SVM) L2 settled on Ethereum with Celestia data availability.
    /// Eclipse runs unmodified Solana programs and serves the Solana JSON-RPC, so storage is the shipping
    /// <see cref="SolanaOASIS"/> implementation (signed transactions, encrypted records) against the Eclipse RPC; every
    /// storage member forwards to it (EclipseOASIS.Storage.cs). Records it writes carry Eclipse storage keys so routing retains the composing
    /// provider identity. The previous implementation rejected every write and only read raw transactions.
    /// </summary>
    public partial class EclipseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        public const string MainnetRpc = "https://mainnetbeta-rpc.eclipse.xyz";
        private readonly SolanaOASIS _solana;

        /// <param name="rpcUrl">Eclipse (Solana-compatible) JSON-RPC endpoint.</param>
        /// <param name="privateKey">Base58 64-byte Solana keypair of the funded storage account.</param>
        /// <param name="publicKey">Base58 public key of that account.</param>
        public EclipseOASIS(string rpcUrl = MainnetRpc, string privateKey = "", string publicKey = "")
        {
            _solana = new SolanaOASIS(rpcUrl, privateKey, publicKey, Core.Enums.ProviderType.EclipseOASIS);
            ProviderName = "EclipseOASIS";
            ProviderDescription = "Eclipse SVM L2 Provider — Solana VM on Ethereum settlement with Celestia DA";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EclipseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "EclipseOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "EclipseOASIS: Geolocation not supported."); return r; }
    }
}
