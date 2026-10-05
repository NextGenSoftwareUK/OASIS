//using NextGenSoftware.Utilities;
//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
//using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static async Task MintNFTAsync(object mintParams = null)
//        {
//            IMintNFTTransactionRequest request = await GenerateNFTRequestAsync();

//            CLIEngine.ShowWorkingMessage("Minting OASIS NFT...");
//            OASISResult<IWeb4NFTTransactionRespone> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);

//            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
//                CLIEngine.ShowSuccessMessage($"OASIS NFT Successfully Minted. {nftResult.Message} Transaction Result: {nftResult.Result.TransactionResult}, Id: {nftResult.Result.OASISNFT.Id}, Hash: {nftResult.Result.OASISNFT.Hash} Minted On: {nftResult.Result.OASISNFT.MintedOn}, Minted By Avatar Id: {nftResult.Result.OASISNFT.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.OASISNFT.MintedByAddress}.");
//            else
//            {
//                string msg = nftResult != null ? nftResult.Message : "";
//                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
//            }
//        }

//        public static async Task<OASISResult<IOASISGeoSpatialNFT>> MintGeoNFTAsync(object mintParams = null)
//        {
//            IMintNFTTransactionRequest request = await GenerateNFTRequestAsync();
//            IPlaceGeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);

//            CLIEngine.ShowWorkingMessage("Minting OASIS Geo-NFT...");
//            OASISResult<IOASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.MintAndPlaceGeoNFTAsync(new MintAndPlaceGeoSpatialNFTRequest()
//            {
//                Title = request.Title,
//                Description = request.Description,
//                MemoText = request.MemoText,
//                Image = request.Image,
//                ImageUrl = request.ImageUrl,
//                MintedByAvatarId = request.MintedByAvatarId,
//                MintWalletAddress = request.MintWalletAddress,
//                Thumbnail = request.Thumbnail,
//                ThumbnailUrl = request.ThumbnailUrl,
//                Price = request.Price,
//                Discount = request.Discount,
//                OnChainProvider = request.OnChainProvider,
//                OffChainProvider = request.OffChainProvider,
//                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
//                NumberToMint = request.NumberToMint,
//                MetaData = request.MetaData,
//                AllowOtherPlayersToAlsoCollect = geoRequest.AllowOtherPlayersToAlsoCollect,
//                PermSpawn = geoRequest.PermSpawn,
//                GlobalSpawnQuantity = geoRequest.GlobalSpawnQuantity,
//                PlayerSpawnQuantity = geoRequest.PlayerSpawnQuantity,
//                RespawnDurationInSeconds = geoRequest.RespawnDurationInSeconds,
//                Lat = geoRequest.Lat,
//                Long = geoRequest.Long,
//                Nft2DSprite = geoRequest.Nft2DSprite,
//                Nft3DSpriteURI = geoRequest.Nft3DSpriteURI,
//                Nft3DObject = geoRequest.Nft3DObject,
//                Nft3DObjectURI = geoRequest.Nft3DObjectURI
//            });

//            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
//                CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Minted. {nftResult.Message} Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
//            else
//            {
//                string msg = nftResult != null ? nftResult.Message : "";
//                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
//            }

//            return nftResult;
//        }

//        public static async Task PlaceGeoNFTAsync()
//        {
//            IPlaceGeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);
//            CLIEngine.ShowWorkingMessage("Creating OASIS Geo-NFT...");
//            OASISResult<IOASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.PlaceGeoNFTAsync(geoRequest);

//            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
//                CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Created. {nftResult.Message} OriginalOASISNFTId: {nftResult.Result.OriginalOASISNFTId}, Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
//            else
//            {
//                string msg = nftResult != null ? nftResult.Message : "";
//                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
//            }
//        }

//        public static async Task SendNFTAsync()
//        {
//            //string mintWalletAddress = CLIEngine.GetValidInput("What is the original mint address?");
//            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT from?");
//            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT to?");
//            string memoText = CLIEngine.GetValidInput("What is the memo text?");
//            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

