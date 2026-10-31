using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class NoSchemaDdlOwner(string connection):OracleDBOASIS(connection)
    {
        public int ProvisionCalls;
        protected override Task ProvisionStorageSchemaAsync(OracleConnection connection)
        {
            ProvisionCalls++;
            throw new InvalidOperationException("Existing schema activation must not enter provisioning.");
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeExistingSchemaActivationValidatesWithoutProvisioning(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var owner=new NoSchemaDdlOwner(connection);
        var result=synchronous?owner.ActivateProvider():await owner.ActivateProviderAsync();
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.Result);
        Assert.IsTrue(owner.IsProviderActivated);Assert.AreEqual(0,owner.ProvisionCalls);
        Assert.IsTrue((await owner.DeActivateProviderAsync()).Result);
    }
}
