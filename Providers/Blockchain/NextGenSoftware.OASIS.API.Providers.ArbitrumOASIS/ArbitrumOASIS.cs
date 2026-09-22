using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

/// <summary>
/// Arbitrum provider (current live deployment). Uses chain-specific contract and Nethereum.
/// When the generic Web3Core contract is deployed to Arbitrum, use <see cref="ArbitrumOASIS_Web3Core"/> instead.
/// </summary>
public sealed partial class ArbitrumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private static Guid CreateDeterministicGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.Empty;
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes.Take(16).ToArray());
    }

    private readonly string _hostURI;
    private readonly string _chainPrivateKey;
    private readonly BigInteger _chainId;
    private readonly string _contractAddress;
    private readonly HexBigInteger _gasLimit = new(500000);

    private Web3 _web3Client;
    private Account _oasisAccount;
    private Contract _contract;
    private ContractHandler _contractHandler;

    public ArbitrumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
    {
        this.ProviderName = "ArbitrumOASIS";
        this.ProviderDescription = "Arbitrum Provider";
        this.ProviderType = new(Core.Enums.ProviderType.ArbitrumOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _hostURI = hostUri;
        _chainPrivateKey = chainPrivateKey;
        _chainId = chainId;
        _contractAddress = contractAddress;
    }

    public bool IsVersionControlEnabled { get; set; }

    public override Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = ActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        OASISResult<bool> result = new();

        try
        {
            if (!this.IsProviderActivated)
            {
                if (_hostURI is { Length: > 0 } &&
                _chainPrivateKey is { Length: > 0 } &&
                _chainId > 0 &&
                _contractAddress is { Length: > 0 })
                {
                    _oasisAccount = new Account(_chainPrivateKey, _chainId);
                    _web3Client = new Web3(_oasisAccount, _hostURI);
                    _contract = _web3Client.Eth.GetContract(ArbitrumContractHelper.Abi, _contractAddress);
                    _contractHandler = _web3Client.Eth.GetContractHandler(_contractAddress);

                    this.IsProviderActivated = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
        }

        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = DeActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _oasisAccount = null;
        _web3Client = null;

        IsProviderActivated = false;

        return new(value: true);
    }
}
