using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, NFTDNA>
    public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public NFTs(Guid avatarId) : base(new STARNFTManager(avatarId),
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

        public override async Task<OASISResult<STARNFT>> CreateAsync(object createParams, STARNFT newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFT> result = new OASISResult<STARNFT>();

            //Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the GeoNFT you wish to upload to STARNET: ");
            //OASISResult<IOASISGeoSpatialNFT> geoNFTResult = await NFTManager.LoadGeoNftAsync(geoNFTId);

            OASISResult<IOASISNFT> mintResult = await MintNFTAsync(); //Mint WEB4 NFT

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                result = await base.CreateAsync(createParams, new STARNFT() { OASISNFTId = mintResult.Result.Id }, showHeaderAndInro, checkIfSourcePathExists, providerType);
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
                CLIEngine.ShowSuccessMessage($"OASIS NFT Successfully Minted. {nftResult.Message} Transaction Result: {nftResult.Result.TransactionResult}, Id: {nftResult.Result.OASISNFT.Id}, Hash: {nftResult.Result.OASISNFT.Hash} Minted On: {nftResult.Result.OASISNFT.MintedOn}, Minted By Avatar Id: {nftResult.Result.OASISNFT.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.OASISNFT.MintedByAddress}.");
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
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

            CLIEngine.ShowWorkingMessage("Sending NFT...");

            OASISResult<INFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                //MintWalletAddress = mintWalletAddress,
                MemoText = memoText,
                //Amount = amount,
            });

            if (response != null && response.Result != null && !response.IsError)
                CLIEngine.ShowSuccessMessage($"NFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
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