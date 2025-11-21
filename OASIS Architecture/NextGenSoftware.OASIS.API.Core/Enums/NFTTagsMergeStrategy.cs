namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum NFTTagsMergeStrategy
    {
        Merge, //If the tag already exists in the parent WEB4 OASIS NFT tags then keep the existing value and do not overwrite it with the value from the Web3NFT tag.
        MergeAndOverwrite, //If the tag already exists in the parent WEB4 OASIS NFT tags then overwrite it with the value from the Web3NFT tag.
        Replace //Completely replace the parent WEB4 OASIS NFT tags with the Web3NFT tags.
    }
}
