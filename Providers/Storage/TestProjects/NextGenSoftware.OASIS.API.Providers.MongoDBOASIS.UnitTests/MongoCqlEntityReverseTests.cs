using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

public sealed partial class MongoCqlEntityCompositionTests
{
    [TestMethod]
    public void NativeReversalFlipsEffectiveKeysAndIdentityTiesAndNewOrderingResetsDirection()
    {
        var query=Source().Compose().Result.Reverse().Result.Take(2).Result.Reverse().Result
            .OrderByDescending(row=>row.Version).Result.Reverse().Result;
        var command=new MongoCqlTranslator().CompileEntityComposition(Plan(query),10);
        var stages=command.EntityComposition.Stages;
        var first=stages[0].Pipeline.Last(stage=>stage.Contains("$sort"))["$sort"];
        Assert.AreEqual(-1,first["entitySort0.value"].AsInt32);Assert.AreEqual(-1,first["row.HolonId"].AsInt32);
        var restored=stages[2].Pipeline.Last(stage=>stage.Contains("$sort"))["$sort"];
        Assert.AreEqual(1,restored["entitySort0.value"].AsInt32);Assert.AreEqual(1,restored["row.HolonId"].AsInt32);
        var reset=stages[3].Pipeline.Last(stage=>stage.Contains("$sort"))["$sort"];
        Assert.AreEqual(-1,reset["entitySort0.value"].AsInt32);Assert.AreEqual(1,reset["row.HolonId"].AsInt32);
        var tail=stages[4].Pipeline.Last(stage=>stage.Contains("$sort"))["$sort"];
        Assert.AreEqual(1,tail["entitySort0.value"].AsInt32);Assert.AreEqual(-1,tail["row.HolonId"].AsInt32);
    }
}
