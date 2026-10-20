using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task V2PublicManagerPreparesPersistsLoadsAndTracksDeepGraphs(bool synchronous,bool providerChildren)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();var manager=CosmicManager(provider,true);
        var actor=Guid.NewGuid();var rows=Enumerable.Range(0,1024).Select(index=>new Holon{Name="public-depth-"+index}).ToArray();
        for(var index=1;index<rows.Length;index++){rows[index-1].Children.Add(rows[index]);rows[index].ParentHolon=rows[index-1];}
        var saved=synchronous ? manager.SaveHolon(rows[0],actor,saveChildrenOnProvider:providerChildren,providerType:ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(rows[0],actor,saveChildrenOnProvider:providerChildren,providerType:ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        foreach(var row in rows){row.Version.Should().Be(1);row.CreatedByAvatarId.Should().Be(actor);}
        for(var index=1;index<rows.Length;index++){rows[index].ParentHolon.Should().BeSameAs(rows[index-1]);rows[index].ParentHolonId.Should().Be(rows[index-1].Id);}
        rows[0].HasHolonChanged().Should().BeFalse();
        var loaded=synchronous ? manager.LoadHolon(rows[0].Id,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
            : await manager.LoadHolonAsync(rows[0].Id,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);loaded.Result.HasHolonChanged().Should().BeFalse();
        var current=loaded.Result;
        for(var index=0;index<rows.Length;index++)
        {current.Id.Should().Be(rows[index].Id);current.Name.Should().Be(rows[index].Name);if(index+1<rows.Length)current=current.Children.Single();}
        var noOp=await manager.SaveHolonAsync(loaded.Result,actor,saveChildrenOnProvider:providerChildren,providerType:ProviderType.SQLLiteDBOASIS);
        noOp.IsError.Should().BeFalse(noOp.Message+noOp.Exception);noOp.IsSaved.Should().BeFalse();
        using var control=new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Count().Should().Be(rows.Length);
        control.Holons.AsNoTracking().All(row=>row.Version==1).Should().BeTrue("no-op save creates no extra logical revision");
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, false)] [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)] [InlineData(true, false, true)] [InlineData(true, true, true)]
    public async Task CosmicOrmPersistsAndTracksActualGraphWithoutNoOpRevision(bool v2, bool synchronous, bool providerChildren)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, v2);
        var child = new Holon { Name = "child", IsPublic = true };
        var root = new Holon { Name = "root", Children = new List<IHolon> { child } };
        child.ParentHolon = root;
        var actor = Guid.NewGuid();
        var saved = synchronous
            ? manager.SaveHolon(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + " " + saved.Exception);
        root.Id.Should().NotBeEmpty(); child.ParentHolonId.Should().Be(root.Id);
        child.ParentHolon.Should().BeSameAs(root); root.HasHolonChanged().Should().BeFalse();
        var revision = root.VersionId;
        var clean = await manager.SaveHolonAsync(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS);
        clean.IsError.Should().BeFalse(clean.Message); clean.IsSaved.Should().BeFalse(); root.VersionId.Should().Be(revision);
        var loaded = await manager.LoadHolonAsync(root.Id, loadChildrenFromProvider: true, providerType: ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message); loaded.Result.HasHolonChanged().Should().BeFalse();
        loaded.Result.Children.Should().ContainSingle().Which.IsPublic.Should().BeTrue();
        loaded.Result.Name = "edited";
        (await manager.SaveHolonAsync(loaded.Result, actor, saveChildrenOnProvider: false, providerType: ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(row => row.Id == root.Id.ToString()).Name.Should().Be("edited");
        control.Holons.AsNoTracking().Count().Should().Be(2);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CosmicOrmRejectedNativeGraphDoesNotPublishAnyCallerIdentityOrVersion(bool v2)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_cosmic_child BEFORE INSERT ON Holon WHEN NEW.Name = 'reject-child' BEGIN SELECT RAISE(ABORT, 'controlled cosmic child rejection'); END;");
        var manager = CosmicManager(provider, v2);
        var child = new Holon { Name = "reject-child" };
        var root = new Holon { Name = "pending-root", Children = new List<IHolon> { child } };
        child.ParentHolon = root;
        var result = await manager.SaveHolonAsync(root, Guid.NewGuid(), saveChildrenOnProvider: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); root.Id.Should().BeEmpty(); child.Id.Should().BeEmpty();
        root.Version.Should().Be(0); child.Version.Should().Be(0); root.MetaData.Should().BeEmpty();
        child.ParentHolon.Should().BeSameAs(root);
        control.Holons.AsNoTracking().Should().BeEmpty("SQLite must roll back the failed graph transaction");
    }

    private static HolonManager CosmicManager(SQLLiteDBOASIS provider, bool v2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = v2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        var providers = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        return new HolonManager(provider, dna, providers);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task CosmicOrmBatchAcknowledgesEverySelectedRecordAndSkipsTheNextNoOp(bool v2, bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, v2);
        var child = new Holon { Name = "batch-child" };
        var root = new Holon { Name = "batch-root", Children = new List<IHolon> { child } };
        var other = new Holon { Name = "second-root" }; var actor = Guid.NewGuid();
        var saved = synchronous
            ? manager.SaveHolons(new[] { root, other }, actor, saveChildrenOnProvider: true, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonsAsync(new[] { root, other }, actor, saveChildrenOnProvider: true, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + " " + saved.Exception);
        root.Id.Should().NotBeEmpty(); other.Id.Should().NotBeEmpty(); child.Id.Should().NotBeEmpty();
        root.HasHolonChanged().Should().BeFalse(); other.HasHolonChanged().Should().BeFalse();
        var clean = await manager.SaveHolonsAsync(new[] { root, other }, actor, providerType: ProviderType.SQLLiteDBOASIS);
        clean.IsSaved.Should().BeFalse(); clean.IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Count().Should().Be(3);
    }
}
