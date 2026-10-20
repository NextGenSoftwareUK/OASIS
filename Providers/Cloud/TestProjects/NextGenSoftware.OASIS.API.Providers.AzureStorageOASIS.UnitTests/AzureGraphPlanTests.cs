using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureGraphPlanTests
{
    private sealed class WriteProbe : AzureStorageOASIS
    {
        public int Writes;
        public WriteProbe():base("UseDevelopmentStorage=true") { }
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite write,Action validatePublication)
        {var holon=write.Source;var staged=write.Snapshot;validatePublication();Writes++;holon.Id=staged.Id;holon.ParentHolonId=staged.ParentHolonId;return Task.FromResult(new OASISResult<IHolon> {Result=holon,IsSaved=true});}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidSelectedGraphDoesNotWriteOrPublishIdentities(bool synchronous)
    {
        foreach(var invalid in new[] {"cycle","duplicate-id","reference","parent","null","blob-key","depth"})
        {
            var provider=new WriteProbe();var root=new Holon();var child=new Holon();
            root.Children=new List<IHolon> {child};
            if(invalid=="cycle") child.Children=new List<IHolon> {root};
            if(invalid=="duplicate-id") {root.Id=Guid.NewGuid();child.Id=root.Id;}
            if(invalid=="reference") root.Children.Add(child);
            if(invalid=="parent") child.ParentHolonId=Guid.NewGuid();
            if(invalid=="null") root.Children.Add(null!);
            if(invalid=="blob-key") {root.CustomKey="owned-collision";child.CustomKey="owned-collision";}
            var rootId=root.Id;var childId=child.Id;var depth=invalid=="depth" ? -1:0;
            var result=synchronous ? provider.SaveHolons(new[] {root},maxChildDepth:depth):await provider.SaveHolonsAsync(new[] {root},maxChildDepth:depth);
            Assert.IsTrue(result.IsError,invalid);Assert.IsNotNull(result.Exception);Assert.AreEqual(0,provider.Writes,invalid);
            Assert.AreEqual(rootId,root.Id);Assert.AreEqual(childId,child.Id);
        }
    }
}
