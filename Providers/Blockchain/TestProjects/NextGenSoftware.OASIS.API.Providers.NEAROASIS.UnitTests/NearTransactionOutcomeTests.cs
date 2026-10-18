using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearTransactionOutcomeTests
{
    private static string Classify(string json)
    {
        using var document=JsonDocument.Parse(json);
        return (string)typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!
            .GetMethod("ClassifyTransactionOutcome",BindingFlags.Static|BindingFlags.NonPublic)!
            .Invoke(null,new object[]{document.RootElement})!;
    }
    [DataTestMethod]
    [DataRow("{\"final_execution_status\":\"FINAL\",\"status\":{\"SuccessValue\":\"\"}}","Completed")]
    [DataRow("{\"final_execution_status\":\"FINAL\",\"status\":{\"Failure\":{\"ActionError\":{\"index\":0}}}}","Failed")]
    [DataRow("{\"final_execution_status\":\"EXECUTED\",\"status\":{\"SuccessValue\":\"\"}}","Pending")]
    [DataRow("{\"final_execution_status\":\"EXECUTED_OPTIMISTIC\"}","Pending")]
    [DataRow("{\"final_execution_status\":\"INCLUDED_FINAL\"}","Pending")]
    public void ExecutionSuccessIsSeparateFromFinality(string json,string expected)=>Assert.AreEqual(expected,Classify(json));

    [DataTestMethod]
    [DataRow("{}")]
    [DataRow("{\"final_execution_status\":\"UNKNOWN\"}")]
    [DataRow("{\"final_execution_status\":\"FINAL\",\"status\":\"Started\"}")]
    [DataRow("{\"final_execution_status\":\"FINAL\",\"status\":{\"SuccessValue\":\"\",\"Failure\":{}}}")]
    public void InvalidFinalOutcomeNeverFabricatesSuccess(string json)
        =>Assert.IsInstanceOfType<System.IO.InvalidDataException>(Assert.ThrowsException<TargetInvocationException>(()=>Classify(json)).InnerException);
}
