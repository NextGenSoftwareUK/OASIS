using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationDestinationBinding>> ReadDestinationBindingAsync(
        ProviderType targetAuthority, CancellationToken cancellationToken = default)
        => Execute(async tx => {
            CosmicConcurrency.ValidateAuthority(targetAuthority);
            if (_expectedSource == null)
                return (new OASISResult<HolonReplicationDestinationBinding> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_DESTINATION_CATALOGUE_SOURCE_REQUIRED",
                    Message = "A native-bound source is required to read approved destinations." }, false);
            if (targetAuthority == _expectedSource.Authority)
                throw new ArgumentException("A destination must use a different authority.", nameof(targetAuthority));
            var known = await NeoDestinationBindings.ReadAsync(tx, _expectedSource, targetAuthority).ConfigureAwait(false);
            return (new OASISResult<HolonReplicationDestinationBinding>(known.Binding), false);
        }, () => { }, cancellationToken);
}
