using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task OrdinaryAdmissionDoesNotDiscardPreexistingPendingContextWork()
    {
        using(var setup=new SQLLiteDBOASIS(TestConnectionString))(await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var context=new DataContext(TestConnectionString);
        var source=new Holon{Id=Guid.NewGuid(),Name="submitted"};
        var pending=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.HolonModel{Id=source.Id.ToString(),Name="preexisting pending"};
        context.Holons.Add(pending);
        var repository=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.HolonRepository(context);
        var result=repository.SaveHolon(source);
        result.IsError.Should().BeTrue();context.Entry(pending).State.Should().Be(EntityState.Added);
        pending.Name.Should().Be("preexisting pending");
        using var independent=new DataContext(TestConnectionString);independent.Holons.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task OrdinaryGraphSavesArchiveAndRollbackWithinOneTransaction(bool synchronous,bool batch)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var creator=Guid.NewGuid();
        var root=new Holon{Name="before",Version=1,CreatedByAvatarId=creator,CreatedDate=DateTime.UtcNow};
        var child=new Holon{Name="child-before",Version=1};root.Children.Add(child);
        async Task<OASISResult<IEnumerable<IHolon>>> Save()
        {
            if(batch)return synchronous ? provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
            var single=synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            var result=NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(single);
            if(single.Result!=null)result.Result=new[]{single.Result};return result;
        }
        var first=await Save();first.IsError.Should().BeFalse(first.Message+first.Exception);
        root.Name="after";root.Version=2;child.Name="child-after";child.Version=2;
        var second=await Save();second.IsError.Should().BeFalse(second.Message+second.Exception);
        second.Result.Single().CreatedByAvatarId.Should().Be(creator);
        provider.IsVersionControlEnabled=false;
        var old=await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);
        old.IsError.Should().BeFalse(old.Message+old.Exception);old.Result.Name.Should().Be("before");
        provider.IsVersionControlEnabled=true;
        using var control=new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw($"CREATE TRIGGER reject_ordinary_child BEFORE UPDATE ON Holon WHEN NEW.Id = '{child.Id:D}' BEGIN SELECT RAISE(ABORT,'reject ordinary child'); END");
        root.Name="failed";root.Version=3;child.Version=3;
        try{var failed=await Save();failed.IsError.Should().BeTrue();failed.IsSaved.Should().BeFalse();}
        finally{control.Database.ExecuteSqlRaw("DROP TRIGGER reject_ordinary_child");}
        var current=await provider.LoadHolonAsync(root.Id,loadChildren:false);
        current.Result.Name.Should().Be("after");current.Result.Version.Should().Be(2);
        control.Database.OpenConnection();using var query=control.Database.GetDbConnection().CreateCommand();
        query.CommandText="SELECT COUNT(*) FROM CosmicHolonRevision WHERE HolonId=$id AND Version=2";
        var id=query.CreateParameter();id.ParameterName="$id";id.Value=root.Id.ToString("D");query.Parameters.Add(id);
        Convert.ToInt32(query.ExecuteScalar()).Should().Be(0);
    }
}
