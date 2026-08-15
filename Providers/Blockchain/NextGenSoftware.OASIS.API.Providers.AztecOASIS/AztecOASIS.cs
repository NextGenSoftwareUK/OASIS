using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider
    {
        private readonly AztecAPIClient _apiClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _network;
        private readonly string _bridgeContractAddress;
        private readonly string _operatorAccountAlias;

        private IAztecService _aztecService;
        private IAztecBridgeService _bridgeService;
        private IAztecRepository _aztecRepository;

        public AztecOASIS(string apiBaseUrl = null, string apiKey = null, string network = "sandbox",
            string bridgeContractAddress = "", string operatorAccountAlias = "oasis_operator")
        {
            ProviderName = nameof(AztecOASIS);
            ProviderDescription = "Aztec Privacy Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AztecOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _apiBaseUrl = apiBaseUrl ?? "http://localhost:8080";
            _apiKey = apiKey ?? "";
            _network = network ?? "sandbox";
            _bridgeContractAddress = bridgeContractAddress ?? "";
            _operatorAccountAlias = string.IsNullOrWhiteSpace(operatorAccountAlias) ? "oasis_operator" : operatorAccountAlias;

            _apiClient = new AztecAPIClient(_apiBaseUrl, _apiKey);
        }
    }
}
