using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task AnnotatedScalarMemberPredicateUsesSharedCompilerAndNativeMetadata()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var selected=new Holon{Id=Guid.NewGuid(),Name="annotated scalar selected",MetaData=new(){["Rating"]=7,["Name"]="forged"}};
        var excluded=new Holon{Id=Guid.NewGuid(),Name="annotated scalar excluded",MetaData=new(){["Rating"]="7"}};
        (await provider.SaveHolonsAsync(new[]{selected,excluded},saveChildren:false)).IsError.Should().BeFalse();
        ICosmicQueryStorageProvider adapter=provider;
        var built=CosmicQuery<InvalidMetadataRow>.Create().Where(row=>row.Rating>=5);built.IsError.Should().BeFalse(built.Message);
        var plan=CosmicQueryPlanner.Plan(built.Result,ProviderType.SQLLiteDBOASIS,new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var manager=CosmicManager(provider,true);var result=await manager.ExecuteQueryAsync(plan.Result,new[]{adapter});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var row=result.Result.Providers.Single().Rows.Single();row.Id.Should().Be(selected.Id);row.Rating.Should().Be(7);
        row.Name.Should().Be(selected.Name);row.HasHolonChanged().Should().BeFalse();
        var version=row.VersionId;
        var noOp=await manager.SaveHolonAsync(row,Guid.NewGuid(),saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        noOp.IsError.Should().BeFalse(noOp.Message+noOp.Exception);noOp.IsSaved.Should().BeFalse();row.VersionId.Should().Be(version);
    }
}
