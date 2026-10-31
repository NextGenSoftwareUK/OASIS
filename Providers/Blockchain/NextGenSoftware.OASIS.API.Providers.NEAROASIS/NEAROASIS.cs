using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider,
    IOASISBlockchainStorageProvider, IOASISIdBoundTokenEscrowProvider, IOASISSmartContractProvider, IOASISNFTProvider
{
    private readonly string _rpcEndpoint;
    private readonly string _networkId;
    private readonly string _chainId;
    private readonly string _privateKey;
    private readonly string _accountId;
    private readonly string _contractAddress;
    private readonly NearSdkService _sdk;
    private bool _isActivated;
    private WalletManager _walletManager;
    public WalletManager WalletManager
    {
        get=>_walletManager??=new WalletManager(this,OASISDNA);
        set=>_walletManager=value;
    }

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

        _walletManager=walletManager;
        _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
        _networkId = networkId ?? throw new ArgumentNullException(nameof(networkId));
        _chainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
        _contractAddress = contractAddress ?? throw new ArgumentNullException(nameof(contractAddress));
        _accountId = accountId ?? string.Empty;
        _privateKey = privateKey ?? string.Empty;
        _sdk = new NearSdkService(_rpcEndpoint, _networkId, _contractAddress, _accountId, _privateKey);
    }
}
