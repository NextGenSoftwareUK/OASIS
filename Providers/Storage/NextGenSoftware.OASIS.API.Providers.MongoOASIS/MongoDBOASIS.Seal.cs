using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IHolonSealVerifier
{
    /// <summary>Walks every stored version of the holon (archive plus current) and checks the opt-in seal chain.</summary>
    public async Task<HolonSealVerification> VerifyHolonSealAsync(Guid holonId, IHolonSealPolicy policy)
    {
        if (_holonRepository == null)
            return new HolonSealVerification { HolonId = holonId, Status = HolonSealStatus.Unavailable, Message = "The Mongo provider is not activated." };
        var document = await _holonRepository.GetRawHolonDocumentAsync(holonId).ConfigureAwait(false);
        if (document == null)
            return new HolonSealVerification { HolonId = holonId, Status = HolonSealStatus.Unavailable, Message = "Holon not found." };
        return HolonSealing.VerifyChain(holonId, MongoHolonSeal.Entries(document), policy);
    }
}
