using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicQueryPlan<Holon> WindowPlan(CosmicQuery<Holon> query)
    {
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);return plan.Result;
    }

    [Fact]
    public async Task NativeCqlOrderingUsesOrdinalKeysThenExplicitSecondaryAndIdentityTies()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        string?[] names={"b","a","b",null,"a","B"};var rows=new List<Holon>();
        for(var i=0;i<names.Length;i++)
        {
            var row=new Holon{Id=Guid.Parse("00000000-0000-0000-0000-"+(i+1).ToString("D12")),Name=names[i],Version=i+1};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();rows.Add(row);
        }
        async Task Check(CosmicQuery<Holon> query,params int[] expected)
        {
            var result=await provider.ExecuteCosmicQueryAsync(WindowPlan(query));
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            result.Result.Select(h=>h.Id).Should().Equal(expected.Select(i=>rows[i].Id));
        }
        var byName=CosmicQuery<Holon>.Create().OrderBy(h=>h.Name).Result;
        await Check(byName,3,5,1,4,0,2);
        await Check(byName.Skip(1).Result.Take(3).Result,5,1,4);
        await Check(byName.ThenByDescending(h=>h.Version).Result,3,5,4,1,2,0);
        await Check(byName.ThenByDescending(h=>h.Version).Result.Take(4).Result.Skip(2).Result,4,1);
        await Check(CosmicQuery<Holon>.Create().OrderByDescending(h=>h.Id).Result,5,4,3,2,1,0);
        await Check(byName.Skip(100).Result.Take(5).Result);
    }

    [Fact]
    public async Task NativeCqlMetadataOrderingPreservesMissingNullTypeAndIntegerPrecision()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values={long.MaxValue,long.MinValue,null,"2",2L,2L};var ids=new List<Guid>();
        const string key="a.b'; $sort";
        for(var i=0;i<=values.Length;i++)
        {
            var row=new Holon{Id=Guid.Parse("00000000-0000-0000-0000-"+(i+1).ToString("D12")),MetaData=i==values.Length?new():new(){["nested"]=new Dictionary<string,object>{{key,values[i]!}}}};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        var asc=CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<long?>(h,"nested",key)).Result;
        var result=await provider.ExecuteCosmicQueryAsync(WindowPlan(asc));
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Select(h=>h.Id).Should().Equal(new[]{6,2,1,4,5,0,3}.Select(i=>ids[i]));
        var desc=CosmicQuery<Holon>.Create().OrderByDescending(h=>CosmicQueryMetadata.Value<long?>(h,"nested",key)).Result;
        var page=await provider.ExecuteCosmicQueryAsync(WindowPlan(desc.Skip(1).Result.Take(3).Result));
        page.IsError.Should().BeFalse(page.Message+page.Exception);
        page.Result.Select(h=>h.Id).Should().Equal(new[]{0,4,5}.Select(i=>ids[i]));
    }

    [Fact]
    public async Task NativeCqlEmptyWindowDoesNotOpenDatabaseAndTakeIsNotAnOverflow()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var empty=await provider.ExecuteCosmicQueryAsync(WindowPlan(CosmicQuery<Holon>.Create().Take(0).Result));
        empty.IsError.Should().BeFalse(empty.Message);empty.IsLoaded.Should().BeTrue();empty.Result.Should().BeEmpty();
        File.Exists(_databasePath).Should().BeFalse();
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        for(var i=0;i<4;i++)(await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid()},saveChildren:false)).IsError.Should().BeFalse();
        var page=await provider.ExecuteCosmicQueryAsync(WindowPlan(CosmicQuery<Holon>.Create().Take(2).Result),maximumResults:2);
        page.IsError.Should().BeFalse(page.Message);page.Result.Should().HaveCount(2);
        var overflow=await provider.ExecuteCosmicQueryAsync(WindowPlan(CosmicQuery<Holon>.Create().Take(4).Result),maximumResults:2);
        overflow.ErrorCode.Should().Be("CQL_QUERY_LIMIT");overflow.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeCqlScalarOrderingKeepsUuidIdentityUtcTicksAndBooleanDomains()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        const string id="aabbccdd-1234-5678-9abc-def012345678";
        var instant=new DateTime(2026,10,20,12,0,0,DateTimeKind.Utc);
        object?[] guids={id,id.ToUpperInvariant(),"12345678-1234-5678-9abc-def012345678",null,null,"not-guid"};
        object?[] dates={instant.AddTicks(1),instant.AddTicks(2),null,null,"2026-10-20T12:00:00+01:00",DateTime.SpecifyKind(DateTime.MinValue,DateTimeKind.Utc)};
        object?[] flags={true,false,null,null,"true",false};var ids=new List<Guid>();
        for(var i=0;i<6;i++)
        {
            var row=new Holon{Id=Guid.Parse("00000000-0000-0000-0000-"+(i+1).ToString("D12")),MetaData=new()};
            if(i!=4)row.MetaData["g"]=guids[i]!;
            if(i!=3){row.MetaData["d"]=dates[i]!;row.MetaData["b"]=flags[i]!;}
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        async Task Check(CosmicQuery<Holon> query,params int[] selected)
        {
            var result=await provider.ExecuteCosmicQueryAsync(WindowPlan(query));
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            result.Result.Select(h=>h.Id).Should().Equal(selected.Select(i=>ids[i]));
        }
        await Check(CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<Guid?>(h,"g")).Result,4,3,2,0,1,5);
        await Check(CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<DateTime?>(h,"d")).Result,3,2,5,0,1,4);
        await Check(CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<bool?>(h,"b")).Result,3,2,1,5,0,4);
    }
}
