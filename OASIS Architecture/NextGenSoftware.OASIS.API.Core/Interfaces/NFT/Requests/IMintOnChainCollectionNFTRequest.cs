namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IMintOnChainCollectionNFTRequest : IMintWeb4NFTRequest
    {
        ulong InitialSize { get; set; }
        bool WaitTillCollectionSizeSet { get; set; }
        int WaitForCollectionSizeToBeSetInSeconds { get; set; }
        int AttemptToSetCollectionSizeEveryXSeconds { get; set; }
    }
}
