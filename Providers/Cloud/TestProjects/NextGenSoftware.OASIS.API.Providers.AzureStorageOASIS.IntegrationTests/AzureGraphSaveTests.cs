using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false,true,0)] [DataRow(true,false,true,0)]
    [DataRow(false,true,false,0)] [DataRow(true,true,false,0)]
    [DataRow(false,true,true,1)] [DataRow(true,true,true,1)]
    [DataRow(false,true,true,0)] [DataRow(true,true,true,0)]
    public async Task NativeGraphSaveHonorsSelectionAndParentLinks(bool synchronous,bool children,bool recursive,int depth)
    {
        var root=new Holon {Name="owned-graph-root",IsActive=true};
        var child=new Holon {Name="owned-graph-child",IsActive=true};
        var leaf=new Holon {Name="owned-graph-leaf",IsActive=true};
        root.Children=new List<IHolon> {child};child.Children=new List<IHolon> {leaf};
        try
        {
            var result=synchronous ? _provider.SaveHolon(root,children,recursive,depth):await _provider.SaveHolonAsync(root,children,recursive,depth);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreSame(root,result.Result);
            var expected=children ? recursive && depth==0 ? 3:2:1;
            var nodes=new[] {root,child,leaf};
            for(var index=0;index<nodes.Length;index++)
            {
                var node=nodes[index];
                if(index>=expected) {Assert.AreEqual(Guid.Empty,node.Id);continue;}
                Assert.AreNotEqual(Guid.Empty,node.Id);
                var blob=_service.GetBlobContainerClient(_containers[0]).GetBlobClient(node.Id.ToString());
                Assert.IsTrue((await blob.ExistsAsync()).Value);
                var stored=OasisJson.Deserialize<Holon>((await blob.DownloadContentAsync()).Value.Content.ToString());
                Assert.AreEqual(node.Id,stored.Id);Assert.AreEqual(node.Name,stored.Name);
                if(index>0) {Assert.AreEqual(nodes[index-1].Id,node.ParentHolonId);Assert.AreEqual(node.ParentHolonId,stored.ParentHolonId);}
            }
            if(children)
            {
                var loaded=await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
                Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);
                if(expected==3) Assert.AreEqual(leaf.Id,loaded.Result.Children.Single().Children.Single().Id);
                else Assert.AreEqual(0,loaded.Result.Children.Single().Children.Count());
            }
        }
        finally
        {
            foreach(var node in new[] {root,child,leaf}) if(node.Id!=Guid.Empty)
                await _service.GetBlobContainerClient(_containers[0]).GetBlobClient(node.Id.ToString()).DeleteIfExistsAsync();
        }
    }
}
