using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    public static IEnumerable<object[]> NonfiniteNumericHttpCases()
    {
        foreach (var single in new[] { false, true })
        foreach (var operation in new[] { "Sum", "Average" })
        foreach (var sample in new[] { 0, 1, 2 })
            yield return new object[] { single, operation, sample };
    }

    [Theory]
    [MemberData(nameof(NonfiniteNumericHttpCases))]
    public async Task FloatingArithmeticCannotPublishNonfiniteProviderResults(bool single, string operation, int sample)
    {
        var provider = ProviderType.MongoDBOASIS;
        var source = new Source(provider);
        var adapter = ScalarAdapter(source, provider);
        var domain = single ? typeof(float) : typeof(double);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { domain });
        var numericOperation = operation == "Sum" ? CosmicScalarOperation.Sum : CosmicScalarOperation.Average;
        adapter.SetupGet(a => a.ScalarCapabilities).Returns(new CosmicScalarCapabilities(
            Array.Empty<CosmicScalarOperation>(), new[] { new CosmicScalarSelectionCapability(numericOperation, reads, new[] { domain }) }));
        if (single)
        {
            var value = new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }[sample];
            adapter.Setup(a => a.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, float>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OASISResult<float>(value) { IsLoaded = true });
        }
        else
        {
            var value = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }[sample];
            adapter.Setup(a => a.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, double>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OASISResult<double>(value) { IsLoaded = true });
        }
        var admitted = Admission(provider);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request
            {
                Operation = operation,
                Selection = new() { MetadataPath = new[] { "reading" }, ValueType = single ? "Single" : "Double" }
            }, source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(response));
        Assert.IsType<ObjectResult>(response);
        Assert.Equal(1, source.Managers);
    }
}
