using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class BillingProjectionReader(BillingReader inner, string mode) : ICosmicChainQueryIndexProjectionReader
    {
        public CosmicChainQuerySource Source => inner.Source;
        public CosmicQueryCapabilities IndexedRowCapabilities => inner.IndexedRowCapabilities;
        public CosmicProjectionCapabilities IndexedProjectionCapabilities => new(new[] {
            new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Selected HTTP must dispatch its native selected-column contract.");
        public async Task<OASISResult<CosmicChainIndexProjectionRows>> QueryProjectionAsync<T, TResult>(CosmicProjectionPlan<T, TResult> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            // Controlled transport fixture; native SQL is proved by SQLite/live gates.
            var result = await inner.QueryRowsAsync(plan.SourcePlan, observer, maximumResults, cancellationToken);
            if(result.IsError) return new() { IsError = true, ErrorCode = result.ErrorCode, Message = result.Message };
            return new(new(result.Result, result.Result.Rows.Select(row => new CosmicProjectionRow(
                mode == "projection-forged" ? "forged" : row.Name)))) { IsLoaded = true };
        }
    }

    [Fact]
    public void IndexedSelectedPreparationFreezesColumnsAndSeparatesRowIntent()
    {
        var fixture = Fixture("rows");
        var reader = new BillingProjectionReader(new BillingReader(source, fixture.Reader.Result, "rows"), "rows");
        var binding = new CqlIndexedV1Binding(reader, Moq.Mock.Of<ICosmicChainQuerySourceObserver>(), Tariffs(), position, 0,
            TimeSpan.FromMinutes(1), CosmicQueryReadAccess.OwnerOrPublic(actor).Result, Manager());
        var request = WebRequest();
        request.Columns = new[] { new CqlProjectionV1Column { Alias = "label", Field = "Name" } };
        var prepared = CqlIndexedV1Preparation.Prepare(actor, request, new BindingRegistry(binding));
        Assert.False(prepared.IsError, prepared.Message);
        Assert.NotNull(prepared.Result.ProjectionPlan);
        Assert.True(prepared.Result.Matches(request));
        var fingerprint = prepared.Result.RequestFingerprint;
        request.Columns[0].Alias = "changed";
        Assert.False(prepared.Result.Matches(request));
        Assert.Equal("label", prepared.Result.ProjectionPlan.Query.Columns.Single().Alias);
        request.Columns = null;
        Assert.False(prepared.Result.Matches(request));
        var row = CqlIndexedV1Preparation.Prepare(actor, request, new BindingRegistry(binding));
        Assert.False(row.IsError, row.Message);
        Assert.NotEqual(fingerprint, row.Result.RequestFingerprint);
        Assert.Null(row.Result.ProjectionPlan);
    }

    [Fact]
    public void IndexedColumnsAreCapturedBeforeTheServerResolverCallback()
    {
        var fixture = Fixture("rows");
        var reader = new BillingProjectionReader(new BillingReader(source, fixture.Reader.Result, "rows"), "rows");
        var binding = new CqlIndexedV1Binding(reader, Moq.Mock.Of<ICosmicChainQuerySourceObserver>(), Tariffs(), position, 0,
            TimeSpan.FromMinutes(1), CosmicQueryReadAccess.OwnerOrPublic(actor).Result, Manager());
        var request = WebRequest();
        request.Columns = new[] { new CqlProjectionV1Column { Alias = "label", Field = "Name" } };
        var registry = new BindingRegistry(binding, () => { request.Columns[0].Alias = "changed"; request.Columns[0].Field = "Id"; });
        var prepared = CqlIndexedV1Preparation.Prepare(actor, request, registry);
        Assert.False(prepared.IsError, prepared.Message);
        Assert.Equal("label", prepared.Result.ProjectionPlan.Query.Columns.Single().Alias);
        Assert.Equal("Name", prepared.Result.ProjectionPlan.Query.Columns.Single().Path.Segments.Single());
        Assert.False(prepared.Result.Matches(request));
        Assert.Equal(1, registry.Calls);
    }
}
