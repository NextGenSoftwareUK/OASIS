using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoExportArchiveValidationTests
{
    private static INode Node(Dictionary<string,object> fields){var node=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();((Neo4jNativeIdentityTests.NodeProbe)node).Values=fields;return node;}
    [DataTestMethod]
    [DataRow("kind")][DataRow("identity")][DataRow("missing-version")][DataRow("zero")][DataRow("future")]
    [DataRow("text")][DataRow("duplicate")][DataRow("missing-snapshot")][DataRow("snapshot-id")][DataRow("snapshot-version")]
    public async Task InvalidNativeArchiveCannotBecomePartialSuccessfulExport(string corruption)
    {
        var current=new Holon{Id=Guid.NewGuid(),Version=3};var archived=new Holon{Id=current.Id,Version=1};
        var fields=new Dictionary<string,object>{{"Kind","Holon"},{"RecordId",current.Id.ToString()},{"Version",1L},{"SnapshotJson",OasisJson.Serialize(archived)}};
        switch(corruption){
            case "kind":fields["Kind"]="Avatar";break;case "identity":fields["RecordId"]=Guid.NewGuid().ToString();break;
            case "missing-version":fields.Remove("Version");break;case "zero":fields["Version"]=0L;break;
            case "future":fields["Version"]=3L;break;case "text":fields["Version"]="1";break;
            case "missing-snapshot":fields.Remove("SnapshotJson");break;
            case "snapshot-id":archived.Id=Guid.NewGuid();fields["SnapshotJson"]=OasisJson.Serialize(archived);break;
            case "snapshot-version":archived.Version=2;fields["SnapshotJson"]=OasisJson.Serialize(archived);break;
        }
        var node=Node(fields);var nodes=corruption=="duplicate"?new[]{node,node}:new[]{node};
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Provider.SelectStoredHolonVersionsAsync(current,nodes));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OnlyStoredNondeletedVersionsAreSelectedWithoutMutatingPayloads(bool deleted)
    {
        var current=new Holon{Id=Guid.NewGuid(),Version=3,DeletedDate=deleted?DateTime.UtcNow:DateTime.MinValue};
        var archived=new Holon{Id=current.Id,Version=1,DeletedDate=current.DeletedDate};
        var json=OasisJson.Serialize(archived);var currentJson=OasisJson.Serialize(current);
        var fields=new Dictionary<string,object>{{"Kind","Holon"},{"RecordId",current.Id.ToString()},{"Version",1L},{"SnapshotJson",json}};
        var result=await Provider.SelectStoredHolonVersionsAsync(current,new[]{Node(fields)});
        CollectionAssert.AreEqual(deleted?Array.Empty<int>():new[]{1,3},result.Select(row=>row.Version).ToArray());
        Assert.AreEqual(json,fields["SnapshotJson"]);Assert.AreEqual(currentJson,OasisJson.Serialize(current));
    }
}