//            CLIEngine.ShowWorkingMessage("Sending NFT...");

//            OASISResult<IWeb4NFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
//            {
//                FromWalletAddress = fromWalletAddress,
//                ToWalletAddress = toWalletAddress,
//                //MintWalletAddress = mintWalletAddress,
//                MemoText = memoText,
//                //Amount = amount,
//            });

//            if (response != null && response.Result != null && !response.IsError)
//                CLIEngine.ShowSuccessMessage($"NFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
//            else
//            {
//                string msg = response != null ? response.Message : "";
//                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
//            }
//        }

//        public static async Task ListAllGeoNFTsForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            CLIEngine.ShowWorkingMessage("Loading Geo-NFTs...");
//            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geonftsResult = await STAR.OASISAPI.NFTs.LoadAllGeoNFTsForAvatarAsync(STAR.BeamedInAvatar.Id);
//           // ListHolons<IOASISGeoSpatialNFT>(geonftsResult, "Geo-NFT", ShowGeoNFT(x) => { });
//            ListGeoNFTs(geonftsResult);
//        }

//        public static async Task ListAllGeoNFTsAsync(ProviderType providerType = ProviderType.Default)
//        {
//            CLIEngine.ShowWorkingMessage("Loading Geo-NFTs...");
//            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geonftsResult = await STAR.OASISAPI.NFTs.LoadAllGeoNFTsAsync();
//            ListGeoNFTs(geonftsResult);
//        }

//        public static async Task ListNFTsForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            CLIEngine.ShowWorkingMessage("Loading NFTs...");
//            OASISResult<IEnumerable<IOASISNFT>> nftsResult = await STAR.OASISAPI.NFTs.LoadAllNFTsForAvatarAsync(STAR.BeamedInAvatar.Id);
//            ListNFTs(nftsResult);
//        }

//        public static async Task ListAllNFTsAsync(ProviderType providerType = ProviderType.Default)
//        {
//            CLIEngine.ShowWorkingMessage("Loading NFTs...");
//            OASISResult<IEnumerable<IOASISNFT>> nftsResult = await STAR.OASISAPI.NFTs.LoadAllNFTsAsync();
//            ListNFTs(nftsResult);
//        }

//        public static async Task SearchNFTsAsync(string searchTerm, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            ListNFTs(await STAR.OASISAPI.NFTs.SearchNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar, providerType));
//        }

//        public static async Task SearchGeoNFTsAsync(string searchTerm, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            ListGeoNFTs(await STAR.OASISAPI.NFTs.SearchGeoNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar, providerType));
//        }

//        public static async Task ShowNFTAsync(Guid id = new Guid(), ProviderType providerType = ProviderType.Default)
//        {
//            if (id == Guid.Empty)
//                id = CLIEngine.GetValidInputForGuid("What is the GUID/ID to the NFT you wish to view?");

//            CLIEngine.ShowWorkingMessage("Loading NFT...");
//            OASISResult<IOASISNFT> nft = await STAR.OASISAPI.NFTs.LoadNftAsync(id);

//            if (nft != null && !nft.IsError && nft.Result != null)
//            {
//                CLIEngine.ShowDivider();
//                ShowNFT(nft.Result);
//            }
//            else
//                CLIEngine.ShowErrorMessage("No NFT Found.");
//        }

//        public static void ShowNFT(IOASISNFT nft)
//        {
//            string image = nft.Image != null ? "Yes" : "No";

