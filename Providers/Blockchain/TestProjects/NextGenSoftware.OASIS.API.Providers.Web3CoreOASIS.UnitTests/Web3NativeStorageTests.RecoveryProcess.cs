using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using System.Diagnostics;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyFreshRecoveryProcessAsync(string operationId,string contract,string calldata)
    {
        var directory=Path.GetDirectoryName(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE"))!;
        var report=Path.Combine(directory,"web3-prepared-recovery-child.trx");var started=DateTime.UtcNow;
        var start=new ProcessStartInfo("dotnet") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add("vstest");start.ArgumentList.Add(typeof(Web3NativeStorageTests).Assembly.Location);
        start.ArgumentList.Add("/TestCaseFilter:FullyQualifiedName~RecoverPreparedAttemptInFreshProcess");
        start.ArgumentList.Add("/Logger:trx;LogFileName=web3-prepared-recovery-child.trx");start.ArgumentList.Add("/ResultsDirectory:"+directory);
        start.Environment["OASIS_WEB3_RECOVERY_CHILD"]="1";start.Environment["OASIS_WEB3_RECOVERY_OPERATION"]=operationId;
        start.Environment["OASIS_WEB3_RECOVERY_CONTRACT"]=contract;start.Environment["OASIS_WEB3_RECOVERY_CALLDATA"]=calldata;
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try {await process.WaitForExitAsync(timeout.Token);}
        catch(OperationCanceledException)
        {
            if(!process.HasExited) process.Kill(entireProcessTree:true);
            await process.WaitForExitAsync();throw;
        }
        var output=await stdout;var errors=await stderr;
        Assert.AreEqual(0,process.ExitCode,output+errors);
        Assert.IsTrue(File.GetLastWriteTimeUtc(report)>=started,"Child evidence must be fresh.");
        var xml=System.Xml.Linq.XDocument.Load(report);
        var counters=xml.Descendants().Single(row=>row.Name.LocalName=="Counters");
        Assert.AreEqual("1",counters.Attribute("total")!.Value);Assert.AreEqual("1",counters.Attribute("passed")!.Value);
        Assert.IsTrue(File.ReadAllText(report).Contains("WEB3_FRESH_PROCESS_RECOVERY_VERIFIED"));
    }

}

[TestClass]
[DoNotParallelize]
public class Web3RecoveryProcessTests
{
    [TestMethod]
    public async Task RecoverPreparedAttemptInFreshProcess()
    {
        if(Environment.GetEnvironmentVariable("OASIS_WEB3_RECOVERY_CHILD")!="1") Assert.Inconclusive("Run only as the bounded reusable real-outbox child fixture.");
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        Assert.IsTrue(uri?.StartsWith("mongodb://127.0.0.1:27029/",StringComparison.Ordinal)==true);
        var operation=Environment.GetEnvironmentVariable("OASIS_WEB3_RECOVERY_OPERATION")!;
        var store=new MongoUsageOutboxStore(uri,"oasis_web3_prepared_native");
        var retained=await store.GetAsync(operation,default);Assert.IsNotNull(retained);
        var attempt=retained.ProviderAttempts.Single();
        var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545",Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,
            Environment.GetEnvironmentVariable("OASIS_WEB3_RECOVERY_CONTRACT")!,31337);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var result=await provider.GetPreparedNativeReceiptAsync(attempt,Environment.GetEnvironmentVariable("OASIS_WEB3_RECOVERY_CALLDATA")!);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.IsFalse(result.IsSaved);
            Assert.AreEqual(attempt.ProviderRequestId,result.Result.TransactionHash);Assert.AreEqual(System.Numerics.BigInteger.One,result.Result.Status.Value);
            Assert.AreEqual(1,(await store.GetAsync(operation,default)).ProviderAttempts.Count);
            Console.WriteLine("WEB3_FRESH_PROCESS_RECOVERY_VERIFIED");
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
