using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.Utilities;
using Newtonsoft.Json;
using Solnet.Rpc.Models;
using System.IO;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class NFTCommon
    {
        public OASISResult<IEnumerable<IWeb3NFT>> ListWeb3NFTs(OASISResult<IEnumerable<IWeb3NFT>> nfts, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (nfts != null)
            {
                if (!nfts.IsError)
                {
                    if (nfts.Result != null && nfts.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (nfts.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB3 NFT Found:");
                        else
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB3 NFT's Found:");

                        for (int i = 0; i < nfts.Result.Count(); i++)
                            ShowWeb3NFT(nfts.Result.ElementAt(i), i == 0, false, showNumbers, i + 1, showDetailedInfo);
                            //ShowWeb3NFT(nfts.Result.ElementAt(i), i == 0, i == nfts.Result.Count() - 1, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB3 NFT's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB3 NFT's. Reason: {nfts.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB3 NFT's.");

            return nfts;
        }

        public void ShowWeb3NFT(IWeb3NFT web3NFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            //if (DisplayFieldLength > displayFieldLength)
            //    displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            ShowNFTDetails(web3NFT, null, displayFieldLength);
            DisplayProperty("Send NFT Transaction Hash", web3NFT.SendNFTTransactionHash, displayFieldLength);
            DisplayProperty("OASIS MintWallet Address", web3NFT.OASISMintWalletAddress, displayFieldLength);
            DisplayProperty("Mint Transaction Hash", web3NFT.MintTransactionHash, displayFieldLength);
            DisplayProperty("NFT Token Address", web3NFT.NFTTokenAddress, displayFieldLength);
            DisplayProperty("Update Authority", web3NFT.UpdateAuthority, displayFieldLength);
            
            CLIEngine.ShowDivider();
               
            //if (showFooter)
            //    CLIEngine.ShowDivider();
        }

        public void ShowNFTDetails(INFTBase nft, IWeb4NFT web4NFT, int displayFieldLength, bool displayTags = true, bool displayMetaData = true)
        {
            DisplayProperty("NFT Id", nft.Id.ToString(), displayFieldLength);

            if (web4NFT != null && web4NFT.ParentWeb5NFTIds != null && web4NFT.ParentWeb5NFTIds.Count > 0)
            {
                foreach (Guid id in web4NFT.ParentWeb5NFTIds)
                    DisplayProperty("Parent Web5 Id:", id.ToString(), displayFieldLength);
            }

            if ((web4NFT != null && nft.Title != web4NFT.Title) || web4NFT == null)
                DisplayProperty("Title", nft.Title, displayFieldLength);

            if ((web4NFT != null && nft.Description != web4NFT.Description) || web4NFT == null)
                DisplayProperty("Description", nft.Description, displayFieldLength);

            if ((web4NFT != null && nft.Price != web4NFT.Price) || web4NFT == null)
                DisplayProperty("Price", nft.Price.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.Discount != web4NFT.Discount) || web4NFT == null)
                DisplayProperty("Discount", nft.Discount.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.RoyaltyPercentage != web4NFT.RoyaltyPercentage) || web4NFT == null)
                DisplayProperty("Royalty Percentage", nft.RoyaltyPercentage.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.IsForSale != web4NFT.IsForSale) || web4NFT == null)
                DisplayProperty("For Sale", nft.IsForSale ? string.Concat("Yes (StartDate: ", nft.SaleStartDate.HasValue ? nft.SaleStartDate.Value.ToShortDateString() : "Not Set", nft.SaleEndDate.HasValue ? nft.SaleEndDate.Value.ToShortDateString() : "Not Set") : "No", displayFieldLength);

            if ((web4NFT != null && nft.MintedByAvatarId != web4NFT.MintedByAvatarId) || web4NFT == null)
                DisplayProperty("Minted By Avatar Id", nft.MintedByAvatarId.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.MintedOn != web4NFT.MintedOn) || web4NFT == null)
                DisplayProperty("Minted On", nft.MintedOn.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.OnChainProvider.Name != web4NFT.OnChainProvider.Name) || web4NFT == null)
                DisplayProperty("OnChain Provider", nft.OnChainProvider.Name, displayFieldLength);

            if ((web4NFT != null && nft.OffChainProvider.Name != web4NFT.OffChainProvider.Name) || web4NFT == null)
                DisplayProperty("OffChain Provider", nft.OffChainProvider.Name, displayFieldLength);

            if ((web4NFT != null && nft.StoreNFTMetaDataOnChain != web4NFT.StoreNFTMetaDataOnChain) || web4NFT == null)
                DisplayProperty("Store NFT Meta Data OnChain", nft.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.NFTOffChainMetaType.Name != web4NFT.NFTOffChainMetaType.Name) || web4NFT == null)
                DisplayProperty("NFT OffChain Meta Type", nft.NFTOffChainMetaType.Name, displayFieldLength);

            if ((web4NFT != null && nft.NFTStandardType.Name != web4NFT.NFTStandardType.Name) || web4NFT == null)
                DisplayProperty("NFT Standard Type", nft.NFTStandardType.Name, displayFieldLength);

            if ((web4NFT != null && nft.Symbol != web4NFT.Symbol) || web4NFT == null)
                DisplayProperty("Symbol", nft.Symbol, displayFieldLength);

            if ((web4NFT != null && nft.Image != web4NFT.Image) || web4NFT == null)
                DisplayProperty("Image", nft.Image != null ? "Yes" : "None", displayFieldLength);

            if ((web4NFT != null && nft.ImageUrl != web4NFT.ImageUrl) || web4NFT == null)
                DisplayProperty("Image Url", nft.ImageUrl, displayFieldLength);

            if ((web4NFT != null && nft.Thumbnail != web4NFT.Thumbnail) || web4NFT == null)
                DisplayProperty("Thumbnail", nft.Thumbnail != null ? "Yes" : "None", displayFieldLength);

            if ((web4NFT != null && nft.ThumbnailUrl != web4NFT.ThumbnailUrl) || web4NFT == null)
                DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None", displayFieldLength);

            if ((web4NFT != null && nft.JSONMetaDataURL != web4NFT.JSONMetaDataURL) || web4NFT == null)
                DisplayProperty("JSON MetaData URL", nft.JSONMetaDataURL, displayFieldLength);

            if ((web4NFT != null && nft.JSONMetaDataURLHolonId != web4NFT.JSONMetaDataURLHolonId) || web4NFT == null)
                DisplayProperty("JSON MetaData URL Holon Id", nft.JSONMetaDataURLHolonId != Guid.Empty ? nft.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);

            if ((web4NFT != null && nft.SellerFeeBasisPoints != web4NFT.SellerFeeBasisPoints) || web4NFT == null)
                DisplayProperty("Seller Fee Basis Points", nft.SellerFeeBasisPoints.ToString(), displayFieldLength);

            if ((web4NFT != null && nft.SendToAddressAfterMinting != web4NFT.SendToAddressAfterMinting) || web4NFT == null)
                DisplayProperty("Send To Address After Minting", nft.SendToAddressAfterMinting, displayFieldLength);

            if ((web4NFT != null && nft.SendToAvatarAfterMintingId != web4NFT.SendToAvatarAfterMintingId) || web4NFT == null)
                DisplayProperty("Send To Avatar After Minting Id", nft.SendToAvatarAfterMintingId != Guid.Empty ? nft.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);

            if ((web4NFT != null && nft.SendToAvatarAfterMintingUsername != web4NFT.SendToAvatarAfterMintingUsername) || web4NFT == null)
                DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername) ? nft.SendToAvatarAfterMintingUsername : "None", displayFieldLength);

            if ((web4NFT != null && displayTags && TagHelper.GetTags(nft.Tags) != TagHelper.GetTags(web4NFT.Tags)) || web4NFT == null)
                TagHelper.ShowTags(nft.Tags, displayFieldLength);

            if ((web4NFT != null && displayMetaData && MetaDataHelper.GetMetaData(nft.MetaData) != MetaDataHelper.GetMetaData(web4NFT.MetaData)) || web4NFT == null)
            {
                MetaDataHelper.ShowMetaData(nft.MetaData, displayFieldLength);
                Console.WriteLine("");
            }

            //CLIEngine.ShowDivider();
        }

        public SalesInfo UpdateSalesInfo(SalesInfo salesInfo, bool edit = true)
        {
            salesInfo.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.", addLineBefore: false);

            if (salesInfo.IsForSale.HasValue && salesInfo.IsForSale.Value)
            {
                string existingSaleStartDate = salesInfo.SaleStartDate.HasValue ? salesInfo.SaleStartDate.Value == DateTime.MinValue ? "None" : salesInfo.SaleStartDate.Value.ToShortDateString() : "None";

                if (!edit || (edit && CLIEngine.GetConfirmation($"Do you wish to edit the Sale Start Date? (currently is: {existingSaleStartDate})", addLineBefore: true)))
                    salesInfo.SaleStartDate = CLIEngine.GetValidInputForDate("Please enter the Sale Start Date or 'none' to clear:", addLineBefore: true);
                else
                    Console.WriteLine("");

                if (salesInfo.SaleStartDate.HasValue)
                {
                    string existingSaleEndDate = salesInfo.SaleEndDate.HasValue ? salesInfo.SaleEndDate.Value == DateTime.MinValue ? "None" : salesInfo.SaleEndDate.Value.ToShortDateString() : "None";

                    if (!edit || (edit && CLIEngine.GetConfirmation($"Do you wish to edit Sale End Date? (currently is: {existingSaleEndDate})")))
                    {
                        do
                        {
                            salesInfo.SaleEndDate = CLIEngine.GetValidInputForDate("Please enter the Sale End Date or 'none' to clear:", addLineBefore: true);

                            if (salesInfo.SaleEndDate.HasValue && salesInfo.SaleEndDate.Value <= salesInfo.SaleStartDate.Value)
                                CLIEngine.ShowWarningMessage("The end date must be after the start date!");
                        }
                        while (salesInfo.SaleEndDate.HasValue && salesInfo.SaleEndDate.Value <= salesInfo.SaleStartDate.Value);
                    }
                    else
                        Console.WriteLine("");
                }
                else
                    salesInfo.SaleEndDate = null;
            }
            else
                Console.WriteLine("");

            return salesInfo;
        }

        public OASISResult<IUpdateWeb4NFTCollectionRequestBase> UpdateWeb4NFTCollection(IUpdateWeb4NFTCollectionRequestBase request, IWeb4NFTCollectionBase collection, string displayName, bool updateTags = true, bool updateMetaData = true)
        {
            OASISResult<IUpdateWeb4NFTCollectionRequestBase> result = new OASISResult<IUpdateWeb4NFTCollectionRequestBase>();

            request.Id = collection.Id;
            request.ModifiedBy = STAR.BeamedInAvatar.Id;

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {collection.Name})"))
                request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Description? (currently is: {collection.Description})"))
                request.Description = CLIEngine.GetValidInput("Please enter the new description: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
            {
                Console.WriteLine("");
                OASISResult<ImageAndThumbnail> imageAndThumbnailResult = ProcessImageAndThumbnail(displayName);

                if (imageAndThumbnailResult != null && imageAndThumbnailResult.Result != null && !imageAndThumbnailResult.IsError)
                {
                    request.Image = imageAndThumbnailResult.Result.Image;
                    request.ImageUrl = imageAndThumbnailResult.Result.ImageUrl;
                    request.Thumbnail = imageAndThumbnailResult.Result.Thumbnail;
                    request.ThumbnailUrl = imageAndThumbnailResult.Result.ThumbnailUrl;
                }
                else
                {
                    string msg = imageAndThumbnailResult != null ? imageAndThumbnailResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Processing Image and Thumbnail: {msg}");
                    return result;
                }
            }
            else
                Console.WriteLine("");

            if (updateTags)
                request.Tags = TagHelper.ManageTags(collection.Tags);

            if (updateMetaData)
                request.MetaData = MetaDataHelper.ManageMetaData(collection.MetaData, displayName);

            result.Result = request;
            return result;
        }

        //public async Task<OASISResult<T5>> UpdateSTARNETHolonAsync<T1, T2, T3, T4, T5>(string web5IdMetaDataKey, string starnetDNAKeyForWeb4Object, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, Dictionary<string, string> metaData, OASISResult<T5> result, ProviderType providerType = ProviderType.Default) 
        public async Task<OASISResult<T5>> UpdateSTARNETHolonAsync<T1, T2, T3, T4, T5>(Guid web5NFTId, string starnetDNAKeyForWeb4Object, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, OASISResult<T5> result, ProviderType providerType = ProviderType.Default)
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Updating WEB5 STAR {STARNETManager.STARNETHolonUIName} with updated WEB4 OASIS {STARNETManager.STARNETHolonUIName} data...");
            OASISResult<T1> starNFTCollection = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, web5NFTId, providerType: providerType);

            //TODO: DO WE WANT TO UPDATE ALL VERSIONS LIKE WE DO FOR DELETE BELOW?
            if (starNFTCollection != null && starNFTCollection.Result != null && !starNFTCollection.IsError)
            {
                starNFTCollection.Result.STARNETDNA.MetaData[starnetDNAKeyForWeb4Object] = result.Result;
                starNFTCollection = await STARNETManager.UpdateAsync(STAR.BeamedInAvatar.Id, starNFTCollection.Result, updateDNAJSONFile: true, providerType: providerType);

                if (starNFTCollection != null && starNFTCollection.Result != null && !starNFTCollection.IsError)
                    CLIEngine.ShowSuccessMessage($"WEB5 STAR {STARNETManager.STARNETHolonUIName} Successfully Updated.");
                else
                {
                    string msg = starNFTCollection != null ? starNFTCollection.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB5 STAR {STARNETManager.STARNETHolonUIName} after updating WEB4 OASIS {STARNETManager.STARNETHolonUIName}. Reason: {msg}");
                }
            }
            else
            {
                string msg = starNFTCollection != null ? starNFTCollection.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Loading WEB5 STAR {STARNETManager.STARNETHolonUIName}. Reason: {msg}");
            }
            
            return result;
        }

        public async Task<OASISResult<T5>> DeleteAllSTARNETVersionsAsync<T1, T2, T3, T4, T5>(Guid web5NFTId, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, OASISResult<T5> result, ProviderType providerType = ProviderType.Default)
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Deleting All WEB5 STAR {STARNETManager.STARNETHolonUIName} Versions...");

            OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(web5NFTId, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 version in versionsResult.Result)
                {
                    OASISResult<T1> deleteResult = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, web5NFTId, version.STARNETDNA.VersionSequence, providerType: providerType);

                    if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                        CLIEngine.ShowSuccessMessage($"Successfully Deleted Version {version.STARNETDNA.Version}.");
                    else
                    {
                        string msg = versionsResult != null ? versionsResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Deleting WEB5 STAR {STARNETManager.STARNETHolonUIName} Version {version.STARNETDNA.Version}. Reason: {msg}");
                    }
                }
            }
            else
            {
                string msg = versionsResult != null ? versionsResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Loading WEB5 STAR {STARNETManager.STARNETHolonUIName} versions. Reason: {msg}");
            }   
    
            return result;
        }

        private void DisplayProperty(string heading, string value, int displayFieldLength, bool displayColon = true)
        {
            CLIEngine.DisplayProperty(heading, value, displayFieldLength, displayColon);
        }

        /// <summary>
        /// Writes WEB4 and WEB3 NFT JSON files to the given path (used by NFTCollections and GeoNFTCollections).
        /// </summary>
        public static OASISResult<bool> UpdateWeb4AndWeb3NFTJSONFiles(IWeb4NFT NFT, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_NFT_{NFT.Id}.json"), JsonConvert.SerializeObject(NFT));
                if (!string.IsNullOrEmpty(NFT.JSONMetaData))
                    File.WriteAllText(Path.Combine(path, $"WEB4_JSONMetaData_{NFT.Id}.json"), NFT.JSONMetaData);
                if (NFT.Web3NFTs != null)
                {
                    foreach (IWeb3NFT web3Nft in NFT.Web3NFTs)
                    {
                        File.WriteAllText(Path.Combine(path, $"WEB3_NFT_{web3Nft.Id}.json"), JsonConvert.SerializeObject(web3Nft));
                        if (!string.IsNullOrEmpty(web3Nft.JSONMetaData))
                            File.WriteAllText(Path.Combine(path, $"WEB3_JSONMetaData_{web3Nft.Id}.json"), web3Nft.JSONMetaData);
                    }
                }
                result.Result = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 and WEB3 NFT JSON files. Reason: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// Writes WEB4 NFT Collection JSON to the given path (used by NFTCollections).
        /// </summary>
        public static OASISResult<bool> UpdateWeb4AndWeb3NFTJSONFiles(IWeb4NFTCollection collection, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_NFT_Collection_{collection.Id}.json"), JsonConvert.SerializeObject(collection));
                result.Result = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 NFT Collection JSON files. Reason: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// Writes WEB4 Geo NFT Collection JSON to the given path (used by GeoNFTCollections).
        /// </summary>
        public static OASISResult<bool> UpdateWeb4AndWeb3NFTJSONFiles(IWeb4GeoNFTCollection collection, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_GeoNFT_Collection_{collection.Id}.json"), JsonConvert.SerializeObject(collection));
                result.Result = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 Geo NFT Collection JSON files. Reason: {e.Message}");
            }
            return result;
        }
    }
}
