using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NativeProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS:ICosmicNativeTransactionStorageProvider
{
    public async Task<OASISResult<ICosmicNativeTransactionCommands>> OpenCosmicNativeTransactionAsync(
        CosmicQueryReadAccess access,CosmicNativeTransactionOptions options,CancellationToken token=default)
    {
        IAsyncSession session=null;IAsyncTransaction transaction=null;var gateHeld=false;
        try
        {
            if(access==null||access.ActorId==Guid.Empty||options==null
                ||access.Authority.HasValue&&access.Authority.Value!=NativeProviderType.Neo4jOASIS)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="Bind one Neo4j actor and transaction bounds."};
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(options.Duration);
            await _lifecycleGate.WaitAsync(deadline.Token).ConfigureAwait(false);gateHeld=true;
            if(!IsProviderActivated||_driver==null)
                return new(){IsError=true,ErrorCount=1,ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate the writable Neo4j source before opening a held transaction."};
            var driver=_driver;
            session=driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Write));
            transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(options.Duration)
                .WithMetadata(new Dictionary<string,object>{["COSMIC.TransactionId"]=Guid.NewGuid().ToString("D")})).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            return new(new NeoHeldTransactionCommands(this,driver,session,transaction,access,options));
        }
        catch(Exception cause)
        {
            foreach(var resource in new IAsyncDisposable[]{transaction,session})
                if(resource!=null)try{await resource.DisposeAsync().ConfigureAwait(false);}
                catch(Exception cleanup){cause=new AggregateException(cause,cleanup);}
            return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException?"COSMIC_TRANSACTION_CANCELLED":"COSMIC_TRANSACTION_OPEN_FAILED",
                Message="The Neo4j native transaction could not start.",Exception=cause};
        }
        finally{if(gateHeld)_lifecycleGate.Release();}
    }

    private sealed class NeoHeldTransactionCommands:ICosmicNativeTransactionCommands,ICosmicQueryStorageProvider
    {
        private readonly Neo4jOASIS provider;
        private readonly IAsyncSession session;
        private readonly CosmicNativeTransactionOptions options;
        private readonly System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
        private bool commitDispatched,completed,disposed;
        internal IDriver Driver {get;}
        internal IAsyncTransaction Transaction {get;}
        public CosmicQueryReadAccess Access {get;}
        public NativeProviderType Authority=>NativeProviderType.Neo4jOASIS;
        public ICosmicQueryStorageProvider Reads=>this;
        NativeProviderType ICosmicQueryStorageProvider.QueryProviderType=>Authority;
        CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities=>CqlCapabilities;
        internal NeoHeldTransactionCommands(Neo4jOASIS provider,IDriver driver,IAsyncSession session,IAsyncTransaction transaction,
            CosmicQueryReadAccess access,CosmicNativeTransactionOptions options)
        {this.provider=provider;Driver=driver;this.session=session;Transaction=transaction;Access=access;this.options=options;}
        private void RequireActive(bool requireSource=true)
        {
            if(disposed||completed||commitDispatched||requireSource&&(!provider.IsProviderActivated||!ReferenceEquals(provider._driver,Driver)))
                throw new InvalidOperationException("The held Neo4j native owner is no longer active on its captured source.");
        }
        private CancellationTokenSource Bounded(CancellationToken token)
        {
            RequireActive();var remaining=options.Duration-elapsed.Elapsed;
            if(remaining<=TimeSpan.Zero)throw new OperationCanceledException("The held Neo4j transaction deadline expired.",token);
            var cancellation=CancellationTokenSource.CreateLinkedTokenSource(token);cancellation.CancelAfter(remaining);return cancellation;
        }
        public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            if(plan?.Query.ReadAccess==null||!plan.Query.ReadAccess.SamePolicy(Access)||maximumResults>options.MaximumRows)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="The held Neo4j read changed its actor policy or bounds."};
            using var cancellation=Bounded(cancellationToken);
            return await ProjectCqlRowsAsync(provider.ExecuteCqlReadAsync(plan,maximumResults,cancellation.Token,held:this)).ConfigureAwait(false);
        }
        public async Task<OASISResult<IHolon>> ReadAuthorityRecordAsync(Guid id,CancellationToken token)
        {
            using var cancellation=Bounded(token);
            var plan=CosmicNativeTransactionAuthorityRead.Plan(id,Authority,CqlCapabilities);
            if(plan.IsError)return new(){IsError=true,ErrorCount=1,ErrorCode=plan.ErrorCode,Message=plan.Message,Exception=plan.Exception};
            var result=await ProjectCqlRowsAsync(provider.ExecuteCqlReadAsync(plan.Result,2,cancellation.Token,held:this,authorityRead:true)).ConfigureAwait(false);
            return CosmicNativeTransactionAuthorityRead.Capture(id,result);
        }
        public async Task<OASISResult<CosmicStagedUnitOfWork>> StageUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,CancellationToken token)
        {
            using var cancellation=Bounded(token);
            if(request==null||request.Write.ActorId!=Access.ActorId||request.Write.Authority!=Authority)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="The staged Neo4j write changed its captured actor or authority."};
            await provider._lifecycleGate.WaitAsync(cancellation.Token).ConfigureAwait(false);
            try
            {
                RequireActive();
                var staged=await provider.StageNativeUnitOfWorkAsync(request,Transaction,cancellation.Token,allowReplay:false).ConfigureAwait(false);
                var result=staged.Result;
                if(result==null||result.IsError||result.IsWarning||!result.IsSaved||result.Result==null)
                    return new(){IsError=true,ErrorCount=1,ErrorCode=result?.ErrorCode??"COSMIC_TRANSACTION_STAGE_INCOMPLETE",
                        Message=result?.Message??"Neo4j did not return a complete provisional outcome.",Exception=result?.Exception};
                return CosmicStagedUnitOfWork.CaptureProvisional(request.Write.OperationId,result.Result);
            }
            finally{provider._lifecycleGate.Release();}
        }
        public async Task CommitAsync(CancellationToken token)
        {RequireActive(false);token.ThrowIfCancellationRequested();commitDispatched=true;await Transaction.CommitAsync().ConfigureAwait(false);completed=true;}
        public async Task RollbackAsync(CancellationToken token)
        {RequireActive(false);token.ThrowIfCancellationRequested();await Transaction.RollbackAsync().ConfigureAwait(false);completed=true;}
        public async ValueTask DisposeAsync()
        {
            if(disposed)return;
            if(!completed&&!commitDispatched){await Transaction.RollbackAsync().ConfigureAwait(false);completed=true;}
            await Transaction.DisposeAsync().ConfigureAwait(false);await session.DisposeAsync().ConfigureAwait(false);disposed=true;
        }
    }
}
