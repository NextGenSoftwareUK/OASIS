using Solnet.Metaplex.NFT.Library;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses
{
    public sealed class NftCreatorMedataResult
    {
        public string PublicKey { get; set; }
        public bool Verified { get; set; }
        public byte Share { get; set; }

        public NftCreatorMedataResult(Creator creator)
        {
            if (creator == null)
                return;

            PublicKey = creator.key;
            Verified = creator.verified;
            Share = creator.share;
        }
    }
}