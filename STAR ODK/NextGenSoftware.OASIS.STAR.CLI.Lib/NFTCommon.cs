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
using Solnet.Rpc.Models;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class NFTCommon
    {
        public NFTManager NFTManager { get; set; } = new NFTManager(STAR.BeamedInAvatar.Id);

        public async Task<IMintWeb4NFTRequest> GenerateNFTRequestAsync(string web3JSONMetaDataFile = "")
        {
            MintWeb4NFTRequest request = new MintWeb4NFTRequest();

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the WEB4 OASIS NFT wizard");
            CLIEngine.ShowDivider();

            request.MintedByAvatarId = STAR.BeamedInAvatar.Id;
            request.Title = CLIEngine.GetValidInput("What is the NFT's title?");
            request.Description = CLIEngine.GetValidInput("What is the NFT's description?");
            request.MemoText = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");
            request.Price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");

            if (CLIEngine.GetConfirmation("Is there any discount for the NFT? (This can always be changed later)"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForLong("What is the discount?");
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Is there any Royalty Percentage?"))
                request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false, addLineBefore: true);
            else
                Console.WriteLine("");

            SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo());

            request.IsForSale = salesInfo.IsForSale;
            request.SaleStartDate = salesInfo.SaleStartDate;
            request.SaleEndDate = salesInfo.SaleEndDate;

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType));
            request.OnChainProvider = new EnumValue<ProviderType>((ProviderType)onChainProviderObj);

            request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            if (!request.StoreNFTMetaDataOnChain)
            {
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
                request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>((NFTOffChainMetaType)offChainMetaDataTypeObj);

                object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? If you selected ExternalJSONURL above then it will only store the web4 metadata on the OASIS otherwise if you selected OASIS it will store both web3 and web4 metadata. NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive.", typeof(ProviderType));
                request.OffChainProvider = new EnumValue<ProviderType>((ProviderType)offChainProviderObj);

                if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL)
                {
                    Uri uriResult = await CLIEngine.GetValidURIAsync("What is the URI to the JSON meta data you have created for this NFT?");
                    request.JSONMetaDataURL = uriResult.AbsoluteUri;
                }
            }
            else
                request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.None);

            if (string.IsNullOrEmpty(web3JSONMetaDataFile) && request.NFTOffChainMetaType.Value != NFTOffChainMetaType.ExternalJSONURL)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import the JSON meta data now? (Press Y to import or N to generate new meta data)"))
                    web3JSONMetaDataFile = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");
            }

            if (!string.IsNullOrEmpty(web3JSONMetaDataFile) && File.Exists(web3JSONMetaDataFile))
                request.JSONMetaData = File.ReadAllText(web3JSONMetaDataFile);
            else
                CLIEngine.ShowMessage("The JSON meta data file path you entered does not exist. A new JSON meta data file will be generated instead.", addLineBefore: true);

            bool validStandard = false;
            do
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT standard do you wish to use? ERC721, ERC1155 or SPL? (ERC standards are only supported by EVM chains such as EthereumOASIS, PolygonsOASIS & ArbitrumOASIS. SPL is only supported by SolanaOASIS)", typeof(NFTStandardType));
                request.NFTStandardType = new EnumValue<NFTStandardType>((NFTStandardType)nftStandardObj);

                OASISResult<bool> nftStandardValid = NFTManager.IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value);

                if (!nftStandardValid.IsError)
                    validStandard = true;

            } while (!validStandard);

            if (request.NFTOffChainMetaType.Value != NFTOffChainMetaType.ExternalJSONURL)
            {
                if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT or input a URI to an online image? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT?");
                    request.Image = File.ReadAllBytes(localImagePath);
                }
                else
                {
                    Console.WriteLine("");
                    request.ImageUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?").Result.AbsoluteUri;
                }


                if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT Thumbnail?");
                    request.Thumbnail = File.ReadAllBytes(localImagePath);
                }
                else
                {
                    Console.WriteLine("");
                    request.ThumbnailUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT Thumbnail?").Result.AbsoluteUri;
                }
            }

            request.Tags = TagHelper.ManageTags(request.Tags);
            request.MetaData = MetaDataHelper.ManageMetaData(request.MetaData, "NFT");
            Console.WriteLine("");
            request.NumberToMint = CLIEngine.GetValidInputForInt("How many NFT's do you wish to mint?");
            request = (MintWeb4NFTRequest)GetSendAndAdvancedOptions(request);
            request.Web3NFTs = await GenerateWeb3NFTRequestsAsync(request);
            return request;
        }

        public IMintWeb4NFTRequest GetSendAndAdvancedOptions(IMintWeb4NFTRequest request)
        {
            if (CLIEngine.GetConfirmation("Do you wish to send the NFT to yourself after it is minted?"))
            {
                request.SendToAvatarAfterMintingId = STAR.BeamedInAvatar.Id;
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                int selection = CLIEngine.GetValidInputForInt("Do you wish to send the NFT using the users (1) Wallet Address, (2) Avatar Id, (3) Username or (4) Email? (Please enter 1, 2, 3 or 4)", true, 1, 4);

                switch (selection)
                {
                    case 1:
                        request.SendToAddressAfterMinting = CLIEngine.GetValidInput("What is the wallet address you want to send the NFT after it is minted?");
                        break;

                    case 2:
                        request.SendToAvatarAfterMintingId = CLIEngine.GetValidInputForGuid("What is the Id of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 3:
                        request.SendToAvatarAfterMintingUsername = CLIEngine.GetValidInput("What is the Username of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 4:
                        request.SendToAvatarAfterMintingEmail = CLIEngine.GetValidInputForEmail("What is the Email of the Avatar you want to send the NFT after it is minted?");
                        break;
                }
            }

            if (CLIEngine.GetConfirmation("Do you wish to view the Advanced Options? (allows you to configure minting and sending retry timeouts, polling etc)."))
            {
                Console.WriteLine("");
                request.WaitTillNFTMinted = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been minted before continuing? If you select yes it will continue to attempt minting for X seconds (defined in next question). Default is Yes.");

                if (request.WaitTillNFTMinted)
                {
                    Console.WriteLine("");
                    request.WaitForNFTToMintInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to mint before timing out? (default is 60 seconds)");
                    request.AttemptToMintEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to mint? (default is every 1 second)");
                }
                else
                    Console.WriteLine("");

                request.WaitTillNFTSent = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been sent before continuing? If you select yes it will continue to attempt sending for X seconds (defined in next question). Default is Yes.");

                if (request.WaitTillNFTSent)
                {
                    Console.WriteLine("");
                    request.WaitForNFTToSendInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to send before timing out? (default is 60 seconds)");
                    request.AttemptToSendEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to send? (default is every 1 second)");
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            return request;
        }

        public async Task<List<IMintWeb3NFTRequest>> GenerateWeb3NFTRequestsAsync(IMintWeb4NFTRequest request)
        {
            List<IMintWeb3NFTRequest> mintRequests = new List<IMintWeb3NFTRequest>();

            if (request.NumberToMint > 1 && !CLIEngine.GetConfirmation("Do all of the WEB3 NFT's share the same parent WEB4 NFT MetaData? (Select 'N' if you wish to create WEB3 NFT varients that share some or none of their parent WEB4 NFT MetaData)."))
            {
                if (request.Web3NFTs == null)
                    request.Web3NFTs = new List<IMintWeb3NFTRequest>();

                Console.WriteLine("");

                for (int i = 0; i < request.NumberToMint; i++)
                {
                    CLIEngine.ShowDivider();
                    CLIEngine.ShowMessage($"WEB3 OASIS NFT {i + 1}/{request.NumberToMint}");
                    CLIEngine.ShowDivider();
                    MintWeb3NFTRequest web3Request = (MintWeb3NFTRequest)await GenerateWeb3NFTRequestAsync(request);
                    //request.Web3NFTs.Add(web3Request);
                    mintRequests.Add(web3Request);
                    Console.WriteLine("");
                    CLIEngine.ShowSuccessMessage($"WEB3 NFT Varient {i + 1} Request Created.");

                    if (i < request.NumberToMint - 1 && CLIEngine.GetConfirmation("Would you like the rest of the WEB3 NFT Varients to share the same propetites/metadata? If you select 'N' then you will need to continue inputting the values you want for each WEB3 NFT Varient."))
                    {
                        for (int j = i + 1; j < request.NumberToMint; j++)
                        {
                            MintWeb3NFTRequest web3RequestInternal = new MintWeb3NFTRequest();
                            // Copy retry/wait settings
                            web3RequestInternal.AttemptToMintEveryXSeconds = web3Request.AttemptToMintEveryXSeconds;
                            web3RequestInternal.AttemptToSendEveryXSeconds = web3Request.AttemptToSendEveryXSeconds;

                            // Basic fields
                            web3RequestInternal.Title = web3Request.Title;
                            web3RequestInternal.Description = web3Request.Description;
                            web3RequestInternal.MemoText = web3Request.MemoText;

                            // Pricing
                            web3RequestInternal.Price = web3Request.Price;
                            web3RequestInternal.Discount = web3Request.Discount;
                            web3RequestInternal.RoyaltyPercentage = web3Request.RoyaltyPercentage;

                            // Mint counts
                            web3RequestInternal.NumberToMint = web3Request.NumberToMint ?? 1;

                            // Sale info
                            web3RequestInternal.IsForSale = web3Request.IsForSale;
                            web3RequestInternal.SaleStartDate = web3Request.SaleStartDate;
                            web3RequestInternal.SaleEndDate = web3Request.SaleEndDate;

                            // Providers / standards
                            web3RequestInternal.OnChainProvider = web3Request.OnChainProvider;
                            web3RequestInternal.OffChainProvider = web3Request.OffChainProvider;
                            web3RequestInternal.StoreNFTMetaDataOnChain = web3Request.StoreNFTMetaDataOnChain;

                            if (web3Request.NFTOffChainMetaType.HasValue)
                                web3RequestInternal.NFTOffChainMetaType = web3Request.NFTOffChainMetaType.Value;

                            if (web3Request.NFTStandardType.HasValue)
                                web3RequestInternal.NFTStandardType = web3Request.NFTStandardType.Value;

                            // Images
                            web3RequestInternal.Image = web3Request.Image;
                            web3RequestInternal.ImageUrl = web3Request.ImageUrl;
                            web3RequestInternal.Thumbnail = web3Request.Thumbnail;
                            web3RequestInternal.ThumbnailUrl = web3Request.ThumbnailUrl;

                            // JSON metadata
                            web3RequestInternal.JSONMetaDataURL = web3Request.JSONMetaDataURL;
                            web3RequestInternal.JSONMetaData = web3Request.JSONMetaData;

                            // Tags
                            if (web3Request.Tags != null)
                                web3RequestInternal.Tags = new List<string>(web3Request.Tags);

                            // MetaData
                            if (web3Request.MetaData != null)
                                web3RequestInternal.MetaData = new Dictionary<string, string>(web3Request.MetaData);

                            // Merge strategies
                            web3RequestInternal.NFTTagsMergeStrategy = web3Request.NFTTagsMergeStrategy;
                            web3RequestInternal.NFTMetaDataMergeStrategy = web3Request.NFTMetaDataMergeStrategy;

                            // Send options
                            web3RequestInternal.SendToAddressAfterMinting = web3Request.SendToAddressAfterMinting;
                            web3RequestInternal.SendToAvatarAfterMintingId = web3Request.SendToAvatarAfterMintingId;
                            web3RequestInternal.SendToAvatarAfterMintingUsername = web3Request.SendToAvatarAfterMintingUsername;
                            web3RequestInternal.SendToAvatarAfterMintingEmail = web3Request.SendToAvatarAfterMintingEmail;

                            // Wait/send advanced options
                            web3RequestInternal.WaitTillNFTMinted = web3Request.WaitTillNFTMinted;
                            web3RequestInternal.WaitForNFTToMintInSeconds = web3Request.WaitForNFTToMintInSeconds;
                            web3RequestInternal.WaitTillNFTSent = web3Request.WaitTillNFTSent;
                            web3RequestInternal.WaitForNFTToSendInSeconds = web3Request.WaitForNFTToSendInSeconds;

                            Console.WriteLine("");
                            CLIEngine.ShowSuccessMessage($"WEB3 NFT Varient {j + 1} Request Created.");

                            //request.Web3NFTs.Add(web3RequestInternal);
                            mintRequests.Add(web3RequestInternal);
                        }

                        break;
                    }
                }
            }
            else
                Console.WriteLine("");

            return mintRequests;
        }

        public async Task<IMintWeb3NFTRequest> GenerateWeb3NFTRequestAsync(IMintWeb4NFTRequest request)
        {
            MintWeb3NFTRequest web3Request = new MintWeb3NFTRequest();

            if (CLIEngine.GetConfirmation($"Do you wish to edit the title for this WEB3 Request? (It currently inherits '{request.Title}' from its parent WEB4 NFT.)"))
                web3Request.Title = CLIEngine.GetValidInput("What is the title?", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the description for this WEB3 Request? (It currently inherits '{request.Description}' from its parent WEB4 NFT.)"))
                web3Request.Description = CLIEngine.GetValidInput("What is the description?", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the memotext for this WEB3 Request? (It currently inherits '{request.MemoText}' from its parent WEB4 NFT.)"))
                web3Request.MemoText = CLIEngine.GetValidInput("What is the memotext?", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the price for this WEB3 Request? (It currently inherits '{request.Price}' from its parent WEB4 NFT.)"))
                web3Request.Price = CLIEngine.GetValidInputForLong("What is the price?", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the discount for this WEB3 Request? (It currently inherits '{request.Discount}' from its parent WEB4 NFT.)"))
                web3Request.Discount = CLIEngine.GetValidInputForLong("What is the discount?", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Royalty Percentage for this WEB3 Request? (It currently inherits '{request.RoyaltyPercentage}' from its parent WEB4 NFT.)"))
                web3Request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("What is the Royalty Percentage (integer)", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation(string.Concat("Do you wish to edit the sales info (IsForSale, SaleStartDate & SaleEndDate) for this WEB3 Request? (It currently inherits 'IsForSale: ", request.IsForSale, ", SaleStartDate: ", request.SaleStartDate.HasValue ? request.SaleStartDate.Value : "None", ", SaleEndDate: ", request.SaleEndDate.HasValue ? request.SaleEndDate.Value : "None", " from its parent WEB4 NFT.)")))
            {
                Console.WriteLine("");
                SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo());

                web3Request.IsForSale = salesInfo.IsForSale;
                web3Request.SaleStartDate = salesInfo.SaleStartDate;
                web3Request.SaleEndDate = salesInfo.SaleEndDate;
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the on-chain provider for this WEB3 Request? (It currently inherits '{request.OnChainProvider.Name}' from its parent WEB4 NFT.)"))
            {
                object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType), addLineBefore: true);
                web3Request.OnChainProvider = (ProviderType)onChainProviderObj;
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit whether the NFT metadata is stored on-chain or off-chain? (It currently inherits '{request.StoreNFTMetaDataOnChain}' from its parent WEB4 NFT. True is store on-chain, False is off-chain.)"))
            {
                web3Request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)", addLineBefore: true);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                web3Request.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain;
            }

            if (web3Request.StoreNFTMetaDataOnChain.HasValue && !web3Request.StoreNFTMetaDataOnChain.Value)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to edit the offchain metadata type for this WEB3 Request? (It currently inherits '{request.NFTOffChainMetaType.Name}' from its parent WEB4 NFT.)"))
                {
                    object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType), addLineBefore: true);
                    web3Request.NFTOffChainMetaType = (NFTOffChainMetaType)offChainMetaDataTypeObj;
                }
                else
                {
                    web3Request.NFTOffChainMetaType = request.NFTOffChainMetaType.Value;
                    Console.WriteLine("");
                }

                if (CLIEngine.GetConfirmation($"Do you wish to edit the off-chain provider for this WEB3 Request? (It currently inherits '{request.OffChainProvider.Name}' from its parent WEB4 NFT.)"))
                {
                    object offChainProviderObj = CLIEngine.GetValidInputForEnum("What off-chain provider do you wish to mint on?", typeof(ProviderType), addLineBefore: true);
                    web3Request.OffChainProvider = (ProviderType)offChainProviderObj;
                }
                //else
                //    Console.WriteLine("");

                if (web3Request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL && CLIEngine.GetConfirmation($"Do you wish to edit the JSON metadata URI for this WEB3 Request? (It currently inherits '{request.JSONMetaDataURL}' from its parent WEB4 NFT.)"))
                {
                    Uri uriResult = await CLIEngine.GetValidURIAsync("What is the URI to the JSON meta data you have created for this NFT?", addLineBefore: true);
                    web3Request.JSONMetaDataURL = uriResult.AbsoluteUri;
                }
                else
                    Console.WriteLine("");
            }

            // NFT standard
            if (CLIEngine.GetConfirmation($"Do you wish to edit the NFT standard for this WEB3 Request? (It currently inherits '{request.NFTStandardType?.Name}')"))
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT standard do you wish to use? ERC721, ERC1155 or SPL?", typeof(NFTStandardType), addLineBefore: true);
                web3Request.NFTStandardType = (NFTStandardType)nftStandardObj;
            }
            else
                Console.WriteLine("");


            // Allow editing image/thumbnail for this web3 request
            if (CLIEngine.GetConfirmation($"Do you wish to update the Image and Thumbnail for this WEB3 Request? (It currently inherits from the parent WEB4 NFT)"))
            {
                Console.WriteLine("");
                OASISResult<ImageAndThumbnail> web3ImageResult = ProcessImageAndThumbnail("WEB3 NFT");

                if (web3ImageResult != null && web3ImageResult.Result != null && !web3ImageResult.IsError)
                {
                    web3Request.Image = web3ImageResult.Result.Image;
                    web3Request.ImageUrl = web3ImageResult.Result.ImageUrl;
                    web3Request.Thumbnail = web3ImageResult.Result.Thumbnail;
                    web3Request.ThumbnailUrl = web3ImageResult.Result.ThumbnailUrl;
                }
                else
                {
                    string msg = web3ImageResult != null ? web3ImageResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Processing Image and Thumbnail for WEB3 NFT: {msg}");
                }
            }
            else
                Console.WriteLine("");

            // Tags
            if (CLIEngine.GetConfirmation($"Do you wish to edit the Tags for this WEB3 Request? (It currently inherits '{(request.Tags != null ? string.Join(", ", request.Tags) : "none")}')"))
            {
                web3Request.Tags = request.Tags != null ? new List<string>(request.Tags) : new List<string>();
                web3Request.Tags = TagHelper.ManageTags(web3Request.Tags);
            }
            else
                Console.WriteLine("");

            // MetaData
            if (CLIEngine.GetConfirmation($"Do you wish to edit the MetaData for this WEB3 Request? (It currently inherits from the parent WEB4 NFT)"))
            {
                web3Request.MetaData = request.MetaData != null ? new Dictionary<string, string>(request.MetaData) : new Dictionary<string, string>();
                web3Request.MetaData = MetaDataHelper.ManageMetaData(web3Request.MetaData, "WEB3 NFT");
            }
            else
                Console.WriteLine("");

            // Number to mint for this web3 request
            if (CLIEngine.GetConfirmation($"Do you wish to change the number of tokens to mint for this WEB3 Request? Defaults to 1."))
                web3Request.NumberToMint = CLIEngine.GetValidInputForInt("How many of this WEB3 NFT should be minted?", true, 1, int.MaxValue, addLineBefore: true);
            else
            {
                Console.WriteLine("");
                web3Request.NumberToMint = 1;
            }

            // Advanced options: wait & attempts
            if (CLIEngine.GetConfirmation("Do you wish to edit the Advanced Options for this WEB3 Request? (retry/wait settings). Defaults to WEB4 NFT settings."))
            {
                Console.WriteLine("");
                web3Request.WaitTillNFTMinted = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been minted before continuing?");

                if (web3Request.WaitTillNFTMinted.HasValue && web3Request.WaitTillNFTMinted.Value)
                {
                    Console.WriteLine("");
                    web3Request.WaitForNFTToMintInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to mint before timing out? (default is 60 seconds)", true, 1, int.MaxValue);
                    web3Request.AttemptToMintEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to mint? (default is every 1 second)", true, 1, int.MaxValue);
                }
                else
                    Console.WriteLine("");

                web3Request.WaitTillNFTSent = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been sent before continuing?");

                if (web3Request.WaitTillNFTSent.HasValue && web3Request.WaitTillNFTSent.Value)
                {
                    Console.WriteLine("");
                    web3Request.WaitForNFTToSendInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to send before timing out? (default is 60 seconds)", true, 1, int.MaxValue);
                    web3Request.AttemptToSendEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to send? (default is every 1 second)", true, 1, int.MaxValue);
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            // Merge strategies for tags and meta data
            if (CLIEngine.GetConfirmation("Do you wish to set how WEB3 tags should merge with parent WEB4 tags? If the tag already exists in the parent WEB4 OASIS NFT tags then select 'Merge' to keep the existing tag and do not overwrite it with the tag from the WEB3 NFT tags (default), select 'Replace' to completely replace the parent WEB4 OASIS NFT tags with the WEB3 NFT tags."))
            {
                Console.WriteLine("");
                object tagMergeObj = CLIEngine.GetValidInputForEnum("Select tag merge strategy:", typeof(NFTTagsMergeStrategy), addLineBefore: true);
                web3Request.NFTTagsMergeStrategy = (NFTTagsMergeStrategy)tagMergeObj;
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to set how WEB3 meta-data should merge with parent WEB4 meta-data? If the key already exists in the parent WEB4 OASIS NFT meta data then select 'Merge' to keep the existing value and do not overwrite it with the value from the WEB3 NFT meta data (default), select 'MergeAndOverwrite' to overwrite it with the value from the WEB3 NFT meta data and select 'Replace' to completely replace the parent WEB4 OASIS NFT meta data with the WEB3 NFT meta data."))
            {
                Console.WriteLine("");
                object metaMergeObj = CLIEngine.GetValidInputForEnum("Select meta-data merge strategy:", typeof(NFTMetaDataMergeStrategy), addLineBefore: true);
                web3Request.NFTMetaDataMergeStrategy = (NFTMetaDataMergeStrategy)metaMergeObj;
            }
            else
                Console.WriteLine("");

            // Sending options after mint
            if (CLIEngine.GetConfirmation($"Do you wish to change who the minted WEB3 NFT will be sent to after minting? (It currently inherits SendToAddressAfterMinting: '{request.SendToAddressAfterMinting}', SendToAvatarAfterMintingId: '{request.SendToAvatarAfterMintingId}', SendToAvatarAfterMintingUsername: '{request.SendToAvatarAfterMintingUsername}', SendToAvatarAfterMintingEmail: '{request.SendToAvatarAfterMintingEmail}' from its parent WEB4 NFT.)"))
            {
                if (CLIEngine.GetConfirmation("Do you wish to send the NFT to yourself after it is minted?"))
                {
                    web3Request.SendToAvatarAfterMintingId = STAR.BeamedInAvatar.Id;
                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine("");
                    int selection = CLIEngine.GetValidInputForInt("Do you wish to send the NFT using the users (1) Wallet Address, (2) Avatar Id, (3) Username or (4) Email? (Please enter 1, 2, 3 or 4)", true, 1, 4);

                    switch (selection)
                    {
                        case 1:
                            web3Request.SendToAddressAfterMinting = CLIEngine.GetValidInput("What is the wallet address you want to send the NFT after it is minted?");
                            break;

                        case 2:
                            web3Request.SendToAvatarAfterMintingId = CLIEngine.GetValidInputForGuid("What is the Id of the Avatar you want to send the NFT after it is minted?");
                            break;

                        case 3:
                            web3Request.SendToAvatarAfterMintingUsername = CLIEngine.GetValidInput("What is the Username of the Avatar you want to send the NFT after it is minted?");
                            break;

                         case 4:
                            web3Request.SendToAvatarAfterMintingEmail = CLIEngine.GetValidInputForEmail("What is the Email of the Avatar you want to send the NFT after it is minted?");
                            break;
                    }
                }
            }

            return web3Request;
        }

        public async Task<IRemintWeb4NFTRequest> GenerateWeb4NFTRemintRequestAsync(IWeb4NFT web4NFT)
        {
            int numberToMint = CLIEngine.GetValidInputForInt("How many additional WEB3 NFT's do you wish to mint for this WEB4 OASIS NFT? ");

            MintWeb4NFTRequest request = new MintWeb4NFTRequest()
            {
                MintedByAvatarId = web4NFT.MintedByAvatarId,
                Title = web4NFT.Title,
                Description = web4NFT.Description,
                MemoText = web4NFT.MemoText,
                Price = web4NFT.Price,
                Discount = web4NFT.Discount,
                RoyaltyPercentage = web4NFT.RoyaltyPercentage,
                IsForSale = web4NFT.IsForSale,
                SaleStartDate = web4NFT.SaleStartDate,
                SaleEndDate = web4NFT.SaleEndDate,
                OnChainProvider = web4NFT.OnChainProvider,
                OffChainProvider = web4NFT.OffChainProvider,
                StoreNFTMetaDataOnChain = web4NFT.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = web4NFT.NFTOffChainMetaType,
                NFTStandardType = web4NFT.NFTStandardType,
                Image = web4NFT.Image,
                ImageUrl = web4NFT.ImageUrl,
                Thumbnail = web4NFT.Thumbnail,
                ThumbnailUrl = web4NFT.ThumbnailUrl,
                MetaData = web4NFT.MetaData,
                Tags = web4NFT.Tags,
                JSONMetaData = web4NFT.JSONMetaData,
                JSONMetaDataURL = web4NFT.JSONMetaDataURL,
                Symbol = web4NFT.Symbol,
                NumberToMint = numberToMint
            };

            request = GetSendAndAdvancedOptions(request) as MintWeb4NFTRequest;
            request.Web3NFTs = await GenerateWeb3NFTRequestsAsync(request);

            return new RemintWeb4NFTRequest()
            {
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitTillNFTSent = request.WaitTillNFTSent,
                Web3NFTs = request.Web3NFTs,
                Web4NFT = web4NFT
            };
        }

        public async Task<IRemintWeb4GeoNFTRequest> GenerateWeb4GeoNFTRemintRequestAsync(IWeb4GeoSpatialNFT web4GeoNFT)
        {
            int numberToMint = CLIEngine.GetValidInputForInt("How many additional WEB3 NFT's do you wish to mint for this WEB4 OASIS Geo-NFT? ");

            MintWeb4NFTRequest request = new MintWeb4NFTRequest()
            {
                MintedByAvatarId = web4GeoNFT.MintedByAvatarId,
                Title = web4GeoNFT.Title,
                Description = web4GeoNFT.Description,
                MemoText = web4GeoNFT.MemoText,
                Price = web4GeoNFT.Price,
                Discount = web4GeoNFT.Discount,
                RoyaltyPercentage = web4GeoNFT.RoyaltyPercentage,
                IsForSale = web4GeoNFT.IsForSale,
                SaleStartDate = web4GeoNFT.SaleStartDate,
                SaleEndDate = web4GeoNFT.SaleEndDate,
                OnChainProvider = web4GeoNFT.OnChainProvider,
                OffChainProvider = web4GeoNFT.OffChainProvider,
                StoreNFTMetaDataOnChain = web4GeoNFT.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = web4GeoNFT.NFTOffChainMetaType,
                NFTStandardType = web4GeoNFT.NFTStandardType,
                Image = web4GeoNFT.Image,
                ImageUrl = web4GeoNFT.ImageUrl,
                Thumbnail = web4GeoNFT.Thumbnail,
                ThumbnailUrl = web4GeoNFT.ThumbnailUrl,
                MetaData = web4GeoNFT.MetaData,
                Tags = web4GeoNFT.Tags,
                JSONMetaData = web4GeoNFT.JSONMetaData,
                JSONMetaDataURL = web4GeoNFT.JSONMetaDataURL,
                Symbol = web4GeoNFT.Symbol,
                NumberToMint = numberToMint
            };

            request = GetSendAndAdvancedOptions(request) as MintWeb4NFTRequest;
            request.Web3NFTs = await GenerateWeb3NFTRequestsAsync(request);

            return new RemintWeb4GeoNFTRequest()
            {
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitTillNFTSent = request.WaitTillNFTSent,
                Web3NFTs = request.Web3NFTs,
                Web4GeoNFT = web4GeoNFT
            };
        }

        //public async Task<List<IMintWeb3NFTRequest>> GenerateWeb3NFTRequestsAsync(IMintWeb4NFTRequest request, int numberToMint = 1)
        //{
        //    return await GenerateWeb3NFTRequestsAsync(request);
        //}

        public async Task<IImportWeb3NFTRequest> GenerateImportNFTRequestAsync()
        {
            ImportWeb3NFTRequest request = new ImportWeb3NFTRequest();

            request.NFTTokenAddress = CLIEngine.GetValidInput("Please enter the token address of the NFT you wish to import: ");
            request.ImportedByAvatarId = STAR.BeamedInAvatar.Id;
            request.Title = CLIEngine.GetValidInput("What is the NFT's title?");
            request.Description = CLIEngine.GetValidInput("What is the NFT's description?");
            request.MemoText = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");

            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT?");
                request.Image = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                request.ImageUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?").Result.AbsoluteUri;
            }


            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT Thumbnail?");
                request.Thumbnail = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                request.ThumbnailUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT Thumbnail?").Result.AbsoluteUri;
            }

            request.Price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");

            if (CLIEngine.GetConfirmation("Is there any discount for the NFT? (This can always be changed later)"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForLong("What is the discount?");
            }
            else
                Console.WriteLine("");

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider did you use to mint on?", typeof(ProviderType));
            request.OnChainProvider = new EnumValue<ProviderType>((ProviderType)onChainProviderObj);

            request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Was the NFT metadata stored on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            //if (!request.StoreNFTMetaDataOnChain)
            //{
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain WEB4 OASIS NFT meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
                request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>((NFTOffChainMetaType)offChainMetaDataTypeObj);

                if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.OASIS)
                {
                    object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
                    request.OffChainProvider = new EnumValue<ProviderType>((ProviderType)offChainProviderObj);
                }
                else if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL)
                {
                    Uri uriResult = await CLIEngine.GetValidURIAsync("What is the URI to the JSON meta data you have created for this NFT?");
                    request.JSONMetaDataURL = uriResult.AbsoluteUri;
                }
            //}

            bool validStandard = false;
            do
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT standard did you use? ERC721, ERC1155 or SPL? (ERC standards are only supported by EVM chains such as EthereumOASIS, PolygonsOASIS & ArbitrumOASIS. SPL is only supported by SolanaOASIS)", typeof(NFTStandardType));
                request.NFTStandardType = new EnumValue<NFTStandardType>((NFTStandardType)nftStandardObj);

                OASISResult<bool> nftStandardValid = NFTManager.IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value);

                if (!nftStandardValid.IsError)
                    validStandard = true;

            } while (!validStandard);


            request.MetaData = MetaDataHelper.AddMetaData("NFT");
            return request;
        }

        public OASISResult<ImageAndThumbnail> ProcessImageAndThumbnail(string itemName)
        {
            OASISResult<ImageAndThumbnail> result = new OASISResult<ImageAndThumbnail>(new ImageAndThumbnail());

            if (CLIEngine.GetConfirmation($"Do you want to upload a local image on your device to represent the {itemName} or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile($"What is the full path to the local image you want to represent the {itemName}?");
                result.Result.Image = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                result.Result.ImageUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?").Result.AbsoluteUri;
            }


            if (CLIEngine.GetConfirmation($"Do you want to upload a local image on your device to represent the {itemName} Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile($"What is the full path to the local image you want to represent the {itemName} Thumbnail?");
                result.Result.Thumbnail = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                result.Result.ThumbnailUrl = CLIEngine.GetValidURIAsync($"What is the URI to the image you want to represent the {itemName} Thumbnail?").Result.AbsoluteUri;
            }

            return result;
        }

        public async Task<OASISResult<IUpdateWeb4NFTRequest>> UpdateWeb4NFTAsync(IUpdateWeb4NFTRequest request, IWeb4NFT nft, string displayName, bool updateTags = true, bool updateMetaData = true)
        {
            OASISResult<IUpdateWeb4NFTRequest> result = new OASISResult<IUpdateWeb4NFTRequest>();

            request.Id = nft.Id;
            request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {nft.Title})"))
                request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Description? (currently is: {nft.Description})"))
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
                    //OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail: {msg}");
                    //return result;
                }
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Price? (currently is: {nft.Price})"))
            {
                Console.WriteLine("");
                request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price: ", addLineBefore: false);
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Discount? (currently is: {nft.Discount})"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount: ", addLineBefore: false);
            }
            else
                Console.WriteLine("");

            // Allow editing additional NFT-specific fields
            if (CLIEngine.GetConfirmation($"Do you wish to edit the Royalty Percentage? (currently is: {nft.RoyaltyPercentage})"))
                request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false, addLineBefore: true);
            else
                Console.WriteLine("");
            //if (CLIEngine.GetConfirmation("Do you wish to edit the Previous Owner Avatar Id?"))
            //    request.PreviousOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Previous Owner Avatar Id (GUID): ");

            //if (CLIEngine.GetConfirmation("Do you wish to edit the Current Owner Avatar Id?"))
            //    request.CurrentOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Current Owner Avatar Id (GUID): ");

            if (CLIEngine.GetConfirmation($"Do you wish to change the sale status (Is For Sale)? (currently is: {nft.IsForSale})"))
            {
                Console.WriteLine("");
                SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo() { IsForSale = nft.IsForSale, SaleStartDate = nft.SaleStartDate, SaleEndDate = nft.SaleEndDate });
                
                request.IsForSale = salesInfo.IsForSale;
                request.SaleStartDate = salesInfo.SaleStartDate;
                request.SaleEndDate = salesInfo.SaleEndDate;
            }
            else
                Console.WriteLine("");


            if (updateTags)
                request.Tags = TagHelper.ManageTags(nft.Tags);

            if (updateMetaData)
                request.MetaData = MetaDataHelper.ManageMetaData(nft.MetaData, displayName);

            request.UpdateAllChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to apply these edits to all child WEB3 NFT's contained inside this WEB4 OASIS NFT? NOTE: This will override any varients you created!");

            if (!request.UpdateAllChildWeb3NFTs)
            {
                if (request.UpdateChildWebNFTIds == null)
                    request.UpdateChildWebNFTIds = new List<string>();

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to apply these edits to a selection of child WEB3 NFT's contained inside this WEB4 OASIS NFT? NOTE: This will override any varients you created!"))
                {
                    do
                    {
                        Console.WriteLine("");
                        OASISResult<IWeb3NFT> web3NFTResult = await FindWeb3NFTAsync("edit", request.Id);

                        if (web3NFTResult != null && web3NFTResult.Result != null && !web3NFTResult.IsError)
                            request.UpdateChildWebNFTIds.Add(web3NFTResult.Result.Id.ToString());

                    } while (CLIEngine.GetConfirmation("Do you wish to apply this edit to any other WEB3 NFT's?"));
                }
            }

            result.Result = request;
            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> FindWeb3NFTAsync(string operationName, Guid parentWeb4NFTId = default, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB3 NFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb3NFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTManager.LoadAllWeb3NFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, parentWeb4NFTId, providerType);
                        else
                            starHolonsResult = await NFTManager.LoadAllWeb3NFTsAsync(parentWeb4NFTId, providerType);

                        ListWeb3NFTs(starHolonsResult);

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
                    result = await NFTManager.LoadWeb3NftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    Dictionary<string, string> metaData = null;

                    if (parentWeb4NFTId != Guid.Empty)
                        metaData = new Dictionary<string, string>() { { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() } };
                
                    OASISResult<IEnumerable<IWeb3NFT>> searchResults = await NFTManager.SearchWeb3NFTsAsync(idOrName, STAR.BeamedInAvatar.Id, default, metaData, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb3NFTs(searchResults, true);

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
                    ShowWeb3NFT(result.Result);

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

        public async Task<OASISResult<T5>> UpdateSTARNETHolonAsync<T1, T2, T3, T4, T5>(string web5IdMetaDataKey, string starnetDNAKeyForWeb4Object, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, Dictionary<string, string> metaData, OASISResult<T5> result, ProviderType providerType = ProviderType.Default) 
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {
            Guid web5Id = Guid.Empty;

            if (metaData != null && metaData.ContainsKey(web5IdMetaDataKey) && metaData[web5IdMetaDataKey] != null && Guid.TryParse(metaData[web5IdMetaDataKey].ToString(), out web5Id))
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Updating WEB5 STAR {STARNETManager.STARNETHolonUIName} with updated WEB4 OASIS {STARNETManager.STARNETHolonUIName} data...");
                OASISResult<T1> starNFTCollection = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, web5Id, providerType: providerType);

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
            }

            return result;
        }

        public async Task<OASISResult<T5>> DeleteAllSTARNETVersionsAsync<T1, T2, T3, T4, T5>(string web5IdMetaDataKey, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, Dictionary<string, string> metaData, OASISResult<T5> result, ProviderType providerType = ProviderType.Default)
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {
            Guid web5Id = Guid.Empty;

            if (metaData != null && metaData.ContainsKey(web5IdMetaDataKey) && metaData[web5IdMetaDataKey] != null && Guid.TryParse(metaData[web5IdMetaDataKey].ToString(), out web5Id))
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Deleting All WEB5 STAR {STARNETManager.STARNETHolonUIName} Versions...");

                OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(web5Id, providerType);

                if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
                {
                    foreach (T1 version in versionsResult.Result)
                    {
                        OASISResult<T1> deleteResult = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, web5Id, version.STARNETDNA.VersionSequence, providerType: providerType);

                        if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                            CLIEngine.ShowSuccessMessage($"Successfully Deleted Version {version.STARNETDNA.VersionSequence}.");
                        else
                        {
                            string msg = versionsResult != null ? versionsResult.Message : "";
                            OASISErrorHandling.HandleError(ref result, $"Error Occured Deleting WEB5 STAR {STARNETManager.STARNETHolonUIName} Version {version.STARNETDNA.VersionSequence}. Reason: {msg}");
                        }
                    }
                }
                else
                {
                    string msg = versionsResult != null ? versionsResult.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Loading WEB5 STAR {STARNETManager.STARNETHolonUIName} versions. Reason: {msg}");
                }   
            }

            return result;
        }

        private void DisplayProperty(string heading, string value, int displayFieldLength, bool displayColon = true)
        {
            CLIEngine.DisplayProperty(heading, value, displayFieldLength, displayColon);
        }
    }
}