using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass]
public sealed class OracleRestrictedConnectionAdmissionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void RestrictedSchemaReusesBoundedSdkPoolAndPreservesProxyIdentity(bool parentPooling)
    {
        var parent = new OracleConnectionStringBuilder { UserID = "SYSTEM", Password = "synthetic-not-a-secret",
            DataSource = "127.0.0.1:11521/FREEPDB1", Pooling = parentPooling };
        var before = parent.ConnectionString;
        var proxy = new OracleConnectionStringBuilder(OracleNativeAcceptanceTests.RestrictedTestConnectionString(parent));
        Assert.IsTrue(proxy.Pooling); Assert.AreEqual(0, proxy.MinPoolSize); Assert.AreEqual(16, proxy.MaxPoolSize);
        Assert.AreEqual(10, proxy.ConnectionTimeout); Assert.AreEqual("OASIS_PRIV_TEST", proxy.UserID);
        Assert.AreEqual(parent.UserID, proxy.ProxyUserId); Assert.AreEqual(parent.Password, proxy.ProxyPassword);
        Assert.AreEqual(parent.DataSource, proxy.DataSource); Assert.AreEqual(before, parent.ConnectionString);
    }
}
