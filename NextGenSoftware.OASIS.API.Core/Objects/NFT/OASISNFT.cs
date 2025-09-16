using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class OASISNFT : IOASISNFT
    {

        public string SendToAddressAfterMinting { get; set; }
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendNFTTransactionHash { get; set; }
        //public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public Guid MintedByAvatarId { get; set; }
        public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the NFT.
        public string NFTTokenAddress { get; set; } //The address of the actual minted NFT on the chain.
        public string UpdateAuthority { get; set; }
        //public string MintAddress { get; set; } //TODO: What is MintAddress?! How is it different to MintByAddress?!
        
        public string Symbol { get; set; }
        public uint SellerFeeBasisPoints { get; set; }  //     Seller cut
        public Guid Id { get; set; }
        
        public DateTime MintedOn { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MintTransactionHash { get; set; }
        public string JSONMetaData { get; set; }
        public string JSONMetaDataURL { get; set; }
        public Guid JSONMetaDataURLHolonId { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        //public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public string MemoText { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();


        /// <summary>
        /// The Blockchain to store the token on.
        /// </summary>
        public EnumValue<ProviderType> OnChainProvider { get; set; }

        /// <summary>
        /// Where the meta data is stored for the NFT (JSON Meta file and associated media etc) - For example HoloOASIS or IPFSOASIS etc.
        /// </summary>
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }

       


        //TODO: May add some or all of these later (taken from SOLNET Metaplex code)...
        //May be able to store some in metadata but some important ones which are common to all chains should go here...

        //       animation_url

        ////
        //// Summary:
        ////     metadata public key
        //public PublicKey metadataKey;

        //       //
        //       // Summary:
        //       //     update authority key
        //       public PublicKey updateAuthority;

        //       public List<Attribute> attributes { get; set; }
        //       public Collection collection { get; set; }

        //       //     Metadata token content receipt
        //       [JsonProperty("properties")]
        //       public Properties properties { get; set; }


        //       public class Properties
        //       {
        //           //
        //           // Summary:
        //           //     Files linked to token. Core link between the token and its content
        //           [JsonProperty("files")]
        //           public List<FileType> files { get; set; }

        //           //
        //           // Summary:
        //           //     Creators of the token and content. Should always be signed
        //           [JsonProperty("creators")]
        //           public List<Creator> creators { get; set; }
        //       }




        //       //
        //       // Summary:
        //       //     JSON class for the Properties FileType object
        //       public class FileType
        //       {
        //           //
        //           // Summary:
        //           //     Offsite file URI link
        //           [JsonProperty("uri")]
        //           public string uri { get; set; }

        //           //
        //           // Summary:
        //           //     File type used to know how to render the content
        //           [JsonProperty("type")]
        //           public string type { get; set; }
        //       }


        //       //
        //       // Summary:
        //       //     JSON class for the Creator object
        //       public class Creator
        //       {
        //           //
        //           // Summary:
        //           //     Creator account address
        //           [JsonProperty("address")]
        //           public string address { get; set; }

        //           //
        //           // Summary:
        //           //     Creators share
        //           [JsonProperty("share")]
        //           public int share { get; set; }
        //       }
    }
}