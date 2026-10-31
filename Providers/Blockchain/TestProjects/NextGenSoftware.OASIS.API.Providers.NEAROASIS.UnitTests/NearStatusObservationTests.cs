using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearStatusObservationTests
{
    [TestMethod]
    public async Task PrecancelledObservationCannotDefaultToCompleted()
    {
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var result=await new NEAROASIS().GetTransactionStatusAsync("original",cancelled.Token);
        Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
        Assert.IsInstanceOfType(result.Exception,typeof(OperationCanceledException));
    }

    [TestMethod]
    public async Task UnavailableOriginalIsErrorAndPendingNeverCompleted()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","status-observation.mjs"));
            var result=await new NEAROASIS().GetTransactionStatusAsync("original");
            Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
            StringAssert.Contains(result.Message,"UNKNOWN_TRANSACTION");
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task CancellingLiveReadonlyObservationStopsItsOwnedProcess()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        var previousPid=Environment.GetEnvironmentVariable("OASIS_NEAR_STATUS_TEST_PID");
        var marker=Path.GetTempFileName();int pid=0;
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","status-observation.mjs"));
            Environment.SetEnvironmentVariable("OASIS_NEAR_STATUS_TEST_PID",marker);
            using var cancellation=new CancellationTokenSource();
            var running=new NEAROASIS().GetTransactionStatusAsync("original",cancellation.Token);
            var watch=Stopwatch.StartNew();
            while(!int.TryParse(await File.ReadAllTextAsync(marker),out pid)&&watch.Elapsed<TimeSpan.FromSeconds(5))await Task.Delay(20);
            cancellation.Cancel();
            var result=await running.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(pid>0,"Controlled observation must have started before cancellation.");
            Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
            Assert.IsInstanceOfType(result.Exception,typeof(OperationCanceledException));
            Assert.ThrowsException<ArgumentException>(()=>Process.GetProcessById(pid));
        }finally{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);
            Environment.SetEnvironmentVariable("OASIS_NEAR_STATUS_TEST_PID",previousPid);
            File.Delete(marker);
        }
    }
}
