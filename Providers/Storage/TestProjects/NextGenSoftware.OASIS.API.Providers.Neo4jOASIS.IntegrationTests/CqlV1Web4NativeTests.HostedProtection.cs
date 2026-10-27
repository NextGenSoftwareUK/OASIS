extern alias ShippingWeb4;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;
using ShippingPolicy = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic.ICosmicOperationRecoveryPolicy;
using ShippingGrant = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic.CosmicOperationRecoveryGrant;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Synthetic local fixture authorization, never installed in a real host.
    private sealed class ShippingProtection(ProviderType expected) : ShippingPolicy
    {
        public OASISResult<ShippingGrant> Authorize(Guid actor, ProviderType authority)
        {
            if (actor == Guid.Empty || authority != expected)
                return new() { IsError = true, ErrorCode = "COSMIC_RECOVERY_FORBIDDEN" };
            return new(new ShippingGrant(actor, authority,
                _ => new HolonMetadataEncryptionPolicy("owned-http-" + actor.ToString("N") + "-" + authority,
                    "http-fixture-key-v1", new RecoveryPreparationKeys()), new[] { typeof(Holon) }));
        }
    }
}
