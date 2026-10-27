using System;
using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

internal sealed partial class NearSdkService
{
    internal async Task<JsonElement> PrepareNativeActionAsync(object request,CancellationToken token)
    {using var result=await InvokeAsync("prepareNativeAction",new{request},token);return result.RootElement.Clone();}
    internal async Task<JsonElement> VerifyNativePreparationAsync(JsonElement preparation,CancellationToken token)
    {using var result=await InvokeAsync("verifyNativePreparation",new{preparation=preparation.Clone()},token);return result.RootElement.Clone();}
    internal async Task<JsonElement> RecoverNativePreparationAsync(object preparation,CancellationToken token)
    {using var result=await InvokeAsync("recoverNativePreparation",new{preparation},token);return result.RootElement.Clone();}
    internal async Task<JsonElement> SubmitNativePreparationAsync(JsonElement preparation,PreparedSubmissionServer owner,CancellationToken token)
    {using var result=await InvokeCoreAsync("submitNativePreparation",new{preparation=preparation.Clone()},token,owner);return result.RootElement.Clone();}
    internal async Task<JsonElement> ReadPreparedNativeOutcomeAsync(JsonElement preparation,CancellationToken token)
    {using var result=await InvokeAsync("preparedNativeOutcome",new{preparation=preparation.Clone()},token);return result.RootElement.GetProperty("outcome").Clone();}

    internal static string ValidatePreparedOutcome(JsonElement original,JsonElement outcome)
    {
        if(outcome.GetProperty("transaction_outcome").GetProperty("id").GetString()!=original.GetProperty("transactionHash").GetString())
            throw new InvalidDataException("Native outcome differs from the retained original.");
        var intent=original.GetProperty("intent");var transaction=outcome.GetProperty("transaction");
        if(transaction.GetProperty("signer_id").GetString()!=original.GetProperty("sender").GetString()
            ||transaction.GetProperty("receiver_id").GetString()!=intent.GetProperty("recipient").GetString())
            throw new InvalidDataException("Native outcome signer/receiver differs from signed original.");
        var kind=intent.GetProperty("kind").GetString();
        if(kind=="transfer")ValidateMutationAction(transaction,new MutationIntent("Transfer",intent.GetProperty("depositUnits").GetString(),null,default,0));
        else if(kind=="functionCall"){
            if(transaction.GetProperty("actions")[0].GetProperty("FunctionCall").GetProperty("args").GetString()!=intent.GetProperty("argsBase64").GetString())
                throw new InvalidDataException("Native outcome calldata differs byte-for-byte from signed original.");
            using var arguments=JsonDocument.Parse(Convert.FromBase64String(intent.GetProperty("argsBase64").GetString()));
            ValidateMutationAction(transaction,new MutationIntent("FunctionCall",intent.GetProperty("depositUnits").GetString(),
                intent.GetProperty("methodName").GetString(),arguments.RootElement.Clone(),ulong.Parse(intent.GetProperty("gasUnits").GetString(),CultureInfo.InvariantCulture)));
        } else throw new InvalidDataException("Unreviewed native action kind.");
        return ClassifyTransactionOutcome(outcome);
    }
}
