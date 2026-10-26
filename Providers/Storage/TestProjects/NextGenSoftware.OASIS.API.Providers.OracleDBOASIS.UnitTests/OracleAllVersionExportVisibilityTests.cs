using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ProviderTestSupport;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleAllVersionExportVisibilityTests
{
    [TestMethod]
    public void LegacyEmbeddedFixtureUsesConcreteTypesAndDecodesBeforeNativeDispatch()
    {
        var root=new Core.Holons.Holon{Id=Guid.NewGuid(),Version=1};
        var child=new Core.Holons.Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=4};root.Children.Add(child);
        var leaf=new Core.Holons.Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,Version=9};child.Children.Add(leaf);
        var payload=AllVersionExportVisibilityAssertions.Legacy(root).ToString(Newtonsoft.Json.Formatting.None);
        var decoded=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Core.Holons.Holon>(payload);
        Assert.AreEqual(child.Id,decoded.Children.Single().Id);Assert.AreEqual(4,decoded.Children.Single().Version);
        Assert.AreEqual(leaf.Id,decoded.Children.Single().Children.Single().Id);
    }
    private sealed class Records(IReadOnlyList<IHolon> rows):OracleDBOASIS("not a connection string")
    {
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version=0)
        {Assert.AreEqual(-1,version);return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true,LoadedCount=rows.Count});}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public Task AllVersionOwnedExportsPruneEachStoredGraphIndependently(bool sync)
        =>AllVersionExportVisibilityAssertions.Verify(async(rows,owner)=>{
            var provider=new Records(rows);
            return sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1);
        });
}
