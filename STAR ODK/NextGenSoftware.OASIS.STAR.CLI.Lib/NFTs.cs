using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, NFTDNA>
    public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public NFTs(Guid avatarId, STARDNA STARDNA) : base(new STARNFTManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR NFT Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR NFT which wraps around a WEB4 OASIS NFT.",
                "You can mint a WEB4 OASIS NFT using the 'nft mint' sub-command.",
                "You then convert or wrap around the WEB4 OASIS NFT using the sub-command 'nft create' which will create a WEB5 STAR NFT compatible with STARNET.",
                "A WEB5 NFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "A WEB5 GeoNFT can be created from a WEB4 GeoNFT (which in turn is created from a WEB4 NFT) and can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR NFT's can be published to STARNET, version controlled, shared, etc whereas WEB4 NFT's cannot.",
                "The wizard will create an empty folder with a NFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the NFT into this folder.",
                "Finally you run the sub-command 'nft publish' to convert the folder containing the NFT (can contain any number of files and sub-folders) into a OASIS NFT file (.onft) as well as optionally upload to STARNET.",
                "You can then share the .onft file with others across any platform or OS, who can then install the NFT from the file using the sub-command 'nft install'.",
                "You can also optionally choose to upload the .onft file to the STARNET store so others can search, download and install the NFT."
            },
            STAR.STARDNA.DefaultNFTsSourcePath, "DefaultNFTsSourcePath",
            STAR.STARDNA.DefaultNFTsPublishedPath, "DefaultNFTsPublishedPath",
            STAR.STARDNA.DefaultNFTsDownloadedPath, "DefaultNFTsDownloadedPath",
            STAR.STARDNA.DefaultNFTsInstalledPath, "DefaultNFTsInstalledPath")
        { }

        //public override async Task CreateAsync(object createParams, STARNFT newHolon = null, ProviderType providerType = ProviderType.Default)
        //{
        //    Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the NFT you wish to upload to STARNET: ");
        //    OASISResult<IOASISNFT> NFTResult = await NFTManager.LoadNftAsync(geoNFTId);

        //    if (NFTResult != null && !NFTResult.IsError && NFTResult.Result != null)
        //        await base.CreateAsync(createParams, new STARNFT() { OASISNFTId = geoNFTId }, providerType);
        //    else
        //        CLIEngine.ShowErrorMessage("No NFT Found For That Id!");
        //}

        public override async Task<OASISResult<STARNFT>> CreateAsync(object createParams, STARNFT newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, STARNETDNA STARNETDNA = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFT> result = new OASISResult<STARNFT>();

            //Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the GeoNFT you wish to upload to STARNET: ");
            //OASISResult<IOASISGeoSpatialNFT> geoNFTResult = await NFTManager.LoadGeoNftAsync(geoNFTId);

            OASISResult<IOASISNFT> mintResult = await MintNFTAsync(); //Mint WEB4 NFT

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                result = await base.CreateAsync(createParams, new STARNFT() { OASISNFTId = mintResult.Result.Id }, showHeaderAndInro, checkIfSourcePathExists, metaData: mintResult.Result.MetaData, providerType: providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured minting NFT in MintNFTAsync method. Reason: {mintResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> MintNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            IMintNFTTransactionRequest request = await NFTCommon.GenerateNFTRequestAsync();

            CLIEngine.ShowWorkingMessage("Minting OASIS NFT...");
            OASISResult<INFTTransactionRespone> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                //CLIEngine.ShowSuccessMessage($"OASIS NFT Successfully Minted. {nftResult.Message} Transaction Result: {nftResult.Result.TransactionResult}, Id: {nftResult.Result.OASISNFT.Id}, Hash: {nftResult.Result.OASISNFT.Hash} Minted On: {nftResult.Result.OASISNFT.MintedOn}, Minted By Avatar Id: {nftResult.Result.OASISNFT.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.OASISNFT.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(nftResult.Message);
                result.Result = nftResult.Result.OASISNFT;
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
           
            return result;
        }

        public async Task SendNFTAsync()
        {
            //string mintWalletAddress = CLIEngine.GetValidInput("What is the original mint address?");
            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT from?");
            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT to?");
            string tokenAddress = CLIEngine.GetValidInput("What is the token address of the NFT?");
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

            CLIEngine.ShowWorkingMessage("Sending NFT...");

            OASISResult<INFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                TokenAddress = tokenAddress,
                //MintWalletAddress = mintWalletAddress,
                MemoText = memoText,
                Amount = 1
            });

            if (response != null && response.Result != null && !response.IsError)
                //CLIEngine.ShowSuccessMessage($"NFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
                CLIEngine.ShowSuccessMessage(response.Message);
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
        }

        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            DisplayProperty("NFT DETAILS", "", displayFieldLength, false);
            DisplayProperty("NFT Id", ParseMetaData(starHolon.MetaData, "NFT.Id"), displayFieldLength);
            DisplayProperty("Title", ParseMetaData(starHolon.MetaData, "NFT.Title"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(starHolon.MetaData, "NFT.Description"), displayFieldLength);
            DisplayProperty("Price", ParseMetaData(starHolon.MetaData, "NFT.Price"), displayFieldLength);
            DisplayProperty("Discount", ParseMetaData(starHolon.MetaData, "NFT.Discount"), displayFieldLength);
            DisplayProperty("OASIS MintWallet Address", ParseMetaData(starHolon.MetaData, "NFT.OASISMintWalletAddress"), displayFieldLength);
            DisplayProperty("Mint Transaction Hash", ParseMetaData(starHolon.MetaData, "NFT.MintTransactionHash"), displayFieldLength);
            DisplayProperty("NFT Token Address", ParseMetaData(starHolon.MetaData, "NFT.NFTTokenAddress"), displayFieldLength);
            DisplayProperty("Minted By Avatar Id", ParseMetaData(starHolon.MetaData, "NFT.MintedByAvatarId"), displayFieldLength);
            DisplayProperty("Minted On", ParseMetaData(starHolon.MetaData, "NFT.MintedOn"), displayFieldLength);
            DisplayProperty("OnChain Provider", ParseMetaData(starHolon.MetaData, "NFT.OnChainProvider"), displayFieldLength);
            DisplayProperty("OffChain Provider", ParseMetaData(starHolon.MetaData, "NFT.OffChainProvider"), displayFieldLength);
            DisplayProperty("Store NFT Meta Data OnChain", ParseMetaData(starHolon.MetaData, "NFT.StoreNFTMetaDataOnChain"), displayFieldLength);
            DisplayProperty("NFT OffChain Meta Type", ParseMetaData(starHolon.MetaData, "NFT.NFTOffChainMetaType"), displayFieldLength);
            DisplayProperty("NFT Standard Type", ParseMetaData(starHolon.MetaData, "NFT.NFTStandardType"), displayFieldLength);
            DisplayProperty("Symbol", ParseMetaData(starHolon.MetaData, "NFT.Symbol"), displayFieldLength);
            DisplayProperty("Image", ParseMetaDataForByteArray(starHolon.MetaData, "NFT.Image"), displayFieldLength);
            DisplayProperty("Image Url", ParseMetaData(starHolon.MetaData, "NFT.ImageUrl"), displayFieldLength);
            DisplayProperty("Thumbnail", ParseMetaDataForByteArray(starHolon.MetaData, "NFT.Thumbnail"), displayFieldLength);
            DisplayProperty("Thumbnail Url", ParseMetaData(starHolon.MetaData, "NFT.ThumbnailUrl"), displayFieldLength);
            DisplayProperty("JSON MetaData URL", ParseMetaData(starHolon.MetaData, "NFT.JSONMetaDataURL"), displayFieldLength);
            DisplayProperty("JSON MetaData URL Holon Id", ParseMetaData(starHolon.MetaData, "NFT.JSONMetaDataURLHolonId"), displayFieldLength);
            DisplayProperty("Seller Fee Basis Points", ParseMetaData(starHolon.MetaData, "NFT.SellerFeeBasisPoints"), displayFieldLength);
            DisplayProperty("Update Authority", ParseMetaData(starHolon.MetaData, "NFT.UpdateAuthority"), displayFieldLength);
            DisplayProperty("Send To Address After Minting", ParseMetaData(starHolon.MetaData, "NFT.SendToAddressAfterMinting"), displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Id", ParseMetaData(starHolon.MetaData, "NFT.SendToAvatarAfterMintingId"), displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Username", ParseMetaData(starHolon.MetaData, "NFT.SendToAvatarAfterMintingUsername"), displayFieldLength);
            DisplayProperty("Send NFT Transaction Hash", ParseMetaData(starHolon.MetaData, "NFT.SendNFTTransactionHash"), displayFieldLength);

            if (starHolon.MetaData.Count > 0)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in starHolon.MetaData.Keys)
                    CLIEngine.ShowMessage($"          {key} = {starHolon.MetaData[key]}");
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");

            CLIEngine.ShowDivider();
        }

        //public async Task ShowNFTAsync(Guid id = new Guid(), ProviderType providerType = ProviderType.Default)
        //{
        //    if (id == Guid.Empty)
        //        id = CLIEngine.GetValidInputForGuid("What is the GUID/ID to the NFT you wish to view?");

        //    CLIEngine.ShowWorkingMessage("Loading NFT...");
        //    OASISResult<IOASISNFT> nft = await STAR.OASISAPI.NFTs.LoadNftAsync(id);

        //    if (nft != null && !nft.IsError && nft.Result != null)
        //    {
        //        CLIEngine.ShowDivider();
        //        ShowNFT(nft.Result);
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage("No NFT Found.");
        //}

        //public void ShowNFT(IOASISNFT nft)
        //{
        //    string image = nft.Image != null ? "Yes" : "No";

        //    CLIEngine.ShowMessage(string.Concat($"Title: ", !string.IsNullOrEmpty(nft.Title) ? nft.Title : "None"));
        //    CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(nft.Description) ? nft.Description : "None"));
        //    CLIEngine.ShowMessage($"Price: {nft.Price}");
        //    CLIEngine.ShowMessage($"Discount: {nft.Discount}");
        //    CLIEngine.ShowMessage(string.Concat($"MemoText: ", !string.IsNullOrEmpty(nft.MemoText) ? nft.MemoText : "None"));
        //    CLIEngine.ShowMessage($"Id: {nft.Id}");
        //    CLIEngine.ShowMessage(string.Concat($"Hash: ", !string.IsNullOrEmpty(nft.Hash) ? nft.Hash : "None"));
        //    CLIEngine.ShowMessage($"MintedByAvatarId: {nft.MintedByAvatarId}");
        //    CLIEngine.ShowMessage(string.Concat($"MintedByAddress: ", !string.IsNullOrEmpty(nft.MintedByAddress) ? nft.MintedByAddress : "None"));
        //    CLIEngine.ShowMessage($"MintedOn: {nft.MintedOn}");
        //    CLIEngine.ShowMessage($"OnChainProvider: {nft.OnChainProvider.Name}");
        //    CLIEngine.ShowMessage($"OffChainProvider: {nft.OffChainProvider.Name}");
        //    CLIEngine.ShowMessage(string.Concat($"URL: ", !string.IsNullOrEmpty(nft.URL) ? nft.URL : "None"));
        //    CLIEngine.ShowMessage(string.Concat($"ImageUrl: ", !string.IsNullOrEmpty(nft.ImageUrl) ? nft.ImageUrl : "None"));
        //    CLIEngine.ShowMessage(string.Concat("Image: ", nft.Image != null ? "Yes" : "No"));
        //    CLIEngine.ShowMessage(string.Concat($"ThumbnailUrl: ", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None"));
        //    CLIEngine.ShowMessage(string.Concat("Thumbnail: ", nft.Thumbnail != null ? "Yes" : "No"));

        //    if (nft.MetaData.Count > 0)
        //    {
        //        CLIEngine.ShowMessage($"MetaData:");

        //        foreach (string key in nft.MetaData.Keys)
        //            CLIEngine.ShowMessage($"          {key} = {nft.MetaData[key]}");
        //    }
        //    else
        //        CLIEngine.ShowMessage($"MetaData: None");

        //    CLIEngine.ShowDivider();
        //}


        //private void ListNFTs(OASISResult<IEnumerable<IOASISNFT>> nftsResult)
        //{
        //    if (nftsResult != null)
        //    {
        //        if (!nftsResult.IsError)
        //        {
        //            if (nftsResult.Result != null && nftsResult.Result.Count() > 0)
        //            {
        //                Console.WriteLine();

        //                if (nftsResult.Result.Count() == 1)
        //                    CLIEngine.ShowMessage($"{nftsResult.Result.Count()} NFT Found:");
        //                else
        //                    CLIEngine.ShowMessage($"{nftsResult.Result.Count()} NFT's' Found:");

        //                CLIEngine.ShowDivider();

        //                foreach (IOASISGeoSpatialNFT geoNFT in nftsResult.Result)
        //                    ShowNFT(geoNFT);
        //            }
        //            else
        //                CLIEngine.ShowWarningMessage("No NFT's Found.");
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured loading NFT's. Reason: {nftsResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Unknown error occured loading NFT's.");
        //}
    }
}