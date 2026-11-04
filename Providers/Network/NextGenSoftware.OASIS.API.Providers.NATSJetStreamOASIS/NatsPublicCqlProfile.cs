using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS;

/// <summary>Explicit public records in an existing JetStream bucket.</summary>
public sealed class NatsPublicCqlProfile : NATSJetStreamOASIS, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly HashSet<Guid> catalogue;
    private readonly Guid owner;
    private bool disposed;

    public NatsPublicCqlProfile(string url, string bucket, Guid owner, IEnumerable<Guid> ids)
        : base(url, bucket, readOnly: true)
    {
        var selected = ids.Take(101).ToArray();
        catalogue = new(selected);
        if (owner == Guid.Empty || selected.Length is < 1 or > 100 || catalogue.Contains(Guid.Empty) ||
            catalogue.Count != selected.Length)
            throw new ArgumentException("One to 100 distinct public NATS record identities and an owner are required.");
        this.owner = owner;
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await Backend.VerifyAsync(cancellationToken);
            foreach (var id in catalogue)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = await LoadHolonAsync(id, loadChildren: false);
                if (row.IsError || row.Result == null || !IsPublicRecord(row.Result))
                    throw new InvalidOperationException("An enrolled NATS row is missing or has changed visibility.", row.Exception);
            }
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            await DeActivateProviderAsync();
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "NATS public profile activation failed.", Exception = error };
        }
    }

    public override async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default)
    {
        var selected = BoundedIdentityCqlCompiler.Compile(plan?.Query, QueryProviderType, QueryProviderType);
        if (selected.IsError)
            return Failure(selected.ErrorCode, selected.Message);
        if (selected.Result.Any(id => !catalogue.Contains(id)))
            return Failure("CQL_INVALID_SOURCE_KEY", "Only enrolled NATS identities are available.");
        var result = await base.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken);
        return !result.IsError && result.Result.Any(row => !IsPublicRecord(row))
            ? Failure("CQL_SOURCE_VISIBILITY_CHANGED", "An enrolled NATS row is no longer public.") : result;
    }

    private bool IsPublicRecord(IHolon row) => row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == owner;

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DeActivateProviderAsync().GetAwaiter().GetResult();
    }
}
