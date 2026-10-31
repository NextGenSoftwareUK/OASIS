using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoSnapshotScalarAcknowledgementTests
{
    [DataTestMethod]
    [DataRow("avatar",false)][DataRow("avatar",true)]
    [DataRow("detail",false)][DataRow("detail",true)]
    [DataRow("holon",false)][DataRow("holon",true)]
    public void ActualAcknowledgementGuardRejectsNormalizedButDifferentMetadata(string kind,bool number)
    {
        IHolonBase submitted=kind switch{"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        submitted.Id=Guid.NewGuid();
        object Value(bool changed){
            if(!number)return changed?"2030-01-02T00:34:05+03:00":"2030-01-02T03:04:05.0000000+05:30";
            using var json=JsonDocument.Parse(changed?"9007199254740994.0":"9007199254740993.125");return json.RootElement.Clone();
        }
        submitted.MetaData=new(){["owned-scalar"]=Value(false)};
        var serialized=OasisJson.Serialize(submitted);
        IHolonBase acknowledged=kind switch{
            "avatar"=>OasisJson.DeserializeStored<Avatar>(serialized),"detail"=>OasisJson.DeserializeStored<AvatarDetail>(serialized),_=>OasisJson.DeserializeStored<Holon>(serialized)};
        var guard=typeof(Neo4jOASIS).GetMethod("RequireAcknowledgedRecord",BindingFlags.Static|BindingFlags.NonPublic)!;
        guard.Invoke(null,new object[]{submitted,acknowledged}); // Identical detached data must pass first.
        acknowledged.MetaData["owned-scalar"]=Value(true);
        var failure=Assert.ThrowsException<TargetInvocationException>(()=>guard.Invoke(null,new object[]{submitted,acknowledged}));
        Assert.IsInstanceOfType(failure.InnerException,typeof(InvalidDataException));
    }
}
