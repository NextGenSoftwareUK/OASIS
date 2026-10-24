using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicQueryHttpRoutesTests
{
    [Theory]
    [InlineData("/api/data/query-cql")] [InlineData("/api/data/query-cql/scalar")]
    [InlineData("/api/data/query-cql/projection")] [InlineData("/api/data/query-cql/grouped")]
    [InlineData("/API/DATA/QUERY-CQL/GROUPED/")]
    public void EveryRegisteredLeafRequiresAdmission(string path) => Assert.True(CosmicQueryHttpRoutes.Contains(path));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("/api/data/query-cql/grouped/extra")]
    [InlineData("/api/data/query-cql-unsafe")]
    public void UnregisteredPathsAreNotAdmittedByPrefix(string path) => Assert.False(CosmicQueryHttpRoutes.Contains(path));
}
