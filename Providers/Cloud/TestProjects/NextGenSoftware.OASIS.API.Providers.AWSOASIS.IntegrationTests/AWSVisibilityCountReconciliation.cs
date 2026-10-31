using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public sealed partial class AWSNativeRunLifecycle
{
    [TestMethod]
    public async Task CleanupOnlyReviewedVisibilityCountAssertions()
    {
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        Assert.AreEqual("E4A87C6BCC0B8C5DF622BED2B834DB3C5B35C351E68BAE2BCE4035EE27D12AAB",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        Assert.AreEqual("69B10E74E2162F27BDC91D23A02BD1DCDA1496F69C92F9A2241D25A9DE651017",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-suite.pending.json")))));
        var document=XDocument.Load(report);XNamespace ns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var counters=document.Descendants(ns+"Counters").Single();Assert.AreEqual("138",(string?)counters.Attribute("total"));Assert.AreEqual("136",(string?)counters.Attribute("passed"));Assert.AreEqual("2",(string?)counters.Attribute("failed"));
        var failures=document.Descendants(ns+"UnitTestResult").Where(row=>(string?)row.Attribute("outcome")!="Passed").ToArray();
        CollectionAssert.AreEquivalent(new[]{"NativeVisibilityDefaultsPreserveOwnerPublicAndProviderDiagnostics (False)","NativeVisibilityDefaultsPreserveOwnerPublicAndProviderDiagnostics (True)"},failures.Select(row=>(string)row.Attribute("testName")!).ToArray());
        foreach(var failure in failures)
        {
            StringAssert.Contains(failure.Descendants(ns+"StackTrace").Single().Value,"AWSVisibilityInterfaceTests.cs:line 37");
            StringAssert.Contains(failure.Descendants(ns+"Message").Single().Value,"Actual:<2>");
        }
        // Original baseline was exclusively empty; only these two read assertions
        // failed. Reuse exact-id/content conditional cleanup, never replay writes.
        await CleanupObservedAsync("aws-visibility-count-cleanup.json");
    }
}
