using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Theory]
    [InlineData("saturated")]
    [InlineData("duplicate")]
    [InlineData("emptyIdentity")]
    [InlineData("foreignIdentity")]
    public async Task MixedSnapshotAuthorityRejectsIncompleteIdentityProofBeforeReturningRows(string invalid)
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var id = fixture.Row.Id;
        var snapshotRows = invalid == "saturated"
            ? Enumerable.Range(0, 100).Select(_ => new CosmicProjectionRow(Guid.NewGuid())).ToArray()
            : invalid == "duplicate" ? new[] { new CosmicProjectionRow(id), new CosmicProjectionRow(id) }
            : new[] { new CosmicProjectionRow(invalid == "foreignIdentity" ? Guid.NewGuid() : Guid.Empty) };
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Array.Empty<CosmicProjectionRow>(), snapshotRows);
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.Equal(invalid == "saturated" ? "CQL_VIRTUAL_SOURCE_LIMIT" : "CQL_PROVIDER_RESULT_INVALID", Code(response));
        Assert.Equal(1, source.VirtualCalls);
        Assert.Equal(2, admitted.Receipts.Count);
    }
}
