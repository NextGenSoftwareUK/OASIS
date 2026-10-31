using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleLiveRecordAdmissionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void IntentOwnershipIsRawBytesNotDecodedText(bool addBom)
    {
        var original=System.Text.Encoding.UTF8.GetBytes("{\"state\":\"owned\"}");
        var observed=addBom?new byte[]{0xEF,0xBB,0xBF}.Concat(original).ToArray():original.ToArray();
        Assert.AreEqual(!addBom,OracleLiveRecordAcceptance.IntentFingerprint(original)==OracleLiveRecordAcceptance.IntentFingerprint(observed));
    }

    [TestMethod]
    public async Task HostedWritesRequireExplicitApprovalBeforeConfiguration()
        =>await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>OracleLiveRecordAcceptance.RunAsync("","","","","",false));

    [DataTestMethod][DataRow(null)][DataRow("")]
    public async Task HostedConnectionVariableMustBeNamed(string? variable)
    {
        if(variable==null)await Assert.ThrowsExceptionAsync<ArgumentNullException>(()=>OracleLiveRecordAcceptance.RunAsync(variable!,"db","APP","service","",true));
        else await Assert.ThrowsExceptionAsync<ArgumentException>(()=>OracleLiveRecordAcceptance.RunAsync(variable,"db","APP","service","",true));
    }

    [TestMethod]
    public async Task MissingProtectedConnectionCannotReachSdk()
    {
        const string variable="OASIS_ORACLE_OWNED_RECORD_ADMISSION_CONNECTION";Assert.IsNull(Environment.GetEnvironmentVariable(variable));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>OracleLiveRecordAcceptance.RunAsync(variable,"db","APP","service","",true));
    }

    [DataTestMethod]
    [DataRow("oracle-live-records.pending.json")][DataRow("oracle-live-records.current.json")]
    [DataRow("oracle-live-records.pending.json.next")][DataRow("oracle-live-records.current.json.next")]
    public async Task OriginalEvidenceFencesSdkAndRemainsUnchanged(string name)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")??throw new InvalidOperationException("Use the retained Oracle evidence directory for this control.");
        Assert.IsTrue(Directory.Exists(work));var path=Path.Combine(work,name);Assert.IsFalse(File.Exists(path));
        const string owned="owned admission-only control; no native mutation";
        await using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await file.WriteAsync(System.Text.Encoding.UTF8.GetBytes(owned));file.Flush(true);
        }
        try{
            var failure=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>OracleLiveRecordAcceptance.RunLocalControlAsync("UnsupportedOwnedOption=true","db","APP","service",work));
            StringAssert.Contains(failure.Message,"original/interrupted");Assert.AreEqual(owned,await File.ReadAllTextAsync(path));
        }finally{Assert.AreEqual(owned,await File.ReadAllTextAsync(path));File.Delete(path);}
    }
}
