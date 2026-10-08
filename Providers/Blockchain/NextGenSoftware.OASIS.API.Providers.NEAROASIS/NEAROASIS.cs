using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider,
    IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
{
    private readonly string _rpcEndpoint;
    private readonly string _networkId;
    private readonly string _privateKey;
    private readonly string _accountId;
    private readonly string _contractAddress;
    private readonly NearSdkService _sdk;
    private bool _isActivated;

    public NEAROASIS(string rpcEndpoint = "https://rpc.mainnet.near.org", string networkId = "mainnet",
        string chainId = "mainnet", string contractAddress = "oasis.near", string accountId = "",
        string privateKey = "", WalletManager walletManager = null)
    {
        ProviderName = "NEAROASIS";
        ProviderDescription = "NEAR storage, wallet, token, and NFT provider backed by near-api-js";
        ProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.NEAROASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
        _networkId = networkId ?? throw new ArgumentNullException(nameof(networkId));
        _contractAddress = contractAddress ?? throw new ArgumentNullException(nameof(contractAddress));
        _accountId = accountId ?? string.Empty;
        _privateKey = privateKey ?? string.Empty;
        _sdk = new NearSdkService(_rpcEndpoint, _networkId, _contractAddress, _accountId, _privateKey);
    }
}
