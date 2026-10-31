using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class SqliteNativeCommitFaultOwnerTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public Task ExplicitTransactionFaultOccursAfterActualNativeCommit(bool synchronous)
        =>Web3NativeStorageTests.VerifyNativeCommitFaultOwnerAsync(synchronous);
}
