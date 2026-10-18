using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Solnet.Rpc.Core.Http;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

// SDK adapter: transaction construction remains owned by Metaplex; submission has one owner.
public class FinalizedSolanaRpc:DispatchProxy
{
    private IRpcClient _inner;
    private SolanaRepository _submissions;
    private readonly List<string> _finalized=new();
    public IReadOnlyList<string> FinalizedSignatures=>_finalized.AsReadOnly();
    public Exception SubmissionFailure {get;private set;}
    internal Exception DescribeFailure(Exception error)
    {
        var cause=SubmissionFailure ?? error;
        return _finalized.Count>0 ? new SolanaPartialMutationException(_finalized,cause):cause;
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
            if(SubmissionFailure!=null) return Task.FromException<RequestResult<string>>(SubmissionFailure);
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
        try
        {
            var bytes=transaction is byte[] raw ? (byte[])raw.Clone()
                : transaction is string encoded ? Convert.FromBase64String(encoded)
                : throw new ArgumentException("Unsupported Solana SDK transaction encoding.");
            var signature=await _submissions.SubmitSignedTransactionAsync(bytes);
            _finalized.Add(signature);
            using var response=new HttpResponseMessage(HttpStatusCode.OK);
            return new RequestResult<string>(response,signature) {WasRequestSuccessfullyHandled=true};
        }
        catch(Exception cause)
        {
            SubmissionFailure=cause;
            throw;
        }
    }
}
