using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    private const string MixedDateFirst="2026-11-01T00:00:00.1234567Z";
    private const string MixedDateSecond="2026-11-01T00:00:00.1234568Z";
    private static DateTime MixedDate(string text)=>DateTime.Parse(text,CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind);
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task MappedDateTimeKeysKeepUtcTicksNullGroupAndFinalPage(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,dateKeysHttp:true);
}
