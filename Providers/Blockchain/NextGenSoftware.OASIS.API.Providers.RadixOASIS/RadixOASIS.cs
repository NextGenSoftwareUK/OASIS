using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// OASIS Provider for Radix DLT blockchain with cross-chain bridge support and first-party oracle capabilities.
/// Inspired by API3's Airnode - Radix can run their own oracle node with no middleware.
/// </summary>
public partial class RadixOASIS : OASISStorageProviderBase, IOASISStorageProvider, 
    IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNETProvider
{
    private IRadixService? _radixService;
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private RadixOracleNode? _oracleNode;
    private RadixChainObserver? _chainObserver;

    /// <summary>
    /// Gets the Radix bridge service for cross-chain operations
    /// </summary>
    public IRadixService? RadixBridgeService => _radixService;

    /// <summary>
    /// Gets the Radix first-party oracle node - allows Radix to run their own oracle with no middleware
    /// </summary>
    public RadixOracleNode? OracleNode => _oracleNode;

    /// <summary>
    /// Gets the Radix chain observer for oracle operations
    /// </summary>
    public RadixChainObserver? ChainObserver => _chainObserver;

    public RadixOASIS(string hostUri, byte networkId, string accountAddress, string privateKey)
    {
        this.ProviderName = nameof(RadixOASIS);
        this.ProviderDescription = "Radix DLT Blockchain Provider with Bridge Support";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RadixOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _config = new RadixOASISConfig
        {
            HostUri = hostUri,
            NetworkId = networkId,
            AccountAddress = accountAddress,
            PrivateKey = privateKey
        };

        _httpClient = new HttpClient();
    }

    public RadixOASIS(RadixOASISConfig config)
    {
        this.ProviderName = nameof(RadixOASIS);
        this.ProviderDescription = "Radix DLT Blockchain Provider with Bridge Support";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RadixOASIS);
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = new HttpClient();
    }
}
