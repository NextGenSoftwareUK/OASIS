using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearMutationOutcomeTests
{
    public static IEnumerable<object[]> Cases(){foreach(var transfer in new[]{false,true})foreach(var scenario in Enumerable.Range(0,14))yield return new object[]{transfer,scenario};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task MutationRequiresBoundFinalExecutionSuccess(bool transfer,int scenario)
    {
        const string hash="original-native-transaction";
        var response=JsonNode.Parse("""{"transactionHash":"original-native-transaction","outcome":{"transaction_outcome":{"id":"original-native-transaction"},"transaction":{"signer_id":"sender.near","receiver_id":"receiver.near"},"final_execution_status":"FINAL","status":{"SuccessValue":""}}}""")!;
        var outcome=response["outcome"]!;
        switch(scenario){
            case 1:outcome["status"]=JsonNode.Parse("""{"Failure":{"ActionError":{"index":0}}}""");break;
            case 2:outcome["final_execution_status"]="EXECUTED";break;
            case 3:response["outcome"]=null;break;
            case 4:outcome["transaction_outcome"]!["id"]="other-native-transaction";break;
            case 5:outcome["transaction"]!["signer_id"]="other.near";break;
            case 6:outcome["transaction"]!["receiver_id"]="other.near";break;
            case 7:outcome["status"]=new JsonObject();break;
            case 8:outcome["status"]!["Failure"]=new JsonObject();break;
            case 9:outcome["status"]!["SuccessValue"]=42;break;
            case 10:response["transactionHash"]=null;break;
            case 11:outcome.AsObject().Remove("final_execution_status");break;
            case 12:response.AsObject().Remove("outcome");break;
        }
        var envelope=scenario==13?"""{"ok":false,"error":"controlled transport failure"}""":new JsonObject{["ok"]=true,["result"]=response}.ToJsonString();
        const string setting="OASIS_NEAR_MUTATION_RESPONSE";var previous=Environment.GetEnvironmentVariable(setting);
        try{
            Environment.SetEnvironmentVariable(setting,envelope);
            var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","fixture","receiver.near","sender.near","controlled-key","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","mutation-outcome.mjs")})!;
            var task=(Task<string>)serviceType.GetMethod(transfer?"TransferAsync":"CallAsync")!.Invoke(service,transfer
                ?new object?[]{"receiver.near","1",null,null}:new object?[]{"custom_method",new{},null,"0",null,null})!;
            if(scenario==0){Assert.AreEqual(hash,await task);return;}
            Exception? failure=null;try{await task;}catch(Exception ex){failure=ex;}
            Assert.IsNotNull(failure,"A transaction hash alone must not acknowledge successful execution.");
            Assert.AreEqual(scenario==1?"NearMutationRejectedException":"NearMutationUncertainException",failure.GetType().Name);
            Assert.AreEqual(scenario!=1,NativeMutationReconciliation.IsRequired(failure));
            Assert.AreEqual(scenario is 10 or 13?string.Empty:hash,failure.GetType().GetProperty("OriginalTransactionId")!.GetValue(failure));
        }finally{Environment.SetEnvironmentVariable(setting,previous);}
    }
}