//            CLIEngine.ShowMessage(string.Concat($"Title: ", !string.IsNullOrEmpty(nft.Title) ? nft.Title : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(nft.Description) ? nft.Description : "None"));
//            CLIEngine.ShowMessage($"Price: {nft.Price}");
//            CLIEngine.ShowMessage($"Discount: {nft.Discount}");
//            CLIEngine.ShowMessage(string.Concat($"MemoText: ", !string.IsNullOrEmpty(nft.MemoText) ? nft.MemoText : "None"));
//            CLIEngine.ShowMessage($"Id: {nft.Id}");
//            CLIEngine.ShowMessage(string.Concat($"Hash: ", !string.IsNullOrEmpty(nft.Hash) ? nft.Hash : "None"));
//            CLIEngine.ShowMessage($"MintedByAvatarId: {nft.MintedByAvatarId}");
//            CLIEngine.ShowMessage(string.Concat($"MintedByAddress: ", !string.IsNullOrEmpty(nft.MintedByAddress) ? nft.MintedByAddress : "None"));
//            CLIEngine.ShowMessage($"MintedOn: {nft.MintedOn}");
//            CLIEngine.ShowMessage($"OnChainProvider: {nft.OnChainProvider.Name}");
//            CLIEngine.ShowMessage($"OffChainProvider: {nft.OffChainProvider.Name}");
//            CLIEngine.ShowMessage(string.Concat($"URL: ", !string.IsNullOrEmpty(nft.URL) ? nft.URL : "None"));
//            CLIEngine.ShowMessage(string.Concat($"ImageUrl: ", !string.IsNullOrEmpty(nft.ImageUrl) ? nft.ImageUrl : "None"));
//            CLIEngine.ShowMessage(string.Concat("Image: ", nft.Image != null ? "Yes" : "No"));
//            CLIEngine.ShowMessage(string.Concat($"ThumbnailUrl: ", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None"));
//            CLIEngine.ShowMessage(string.Concat("Thumbnail: ", nft.Thumbnail != null ? "Yes" : "No"));

//            if (nft.MetaData.Count > 0)
//            {
//                CLIEngine.ShowMessage($"MetaData:");

//                foreach (string key in nft.MetaData.Keys)
//                    CLIEngine.ShowMessage($"          {key} = {nft.MetaData[key]}");
//            }
//            else
//                CLIEngine.ShowMessage($"MetaData: None");

//            CLIEngine.ShowDivider();
//        }

//        public static async Task ShowGeoNFTAsync(Guid id = new Guid(), ProviderType providerType = ProviderType.Default)
//        {
//            if (id == Guid.Empty)
//                id = CLIEngine.GetValidInputForGuid("What is the GUID/ID to the NFT you wish to view?");

//            CLIEngine.ShowWorkingMessage("Loading Geo-NFT...");
//            OASISResult<IOASISGeoSpatialNFT> nft = await STAR.OASISAPI.NFTs.LoadGeoNftAsync(id);

//            if (nft != null && !nft.IsError && nft.Result != null)
//            {
//                CLIEngine.ShowDivider();
//                ShowGeoNFT(nft.Result);
//            }
//            else
//                CLIEngine.ShowErrorMessage("No Geo-NFT Found.");
//        }

//        public static void ShowGeoNFT(IOASISGeoSpatialNFT nft)
//        {
//            string image = nft.Image != null ? "Yes" : "No";
//            string thumbnail = nft.Thumbnail != null ? "Yes" : "No";

