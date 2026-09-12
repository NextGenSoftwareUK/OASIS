using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed partial class AvalancheOASIS_Legacy
{
    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for all avatars
            var avatars = new List<IAvatar>();
            
            try
            {
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        var avatar = new Avatar();
                        avatar.Id = AvalancheContractHelper.CreateDeterministicGuid($"{ProviderType.Value}:avatar:{avatarData.EntityId}");
                        avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                        
                        avatars.Add(avatar);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatars from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = avatars;
            result.IsError = false;
            result.Message = $"Successfully loaded {avatars.Count} avatars from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for all holons
            var holons = new List<IHolon>();
            
            try
            {
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = AvalancheContractHelper.CreateDeterministicGuid($"{ProviderType.Value}:holon:{holonData.EntityId}");
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

}
