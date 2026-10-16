using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jNativeIdentityTests
{
    public class NodeProbe : DispatchProxy
    {
        public IReadOnlyDictionary<string,object> Values=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
            => method!.Name=="get_Properties" ? Values:method.Name=="get_Item" ? Values[(string)args![0]!]
                :throw new InvalidOperationException("Unexpected node call: "+method.Name);
    }

    private static object Read(string kind,Guid id,Dictionary<string,object> properties,bool populated=false)
    {
        IHolonBase source=kind=="avatar" ? new Avatar {Id=id}:kind=="detail" ? new AvatarDetail {Id=id}:new Holon {Id=id};
        if(populated && source is IAvatar avatar) {avatar.Username="current";avatar.Email="current@example.invalid";}
        if(populated && source is IAvatarDetail detail) {detail.Username="current";detail.Email="current@example.invalid";}
        properties["SnapshotJson"]=OasisJson.Serialize(source);
        var node=DispatchProxy.Create<Neo4j.Driver.INode,NodeProbe>();((NodeProbe)node).Values=properties;
        var method=kind=="avatar" ? "ReadAvatarNode":kind=="detail" ? "ReadDetailNode":"ReadHolonNode";
        return typeof(Provider).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[] {node})!;
    }

    [DataTestMethod]
    [DataRow("avatar","Username")] [DataRow("avatar","Email")]
    [DataRow("detail","Username")] [DataRow("detail","Email")]
    public void NativeLookupProjectionMustMatchCanonicalValue(string kind,string field)
    {
        var id=Guid.NewGuid();
        var valid=new Dictionary<string,object> {{"Id",id.ToString()},{"Username","current"},{"Email","current@example.invalid"}};
        Assert.AreEqual(id,((IHolonBase)Read(kind,id,new(valid),true)).Id);
        foreach(var corruption in new object?[] {null,42L,"stale lookup"})
        {
            var values=new Dictionary<string,object>(valid);
            if(corruption==null) values.Remove(field);else values[field]=corruption;
            var error=Assert.ThrowsException<TargetInvocationException>(()=>Read(kind,id,values,true));
            Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
        }
    }

    [DataTestMethod]
    [DataRow("avatar","missing")] [DataRow("avatar","number")] [DataRow("avatar","invalid")] [DataRow("avatar","empty")]
    [DataRow("detail","missing")] [DataRow("detail","number")] [DataRow("detail","invalid")] [DataRow("detail","empty")]
    [DataRow("holon","missing")] [DataRow("holon","number")] [DataRow("holon","invalid")] [DataRow("holon","empty")]
    public void CorruptNativeIdentityIsTerminalIntegrityFailure(string kind,string corruption)
    {
        var values=new Dictionary<string,object>();
        if(corruption!="missing") values["Id"]=corruption=="number" ? 42L:corruption=="empty" ? Guid.Empty.ToString():"invalid-id";
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Read(kind,Guid.NewGuid(),values));
        Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("avatar")] [DataRow("detail")] [DataRow("holon")]
    public void ValidNativeGuidIdentityStillMatchesCanonicalRecord(string kind)
    {
        var id=Guid.NewGuid();var values=new Dictionary<string,object> {{"Id",id.ToString().ToUpperInvariant()}};
        Assert.AreEqual(id,((IHolonBase)Read(kind,id,values)).Id);
    }
}
