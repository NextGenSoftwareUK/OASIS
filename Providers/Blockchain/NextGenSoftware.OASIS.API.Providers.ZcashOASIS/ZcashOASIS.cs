using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public partial class ZcashOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider
    {
        private IZcashRepository _zcashRepository;
        private IZcashService _zcashService;
        private IZcashBridgeService _zcashBridgeService;
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _rpcUrl;
        private readonly string _rpcUser;
        private readonly string _rpcPassword;
        private readonly string _network; // "mainnet" or "testnet"

        public ZcashOASIS(string rpcUrl = null, string rpcUser = null, string rpcPassword = null, string network = "testnet")
        {
            this.ProviderName = nameof(ZcashOASIS);
            this.ProviderDescription = "Zcash Blockchain Provider with Shielded Transaction Support";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ZcashOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _rpcUrl = rpcUrl ?? Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232";
            _rpcUser = rpcUser ?? Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user";
            _rpcPassword = rpcPassword ?? Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password";
            _network = network;

            _rpcClient = new ZcashRPCClient(_rpcUrl, _rpcUser, _rpcPassword);
        }
    }
}
