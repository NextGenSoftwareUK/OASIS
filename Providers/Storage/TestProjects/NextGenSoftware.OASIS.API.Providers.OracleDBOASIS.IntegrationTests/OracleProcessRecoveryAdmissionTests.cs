using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass]
public sealed class OracleProcessRecoveryAdmissionTests
{
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static OracleEncryptedProcessNativeTests.Proof Proof()=>new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Hash("root"),Hash("child"),Hash("receipt"),Hash("preparation"));
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public void MissingOrChangedOriginalMustFenceRecovery(int field,bool corrupt)
    {
        string?[] values={"root","child","receipt","preparation"};values[field]=corrupt?"changed":null;
        var error=Assert.ThrowsException<InvalidDataException>(()=>OracleEncryptedProcessNativeTests.ValidateDurableSnapshot(Proof(),values[0],values[1],values[2],values[3]));
        StringAssert.Contains(error.Message,"recovery must not dispatch");
    }
    [TestMethod]
    public void ExactOriginalSnapshotPassesWithoutMutationOrServiceAccess()
        =>OracleEncryptedProcessNativeTests.ValidateDurableSnapshot(Proof(),"root","child","receipt","preparation");
}
