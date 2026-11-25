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
        /// <summary>Aztec stores private state in Noir contract notes reached through a PXE (JSON-RPC); there is no OASIS record
        /// store yet. The storage members used a REST API (/api/holons) that no Aztec node serves and reported success on null
        /// results, so they now fail explicitly until a Noir storage contract exists.</summary>
        private static OASISResult<T> NoRecordStore<T>()
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, "AztecOASIS has no OASIS record store yet; Aztec storage requires a Noir storage contract reached through a PXE.");
            return result;
        }

        private readonly AztecAPIClient _apiClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _network;
        private readonly string _bridgeContractAddress;
        private readonly string _operatorAccountAlias;
        private readonly string _stablecoinContractAddress;

        private IAztecService _aztecService;
        private IAztecBridgeService _bridgeService;
        private IAztecRepository _aztecRepository;

        public AztecOASIS(string apiBaseUrl = null, string apiKey = null, string network = "sandbox",
            string bridgeContractAddress = "", string operatorAccountAlias = "oasis_operator",
            string stablecoinContractAddress = "")
        {
            ProviderName = nameof(AztecOASIS);
            ProviderDescription = "Aztec Privacy Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AztecOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _apiBaseUrl = apiBaseUrl ?? "http://localhost:8080";
            _apiKey = apiKey ?? "";
            _network = network ?? "sandbox";
            _bridgeContractAddress = bridgeContractAddress ?? "";
            _operatorAccountAlias = string.IsNullOrWhiteSpace(operatorAccountAlias) ? "oasis_operator" : operatorAccountAlias;
            _stablecoinContractAddress = stablecoinContractAddress ?? "";

            _apiClient = new AztecAPIClient(_apiBaseUrl, _apiKey);
        }
    }
}
