using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;
[TestClass]
public class PostgreSqlFamilyIdentityTests
{
    [DataTestMethod] [DataRow("CockroachDBOASIS")] [DataRow("TimescaleDBOASIS")]
    public void PostgreSqlWireFacadesReuseOneImplementationAndKeepTheirIdentity(string name)
    {
        const string connection="Host=127.0.0.1;Port=1;Database=synthetic;Username=synthetic";
        NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.PostgreSqlFamilyProviderBase p=name=="CockroachDBOASIS"
            ? new NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.CockroachDBOASIS(connection)
            : new NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS.TimescaleDBOASIS(connection);
        Assert.AreEqual(name,p.ProviderName);Assert.AreEqual(name,p.ProviderType.Value.ToString());
        Assert.IsFalse(p.IsProviderActivated);
        Assert.AreEqual(typeof(NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.PostgreSQLOASIS),
            p.GetType().GetMethod("SaveAvatarAsync")!.DeclaringType,"No duplicate save implementation in a facade.");
    }
}
