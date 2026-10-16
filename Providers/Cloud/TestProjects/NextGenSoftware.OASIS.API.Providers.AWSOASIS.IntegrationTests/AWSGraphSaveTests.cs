using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
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
            if(children)
            {
                foreach(var providerChildren in new[] {false,true})
                {
                    var loaded=await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:providerChildren);
                    Assert.IsFalse(loaded.IsError,loaded.Message);
                    // OasisJson intentionally excludes holon navigation; provider loading follows persisted parent IDs.
                    if(!providerChildren) {Assert.AreEqual(0,loaded.Result.Children.Count());continue;}
                    Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);
                    Assert.AreEqual(root.Id,loaded.Result.Children.Single().ParentHolonId);
                    if(expected==3) Assert.AreEqual(leaf.Id,loaded.Result.Children.Single().Children.Single().Id);
                    else Assert.AreEqual(0,loaded.Result.Children.Single().Children.Count());
                }
            }
            var nodes=new[] {root,child,leaf};
            for(var index=0;index<nodes.Length;index++)
            {
                var node=nodes[index];
                if(index>=expected) {Assert.AreEqual(Guid.Empty,node.Id,"Unselected caller identity must not be published.");continue;}
                Assert.AreNotEqual(Guid.Empty,node.Id);
                var raw=await Client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(node.Id),ConsistentRead=true});
                Assert.IsTrue(raw.Item.Count>0,"Each selected node must have its own native item.");
                var canonical=OasisJson.Deserialize<Holon>(raw.Item["data_json"].S);
                Assert.AreEqual(node.Id,canonical.Id);Assert.AreEqual(node.Name,canonical.Name);
                if(index>0)
                {
                    Assert.AreEqual(nodes[index-1].Id,node.ParentHolonId);
                    Assert.AreEqual(node.ParentHolonId,canonical.ParentHolonId);
                    Assert.AreEqual(node.ParentHolonId.ToString(),raw.Item["parentHolonId"].S);
                }
            }
        }
        finally
        {
            foreach(var node in new[] {root,child,leaf}) if(node.Id!=Guid.Empty)
                await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(node.Id)});
        }
    }
}
