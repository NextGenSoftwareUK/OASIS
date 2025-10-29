using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class NFTCommon
    {
        public NFTManager NFTManager { get; set; } = new NFTManager(STAR.BeamedInAvatar.Id);

        

        private Dictionary<string, object> AddMetaDataToNFT(Dictionary<string, object> metaData)
        {
            Console.WriteLine("");
            string key = CLIEngine.GetValidInput("What is the key?");
            string value = "";
            byte[] metaFile = null;

            if (CLIEngine.GetConfirmation("Is the value a file?"))
            {
                Console.WriteLine("");
                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                metaFile = File.ReadAllBytes(metaPath);
            }
            else
            {
                Console.WriteLine("");
                value = CLIEngine.GetValidInput("What is the value?");
            }

            if (metaFile != null)
                metaData[key] = metaFile;
            else
                metaData[key] = value;

            return metaData;
        }

        public async Task<IMintNFTTransactionRequest> GenerateNFTRequestAsync(string web3JSONMetaDataFile = "")
        {
            MintNFTTransactionRequest request = new MintNFTTransactionRequest();

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

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType));
            request.OnChainProvider = new EnumValue<ProviderType>((ProviderType)onChainProviderObj);

            request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            if (!request.StoreNFTMetaDataOnChain)
            {
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
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
                //else if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSON)
                //{
                //    if (string.IsNullOrEmpty(web3JSONMetaDataFile))
                //        web3JSONMetaDataFile = CLIEngine.GetValidFile("What is the full path to the JSON meta data file you have created for this NFT?");

                //    request.JSONMetaData = web3JSONMetaDataFile;
                //}
            }

            //if (string.IsNullOrEmpty(web3JSONMetaDataFile))
            if (string.IsNullOrEmpty(web3JSONMetaDataFile) && request.NFTOffChainMetaType.Value != NFTOffChainMetaType.ExternalJSONURL)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import the JSON meta data now? (Press Y to import or N to generate new meta data)"))
                    web3JSONMetaDataFile = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");
            }

            if (File.Exists(web3JSONMetaDataFile))
                request.JSONMetaData = File.ReadAllText(web3JSONMetaDataFile);
            else
                Console.WriteLine("The JSON meta data file path you entered does not exist. A new JSON meta data file will be generated instead.");

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

            if (CLIEngine.GetConfirmation("Do you wish to add any metadata to this NFT?"))
            {
                request.MetaData = new Dictionary<string, object>();
                request.MetaData = AddMetaDataToNFT(request.MetaData);
                bool metaDataDone = false;

                do
                {
                    if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
                        request.MetaData = AddMetaDataToNFT(request.MetaData);
                    else
                        metaDataDone = true;
                }
                while (!metaDataDone);
            }

            Console.WriteLine("");
            request.NumberToMint = CLIEngine.GetValidInputForInt("How many NFT's do you wish to mint?");

            if (CLIEngine.GetConfirmation("Do you wish to send the NFT to yourself after it is minted?"))
                request.SendToAvatarAfterMintingId = STAR.BeamedInAvatar.Id;
            else
            {
                Console.WriteLine("");
                int selection = CLIEngine.GetValidInputForInt("Do you wish to send the NFT using the users (1) Wallet Address, (2) Avatar Id, (3) Username or (4) Email? (Please enter 1, 2, 3 or 4)", true, 1, 4);

                switch (selection)
                {
                    case 1:
                        //Console.WriteLine("");
                        request.SendToAddressAfterMinting = CLIEngine.GetValidInput("What is the wallet address you want to send the NFT after it is minted?");
                        break;

                    case 2:
                        //Console.WriteLine("");
                        request.SendToAvatarAfterMintingId = CLIEngine.GetValidInputForGuid("What is the Id of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 3:
                        //Console.WriteLine("");
                        request.SendToAvatarAfterMintingUsername = CLIEngine.GetValidInput("What is the Username of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 4:
                        //Console.WriteLine("");
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

                request.WaitTillNFTSent = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been sent before continuing? If you select yes it will continue to attempt sending for X seconds (defined in next question). Default is Yes.");

                if (request.WaitTillNFTSent)
                {
                    Console.WriteLine("");
                    request.WaitForNFTToSendInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to send before timing out? (default is 60 seconds)");
                    request.AttemptToSendEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to send? (default is every 1 second)");
                }
            }
            else
                Console.WriteLine("");

            return request;
        }


        public async Task<IImportWeb3NFTRequest> GenerateImportNFTRequestAsync()
        {
            ImportWeb3NFTRequest request = new ImportWeb3NFTRequest();

            request.NFTTokenAddress = CLIEngine.GetValidInput("Please enter the token address of the NFT you wish to import: ");
            request.ImportedByByAvatarId = STAR.BeamedInAvatar.Id;
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


            request.MetaData = AddMetaData("NFT");
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

        public Dictionary<string, object> AddMetaData(string itemName)
        {
            Dictionary<string, object> metaData = new Dictionary<string, object>();

            if (CLIEngine.GetConfirmation($"Do you wish to add any metadata to this {itemName}?"))
            {
                metaData = AddMetaDataToNFT(metaData);
                bool metaDataDone = false;

                do
                {
                    if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
                        metaData = AddMetaDataToNFT(metaData);
                    else
                        metaDataDone = true;
                }
                while (!metaDataDone);
            }

            return metaData;
        }

        public Dictionary<string, object> ManageMetaData(Dictionary<string, object> metaData, string itemName)
        {
            if (metaData == null)
                metaData = new Dictionary<string, object>();

            bool done = false;

            while (!done)
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"Current {itemName} metadata:", false);

                if (metaData.Count == 0)
                    CLIEngine.ShowMessage("  None", false);
                else
                {
                    int i = 1;
                    foreach (var kv in metaData)
                    {
                        string displayValue = kv.Value is byte[]? "<binary>" : kv.Value?.ToString();
                        CLIEngine.ShowMessage($"  {i}. {kv.Key} = {displayValue}", false);
                        i++;
                    }
                }

                Console.WriteLine("");
                CLIEngine.ShowMessage("Choose an action: (A)dd, (E)dit, (D)elete, (Q)uit", false);
                string choice = CLIEngine.GetValidInput("Enter A, E, D or Q:").ToUpper();

                switch (choice)
                {
                    case "A":
                        metaData = AddMetaDataToNFT(metaData);
                        break;

                    case "E":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to edit.");
                            break;
                        }

                        int editIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to edit:", false, 1, metaData.Count);
                        string editKey = metaData.Keys.ElementAt(editIndex - 1);
                        object currentValue = metaData[editKey];

                        if (currentValue is byte[])
                        {
                            if (CLIEngine.GetConfirmation("This value is binary. Do you want to replace it with a file? (Y) or replace with text (N)?"))
                            {
                                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                                metaData[editKey] = File.ReadAllBytes(metaPath);
                            }
                            else
                            {
                                string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
                                if (newValue.ToLower() == "clear")
                                    metaData.Remove(editKey);
                                else
                                    metaData[editKey] = newValue;
                            }
                        }
                        else
                        {
                            if (CLIEngine.GetConfirmation("Do you want to set this value from a file? (Y) or enter text value (N)?"))
                            {
                                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                                metaData[editKey] = File.ReadAllBytes(metaPath);
                            }
                            else
                            {
                                string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
                                if (newValue.ToLower() == "clear")
                                    metaData.Remove(editKey);
                                else
                                    metaData[editKey] = newValue;
                            }
                        }

                        break;

                    case "D":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to delete.");
                            break;
                        }

                        int delIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to delete:", false, 1, metaData.Count);
                        string delKey = metaData.Keys.ElementAt(delIndex - 1);

                        if (CLIEngine.GetConfirmation($"Are you sure you want to delete metadata '{delKey}'?"))
                        {
                            metaData.Remove(delKey);
                            CLIEngine.ShowSuccessMessage($"Metadata '{delKey}' deleted.");
                        }
                        break;

                    case "Q":
                        done = true;
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Invalid choice. Please enter A, E, D or Q.");
                        break;
                }
            }

            return metaData;
        }


    }
}