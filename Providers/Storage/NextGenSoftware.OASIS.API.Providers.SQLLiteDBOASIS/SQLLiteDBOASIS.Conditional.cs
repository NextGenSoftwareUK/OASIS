using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IConditionalHolonDeltaStorageProvider, IAtomicHolonReplicationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment)
        => _holonRepository.ExecuteReplicatingHolonWrite(request, enrollment, CancellationToken.None);

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_holonRepository.ExecuteReplicatingHolonWrite(request, enrollment, cancellationToken));
    }

    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => _holonRepository.ExecuteConditionalHolonWrite(request);

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
        => _holonRepository.ExecuteConditionalHolonWriteAsync(request, cancellationToken);
}