//            CLIEngine.ShowMessage(string.Concat($"Title: ", !string.IsNullOrEmpty(nft.Title) ? nft.Title : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(nft.Description) ? nft.Description : "None"));
//            CLIEngine.ShowMessage($"Price: {nft.Price}");
//            CLIEngine.ShowMessage($"Discount: {nft.Discount}");
//            CLIEngine.ShowMessage(string.Concat($"MemoText: ", !string.IsNullOrEmpty(nft.MemoText) ? nft.MemoText : "None"));
//            CLIEngine.ShowMessage($"Id: {nft.Id}");
//            CLIEngine.ShowMessage(string.Concat($"Hash: ", !string.IsNullOrEmpty(nft.Hash) ? nft.Hash : "None"));
//            CLIEngine.ShowMessage($"MintedByAvatarId: {nft.MintedByAvatarId}");
//            CLIEngine.ShowMessage(string.Concat($"MintedByAddress: ", !string.IsNullOrEmpty(nft.MintedByAddress) ? nft.MintedByAddress : "None"));
//            CLIEngine.ShowMessage($"MintedOn: {nft.MintedOn}");
//            CLIEngine.ShowMessage($"OnChainProvider: {nft.OnChainProvider.Name}");
//            CLIEngine.ShowMessage($"OffChainProvider: {nft.OffChainProvider.Name}");
//            CLIEngine.ShowMessage(string.Concat($"URL: ", !string.IsNullOrEmpty(nft.URL) ? nft.URL : "None"));
//            CLIEngine.ShowMessage(string.Concat($"ImageUrl: ", !string.IsNullOrEmpty(nft.ImageUrl) ? nft.ImageUrl : "None"));
//            CLIEngine.ShowMessage(string.Concat("Image: ", nft.Image != null ? "Yes" : "No"));
//            CLIEngine.ShowMessage(string.Concat($"ThumbnailUrl: ", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None"));
//            CLIEngine.ShowMessage(string.Concat("Thumbnail: ", nft.Thumbnail != null ? "Yes" : "No"));
//            CLIEngine.ShowMessage($"Lat: {nft.Lat}");
//            CLIEngine.ShowMessage($"Long: {nft.Long}");
//            CLIEngine.ShowMessage($"PlacedByAvatarId: {nft.PlacedByAvatarId}");
//            CLIEngine.ShowMessage($"PlacedOn: {nft.PlacedOn}");
//            CLIEngine.ShowMessage($"GeoNFTMetaDataOffChainProvider: {nft.GeoNFTMetaDataOffChainProvider.Name}");
//            CLIEngine.ShowMessage($"PermSpawn: {nft.PermSpawn}");
//            CLIEngine.ShowMessage($"AllowOtherPlayersToAlsoCollect: {nft.AllowOtherPlayersToAlsoCollect}");
//            CLIEngine.ShowMessage($"GlobalSpawnQuantity: {nft.GlobalSpawnQuantity}");
//            CLIEngine.ShowMessage($"PlayerSpawnQuantity: {nft.PlayerSpawnQuantity}");
//            CLIEngine.ShowMessage($"RepawnDurationInSeconds: {nft.RepawnDurationInSeconds}");

//            if (nft.MetaData.Count > 0)
//            {
//                CLIEngine.ShowMessage($"MetaData:");

//                foreach (string key in nft.MetaData.Keys)
//                    CLIEngine.ShowMessage($"          {key} = {nft.MetaData[key]}");
//            }
//            else
//                CLIEngine.ShowMessage($"MetaData: None");

//            CLIEngine.ShowDivider();
//        }

//        private static Dictionary<string, object> AddMetaDataToNFT(Dictionary<string, object> metaData)
//        {
//            Console.WriteLine("");
//            string key = CLIEngine.GetValidInput("What is the key?");
//            string value = "";
//            byte[] metaFile = null;

//            if (CLIEngine.GetConfirmation("Is the value a file?"))
//            {
//                Console.WriteLine("");
//                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
//                metaFile = File.ReadAllBytes(metaPath);
//            }
//            else
//            {
//                Console.WriteLine("");
//                value = CLIEngine.GetValidInput("What is the value?");
//            }

//            if (metaFile != null)
//                metaData[key] = metaFile;
//            else
//                metaData[key] = value;

//            return metaData;
//        }

//        private static async Task<IMintNFTTransactionRequest> GenerateNFTRequestAsync()
//        {
//            string nft3dObjectPath = "";
//            byte[] nft3dObject = null;
//            Uri nft3dObjectURI = null;
//            string nft2dSpritePath = "";
//            byte[] nft2dSprite = null;
//            Uri nft2dSpriteURI = null;
//            byte[] imageLocal = null;
//            byte[] imageThumbnailLocal = null;
//            Uri imageURI = null;
//            Uri imageThumbnailURI = null;
//            string title = CLIEngine.GetValidInput("What is the NFT's title?");
//            string desc = CLIEngine.GetValidInput("What is the NFT's description?");
//            string memotext = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");
//            ProviderType offChainProvider = ProviderType.None;
//            NFTOffChainMetaType NFTOffchainMetaType = NFTOffChainMetaType.OASIS;
//            NFTStandardType NFTStandardType = NFTStandardType.Both;
//            Dictionary<string, object> metaData = new Dictionary<string, object>();

