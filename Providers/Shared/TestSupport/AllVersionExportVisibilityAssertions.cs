using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.ProviderTestSupport;

// Same boundary contract across adapters; no SDK/storage implementation in this fixture.
internal static class AllVersionExportVisibilityAssertions
{
    internal static JObject Legacy(Holon row)
    {
        var value=OasisJson.ParseStoredObject(OasisJson.Serialize(row));
        value["$type"]=typeof(Holon).FullName+", "+typeof(Holon).Assembly.GetName().Name;
        value[nameof(Holon.Children)]=new JArray(row.Children.Select(node=>Legacy((Holon)node)));
        return value;
    }
    internal static async Task Verify(Func<IReadOnlyList<IHolon>,Guid,Task<OASISResult<IEnumerable<IHolon>>>> export)
    {
        var owner=Guid.NewGuid();var rootId=Guid.NewGuid();var childId=Guid.NewGuid();
        var foreignOwner=Guid.NewGuid();var foreignId=Guid.NewGuid();var buriedId=Guid.NewGuid();
        Holon Root(int version,int childVersion)
        {
            var root=new Holon{Id=rootId,CreatedByAvatarId=owner,Version=version,Name="owned-v"+version};
            root.Children.Add(new Holon{Id=childId,ParentHolonId=rootId,CreatedByAvatarId=owner,Version=childVersion});
            var foreign=new Holon{Id=foreignId,ParentHolonId=rootId,CreatedByAvatarId=foreignOwner,IsPublic=true,Version=3};
            foreign.Children.Add(new Holon{Id=buriedId,ParentHolonId=foreignId,CreatedByAvatarId=owner,Version=8});
            root.Children.Add(foreign);return root;
        }
        var roots=new IHolon[]{Root(1,4),Root(2,9),new Holon{Id=rootId,CreatedByAvatarId=foreignOwner,Version=3}};
        var result=await export(roots,owner);
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
        var selected=result.Result.ToArray();Assert.AreEqual(2,result.LoadedCount);Assert.AreEqual(2,selected.Length);
        CollectionAssert.AreEqual(new[]{1,2},selected.Select(row=>row.Version).ToArray());
        for(var index=0;index<2;index++){
            var root=selected[index];Assert.AreEqual(rootId,root.Id);Assert.AreEqual(owner,root.CreatedByAvatarId);
            CollectionAssert.AreEqual(new[]{childId},root.Children.Select(row=>row.Id).ToArray(),"Foreign branches must not escape through historical payloads.");
            Assert.AreEqual(index==0?4:9,root.Children.Single().Version,"Keep the stored child version; do not hydrate a current/version-wide graph.");
            Assert.AreNotSame(roots[index],root);
            Assert.AreEqual(2,roots[index].Children.Count());Assert.AreEqual(buriedId,roots[index].Children.Last().Children.Single().Id);
        }
    }
}
