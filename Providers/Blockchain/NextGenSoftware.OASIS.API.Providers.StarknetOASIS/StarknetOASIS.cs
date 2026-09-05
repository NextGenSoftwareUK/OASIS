using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS : OASISStorageProviderBase,
    IOASISStorageProvider,
    IOASISBlockchainStorageProvider,
    IOASISNETProvider,
    IOASISSmartContractProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _network;
    private readonly IStarknetRpcClient _rpcClient;
    private readonly string _contractAddress;
    private bool _isActivated;

    public StarknetOASIS(string network = "alpha-goerli", string apiBaseUrl = "https://alpha4.starknet.io", string contractAddress = null)
    {
        ProviderName = nameof(StarknetOASIS);
        ProviderDescription = "Starknet privacy provider for cross-chain swaps";
        ProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.StarknetOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _network = network;
        _contractAddress = contractAddress ?? Environment.GetEnvironmentVariable("STARKNET_OASIS_CONTRACT_ADDRESS");
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        };
        _rpcClient = new StarknetRpcClient(_httpClient, apiBaseUrl);
    }
}