//            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT or input a URI to an online image? (Press Y for local or N for online)"))
//            {
//                Console.WriteLine("");
//                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT?");
//                imageLocal = File.ReadAllBytes(localImagePath);
//            }
//            else
//            {
//                Console.WriteLine("");
//                imageURI = await CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?");
//            }


//            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
//            {
//                Console.WriteLine("");
//                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT Thumbnail?");
//                imageThumbnailLocal = File.ReadAllBytes(localImagePath);
//            }
//            else
//            {
//                Console.WriteLine("");
//                imageThumbnailURI = await CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT Thumbnail?");
//            }

//            string mintWalletAddress = CLIEngine.GetValidInput("What is the mint wallet address?");
//            long price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");
//            long discount = CLIEngine.GetValidInputForLong("Is there any discount for the NFT? If so enter it now or leave blank. (This can always be changed later.)");

//            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType));
//            ProviderType onChainProvider = (ProviderType)onChainProviderObj;

//            bool storeMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
//            Console.WriteLine("");

//            if (!storeMetaDataOnChain)
//            {
//                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? IPFS, OASIS or Pinata? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
//                NFTOffchainMetaType = (NFTOffChainMetaType)offChainMetaDataTypeObj;

//                if (NFTOffchainMetaType == NFTOffChainMetaType.OASIS)
//                {
//                    object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
//                    offChainProvider = (ProviderType)offChainProviderObj;
//                }
//            }

//            if (onChainProvider != ProviderType.SolanaOASIS)
//            {
//                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT ERC standard do you wish to support? ERC721, ERC1155 or both?", typeof(NFTStandardType));
//                NFTStandardType = (NFTStandardType)nftStandardObj;
//            }
//            //else
//            //    NFTStandardType = NFTStandardType.Metaplex;

//            if (CLIEngine.GetConfirmation("Do you wish to add any metadata to this NFT?"))
//            {
//                metaData = AddMetaDataToNFT(metaData);
//                bool metaDataDone = false;

//                do
//                {
//                    if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
//                        metaData = AddMetaDataToNFT(metaData);
//                    else
//                        metaDataDone = true;
//                }
//                while (!metaDataDone);
//            }

//            Console.WriteLine("");
//            int numberToMint = CLIEngine.GetValidInputForInt("How many NFT's do you wish to mint?");

//            return new MintNFTTransactionRequest()
//            {
//                Title = title,
//                Description = desc,
//                MemoText = memotext,
//                Image = imageLocal,
//                ImageUrl = imageURI != null ? imageURI.AbsoluteUri : null,
//                MintedByAvatarId = STAR.BeamedInAvatar.Id,
//                MintWalletAddress = mintWalletAddress,
//                Thumbnail = imageThumbnailLocal,
//                ThumbnailUrl = imageThumbnailURI != null ? imageThumbnailURI.AbsoluteUri : null,
//                Price = price,
//                Discount = discount,
//                OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
//                OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
//                StoreNFTMetaDataOnChain = storeMetaDataOnChain,
//                NumberToMint = numberToMint,
//                MetaData = metaData
//            };
//        }

//        private static async Task<IPlaceGeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
//        {
//            Guid originalOASISNFTId = Guid.Empty;
//            ProviderType providerType = ProviderType.None;
//            ProviderType originalOffChainProviderType = ProviderType.All;
//            string nft3dObjectPath = "";
//            string nft2dSpritePath = "";
//            byte[] nft3dObject = null;
//            byte[] nft2dSprite = null;
//            Uri nft3dObjectURI = null;
//            Uri nft2dSpriteURI = null;
//            int globalSpawnQuanity = 0;
//            int respawnDurationInSeconds = 0;
//            int playerSpawnQuanity = 0;
//            bool allowOtherPlayersToAlsoCollect = false;

