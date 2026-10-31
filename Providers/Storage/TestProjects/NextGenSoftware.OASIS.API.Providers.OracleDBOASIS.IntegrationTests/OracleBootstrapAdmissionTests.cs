using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleBootstrapAdmissionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task BootstrapRequiresExplicitLocalAuthorityBeforeOpeningConnection(bool wrongEndpoint)
    {
        var previousFlag=Environment.GetEnvironmentVariable("OASIS_ORACLE_ALLOW_BOOTSTRAP_TEST");
        var previousConnection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING");
        try{
            Environment.SetEnvironmentVariable("OASIS_ORACLE_ALLOW_BOOTSTRAP_TEST",wrongEndpoint?"1":null);
            Environment.SetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING","User Id=SYSTEM;Password=synthetic;Data Source=example.invalid/DO_NOT_CONNECT");
            var rejected=await Assert.ThrowsExceptionAsync<AssertFailedException>(()=>new OracleBootstrapNativeTests().NativeInitialAndPartialSchemaProvisioningUsesOneOwnedEmptyNamespace());
            StringAssert.Contains(rejected.Message,wrongEndpoint?"Bootstrap fixture is local-only":"Explicit retained-local bootstrap fixture approval");
        }finally{
            Environment.SetEnvironmentVariable("OASIS_ORACLE_ALLOW_BOOTSTRAP_TEST",previousFlag);
            Environment.SetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING",previousConnection);
        }
    }
}
