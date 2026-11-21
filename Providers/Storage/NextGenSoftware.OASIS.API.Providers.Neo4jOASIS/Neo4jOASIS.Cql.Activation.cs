using System;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicReadOnlyQueryActivation
{
    private volatile bool _querySourceActivated;
    public bool IsQuerySourceActivated => _querySourceActivated;

    /// <summary>Verifies existing projections without provisioning schema or repairing records.</summary>
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _querySourceActivated = false;
            _driver ??= _driverFactory();
            cancellationToken.ThrowIfCancellationRequested();
            await _driver.VerifyConnectivityAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            bool requiresUpgrade;
            using (var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read)))
            using (var transaction = await session.BeginTransactionAsync(config =>
                config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false))
            {
            var invalid = await (await transaction.RunAsync(
                "MATCH (h:Holon) OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
                + "WITH h,collect(p) AS projections WHERE size(projections)<>1 "
                + "OR any(p IN projections WHERE coalesce(p.SchemaVersion,0)<>$schema "
                + "OR coalesce(p.ProviderRevisionId,'')<>coalesce(h.ProviderRevisionId,'')) "
                + "RETURN 1 AS invalid LIMIT 1", new { schema = NeoCqlProjection.SchemaVersion })
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync().ConfigureAwait(false);
            requiresUpgrade = invalid.Count != 0;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (requiresUpgrade)
                return new OASISResult<bool>(false)
                {
                    IsError = true, ErrorCount = 1, ErrorCode = "CQL_SCHEMA_UPGRADE_REQUIRED",
                    Message = "Neo4j CQL projections require an explicit storage upgrade; read-only activation made no repairs."
                };
            _querySourceActivated = true;
            return new OASISResult<bool>(true);
        }
        catch (Exception failure)
        {
            _querySourceActivated = false;
            return new OASISResult<bool>(false)
            {
                IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                Message = "Neo4j read-only source verification failed.", Exception = failure
            };
        }
        finally { _lifecycleGate.Release(); }
    }
}
