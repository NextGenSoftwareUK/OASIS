using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS:ICosmicNativeTransactionStorageProvider
{
    public async Task<OASISResult<ICosmicNativeTransactionCommands>> OpenCosmicNativeTransactionAsync(
        CosmicQueryReadAccess access,CosmicNativeTransactionOptions options,CancellationToken token=default)
    {
        try
        {
            if(access==null||access.ActorId==Guid.Empty||options==null
                ||access.Authority.HasValue&&access.Authority.Value!=ProviderType.MongoDBOASIS)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="Bind one native MongoDB actor and transaction bounds."};
            if(!IsProviderActivated||_holonRepository is not HolonRepository repository)
                return new(){IsError=true,ErrorCount=1,ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate MongoDB before opening a held transaction."};
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(options.Duration);
            var session=await repository.OpenHeldTransactionAsync(deadline.Token).ConfigureAwait(false);
            return new(new MongoHeldTransactionCommands(this,repository,session,access,options));
        }
        catch(Exception cause)
        {return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException?"COSMIC_TRANSACTION_CANCELLED":"COSMIC_TRANSACTION_OPEN_FAILED",
            Message="The MongoDB native transaction could not start.",Exception=cause};}
    }

    private sealed class MongoHeldTransactionCommands:ICosmicNativeTransactionCommands,ICosmicQueryStorageProvider
    {
        private readonly MongoDBOASIS provider;
        private readonly CosmicNativeTransactionOptions options;
        private readonly System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
        private bool commitDispatched,completed,disposed;
        internal HolonRepository Repository {get;}
        internal IClientSessionHandle Session {get;}
        public CosmicQueryReadAccess Access {get;}
        public ProviderType Authority=>ProviderType.MongoDBOASIS;
        public ICosmicQueryStorageProvider Reads=>this;
        ProviderType ICosmicQueryStorageProvider.QueryProviderType=>Authority;
        CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities=>CqlCapabilities;
        internal MongoHeldTransactionCommands(MongoDBOASIS provider,HolonRepository repository,IClientSessionHandle session,
            CosmicQueryReadAccess access,CosmicNativeTransactionOptions options)
        {this.provider=provider;Repository=repository;Session=session;Access=access;this.options=options;}
        internal TimeSpan Remaining()=>options.Duration-elapsed.Elapsed;
        private void RequireActive()
        {if(disposed||completed||commitDispatched)throw new InvalidOperationException("The native MongoDB transaction is no longer active.");}
        private CancellationTokenSource Bounded(CancellationToken token)
        {
            RequireActive();var remaining=Remaining();
            if(remaining<=TimeSpan.Zero)throw new OperationCanceledException("The held MongoDB transaction deadline expired.",token);
            var cancellation=CancellationTokenSource.CreateLinkedTokenSource(token);cancellation.CancelAfter(remaining);return cancellation;
        }
        public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            if(plan?.Query.ReadAccess==null||!plan.Query.ReadAccess.SamePolicy(Access)||maximumResults>options.MaximumRows)
                return CqlFailure("COSMIC_TRANSACTION_SCOPE_INVALID","The held MongoDB read changed its actor policy or bounds.");
            using var cancellation=Bounded(cancellationToken);
            return await provider.ExecuteCqlRowsAsync(plan,maximumResults,cancellation.Token,held:this).ConfigureAwait(false);
        }
        public async Task<OASISResult<CosmicStagedUnitOfWork>> StageUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,CancellationToken token)
        {
            using var cancellation=Bounded(token);
            var result=await provider.ExecuteConditionalUnitOfWorkWithinOwnerAsync(request,cancellation.Token,this).ConfigureAwait(false);
            if(result==null||result.IsError||result.IsWarning||!result.IsSaved||result.Result==null)
                return new(){IsError=true,ErrorCount=1,ErrorCode=result?.ErrorCode??"COSMIC_TRANSACTION_STAGE_INCOMPLETE",
                    Message=result?.Message??"MongoDB did not return a complete provisional write outcome.",Exception=result?.Exception};
            return CosmicStagedUnitOfWork.CaptureProvisional(request.Write.OperationId,result.Result);
        }
        public async Task CommitAsync(CancellationToken token)
        {
            RequireActive();commitDispatched=true;
            await Session.CommitTransactionAsync(token).ConfigureAwait(false);completed=true;
        }
        public async Task RollbackAsync(CancellationToken token)
        {
            RequireActive();
            if(Session.IsInTransaction)await Session.AbortTransactionAsync(token).ConfigureAwait(false);
            completed=true;
        }
        public async ValueTask DisposeAsync()
        {
            if(disposed)return;
            if(!completed&&!commitDispatched&&Session.IsInTransaction)
            {
                using var cleanup=new CancellationTokenSource(options.Duration);
                await Session.AbortTransactionAsync(cleanup.Token).ConfigureAwait(false);completed=true;
            }
            Session.Dispose();disposed=true;
        }
    }
}
