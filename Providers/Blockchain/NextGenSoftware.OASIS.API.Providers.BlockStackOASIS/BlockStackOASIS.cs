using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly BlockStackClient _blockStackClient;
        private readonly string _contractAddress;

        /// <summary>Unconfigured provider: storage and Gaia NFT paths fail explicitly until a hub, bucket key and record
        /// encryption key are supplied.</summary>
        public BlockStackOASIS(string contractAddress = null)
            : this(new BlockStackClient(), null, contractAddress) { }

        /// <param name="gaiaHubUrl">Gaia hub write endpoint, for example https://hub.blockstack.org.</param>
        /// <param name="bucketPrivateKeyHex">secp256k1 key that owns the Gaia bucket; it signs every hub auth token.</param>
        /// <param name="encryptionKey">32-byte record encryption key: Gaia reads are public, so records are stored encrypted.</param>
        public BlockStackOASIS(string gaiaHubUrl, string bucketPrivateKeyHex, byte[] encryptionKey, string contractAddress = null)
            : this(new BlockStackClient(gaiaHubUrl, bucketPrivateKeyHex), encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey)), contractAddress) { }

        private BlockStackOASIS(BlockStackClient client, byte[] encryptionKey, string contractAddress)
            : base(encryptionKey == null
                ? new UnconfiguredGaiaBackend()
                : new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PrivateKeyValueBackend(new GaiaHubKeyValueBackend(client), encryptionKey, compress: true))
        {
            this.ProviderName = "BlockStackOASIS";
            this.ProviderDescription = "BlockStack Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BlockStackOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
            _blockStackClient = client;
            _contractAddress = contractAddress ?? Environment.GetEnvironmentVariable("BLOCKSTACK_CONTRACT_ADDRESS") ?? "SP000000000000000000002Q6VF78";
        }

        /// <summary>Gaia bucket address that holds this provider's records.</summary>
        public string GaiaBucketAddress => _blockStackClient.BucketAddress;

        private sealed class UnconfiguredGaiaBackend : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "BlockStack storage requires an explicit Gaia hub URL, bucket key and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }
    }
}
