using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Solnet.Rpc.Core.Http;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

// SDK adapter: transaction construction remains owned by Metaplex; submission has one owner.
public class FinalizedSolanaRpc:DispatchProxy
{
    private IRpcClient _inner;
    private SolanaRepository _submissions;
    private readonly List<string> _finalized=new();
    private readonly SemaphoreSlim _submissionGate=new(1,1);
    private readonly object _stateGate=new();
    private Exception _submissionFailure;
    public IReadOnlyList<string> FinalizedSignatures {get{lock(_stateGate)return _finalized.ToArray();}}
    public Exception SubmissionFailure {get{lock(_stateGate)return _submissionFailure;}}
    internal Exception DescribeFailure(Exception error)
    {
        var cause=SubmissionFailure ?? error;
        var finalized=FinalizedSignatures;
        return finalized.Count>0 ? new SolanaPartialMutationException(finalized,cause):cause;
    }

    internal static (IRpcClient Client,FinalizedSolanaRpc State) Wrap(IRpcClient client,Solnet.Wallet.Account payer)
    {
        var decorated=Create<IRpcClient,FinalizedSolanaRpc>();
        var state=(FinalizedSolanaRpc)(object)decorated;
        state._inner=client;state._submissions=new SolanaRepository(payer,client);
        return (decorated,state);
    }

    protected override object Invoke(MethodInfo method,object[] args)
    {
        if(method.Name==nameof(IRpcClient.SendTransactionAsync))
        {
            return SubmitAsync(args[0]);
        }
        try {return method.Invoke(_inner,args);}
        catch(TargetInvocationException cause) when(cause.InnerException!=null)
        {
            ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;
        }
    }

    private async Task<RequestResult<string>> SubmitAsync(object transaction)
    {
        // Freeze caller-owned bytes before this invocation can wait behind another submission.
        var frozen=transaction is byte[] source?(byte[])source.Clone():transaction;
        await _submissionGate.WaitAsync();
        try
        {
            if(SubmissionFailure is Exception pending)throw pending;
            var bytes=frozen is byte[] raw ? raw
                : frozen is string encoded ? Convert.FromBase64String(encoded)
                : throw new ArgumentException("Unsupported Solana SDK transaction encoding.");
            var signature=await _submissions.SubmitSignedTransactionAsync(bytes);
            lock(_stateGate)_finalized.Add(signature);
            using var response=new HttpResponseMessage(HttpStatusCode.OK);
            return new RequestResult<string>(response,signature) {WasRequestSuccessfullyHandled=true};
        }
        catch(Exception cause)
        {
            lock(_stateGate)_submissionFailure??=cause;
            throw;
        }
        finally{_submissionGate.Release();}
    }
}
