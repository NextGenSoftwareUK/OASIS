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

        public async Task<OASISResult<IWeb4NFT>> ConvertNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            OASISErrorHandling.HandleError(ref result, "WEB4 NFT convert is not available: the STAR CLI has no wired ONODE/NFTManager convert API yet (use remint, wrap/create, or interactive flows where applicable).");
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<IWeb4NFT>> UpdateWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            UpdateWeb4NFTRequest request = new UpdateWeb4NFTRequest();

            OASISResult<IWeb4NFT> nftResult = await FindWeb4NFTAsync("update", idOrName, providerType: providerType);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                OASISResult<IUpdateWeb4NFTRequest> updateResult = await NFTCommon.UpdateWeb4NFTAsync(request, nftResult.Result, "WEB4 NFT");

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4NFTRequest)updateResult.Result;

                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage("Saving WEB4 NFT...");
                    result = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Updated.");

                        if (result != null && result.Result != null && !result.IsError && result.Result.ParentWeb5NFTIds != null && result.Result.ParentWeb5NFTIds.Count > 0)
                        {
                            foreach (Guid id in result.Result.ParentWeb5NFTIds)
                            {
                                result = await NFTCommon.UpdateSTARNETHolonAsync(id, "WEB4 NFT", STARNETManager, result, providerType);

                                var starNFTResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, id, providerType: providerType);

                                if (starNFTResult != null && starNFTResult.Result != null && !starNFTResult.IsError)
                                    UpdateWeb4AndWeb3NFTJSONFiles(result.Result, starNFTResult.Result.STARNETDNA.SourcePath);
                            }
                        }
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 NFT Collection in UpdateWeb4NFTCollectionAsync method. Reason: {msg}");
                    }
                }
                else
                {
                    string msg = updateResult != null ? updateResult.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 NFT to update: {msg}");
                }
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> DeleteWeb4NFTAsync(string idOrName, ProviderType providerType = ProviderType.Default)
        {
            return await DeleteWeb4NFTAsync(idOrName, null, null, null, providerType);
        }

        public async Task<OASISResult<IWeb4NFT>> DeleteWeb4NFTAsync(string idOrName, bool? softDelete, bool? deleteChildWeb3NFTs, bool? burnChildWeb3NFTs, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> nft = await FindWeb4NFTAsync("delete", idOrName, true);

            if (nft == null || nft.Result == null || nft.IsError)
            {
                OASISErrorHandling.HandleError(ref nft, $"Error occured finding WEB4 NFT to delete. Reason: {nft.Message}");
                return nft;
            }

            if (!softDelete.HasValue)
                softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web4 NFT? (defaults to false)");
            
            if (!deleteChildWeb3NFTs.HasValue)
                deleteChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web3 NFTs? (the OASIS holon/metadata)(recommeneded/default)", addLineBefore: true);
            
            if (!burnChildWeb3NFTs.HasValue)
                burnChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also burn the child Web3 NFTs? (permanently destroy the Web3 NFTs on-chain) (recommeneded/default)", addLineBefore: true);

            CLIEngine.ShowWorkingMessage("Deleting WEB4 NFT...", addLineBefore: true);
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb4NFTAsync(STAR.BeamedInAvatar.Id, nft.Result.Id, softDelete.Value, deleteChildWeb3NFTs.Value, burnChildWeb3NFTs.Value, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 NFT Successfully Deleted.");

                foreach (Guid id in nft.Result.ParentWeb5NFTIds)
                    nft = await NFTCommon.DeleteAllSTARNETVersionsAsync(id, STARNETManager, nft, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref nft, $"Error occured deleting WEB4 NFT. Reason: {msg}");
            }

            return nft;
        }

        public virtual async Task<OASISResult<IEnumerable<IWeb4NFT>>> ListAllWeb4NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllWeb4NFTsAsync(providerType));
        }

        public virtual OASISResult<IEnumerable<IWeb4NFT>> ListAllWeb4NFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllWeb4NFTs(providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IWeb4NFT>>> ListAllWeb4NFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }
        public virtual OASISResult<IEnumerable<IWeb4NFT>> ListAllWeb4NFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual async Task<OASISResult<IWeb4NFT>> ShowWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");

            result = await FindWeb4NFTAsync("view", idOrName, true, providerType: providerType);
            return result;
        }

        public virtual async Task SearchWeb4NFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 NFT's...");
            ListWeb4NFTs(await NFTCommon.NFTManager.SearchWeb4NFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar: !showForAllAvatars, providerType: providerType));
        }

        // Web3 NFT Methods
        public virtual async Task<OASISResult<IEnumerable<IWeb3NFT>>> ListAllWeb3NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");
            return NFTCommon.ListWeb3NFTs(await NFTCommon.NFTManager.LoadAllWeb3NFTsAsync(providerType: providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IWeb3NFT>>> ListAllWeb3NFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");
            return NFTCommon.ListWeb3NFTs(await NFTCommon.NFTManager.LoadAllWeb3NFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType: providerType));
        }

        public virtual async Task<OASISResult<IWeb3NFT>> ShowWeb3NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");

            result = await NFTCommon.FindWeb3NFTAsync("view", default, idOrName, true, providerType: providerType);

            return result;
        }

        public virtual async Task SearchWeb3NFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB3 NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB3 NFT's...");
            NFTCommon.ListWeb3NFTs(await NFTCommon.NFTManager.SearchWeb3NFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, default, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, providerType: providerType));
        }

        public virtual async Task<OASISResult<IWeb3NFT>> UpdateWeb3NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");

            OASISResult<IWeb3NFT> nftResult = await NFTCommon.FindWeb3NFTAsync("update", default, idOrName, true, providerType: providerType);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                // Note: Web3 NFTs are typically updated through their parent Web4 NFT
                // This is a placeholder for future implementation if direct Web3 NFT updates are needed
                //TODO: Needs implementing properly!
                CLIEngine.ShowWarningMessage("Web3 NFT updates are typically done through their parent Web4 NFT. Use 'nft update {id/name} web4' to update the parent Web4 NFT.");
                result = nftResult;
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No WEB3 NFT Found For That Id or Name!");
            }

            return result;
        }

        //TODO: Finish implementing ASAP! :)
        //public virtual async Task<OASISResult<IWeb3NFT>> UpdateWeb3NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
        //    UpdateWeb3NFTRequest request = new UpdateWeb3NFTRequest();

        //    Console.WriteLine("");
        //    CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");

        //    OASISResult<IWeb3NFT> nftResult = await NFTCommon.FindWeb3NFTAsync("update", default, idOrName, true, providerType: providerType);

        //    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
        //    {
        //        // Let the common helper prepare the update request (prompts etc).
        //        OASISResult<IUpdateWeb3NFTRequest> updateResult = await NFTCommon.UpdateWeb3NFTAsync(request, nftResult.Result, "WEB3 NFT");

        //        if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
        //        {
        //            request = (UpdateWeb3NFTRequest)updateResult.Result;

        //            Console.WriteLine("");
        //            CLIEngine.ShowWorkingMessage("Saving WEB3 NFT...");
        //            // Call manager to perform the update on the Web3 NFT
        //            result = await NFTCommon.NFTManager.UpdateWeb3NFTAsync(request, providerType);

        //            if (result != null && result.Result != null && !result.IsError)
        //            {
        //                CLIEngine.ShowSuccessMessage("WEB3 NFT Successfully Updated.");

        //                // Try to update any STARNET holon mapping if present in the metadata (keeps STARNET in sync).
        //                // This mirrors how WEB4 updates propagate to STARNET holons.
        //                try
        //                {
        //                    result = await NFTCommon.UpdateSTARNETHolonAsync("Web5STARNFTId", "WEB3 NFT", STARNETManager, result.Result.MetaData, result, providerType);
        //                }
        //                catch
        //                {
        //                    // If STARNET sync fails we don't want to lose the successful NFT update, just log the error.
        //                    // The UpdateSTARNETHolonAsync method will set the result error/message if needed; swallow exceptions here.
        //                }
        //            }
        //            else
        //            {
        //                string msg = result != null ? result.Message : "";
        //                OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB3 NFT in UpdateWeb3NFTAsync method. Reason: {msg}");
        //            }
        //        }
        //        else
        //        {
        //            string msg = updateResult != null ? updateResult.Message : "";
        //            OASISErrorHandling.HandleError(ref result, $"Error Occured preparing WEB3 NFT update: {msg}");
        //        }
        //    }
        //    else
        //    {
        //        string msg = nftResult != null ? nftResult.Message : "";
        //        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB3 NFT to update: {msg}");
        //    }

        //    return result;
        //}

        public virtual async Task<OASISResult<bool>> DeleteWeb3NFTAsync(string idOrName, ProviderType providerType = ProviderType.Default)
        {
            return await DeleteWeb3NFTAsync(idOrName, null, null, providerType);
        }

        public virtual async Task<OASISResult<bool>> DeleteWeb3NFTAsync(string idOrName, bool? softDelete = true, bool? burnWeb3NFT = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB3 NFT's...");

            OASISResult<IWeb3NFT> nft = await NFTCommon.FindWeb3NFTAsync("delete", default, idOrName, true, providerType: providerType);

            if (nft != null && nft.Result != null && !nft.IsError)
            {
                if (!softDelete.HasValue)
                    softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web3 NFT? (defaults to false)");

                if (!burnWeb3NFT.HasValue)
                    burnWeb3NFT = CLIEngine.GetConfirmation("Do you wish to also burn the Web3 NFT? (permanently destroy the Web3 NFT on-chain) (recommeneded/default)", addLineBefore: true);

                CLIEngine.ShowWorkingMessage("Deleting WEB3 NFT...", addLineBefore: true);
                OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb3NFTAsync(STAR.BeamedInAvatar.Id, nft.Result.Id, softDelete.Value, burnWeb3NFT.Value, providerType: providerType);

                if (deleteResult != null && !deleteResult.IsError && deleteResult.Result)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = deleteResult.Message;
                    CLIEngine.ShowSuccessMessage(deleteResult.Message);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Deleting WEB3 NFT. Reason: {deleteResult?.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No WEB3 NFT Found For That Id or Name!");

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> FindWeb4NFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string UIName = "WEB4 NFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a WEB4 NFT id or name for '{operationName}'. Example: nft remint <guid> | nft export <id> <path>.");

                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4NFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4NFTsAsync(providerType);

                        ListWeb4NFTs(starHolonsResult);

                        if (!(starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0))
                            cont = false;
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {UIName} you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {UIName}...");
                    result = await NFTCommon.NFTManager.LoadWeb4NftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4NFT>> searchResults = await NFTCommon.NFTManager.SearchWeb4NFTsAsync(idOrName, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar: showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    $"Multiple WEB4 NFT matches for '{idOrName}'. Use a GUID in non-interactive mode.");

                            ListWeb4NFTs(searchResults, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the {UIName} you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                                } while (result.Result == null || result.IsError);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {UIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null)
                    ShowWeb4NFT(result.Result);

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {UIName}?"))
                    {

                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {UIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }

                idOrName = "";
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }
            else if (idOrName == "nonefound")
            {
                result.IsError = true;
                result.Message = "None Found";
            }

            return result;
        }

        private OASISResult<IEnumerable<IWeb4NFT>> ListWeb4NFTs(OASISResult<IEnumerable<IWeb4NFT>> nfts, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (nfts != null)
            {
                if (!nfts.IsError)
                {
                    if (nfts.Result != null && nfts.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (nfts.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 NFT Found:");
                        else
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 NFT's Found:");

                        for (int i = 0; i < nfts.Result.Count(); i++)
                            ShowWeb4NFT(nfts.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 NFT's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 NFT's. Reason: {nfts.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 NFT's.");

            return nfts;
        }

        private void ShowWeb4NFT(IWeb4NFT web4NFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            NFTCommon.ShowNFTDetails(web4NFT, null, displayFieldLength);

            if (web4NFT.Web3NFTs.Count > 0)
            {
                //Console.WriteLine("");
                DisplayProperty($"WEB3 NFT's ({web4NFT.Web3NFTs.Count})", "", displayFieldLength);
                Console.WriteLine("");

                foreach (Web3NFT web3NFT in web4NFT.Web3NFTs)
                {
                    NFTCommon.ShowNFTDetails(web3NFT, web4NFT, displayFieldLength);
                    DisplayProperty("Send NFT Transaction Hash", web3NFT.SendNFTTransactionHash, displayFieldLength);
                    DisplayProperty("OASIS MintWallet Address", web3NFT.OASISMintWalletAddress, displayFieldLength);
                    DisplayProperty("Mint Transaction Hash", web3NFT.MintTransactionHash, displayFieldLength);
                    DisplayProperty("NFT Token Address", web3NFT.NFTTokenAddress, displayFieldLength);
                    DisplayProperty("Update Authority", web3NFT.UpdateAuthority, displayFieldLength);
                    CLIEngine.ShowDivider();
                }

                CLIEngine.ShowMessage("NOTE: Only the deltas are shown between the WEB3 NFT and it's parent WEB4 NFT so if a field/property is not shown above for the WEB3 NFT then that means it defaults to it's parent WEB4 NFT.");
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private OASISResult<bool> UpdateWeb4AndWeb3NFTJSONFiles(IWeb4NFT NFT, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_NFT_{NFT.Id}.json"), JsonConvert.SerializeObject(NFT));

                if (!string.IsNullOrEmpty(NFT.JSONMetaData))
                    File.WriteAllText(Path.Combine(path, $"WEB4_JSONMetaData_{NFT.Id}.json"), NFT.JSONMetaData);

                foreach (IWeb3NFT web3Nft in NFT.Web3NFTs)
                {
                    File.WriteAllText(Path.Combine(path, $"WEB3_NFT_{web3Nft.Id}.json"), JsonConvert.SerializeObject(web3Nft));

                    if (!string.IsNullOrEmpty(web3Nft.JSONMetaData))
                        File.WriteAllText(Path.Combine(path, $"WEB3_JSONMetaData_{web3Nft.Id}.json"), web3Nft.JSONMetaData);
                }
            }
            catch (Exception e)
            { 
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 and WEB3 NFT JSON files. Reason: {e.Message}");
            }

            result.Result = true;
            return result;
        }
    }
}
