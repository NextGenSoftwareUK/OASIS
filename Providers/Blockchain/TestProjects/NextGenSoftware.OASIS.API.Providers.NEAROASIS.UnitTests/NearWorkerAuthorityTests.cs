using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearWorkerAuthorityTests
{
    [DataTestMethod]
    [DataRow("probe")][DataRow("get")][DataRow("entries")][DataRow("balance")]
    [DataRow("generateKey")][DataRow("generateMnemonic")][DataRow("derivePublicKey")][DataRow("deriveMnemonic")]
    [DataRow("transactionStatus")][DataRow("view")][DataRow("verifyNativePreparation")][DataRow("recoverNativePreparation")][DataRow("preparedNativeOutcome")]
    [DataRow("transfer")][DataRow("call")][DataRow("putRecordChecked")][DataRow("prepareNativeAction")]
    public async Task ChildReceivesOnlyTheAuthorityItsOperationNeeds(string operation)
    {
        var names=new[]{"NEAROASIS_PRIVATEKEY","OASIS_NEAR_STORAGE_ENCRYPTION_KEY"};var prior=names.Select(Environment.GetEnvironmentVariable).ToArray();
        try{
            Environment.SetEnvironmentVariable(names[0],"controlled-ambient-signing-key");Environment.SetEnvironmentVariable(names[1],"controlled-ambient-storage-key");
        var type=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(type,new object?[]{"http://127.0.0.1:1","fixture","contract.near","service.near","configured-signing-key",null,
            Path.Combine(AppContext.BaseDirectory,"Fixtures","worker-authority.mjs")})!;
        var args=new{privateKey="explicit-signing-key",secretKey="explicit-derivation-key",seedPhrase="explicit-derivation-seed",
            receiverId="receiver.near",amountYocto="1",targetContractId="contract.near",depositYocto="0",methodName="get",argsBase64="e30="};
        var task=(Task<JsonDocument>)type.GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(service,new object?[]{operation,args,CancellationToken.None})!;
        using var response=await task;Assert.IsTrue(response.RootElement.GetProperty("processed").GetBoolean());
        }finally{for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}
    }
}
