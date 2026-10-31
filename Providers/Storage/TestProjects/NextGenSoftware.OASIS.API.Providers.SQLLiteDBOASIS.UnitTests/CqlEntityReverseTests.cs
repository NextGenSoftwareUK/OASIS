using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public sealed partial class CqlEntityCompositionSqlTests
{
    [TestMethod]
    public async Task NativeReversalRenumbersTheCurrentRelationWithoutMovingEarlierWindows()
    {
        await using var connection=await Seed();
        var source=CosmicQuery<Holon>.Create().OrderBy(row=>row.Id).Take(3).Result;
        var query=source.Compose().Result.Reverse().Result.Take(2).Result.Reverse().Result;
        CollectionAssert.AreEqual(new[] {"B","A"},await Read(connection,Compile(query)));
        var twice=source.Compose().Result.Reverse().Result.Reverse().Result;
        CollectionAssert.AreEqual(new[] {"C","B","A"},await Read(connection,Compile(twice)));
    }
}
