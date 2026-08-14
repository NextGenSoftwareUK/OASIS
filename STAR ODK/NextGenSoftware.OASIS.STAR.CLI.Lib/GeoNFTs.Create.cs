using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ADRaffy.ENSNormalize;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>
    {
        public override async Task<OASISResult<STARGeoNFT>> CreateAsync(ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            if (createOptions?.CustomCreateParams != null
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.Scripted, out object scriptedFlag)
                && scriptedFlag is bool sbf && sbf
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.WrapWeb4GeoSpatialNFTId, out object widObj)
                && widObj != null)
                return await CreateAsyncScriptedWrapFromWeb4GeoAsync(widObj.ToString(), holonSubType, showHeaderAndInro, addDependencies, providerType);

            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();
            OASISResult<IWeb4GeoSpatialNFT> geoNFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS Geo-NFT you wish to create a WEB5 Geo-NFT from?"))
            {
                Console.WriteLine("");
                geoNFTResult = await FindWeb4GeoNFTAsync("wrap");
            }
            else
            {
                Console.WriteLine("");
                geoNFTResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (geoNFTResult != null && geoNFTResult.Result != null && !geoNFTResult.IsError)
            {
                IWeb4GeoSpatialNFT geoNFT = geoNFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT to WEB5 STARNET which will create a WEB5 STAR GeoNFT that wraps around the WEB4 GeoNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASIS GeoNFT json file in the WEB5 STAR GeoNFT folder.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "WEB4 GeoNFT", geoNFT } }
                        },
                        STARNETHolon = new STARGeoNFT() 
                        { 
                            GeoNFTId = geoNFTResult.Result.Id 
                        }
                    }, holonSubType, showHeaderAndInro, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        UpdateWeb4AndWeb3GeoNFTJSONFiles(geoNFT, result.Result.STARNETDNA.SourcePath);

                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        
                        if (!result.Result.ChildrenIds.Contains(geoNFT.Id))
                            result.Result.ChildrenIds.Add(geoNFT.Id);
                        else
                            OASISErrorHandling.HandleError(ref result, "Error occured adding child WEB4 GeoNFT id to the parent WEB5 GeoNFT as it already exists in the list.");

                        OASISResult<STARGeoNFT> saveResult = await result.Result.SaveAsync<STARGeoNFT>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            if (!geoNFT.ParentWeb5NFTIds.Contains(saveResult.Result.Id))
                            {
                                geoNFT.ParentWeb5NFTIds.Add(saveResult.Result.Id);
                                OASISResult<IWeb4GeoSpatialNFT> web4GeoNFT = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(new UpdateWeb4GeoNFTRequest() { Id = geoNFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = geoNFT.MetaData }, providerType: providerType);

                                if (!(web4GeoNFT != null && web4GeoNFT.Result != null && !web4GeoNFT.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 Geo-NFT after creation of WEB5 STAR Geo-NFT in CreateAsync method. Reason: {web4GeoNFT.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, "Error occured adding WEB5 STAR Geo-NFT ID link to the child/wrapped WEB4 GeoNFT as it already exists in the list.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR Geo-NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured minting WEB4 GeoNFT in MintGeoNFTAsync method. Reason: {geoNFTResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 GeoNFT in LoadGeoNftAsync method. Reason: {geoNFTResult.Message}");
            }

            return result;
        }

        private async Task<OASISResult<STARGeoNFT>> CreateAsyncScriptedWrapFromWeb4GeoAsync(string web4IdOrName, object holonSubType, bool showHeaderAndInro, bool addDependencies, ProviderType providerType)
        {
            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();
            OASISResult<IWeb4GeoSpatialNFT> geoNFTResult = await FindWeb4GeoNFTAsync("wrap", web4IdOrName, providerType: providerType);
            if (geoNFTResult == null || geoNFTResult.Result == null || geoNFTResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading WEB4 GeoNFT for wrap. Reason: {geoNFTResult?.Message}");
                return result;
            }

            IWeb4GeoSpatialNFT geoNFT = geoNFTResult.Result;

            result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFT, STARNETDNA>()
            {
                STARNETDNA = new STARNETDNA()
                {
                    MetaData = new Dictionary<string, object>() { { "WEB4 GeoNFT", geoNFT } }
                },
                STARNETHolon = new STARGeoNFT()
                {
                    GeoNFTId = geoNFTResult.Result.Id
                }
            }, holonSubType, showHeaderAndInro, addDependencies, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                UpdateWeb4AndWeb3GeoNFTJSONFiles(geoNFT, result.Result.STARNETDNA.SourcePath);
                result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());

                if (!result.Result.ChildrenIds.Contains(geoNFT.Id))
                    result.Result.ChildrenIds.Add(geoNFT.Id);
                else
                    OASISErrorHandling.HandleError(ref result, "Error occured adding child WEB4 GeoNFT id to the parent WEB5 GeoNFT as it already exists in the list.");

                OASISResult<STARGeoNFT> saveResult = await result.Result.SaveAsync<STARGeoNFT>();
                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                {
                    if (!geoNFT.ParentWeb5NFTIds.Contains(saveResult.Result.Id))
                    {
                        geoNFT.ParentWeb5NFTIds.Add(saveResult.Result.Id);
                        OASISResult<IWeb4GeoSpatialNFT> web4GeoNFT = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(new UpdateWeb4GeoNFTRequest() { Id = geoNFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = geoNFT.MetaData }, providerType: providerType);
                        if (!(web4GeoNFT != null && web4GeoNFT.Result != null && !web4GeoNFT.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 Geo-NFT after creation of WEB5 STAR Geo-NFT. Reason: {web4GeoNFT.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured adding WEB5 STAR Geo-NFT ID link to the child/wrapped WEB4 GeoNFT as it already exists in the list.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR Geo-NFT after creation. Reason: {saveResult.Message}");
            }

            return result;
        }

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            //if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFTId") && starHolon.STARNETDNA.MetaData["GeoNFTId"] != null)
            //{
            //    Guid id = Guid.Empty;

            //    if (Guid.TryParse(starHolon.STARNETDNA.MetaData["GeoNFTId"].ToString(), out id))
            //    {
            //        OASISResult<IWeb4GeoSpatialNFT> web4GeoNFT = await NFTCommon.NFTManager.LoadGeoNftAsync(id);

            //        if (web4GeoNFT != null && web4GeoNFT.Result != null && !web4GeoNFT.IsError)
            //        {
            //            Console.WriteLine("");
            //            DisplayProperty("WEB4 GEO-NFT DETAILS", "", displayFieldLength, false);
            //            ShowGeoNFT(web4GeoNFT.Result, showHeader: false, showFooter: false);
            //        }
            //    }
            //}

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("WEB4 GeoNFT") && starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"] != null)
            {
                IWeb4GeoSpatialNFT geoNFT = starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"] as IWeb4GeoSpatialNFT;

                if (geoNFT == null)
                    geoNFT = JsonConvert.DeserializeObject<Web4OASISGeoSpatialNFT>(starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"].ToString());

                if (geoNFT != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 GEO-NFT DETAILS", "", displayFieldLength, false);
                    ShowGeoNFT(geoNFT, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public override async Task<OASISResult<STARGeoNFT>> DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = await base.DeleteAsync(idOrName, softDelete, providerType);

            if (result != null && result.Result != null && !result.IsError && result.IsDeleted)
            {
                if (result.Result.STARNETDNA != null && result.Result.STARNETDNA.MetaData != null && result.Result.STARNETDNA.MetaData.ContainsKey("WEB4 GEONFT") && result.Result.STARNETDNA.MetaData["WEB4 GEONFT"] != null)
                {
                    IWeb4GeoSpatialNFT nft = result.Result.STARNETDNA.MetaData["WEB4 GEONFT"] as IWeb4GeoSpatialNFT;

                    if (nft == null)
                        nft = JsonConvert.DeserializeObject<IWeb4GeoSpatialNFT>(result.Result.STARNETDNA.MetaData["WEB4 GEONFT"].ToString());

                    if (nft != null)
                    {
                        nft.ParentWeb5NFTIds.Remove(result.Result.Id);
                        OASISResult<IWeb4GeoSpatialNFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(new UpdateWeb4GeoNFTRequest() { Id = nft.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = nft.MetaData }, providerType: providerType);

                        if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB5 GeoNFT ID link from the metadata on it's child/wrapped WEB4 GeoNFT {nft.Id} and title {nft.Title}. Reason: {web4NFT.Message}");
                        else
                            CLIEngine.ShowSuccessMessage("WEB4 Link To WEB5 Removed.");

                        if (CLIEngine.GetConfirmation($"Do you wish to also delete the child WEB4 GeoNFT ({nft.Title}) and optionally it's child WEB3 NFT's?"))
                            await DeleteWeb4GeoNFTAsync(nft.Id.ToString(), providerType);
                        else
                            Console.WriteLine("");
                    }
                }
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintGeoNFTAsync(object mintParams = null)
        {
            IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync();
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);

            CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4GeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.MintAndPlaceWeb4GeoNFTAsync(new MintAndPlaceWeb4GeoSpatialNFTRequest()
            {
                Title = request.Title,
                Description = request.Description,
                MemoText = request.MemoText,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                MintedByAvatarId = request.MintedByAvatarId,
                //MintWalletAddress = request.MintWalletAddress,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Discount = request.Discount,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NumberToMint = request.NumberToMint,
                MetaData = request.MetaData,
                AllowOtherPlayersToAlsoCollect = geoRequest.AllowOtherPlayersToAlsoCollect,
                PermSpawn = geoRequest.PermSpawn,
                GlobalSpawnQuantity = geoRequest.GlobalSpawnQuantity,
                PlayerSpawnQuantity = geoRequest.PlayerSpawnQuantity,
                RespawnDurationInSeconds = geoRequest.RespawnDurationInSeconds,
                Lat = geoRequest.Lat,
                Long = geoRequest.Long,
                Nft2DSprite = geoRequest.Nft2DSprite,
                Nft2DSpriteURI = geoRequest.Nft2DSpriteURI,
                Nft3DObject = geoRequest.Nft3DObject,
                Nft3DObjectURI = geoRequest.Nft3DObjectURI,
                PlacedByAvatarId = geoRequest.PlacedByAvatarId,
                GeoNFTMetaDataProvider = geoRequest.GeoNFTMetaDataProvider,
                JSONMetaDataURL = request.JSONMetaDataURL,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTSent = request.WaitTillNFTSent,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds
            });

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Minted. {nftResult.Message} Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(nftResult.Message);
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }

            return nftResult;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> RemintGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured reminting WEB4 OASIS GeoNFT in RemintGeoNFTAsync method. Reason: ";
            string idOrName = mintParams != null ? mintParams.ToString() : "";
            result = await FindWeb4GeoNFTAsync("remint", idOrName);

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                {
                    IRemintWeb4GeoNFTRequest remintRequest = await NFTCommon.GenerateWeb4GeoNFTRemintRequestAsync(result.Result);

                    CLIEngine.ShowWorkingMessage("Reminting WEB4 OASIS GeoNFT & WEB3 NFT's...");
                    result = await STAR.OASISAPI.NFTs.RemintGeoNftAsync(remintRequest);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        foreach (Guid id in result.Result.ParentWeb5NFTIds)
                        {
                            CLIEngine.ShowWorkingMessage($"Updating WEB5 STAR GeoNFT {id}...");
                            OASISResult<STARGeoNFT> web5NFT = await STAR.STARAPI.GeoNFTs.LoadAsync(STAR.BeamedInAvatar.Id, id);
                            if (web5NFT != null && web5NFT.Result != null && !web5NFT.IsError)
                            {
                                web5NFT.Result.STARNETDNA.MetaData["WEB4 NFT"] = result.Result;
                                OASISResult<STARGeoNFT> saveWeb5NFT = await STAR.STARAPI.GeoNFTs.UpdateAsync(STAR.BeamedInAvatar.Id, web5NFT.Result, true);
                                if (saveWeb5NFT != null && saveWeb5NFT.Result != null && !saveWeb5NFT.IsError)
                                {
                                    UpdateWeb4AndWeb3GeoNFTJSONFiles(result.Result, web5NFT.Result.STARNETDNA.SourcePath);
                                    CLIEngine.ShowSuccessMessage($"WEB5 STAR GeoNFT {id} Successfully Updated.");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed updating WEB5 STAR GeoNFT {id} after reminting WEB4 GeoNFT. Reason: {saveWeb5NFT.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed loading WEB5 STAR GeoNFT {id} to update after reminting WEB4 GeoNFT. Reason: {web5NFT.Message}");
                        }
  
                        CLIEngine.ShowSuccessMessage(result.Message);
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}", e);
            }

            return result;
        }

        public async Task PlaceGeoNFTAsync()
        {
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(true);
            await PlaceWeb4GeoNFTCoreAsync(geoRequest);
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceGeoNFTFromJsonFileAsync(string jsonFilePath)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
            {
                OASISErrorHandling.HandleError(ref result, "Place GeoNFT: JSON file path is missing or does not exist.");
                CLIEngine.ShowErrorMessage(result.Message);
                return result;
            }

            try
            {
                string json = File.ReadAllText(jsonFilePath);
                PlaceWeb4GeoSpatialNFTRequest request = JsonConvert.DeserializeObject<PlaceWeb4GeoSpatialNFTRequest>(json);
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Place GeoNFT: JSON deserialized to null. Expected PlaceWeb4GeoSpatialNFTRequest.");
                    CLIEngine.ShowErrorMessage(result.Message);
                    return result;
                }

                request.PlacedByAvatarId = STAR.BeamedInAvatar.Id;
                return await PlaceWeb4GeoNFTCoreAsync(request);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Place GeoNFT from JSON failed: {ex.Message}", ex);
                CLIEngine.ShowErrorMessage(result.Message);
                return result;
            }
        }

        private async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceWeb4GeoNFTCoreAsync(IPlaceWeb4GeoSpatialNFTRequest geoRequest)
        {
            CLIEngine.ShowWorkingMessage("Placing WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4GeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.PlaceWeb4GeoNFTAsync(geoRequest);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                CLIEngine.ShowSuccessMessage(nftResult.Message);
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }

            return nftResult ?? new OASISResult<IWeb4GeoSpatialNFT> { IsError = true, Message = "Null result from PlaceWeb4GeoNFTAsync." };
        }

        public async Task SendGeoNFTAsync()
        {
            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the GeoNFT from?");
            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the GeoNFT to?");
            string tokenAddress = CLIEngine.GetValidInput("What is the token address of the NFT?");
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            await SendGeoNFTAsync(fromWalletAddress, toWalletAddress, tokenAddress, memoText);
        }

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendGeoNFTAsync(string fromWalletAddress, string toWalletAddress, string tokenAddress, string memoText)
        {
            CLIEngine.ShowWorkingMessage("Sending WEB4 GeoNFT...");

            OASISResult<ISendWeb4NFTResponse> response = await STAR.OASISAPI.NFTs.SendNFTAsync(STAR.BeamedInAvatar.Id, new SendWeb4NFTRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                TokenAddress = tokenAddress,
                MemoText = memoText ?? "",
            });

            if (response != null && response.Result != null && !response.IsError)
                CLIEngine.ShowSuccessMessage(response.Message);
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }

            if (response == null)
            {
                OASISResult<ISendWeb4NFTResponse> err = new OASISResult<ISendWeb4NFTResponse>();
                OASISErrorHandling.HandleError(ref err, "Null response from SendNFTAsync API.");
                return err;
            }

            return response;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> BurnGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            BurnWeb3NFTRequest burnRequest;

            if (mintParams is string jsonPath && !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath))
            {
                try
                {
                    burnRequest = JsonConvert.DeserializeObject<BurnWeb3NFTRequest>(File.ReadAllText(jsonPath));
                    if (burnRequest == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Burn request JSON deserialized to null. Expected BurnWeb3NFTRequest.");
                        return result;
                    }

                    burnRequest.BurntByAvatarId = STAR.BeamedInAvatar.Id;
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to read burn request JSON. {ex.Message}", ex);
                    return result;
                }
            }
            else
            {
                burnRequest = new BurnWeb3NFTRequest()
                {
                    OwnerPublicKey = CLIEngine.GetValidInput("Please enter the Public Key of the wallet that owns the NFT: "),
                    OwnerPrivateKey = CLIEngine.GetValidInput("Please enter the Private Key of the wallet that owns the NFT: "),
                    OwnerSeedPhrase = CLIEngine.GetValidInput("Please enter the Seed Phrase of the wallet that owns the NFT: "),
                    NFTTokenAddress = CLIEngine.GetValidInput("Please enter the Token Address of the NFT you wish to burn: "),
                    BurntByAvatarId = STAR.BeamedInAvatar.Id
                };
            }

            OASISResult<IWeb3NFTTransactionResponse> burnResult = await NFTCommon.NFTManager.BurnWeb3NFTAsync(burnRequest);

            if (burnResult != null && burnResult.Result != null && !burnResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("NFT Successfully Burnt.");
                result.Message = burnResult.Message ?? "NFT Successfully Burnt.";
            }
            else
            {
                string msg = burnResult?.Message ?? "Error burning NFT.";
                CLIEngine.ShowErrorMessage($"Error Burning NFT, Reason: {msg}");
                OASISErrorHandling.HandleError(ref result, msg);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            if (mintParams is string existingFile && File.Exists(existingFile))
            {
                try
                {
                    OASISResult<IWeb4GeoSpatialNFT> importResult = await NFTCommon.NFTManager.ImportWeb4GeoNFTAsync(STAR.BeamedInAvatar.Id, existingFile);
                    if (importResult != null && importResult.Result != null && !importResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(importResult.Message);
                        result.Result = importResult.Result;
                        result.Message = importResult.Message;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, importResult?.Message ?? "WEB4 GeoNFT import failed.");
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error importing WEB4 OASIS GeoNFT: {ex.Message}", ex);
                }

                return result;
            }

            try
            {
                string filePath = CLIEngine.GetValidFile("Please enter the full path to the WEB4 OASIS GeoNFT file you wish to import: ");

                OASISResult<IWeb4GeoSpatialNFT> importResult = await NFTCommon.NFTManager.ImportWeb4GeoNFTAsync(STAR.BeamedInAvatar.Id, filePath);

                if (importResult != null && importResult.Result != null && !importResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(importResult.Message);
                    result.Result = importResult.Result;
                    result.Message = importResult.Message;
                }
                else
                {
                    string msg = importResult != null ? importResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Failed to import WEB4 OASIS GeoNFT: {msg}");
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error importing WEB4 OASIS GeoNFT: {ex.Message}";
                CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS GeoNFT: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportGeoNFTNonInteractiveAsync(string idOrName, string destinationFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            OASISResult<IWeb4GeoSpatialNFT> geoResult = await FindWeb4GeoNFTAsync("export", idOrName, providerType: providerType);
            if (geoResult == null || geoResult.Result == null || geoResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 Geo-NFT in ExportGeoNFTNonInteractiveAsync. Reason: {geoResult?.Message}");
                return result;
            }

            OASISResult<IWeb4GeoSpatialNFT> exportResult = await NFTCommon.NFTManager.ExportWeb4GeoNFTAsync(geoResult.Result.Id, destinationFilePath);
            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                CLIEngine.ShowSuccessMessage(exportResult.Message);
                result.Result = exportResult.Result;
                result.Message = exportResult.Message;
            }
            else
                OASISErrorHandling.HandleError(ref result, exportResult?.Message ?? "Export failed.");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            try
            {
                OASISResult<IWeb4GeoSpatialNFT> geoNFTResult = await FindWeb4GeoNFTAsync("export");
                
                if (geoNFTResult == null || geoNFTResult.Result == null || geoNFTResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 Geo-NFT in ExportGeoNFTAsync method. Reason: {geoNFTResult.Message}");
                    return result;
                }

                string filePath = CLIEngine.GetValidFile("Please enter the full path to where you would like to export the WEB4 OASIS GeoNFT file: ");
                OASISResult<IWeb4GeoSpatialNFT> exportResult = await NFTCommon.NFTManager.ExportWeb4GeoNFTAsync(geoNFTResult.Result.Id, filePath);

                if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(exportResult.Message);
                    result.Result = exportResult.Result;
                    result.Message = exportResult.Message;
                }
                else
                {
                    string msg = exportResult != null ? exportResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Failed to export WEB4 OASIS GeoNFT: {msg}");
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error exporting WEB4 OASIS GeoNFT: {ex.Message}";
                CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS GeoNFT: {ex.Message}");
            }

            return result;
        }
    }
}
