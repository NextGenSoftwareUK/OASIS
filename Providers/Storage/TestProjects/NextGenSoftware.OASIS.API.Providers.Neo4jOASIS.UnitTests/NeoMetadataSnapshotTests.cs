using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoMetadataSnapshotTests
{
    [DataTestMethod][DataRow(false,MetaKeyValuePairMatchMode.All,0)][DataRow(true,MetaKeyValuePairMatchMode.All,0)]
    [DataRow(false,MetaKeyValuePairMatchMode.Any,0)][DataRow(true,MetaKeyValuePairMatchMode.Any,0)]
    [DataRow(false,MetaKeyValuePairMatchMode.All,1)][DataRow(true,MetaKeyValuePairMatchMode.All,1)]
    [DataRow(false,MetaKeyValuePairMatchMode.Any,1)][DataRow(true,MetaKeyValuePairMatchMode.Any,1)]
    public async Task HistoricalMetadataKeepsOriginalPairsAfterNativeRead(bool sync,MetaKeyValuePairMatchMode mode,int mutation)
    {
        var first=new Holon{Id=Guid.NewGuid(),Version=1,MetaData=new(){{"zone","park"}}};var other=new Holon{Id=Guid.NewGuid(),Version=1,MetaData=new(){{"zone","city"}}};
        var nodes=new[]{first,other}.Select(row=>{var node=DispatchProxy.Create<Neo4j.Driver.INode,Neo4jNativeIdentityTests.NodeProbe>();((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"Id",row.Id.ToString()},{"SnapshotJson",OasisJson.Serialize(row)},{"HolonType",row.HolonType.ToString()}};return node;}).ToArray();
        var pairs=new Dictionary<string,string>{{"zone","park"}};var reads=0;
        void Change(){reads++;if(mutation==0)pairs["zone"]="city";else pairs.Clear();}
        var provider=new Provider("bolt://127.0.0.1:17688","neo4j","owned",()=>NeoReadCountTests.RowsProbe.Make<IDriver>(nodes,Change)){IsProviderActivated=true};
        try{var result=sync?provider.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:1):await provider.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:1);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(first.Id,result.Result.Single().Id);Assert.AreEqual(1,reads);}
        finally{provider.IsProviderActivated=false;}
    }
}
