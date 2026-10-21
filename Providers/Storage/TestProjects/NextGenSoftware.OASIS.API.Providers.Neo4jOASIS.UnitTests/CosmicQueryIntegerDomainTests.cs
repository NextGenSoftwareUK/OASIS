using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryIntegerDomainTests
{
    [TestMethod]
    public void NarrowIntegerAndNullableDomainsHaveExactAdmissionContracts()
    {
        Expression<Func<Holon,bool>>[] predicates={
            h=>CosmicQueryMetadata.Value<byte>(h,"n")>=0,
            h=>CosmicQueryMetadata.Value<sbyte>(h,"n")>=0,
            h=>CosmicQueryMetadata.Value<short>(h,"n")>=0,
            h=>CosmicQueryMetadata.Value<ushort>(h,"n")>=0,
            h=>CosmicQueryMetadata.Value<uint>(h,"n")>=0,
            h=>CosmicQueryMetadata.Value<byte?>(h,"n")!=null,
            h=>CosmicQueryMetadata.Value<sbyte?>(h,"n")!=null,
            h=>CosmicQueryMetadata.Value<short?>(h,"n")!=null,
            h=>CosmicQueryMetadata.Value<ushort?>(h,"n")!=null,
            h=>CosmicQueryMetadata.Value<uint?>(h,"n")!=null};
        foreach(var predicate in predicates)
        {
            var query=CosmicQuery<Holon>.Create().Where(predicate);
            Assert.IsFalse(query.IsError,query.Message);
            var result=Neo4jOASIS.CqlCapabilities.Admit(query.Result);
            Assert.IsFalse(result.IsError,result.Message);
        }
    }
}
