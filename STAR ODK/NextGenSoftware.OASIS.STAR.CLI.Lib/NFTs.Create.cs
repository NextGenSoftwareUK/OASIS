using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {

        public override async Task<OASISResult<STARNFT>> CreateAsync(ISTARNETCreateOptions<STARNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            if (createOptions?.CustomCreateParams != null
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.Scripted, out object scriptedFlag)
                && scriptedFlag is bool sbf && sbf
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.WrapWeb4NFTId, out object widObj)
                && widObj != null)
                return await CreateAsyncScriptedWrapFromWeb4Async(widObj.ToString(), holonSubType, showHeaderAndInro, addDependencies, providerType);

            OASISResult<STARNFT> result = new OASISResult<STARNFT>();
            OASISResult<IWeb4NFT> NFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS NFT you wish to create a WEB5 NFT from?"))
            {
                Console.WriteLine("");
                NFTResult = await FindWeb4NFTAsync("wrap");
            }
            else
            {
                Console.WriteLine("");
                NFTResult = await MintNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (NFTResult != null && NFTResult.Result != null && !NFTResult.IsError)
            {
                IWeb4NFT NFT = NFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT to WEB5 STARNET which will create a WEB5 STAR NFT that wraps around the WEB4 OASISNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASISNFT json file in the WEB5 STAR NFT folder.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "WEB4 NFT", NFT } }
                        },
                        STARNETHolon = new STARNFT()
                        {
                            OASISNFTId = NFTResult.Result.Id
                        }
                    }, holonSubType, showHeaderAndInro, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        UpdateWeb4AndWeb3NFTJSONFiles(NFTResult.Result, result.Result.STARNETDNA.SourcePath);

                        if (!result.Result.ChildrenIds.Contains(NFT.Id))
                            result.Result.ChildrenIds.Add(NFT.Id);
                        else
                            OASISErrorHandling.HandleError(ref result, "Error occured adding child WEB4 NFT id to the parent WEB5 NFT as it already exists in the list.");

                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARNFT> saveResult = await result.Result.SaveAsync<STARNFT>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            if (!NFT.ParentWeb5NFTIds.Contains(saveResult.Result.Id))
                            {
                                NFT.ParentWeb5NFTIds.Add(saveResult.Result.Id);
                                OASISResult<IWeb4NFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(new UpdateWeb4NFTRequest() { Id = NFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = NFT.MetaData }, providerType: providerType);

                                if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 NFT after creation of WEB5 STAR NFT in CreateAsync method. Reason: {web4NFT.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, "Error occured adding WEB5 NFT ID link to the child/wrapped WEB4 NFT as it already exists in the list.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
            {
                if (!mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading NFT in LoadNftAsync method. Reason: {NFTResult.Message}");
            }

            return result;
        }

        private async Task<OASISResult<STARNFT>> CreateAsyncScriptedWrapFromWeb4Async(string web4IdOrName, object holonSubType, bool showHeaderAndInro, bool addDependencies, ProviderType providerType)
        {
            OASISResult<STARNFT> result = new OASISResult<STARNFT>();
            OASISResult<IWeb4NFT> NFTResult = await FindWeb4NFTAsync("wrap", web4IdOrName);

            if (NFTResult == null || NFTResult.Result == null || NFTResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading WEB4 NFT for wrap. Reason: {NFTResult?.Message}");
                return result;
            }

            IWeb4NFT NFT = NFTResult.Result;

            result = await base.CreateAsync(new STARNETCreateOptions<STARNFT, STARNETDNA>()
            {
                STARNETDNA = new STARNETDNA()
                {
                    MetaData = new Dictionary<string, object>() { { "WEB4 NFT", NFT } }
                },
                STARNETHolon = new STARNFT()
                {
                    OASISNFTId = NFTResult.Result.Id
                }
            }, holonSubType, showHeaderAndInro, addDependencies, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                UpdateWeb4AndWeb3NFTJSONFiles(NFTResult.Result, result.Result.STARNETDNA.SourcePath);

                if (!result.Result.ChildrenIds.Contains(NFT.Id))
                    result.Result.ChildrenIds.Add(NFT.Id);
                else
                    OASISErrorHandling.HandleError(ref result, "Error occured adding child WEB4 NFT id to the parent WEB5 NFT as it already exists in the list.");

                result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                OASISResult<STARNFT> saveResult = await result.Result.SaveAsync<STARNFT>();

                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                {
                    if (!NFT.ParentWeb5NFTIds.Contains(saveResult.Result.Id))
                    {
                        NFT.ParentWeb5NFTIds.Add(saveResult.Result.Id);
                        OASISResult<IWeb4NFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(new UpdateWeb4NFTRequest() { Id = NFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = NFT.MetaData }, providerType: providerType);

                        if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 NFT after creation of WEB5 STAR NFT in CreateAsync method. Reason: {web4NFT.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured adding WEB5 NFT ID link to the child/wrapped WEB4 NFT as it already exists in the list.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
            }

            return result;
        }

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("WEB4 NFT") && starHolon.STARNETDNA.MetaData["WEB4 NFT"] != null)
            {
                IWeb4NFT nft = starHolon.STARNETDNA.MetaData["WEB4 NFT"] as IWeb4NFT;

                if (nft == null)
                    nft = JsonConvert.DeserializeObject<Web4NFT>(starHolon.STARNETDNA.MetaData["WEB4 NFT"].ToString());

                if (nft != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 NFT DETAILS", "", displayFieldLength, false);
                    ShowWeb4NFT(nft, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        //Delete WEB5 NFT. Also offers to delete the linked WEB4 NFT and it's child WEB3 NFT's.
        public override async Task<OASISResult<STARNFT>> DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFT> result = await base.DeleteAsync(idOrName, softDelete, providerType);

            if (result != null && result.Result != null && !result.IsError && result.IsDeleted)
            {
                if (result.Result.STARNETDNA != null && result.Result.STARNETDNA.MetaData != null && result.Result.STARNETDNA.MetaData.ContainsKey("WEB4 NFT") && result.Result.STARNETDNA.MetaData["WEB4 NFT"] != null)
                {
                    IWeb4NFT nft = result.Result.STARNETDNA.MetaData["WEB4 NFT"] as IWeb4NFT;

                    if (nft == null)
                        nft = JsonConvert.DeserializeObject<Web4NFT>(result.Result.STARNETDNA.MetaData["WEB4 NFT"].ToString());

                    if (nft != null)
                    {
                        nft.ParentWeb5NFTIds.Remove(result.Result.Id);
                        OASISResult<IWeb4NFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(new UpdateWeb4NFTRequest() { Id = nft.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = nft.MetaData }, providerType: providerType);

                        if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB5 NFT ID link from the metadata on it's child/wrapped WEB4 NFT {nft.Id} and title {nft.Title}. Reason: {web4NFT.Message}");
                        else
                            CLIEngine.ShowSuccessMessage("WEB4 Link To WEB5 Removed.");

                        if (CLIEngine.GetConfirmation($"Do you wish to also delete the child WEB4 NFT ({nft.Title}) and optionally it's child WEB3 NFT's?"))
                            await DeleteWeb4NFTAsync(nft.Id.ToString());
                        else
                            Console.WriteLine("");
                    }
                }
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> MintNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();

            if (mintParams is string jsonPath && !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    MintWeb4NFTRequest request = JsonConvert.DeserializeObject<MintWeb4NFTRequest>(json);
                    if (request == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Mint request JSON deserialized to null. Expected MintWeb4NFTRequest / IMintWeb4NFTRequest shape.");
                        return result;
                    }

                    request.MintedByAvatarId = STAR.BeamedInAvatar.Id;
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT & WEB3 NFT's (from JSON)...");
                    result = await STAR.OASISAPI.NFTs.MintNftAsync(request);
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint from JSON file '{jsonPath}'. {ex.Message}", ex);
                }

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage(result.Message);
                else if (result != null && result.IsError)
                    CLIEngine.ShowErrorMessage($"Error Occured: {result.Message}");
                return result;
            }

            IMintWeb4NFTRequest requestInteractive = await NFTCommon.GenerateNFTRequestAsync();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT & WEB3 NFT's...");
            result = await STAR.OASISAPI.NFTs.MintNftAsync(requestInteractive);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage(result.Message);
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
           
            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> RemintNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured reminting WEB4 OASIS NFT in RemintNFTAsync method. Reason: ";
            string idOrName = mintParams != null ? mintParams.ToString() : "";
            result = await FindWeb4NFTAsync("remint", idOrName, showOnlyForCurrentAvatar: true);

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                {
                    IRemintWeb4NFTRequest remintRequest = await NFTCommon.GenerateWeb4NFTRemintRequestAsync(result.Result);

                    CLIEngine.ShowWorkingMessage("Reminting WEB4 OASIS NFT & WEB3 NFT's...");
                    result = await STAR.OASISAPI.NFTs.RemintNftAsync(remintRequest);

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
                                    UpdateWeb4AndWeb3NFTJSONFiles(result.Result, web5NFT.Result.STARNETDNA.SourcePath);
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

        public async Task SendNFTAsync()
        {
            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT from?");
            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT to?");
            string tokenAddress = CLIEngine.GetValidInput("What is the token address of the NFT?");
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            await SendNFTAsync(fromWalletAddress, toWalletAddress, tokenAddress, memoText);
        }

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(string fromWalletAddress, string toWalletAddress, string tokenAddress, string memoText)
        {
            CLIEngine.ShowWorkingMessage("Sending NFT...");

            OASISResult<ISendWeb4NFTResponse> response = await STAR.OASISAPI.NFTs.SendNFTAsync(STAR.BeamedInAvatar.Id, new SendWeb4NFTRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                TokenAddress = tokenAddress,
                MemoText = memoText ?? "",
                Amount = 1
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

        public async Task<OASISResult<IWeb4NFT>> BurnNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
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

    }
}