//            if (isExistingNFT)
//            {
//                originalOASISNFTId = CLIEngine.GetValidInputForGuid("What is the original OASIS NFT ID?");
//                providerType = (ProviderType)CLIEngine.GetValidInputForEnum("What provider would you like to store the Geo-NFT metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
//                originalOffChainProviderType = (ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType));
//            }

//            long nftLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your NFT to appear in Our World/AR World?");
//            long nftLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your NFT to appear in Our World/AR World?");

//            if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your NFT within Our World/AR World? Press Y for 3D or N for 2D."))
//            {
//                Console.WriteLine("");

//                if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
//                {
//                    Console.WriteLine("");
//                    nft3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");
//                    nft3dObject = File.ReadAllBytes(nft3dObjectPath);
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    nft3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");
//                }
//            }
//            else
//            {
//                Console.WriteLine("");

//                if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
//                {
//                    Console.WriteLine("");
//                    nft2dSpritePath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the NFT Image instead. You can always change this later.)");
//                    nft2dSprite = File.ReadAllBytes(nft2dSpritePath);
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    nft2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the NFT Image instead. You can always change this later.)");
//                }
//            }

//            bool permSpawn = CLIEngine.GetConfirmation("Will the NFT be permantly spawned allowing infinite number of players to collect as many times as they wish? If you select Y to this then the NFT will always be available with zero re-spawn time.");
//            Console.WriteLine("");

//            if (!permSpawn)
//            {
//                allowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars?");

//                if (allowOtherPlayersToAlsoCollect)
//                {
//                    Console.WriteLine("");
//                    globalSpawnQuanity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
//                    respawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
//                    playerSpawnQuanity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
//                }
//            }

//            return new PlaceGeoSpatialNFTRequest()
//            {
//                AllowOtherPlayersToAlsoCollect = allowOtherPlayersToAlsoCollect,
//                PermSpawn = permSpawn,
//                GlobalSpawnQuantity = globalSpawnQuanity,
//                PlayerSpawnQuantity = playerSpawnQuanity,
//                RespawnDurationInSeconds = respawnDurationInSeconds,
//                Lat = nftLat,
//                Long = nftLong,
//                Nft2DSprite = nft2dSprite,
//                Nft3DSpriteURI = nft2dSpriteURI != null ? nft2dSpriteURI.AbsoluteUri : "",
//                Nft3DObject = nft3dObject,
//                Nft3DObjectURI = nft3dObjectURI != null ? nft3dObjectURI.AbsoluteUri : "",
//                OriginalOASISNFTId = originalOASISNFTId,
//                ProviderType = providerType,
//                OriginalOASISNFTOffChainProviderType = originalOffChainProviderType,
//                PlacedByAvatarId = STAR.BeamedInAvatar.Id
//            };
//        }


//        private static void ListNFTs(OASISResult<IEnumerable<IOASISNFT>> nftsResult)
//        {
//            if (nftsResult != null)
//            {
//                if (!nftsResult.IsError)
//                {
//                    if (nftsResult.Result != null && nftsResult.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (nftsResult.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{nftsResult.Result.Count()} NFT Found:");
//                        else
//                            CLIEngine.ShowMessage($"{nftsResult.Result.Count()} NFT's' Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IOASISGeoSpatialNFT geoNFT in nftsResult.Result)
//                            ShowGeoNFT(geoNFT);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No NFT's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading NFT's. Reason: {nftsResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading NFT's.");
//        }

//        private static void ListGeoNFTs(OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geonftsResult)
//        {
//            if (geonftsResult != null)
//            {
//                if (!geonftsResult.IsError)
//                {
//                    if (geonftsResult.Result != null && geonftsResult.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (geonftsResult.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{geonftsResult.Result.Count()} Geo-NFT Found:");
//                        else
//                            CLIEngine.ShowMessage($"{geonftsResult.Result.Count()} Geo-NFT's' Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IOASISGeoSpatialNFT geoNFT in geonftsResult.Result)
//                            ShowGeoNFT(geoNFT);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No Geo-NFT's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading Geo-NFT's. Reason: {geonftsResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading Geo-NFT's.");
//        }
//    }
//}

