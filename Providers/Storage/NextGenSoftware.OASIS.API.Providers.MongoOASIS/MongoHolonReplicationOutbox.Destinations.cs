using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationDestinationBinding>> ReadDestinationBindingAsync(
        ProviderType targetAuthority, CancellationToken cancellationToken = default)
        => Observe(async session => {
            CosmicConcurrency.ValidateAuthority(targetAuthority);
            if (_expectedSource == null)
                return new OASISResult<HolonReplicationDestinationBinding> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_DESTINATION_CATALOGUE_SOURCE_REQUIRED",
                    Message = "A native-bound source is required to read approved destinations." };
            if (targetAuthority == _expectedSource.Authority)
                throw new ArgumentException("A destination must use a different authority.", nameof(targetAuthority));
            var known = await MongoDestinationBindings.ReadAsync(_database, session, _expectedSource,
                targetAuthority, cancellationToken).ConfigureAwait(false);
            return new OASISResult<HolonReplicationDestinationBinding>(known.Binding);
        }, cancellationToken);
}
