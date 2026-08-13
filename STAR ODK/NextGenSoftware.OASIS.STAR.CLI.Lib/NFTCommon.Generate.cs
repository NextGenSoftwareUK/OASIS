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

                object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? If you selected IPFS, Pinata or ExternalJSONURL above then it will only store the web4 metadata on the OASIS otherwise if you selected OASIS it will store both web3 and web4 metadata. NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive.", typeof(ProviderType));
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

        public async Task<List<IMintWeb3NFTRequest>> GenerateWeb3NFTRequestsAsync(IMintWeb4NFTRequest request, bool remint = false)
        {
            List<IMintWeb3NFTRequest> mintRequests = new List<IMintWeb3NFTRequest>();

            if (((request.NumberToMint > 1 && !remint) || remint) && !CLIEngine.GetConfirmation("Do all of the WEB3 NFT(s) share the same parent WEB4 NFT MetaData? (Select 'N' if you wish to create WEB3 NFT varients that share some or none of their parent WEB4 NFT MetaData)."))
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

    }
}
