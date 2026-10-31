using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleSchemaUpgradeAdmissionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ExplicitProvisioningRefusesMissingOptInOrForeignEndpointBeforeIO(bool optedIn)
    {
        var previousOptIn=Environment.GetEnvironmentVariable("OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE");
        var previousConnection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE",optedIn?"1":null);
            Environment.SetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING",optedIn?"User Id=SYSTEM;Data Source=example.invalid/not-the-test-database":null);
            var failure=await Assert.ThrowsExceptionAsync<AssertFailedException>(()=>new OracleIdentitySchemaUpgradeNativeTests().UpgradeVerifiedEmptyRetainedIdentityColumnsToCharacters());
            StringAssert.Contains(failure.Message,optedIn?"127.0.0.1:11521/FREEPDB1":"explicit test-only opt-in");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE",previousOptIn);
            Environment.SetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING",previousConnection);
        }
    }
}
