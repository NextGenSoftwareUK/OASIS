using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlCosmosWeb4AdmissionTests
{
    [Fact]
    public void CosmosReadProfileDoesNotExpandOtherRouteEnrollment()
    {
        Assert.Equal(ProviderType.AzureCosmosDBOASIS,
            CqlV1Compiler.RequestedProvider("AzureCosmosDBOASIS", includeCloudReadProfile: true));
        Assert.True(CqlV1Compiler.SupportedReadProfile(ProviderType.AzureCosmosDBOASIS));
        Assert.False(CqlV1Compiler.Supported(ProviderType.AzureCosmosDBOASIS));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider("AzureCosmosDBOASIS"));
    }

    [Fact]
    public void CosmosWireSourceRetainsActorRestrictionBeforePaging()
    {
        var actor = Guid.NewGuid();
        var request = new CqlV1Request
        {
            Provider = "AzureCosmosDBOASIS", OrderBy = "Id", Skip = 1, Take = 3,
            Filters = new[]
            {
                new CqlV1Filter
                {
                    MetadataPath = new[] { "location", "city" }, Scalar = CqlV1Scalar.String,
                    Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("London")
                }
            }
        };
        var selected = CqlV1Compiler.RequestedProvider(request.Provider, includeCloudReadProfile: true);
        var query = CqlV1Compiler.Compile(request, actor, selected);
        Assert.Equal(actor, query.ReadAccess.ActorId);
        Assert.Equal(1, query.Offset);
        Assert.Equal(3, query.Limit);
        Assert.Equal(ProviderType.AzureCosmosDBOASIS, Assert.Single(query.Scope.Providers));
        Assert.False(query.ReadAccess.CanRead(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() }));
        var business = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>());
        Assert.Equal(new[] { "location", "city" }, business.Path.Segments);
        Assert.Equal("London", business.Literal.Value);
    }

    [Theory]
    [InlineData("azurecosmosdboasis")]
    [InlineData("All")]
    [InlineData("Default")]
    [InlineData("None")]
    [InlineData("solanaoasis")]
    public void CloudReadEnrollmentDoesNotGrantOtherIdentities(string name)
        => Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider(name, includeCloudReadProfile: true));
}
