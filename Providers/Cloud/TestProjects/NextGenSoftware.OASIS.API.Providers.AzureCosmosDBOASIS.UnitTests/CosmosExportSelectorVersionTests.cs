using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosExportSelectorVersionTests
{
    private sealed class Fixture
    {
        internal readonly Guid Owner=Guid.NewGuid(),Own=Guid.NewGuid();
        internal readonly Mock<IAvatarRepository> Avatars=new(MockBehavior.Strict);
        internal readonly Mock<IHolonRepository> Holons=new(MockBehavior.Strict);
        internal readonly AzureCosmosDBOASIS Provider=new(new Uri("https://owned.invalid"),"owned","owned",new(){"holonItems"}){IsProviderActivated=true};
        internal Fixture(bool empty=false)
        {
            Avatars.Setup(r=>r.GetListAsync(0)).ReturnsAsync(new List<IAvatar>{new Avatar{Id=Owner,Username="export-owner",Email="export@example.invalid",Version=1,IsActive=true}});
            Avatars.Setup(r=>r.GetListAsync(3)).ReturnsAsync(new List<IAvatar>());
            var rows=new List<IHolon>{new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Version=3,IsActive=true}};
            if(!empty)rows.Add(new Holon{Id=Own,CreatedByAvatarId=Owner,Version=3,Name="own-v3",IsActive=true});
            Holons.Setup(r=>r.GetListAsync(0)).ReturnsAsync(rows);Holons.Setup(r=>r.GetListAsync(3)).ReturnsAsync(rows);Holons.Setup(r=>r.GetListAsync(-1)).ReturnsAsync(rows);
            typeof(AzureCosmosDBOASIS).GetField("avatarRepository",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(Provider,Avatars.Object);
            typeof(AzureCosmosDBOASIS).GetField("holonRepository",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(Provider,Holons.Object);
        }
    }
    [DataTestMethod][DataRow(false,false,3)][DataRow(false,true,3)][DataRow(true,false,3)][DataRow(true,true,3)]
    [DataRow(false,false,-1)][DataRow(false,true,-1)][DataRow(true,false,-1)][DataRow(true,true,-1)]
    public async Task ExportHolonVersionDoesNotSelectAvatarHistory(bool sync,bool email,int version)
    {
        var fixture=new Fixture();var provider=fixture.Provider;
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",version)
            :sync?provider.ExportAllDataForAvatarByUsername("export-owner",version):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",version);
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        Assert.AreEqual(fixture.Own,result.Result.Single().Id);Assert.AreEqual(3,result.Result.Single().Version);
        fixture.Avatars.Verify(r=>r.GetListAsync(0),Times.Once);fixture.Avatars.Verify(r=>r.GetListAsync(3),Times.Never);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeExportVersionFailsBeforeRepositoryReads(bool sync,bool email)
    {
        var fixture=new Fixture();var provider=fixture.Provider;
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",-2):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",-2)
            :sync?provider.ExportAllDataForAvatarByUsername("export-owner",-2):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",-2);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.AreEqual(0,fixture.Avatars.Invocations.Count);Assert.AreEqual(0,fixture.Holons.Invocations.Count);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task OwnedExportCountsReturnedRootsIncludingEmpty(bool sync,bool empty)
    {
        var fixture=new Fixture(empty);
        var result=sync?fixture.Provider.ExportAllDataForAvatarById(fixture.Owner):await fixture.Provider.ExportAllDataForAvatarByIdAsync(fixture.Owner);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(empty?0:1,result.Result.Count());
        Assert.AreEqual(result.Result.Count(),result.LoadedCount);
    }
}
