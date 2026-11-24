using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

public partial class InfluxDBOASIS : ICosmicReadOnlyQueryActivation
{
    private readonly Guid _queryProbeId;

    /// <summary>A read-only hosted CQL source: a restricted reader token and an existing public probe record.</summary>
    public InfluxDBOASIS(string url, string readerToken, string org, string bucket, Guid queryProbeId)
        : this(url, readerToken, org, bucket)
    {
        if (queryProbeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
    }

    /// <summary>
    /// Read-only activation: never creates buckets (a reader token cannot), only proves the reader can
    /// natively read the probe as one complete live record, then admits CQL queries.
    /// </summary>
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        if (_queryProbeId == Guid.Empty)
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_SOURCE_UNCONFIGURED",
                Message = "This InfluxDB provider was not configured as a read-only query source." };
        try
        {
            var found = 0;
            await foreach (var row in ReadNativeCqlAsync(new List<Guid> { _queryProbeId }, cancellationToken).WithCancellation(cancellationToken))
            {
                if (!row.IsComplete || row.IsDeleted || !Guid.TryParseExact(row.Key, "D", out var id) || id != _queryProbeId)
                    return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_SOURCE_PROBE_INVALID",
                        Message = "The InfluxDB probe is not one complete live record." };
                found++;
            }
            if (found != 1)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_SOURCE_PROBE_MISSING",
                    Message = "The InfluxDB reader cannot read exactly one probe record." };
            IsProviderActivated = true;
            return new OASISResult<bool>(true) { Message = "InfluxDB read-only query source verified." };
        }
        catch (Exception cause)
        {
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The InfluxDB query source could not be verified.", Exception = cause };
        }
    }
}
