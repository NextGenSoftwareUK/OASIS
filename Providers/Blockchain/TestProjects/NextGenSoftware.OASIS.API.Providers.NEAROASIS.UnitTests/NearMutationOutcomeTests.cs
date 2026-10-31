using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearMutationOutcomeTests
{
    public static IEnumerable<object[]> Cases(){foreach(var transfer in new[]{false,true})foreach(var scenario in Enumerable.Range(0,23))yield return new object[]{transfer,scenario};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task MutationRequiresBoundFinalExecutionSuccess(bool transfer,int scenario)
    {
        const string hash="original-native-transaction";
        var response=JsonNode.Parse("""{"transactionHash":"original-native-transaction","outcome":{"transaction_outcome":{"id":"original-native-transaction"},"transaction":{"signer_id":"sender.near","receiver_id":"receiver.near"},"final_execution_status":"FINAL","status":{"SuccessValue":""}}}""")!;
        var outcome=response["outcome"]!;
        var action=transfer?JsonNode.Parse("""{"Transfer":{"deposit":"1"}}""")!
            :JsonNode.Parse("""{"FunctionCall":{"method_name":"custom_method","deposit":"0","gas":100000000000000,"args":""}}""")!;
        if(!transfer)action["FunctionCall"]!["args"]=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("""{"title":"Café","count":1}"""));
        outcome["transaction"]!["actions"]=new JsonArray(action);
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
            case 14:action[transfer?"Transfer":"FunctionCall"]!["deposit"]="999";break;
            case 15:outcome["transaction"]!["actions"]=JsonNode.Parse("""[{"DeleteAccount":{"beneficiary_id":"other.near"}}]""");break;
            case 16:outcome["transaction"]!["actions"]!.AsArray().Add(action.DeepClone());break;
            case 17:outcome["transaction"]!["actions"]=new JsonArray();break;
            case 18:outcome["transaction"]!.AsObject().Remove("actions");break;
            case 19:action[transfer?"Transfer":"FunctionCall"]!["method_name"]="different_method";break;
            case 20:
                if(transfer)action["Transfer"]!["deposit"]=1;
                else action["FunctionCall"]!["args"]=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("""{"title":"Café","count":2}"""));
                break;
            case 21:
                if(transfer)action["Transfer"]!["unexpected"]=true;
                else action["FunctionCall"]!["gas"]="100000000000000";
                break;
            case 22:
                if(transfer)action["Transfer"]!["deposit"]="2";
                else action["FunctionCall"]!["gas"]=100000000000001UL;
                break;
        }
        var envelope=scenario==13?"""{"ok":false,"error":"controlled transport failure"}""":new JsonObject{["ok"]=true,["result"]=response}.ToJsonString();
        const string setting="OASIS_NEAR_MUTATION_RESPONSE";var previous=Environment.GetEnvironmentVariable(setting);
        try{
            Environment.SetEnvironmentVariable(setting,envelope);
            var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","fixture","receiver.near","sender.near","controlled-key","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","mutation-outcome.mjs")})!;
            var task=(Task<string>)serviceType.GetMethod(transfer?"TransferAsync":"CallAsync")!.Invoke(service,transfer
                ?new object?[]{"receiver.near","1",null,null}:new object?[]{"custom_method",new{title="Café",count=1},null,"0",null,null})!;
            if(scenario==0){Assert.AreEqual(hash,await task);return;}
            Exception? failure=null;try{await task;}catch(Exception ex){failure=ex;}
            Assert.IsNotNull(failure,"A transaction hash alone must not acknowledge successful execution.");
            Assert.AreEqual(scenario==1?"NearMutationRejectedException":"NearMutationUncertainException",failure.GetType().Name);
            Assert.AreEqual(scenario!=1,NativeMutationReconciliation.IsRequired(failure));
            Assert.AreEqual(scenario is 10 or 13?string.Empty:hash,failure.GetType().GetProperty("OriginalTransactionId")!.GetValue(failure));
        }finally{Environment.SetEnvironmentVariable(setting,previous);}
    }
}
