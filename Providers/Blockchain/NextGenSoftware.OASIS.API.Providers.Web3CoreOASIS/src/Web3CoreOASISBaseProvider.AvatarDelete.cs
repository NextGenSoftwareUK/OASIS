using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result = new();

        try
        {
            if (string.IsNullOrWhiteSpace(_hostURI) || string.IsNullOrWhiteSpace(_chainPrivateKey) || string.IsNullOrWhiteSpace(_contractAddress))
                throw new InvalidOperationException("The EVM provider requires an RPC URI, private key and deployed contract address.");

            var account = new Account(_chainPrivateKey,_expectedChainId);
            _web3Client = new Nethereum.Web3.Web3(account, _hostURI);
            HexBigInteger remoteChainId = await _web3Client.Eth.ChainId.SendRequestAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (_expectedChainId.HasValue && remoteChainId.Value != _expectedChainId.Value)
                throw new InvalidOperationException($"EVM RPC chain ID '{remoteChainId.Value}' does not match expected chain ID '{_expectedChainId.Value}'.");

            string contractCode = await _web3Client.Eth.GetCode.SendRequestAsync(_contractAddress).WaitAsync(TimeSpan.FromSeconds(10));
            if (string.IsNullOrWhiteSpace(contractCode) || contractCode == "0x" || contractCode == "0x0")
                throw new InvalidOperationException($"No contract bytecode was found at configured address '{_contractAddress}'.");

            _web3CoreOASIS = new(_chainPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi,_expectedChainId);
            IsProviderActivated = true;
            result.Result = true;
        }
        catch (Exception ex)
        {
            _web3CoreOASIS = null;
            _web3Client = null;
            IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error activating {ProviderName} through Web3CoreOASIS. Reason: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().GetAwaiter().GetResult();
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
        _web3CoreOASIS = null;
        IsProviderActivated = false;

        return new(value: true);
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        => DeletePrivateAvatarAsync(id,softDelete).GetAwaiter().GetResult();

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        => ReadPrivateKeyAsync<bool>(providerKey,id=>DeletePrivateAvatarAsync(id,softDelete)).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        => await DeletePrivateAvatarAsync(id,softDelete);

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        => await ReadPrivateKeyAsync<bool>(providerKey,id=>DeletePrivateAvatarAsync(id,softDelete));

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        => DeletePrivateAvatarSelectorAsync(avatarEmail,true,softDelete).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        => await DeletePrivateAvatarSelectorAsync(avatarEmail,true,softDelete);

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        => DeletePrivateAvatarSelectorAsync(avatarUsername,false,softDelete).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        => await DeletePrivateAvatarSelectorAsync(avatarUsername,false,softDelete);

    public override OASISResult<IHolon> DeleteHolon(Guid id)
        => DeleteHolonAsync(id).GetAwaiter().GetResult();

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
        => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        var result=new OASISResult<IHolon>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core holon identity is required.",nameof(id));
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            var key=(uint)HashUtility.GetNumericHash(id.ToString());
            var stored=await _web3CoreOASIS.GetHolonByIdAsync(key);
            if(IsPrivateRecordAbsent(stored))
            {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core holon not found.";return result;}
            var holon=DecodePrivateRecord<Holon>("holon",id,stored);
            await _web3CoreOASIS.DeleteHolonCheckedAsync(key,PrivateExternalId(id),Nethereum.Util.Sha3Keccack.Current.CalculateHash(stored.Info));
            result.Result=holon;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        => DeleteHolonByProviderKeyAsync(providerKey);

    public Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
    {
        if(Guid.TryParse(providerKey,out var id) && id!=Guid.Empty) return DeleteHolonAsync(id);
        var result=new OASISResult<IHolon>();
        var cause=new ArgumentException("A Web3Core provider key must be a nonempty GUID.",nameof(providerKey));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);
        return Task.FromResult(result);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        => LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        => LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        => ExportPrivateSelectorAsync(avatarEmailAddress,true,version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        => await ExportPrivateSelectorAsync(avatarEmailAddress,true,version);

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        => ExportPrivateOwnedAsync(avatarId,version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        => await ExportPrivateOwnedAsync(avatarId,version);

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        => ExportPrivateSelectorAsync(avatarUsername,false,version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        => await ExportPrivateSelectorAsync(avatarUsername,false,version);

}
