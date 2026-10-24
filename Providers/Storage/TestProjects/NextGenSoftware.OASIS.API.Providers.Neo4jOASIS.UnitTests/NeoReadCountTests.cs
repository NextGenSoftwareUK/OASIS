using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoReadCountTests
{
    public class RowsProbe:DispatchProxy
    {
        public Neo4j.Driver.INode[] Nodes=[];public int Index=-1;
        public static T Make<T>(Neo4j.Driver.INode[] nodes) where T:class{var value=Create<T,RowsProbe>();((RowsProbe)(object)value).Nodes=nodes;return value;}
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch{
            "AsyncSession"=>Make<IAsyncSession>(Nodes),"RunAsync"=>Task.FromResult(Make<IResultCursor>(Nodes)),
            "FetchAsync"=>Task.FromResult(++Index<Nodes.Length),"get_Current"=>Make<IRecord>([Nodes[Index]]),
            "get_Item"=>Nodes[0],"Dispose"=>null,_=>throw new InvalidOperationException(method.Name)};
    }
    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,9))foreach(var version in new[]{0,1})foreach(var sync in new[]{false,true})yield return new object[]{route,version,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task PointAndCollectionCountsDescribeSelectedRoots(int route,int version,bool sync)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var count=route<3?1:2;
        var nodes=Enumerable.Range(0,count).Select(index=>{
            IHolonBase row=route is 0 or 3?new Avatar{Username="oak",Email="oak@example.invalid"}:route is 1 or 4?new AvatarDetail{Username="oak",Email="oak@example.invalid"}:new Holon{Name="oak"};
            row.Id=index==0?id:Guid.NewGuid();row.Version=1;row.IsActive=true;row.MetaData["zone"]="park";
            if(row is IHolon holon)holon.ParentHolonId=parent;
            var node=DispatchProxy.Create<Neo4j.Driver.INode,Neo4jNativeIdentityTests.NodeProbe>();
            ((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{["Id"]=row.Id.ToString(),["SnapshotJson"]=OasisJson.Serialize(row),["Username"]="oak",["Email"]="oak@example.invalid",["Name"]="oak",["HolonType"]=row.HolonType.ToString()};
            return node;
        }).ToArray();
        var provider=new Provider("bolt://127.0.0.1:17688","neo4j","owned",()=>RowsProbe.Make<IDriver>(nodes)){IsProviderActivated=true};
        void Check<T>(OASISResult<T> read){Assert.IsFalse(read.IsError,read.Exception?.ToString());Assert.IsTrue(read.IsLoaded);Assert.AreEqual(count,read.LoadedCount);}
        try{
            switch(route){
                case 0:Check(sync?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));break;
                case 1:Check(sync?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));break;
                case 2:Check(sync?provider.LoadHolon(id,loadChildren:false,version:version):await provider.LoadHolonAsync(id,loadChildren:false,version:version));break;
                case 3:Check(sync?provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version));break;
                case 4:Check(sync?provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version));break;
                case 5:Check(sync?provider.LoadAllHolons(loadChildren:false,version:version):await provider.LoadAllHolonsAsync(loadChildren:false,version:version));break;
                case 6:Check(sync?provider.LoadHolonsForParent(parent,loadChildren:false,version:version):await provider.LoadHolonsForParentAsync(parent,loadChildren:false,version:version));break;
                case 7:Check(sync?provider.LoadHolonsByMetaData(new Dictionary<string,string>{{"zone","park"}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"zone","park"}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildren:false,version:version));break;
                case 8:Check(sync?provider.ExportAll(version):await provider.ExportAllAsync(version));break;
            }
        }finally{provider.IsProviderActivated=false;}
    }
}
