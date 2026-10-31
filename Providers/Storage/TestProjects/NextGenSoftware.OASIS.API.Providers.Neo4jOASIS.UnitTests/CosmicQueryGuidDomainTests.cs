using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryGuidDomainTests
{
    [TestMethod]
    public void GuidMetadataEqualityHasExactAdmission()
    {
        var query=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<Guid>(h,"guid")==Guid.Empty);
        Assert.IsFalse(query.IsError,query.Message);
        var admitted=Neo4jOASIS.CqlCapabilities.Admit(query.Result);
        Assert.IsFalse(admitted.IsError,admitted.Message);
    }
    [TestMethod]
    public void NullableGuidMembershipAndOrderingHaveExactAdmission()
    {
        var members=new Guid?[]{Guid.Empty,null};
        var query=CosmicQuery<Holon>.Create().Where(h=>members.Contains(CosmicQueryMetadata.Value<Guid?>(h,"guid")))
            .OrderBy(h=>CosmicQueryMetadata.Value<Guid?>(h,"guid"));
        Assert.IsFalse(query.IsError,query.Message);
        var admitted=Neo4jOASIS.CqlCapabilities.Admit(query.Result);
        Assert.IsFalse(admitted.IsError,admitted.Message);
    }
}
