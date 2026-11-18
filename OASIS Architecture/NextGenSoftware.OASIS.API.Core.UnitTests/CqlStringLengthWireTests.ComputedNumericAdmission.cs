using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlStringLengthWireTests_ComputedNumericAdmission
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void NativeReductionRequiresItsOwnInputFunctionAndExactOutputDomain(
        bool numericFunction, bool wrongOutputDomain, bool accepted)
    {
        var inputReads = new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        var projectedReads = inputReads.WithFunctions(CosmicProjectionFunction.Utf16Length);
        var reductionReads = numericFunction ? projectedReads : inputReads;
        var reduction = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reductionReads,
                new[] { wrongOutputDomain ? typeof(long) : typeof(int) })
        });
        var composition = new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.None, CosmicQueryFeatures.None,
            Array.Empty<CosmicQueryMetadataCapability>(), Array.Empty<Type>(), 32,
            supportsAll: false, supportsCardinality: false, numericReductions: reduction);
        var query = CosmicQuery<Holon>.Create().Select(row => row.Name.Length).Result;
        var plan = CosmicProjectedNumericPlanner.Plan<Holon, int, int>(query, 0, CosmicScalarOperation.Sum,
            ProviderType.MongoDBOASIS, new[]
            {
                new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS,
                    new CosmicQueryCapabilities(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>()),
                    projectedReads, composition)
            });
        if (!accepted)
            Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", plan.ErrorCode);
        else
        {
            Assert.False(plan.IsError, plan.Message);
            Assert.Equal(typeof(int), plan.Result.Scalar.Selector.ValueType);
            Assert.Equal(typeof(string), plan.Result.Composition.Query.Columns[0].Path.ValueType);
            Assert.Equal(CosmicProjectionFunction.Utf16Length, plan.Result.Composition.Query.Columns[0].Function);
        }
        Assert.DoesNotContain(CosmicProjectionFunction.Utf16Length, inputReads.Functions);
    }
}
