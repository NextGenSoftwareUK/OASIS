using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class CreateCollectionNFTRequest : MintWeb4NFTRequest, ICreateCollectionNFTRequest
    {
        public ulong InitialSize { get; set; } = 0;
        public bool WaitTillCollectionSizeSet { get; set; } = true;
        public int WaitForCollectionSizeToBeSetInSeconds { get; set; } = 60;
        public int AttemptToSetCollectionSizeEveryXSeconds { get; set; } = 1;
    }
}
