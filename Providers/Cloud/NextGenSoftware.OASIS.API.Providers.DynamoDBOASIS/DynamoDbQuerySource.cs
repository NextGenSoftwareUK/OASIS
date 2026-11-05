using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

/// <summary>Dedicated DynamoDB teaching source using the AWS SDK regional endpoint.</summary>
public sealed class DynamoDbQuerySource : DynamoDBOASIS, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid _probeId;

    public DynamoDbQuerySource(string region, Guid probeId, string accessKey, string secretKey, string? sessionToken)
        : base(accessKey, secretKey, ValidateSource(region, probeId), null, sessionToken,
            useDefaultCredentials: false, requireRegionalEndpoint: true)
        => _probeId = probeId;

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        if (ProviderType.Value != Core.Enums.ProviderType.DynamoDBOASIS)
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The DynamoDB teaching source requires its declared authority."
            });
        }
        return VerifyPublicQueryProbeAsync(_probeId, cancellationToken);
    }

    public void Dispose() => DeActivateProviderAsync().GetAwaiter().GetResult();

    private static string ValidateSource(string region, Guid probeId)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        return region;
    }
}
