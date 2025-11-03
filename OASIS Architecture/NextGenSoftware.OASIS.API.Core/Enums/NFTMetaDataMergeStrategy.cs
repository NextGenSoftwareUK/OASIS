namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum NFTMetaDataMergeStrategy
    {
        Merge, //If the key already exists in the parent WEB4 OASIS NFT meta data then keep the existing value and do not overwrite it with the value from the Web3NFT meta data.
        MergeAndOverwrite, //If the key already exists in the parent WEB4 OASIS NFT meta data then overwrite it with the value from the Web3NFT meta data.
        Replace //Completely replace the parent WEB4 OASIS NFT meta data with the Web3NFT meta data.
    }
}
