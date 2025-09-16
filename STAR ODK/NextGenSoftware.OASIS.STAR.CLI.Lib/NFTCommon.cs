using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;

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

        public async Task<IMintNFTTransactionRequest> GenerateNFTRequestAsync()
        {
            string nft3dObjectPath = "";
            byte[] nft3dObject = null;
            Uri nft3dObjectURI = null;
            string nft2dSpritePath = "";
            byte[] nft2dSprite = null;
            Uri nft2dSpriteURI = null;
            byte[] imageLocal = null;
            byte[] imageThumbnailLocal = null;
            Uri imageURI = null;
            Uri imageThumbnailURI = null;
            string title = CLIEngine.GetValidInput("What is the NFT's title?");
            string desc = CLIEngine.GetValidInput("What is the NFT's description?");
            string memotext = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");
            ProviderType offChainProvider = ProviderType.None;
            NFTOffChainMetaType NFTOffchainMetaType = NFTOffChainMetaType.OASIS;
            NFTStandardType NFTStandardType = NFTStandardType.ERC1155;
            Dictionary<string, object> metaData = new Dictionary<string, object>();
            long discount = 0;

            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT?");
                imageLocal = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                imageURI = await CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?");
            }


            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT Thumbnail?");
                imageThumbnailLocal = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                imageThumbnailURI = await CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT Thumbnail?");
            }

            string mintWalletAddress = CLIEngine.GetValidInput("What is the mint wallet address?");
            long price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");

            if (CLIEngine.GetConfirmation("Is there any discount for the NFT? (This can always be changed later.)"))
            {
                Console.WriteLine("");
                discount = CLIEngine.GetValidInputForLong("What is the discount?");
            }
            else
                Console.WriteLine("");

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType));
            ProviderType onChainProvider = (ProviderType)onChainProviderObj;

            bool storeMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            if (!storeMetaDataOnChain)
            {
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? IPFS, OASIS or Pinata? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
                NFTOffchainMetaType = (NFTOffChainMetaType)offChainMetaDataTypeObj;

                if (NFTOffchainMetaType == NFTOffChainMetaType.OASIS)
                {
                    object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
                    offChainProvider = (ProviderType)offChainProviderObj;
                }
            }

            if (onChainProvider != ProviderType.SolanaOASIS)
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT ERC standard do you wish to support? ERC721, ERC1155 or both?", typeof(NFTStandardType));
                NFTStandardType = (NFTStandardType)nftStandardObj;
            }
            //else
            //    NFTStandardType = NFTStandardType.Metaplex;

            if (CLIEngine.GetConfirmation("Do you wish to add any metadata to this NFT?"))
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

            Console.WriteLine("");
            int numberToMint = CLIEngine.GetValidInputForInt("How many NFT's do you wish to mint?");

            return new MintNFTTransactionRequest()
            {
                Title = title,
                Description = desc,
                MemoText = memotext,
                Image = imageLocal,
                ImageUrl = imageURI != null ? imageURI.AbsoluteUri : null,
                MintedByAvatarId = STAR.BeamedInAvatar.Id,
                SendToAddressAfterMinting = mintWalletAddress,
                Thumbnail = imageThumbnailLocal,
                ThumbnailUrl = imageThumbnailURI != null ? imageThumbnailURI.AbsoluteUri : null,
                Price = price,
                Discount = discount,
                OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
                StoreNFTMetaDataOnChain = storeMetaDataOnChain,
                NumberToMint = numberToMint,
                MetaData = metaData
            };
        }
    }
}