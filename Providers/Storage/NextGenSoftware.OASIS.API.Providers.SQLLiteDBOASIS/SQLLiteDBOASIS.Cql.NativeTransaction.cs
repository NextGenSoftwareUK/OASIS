using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NativeProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS:ICosmicNativeTransactionStorageProvider
{
    public async Task<OASISResult<ICosmicNativeTransactionCommands>> OpenCosmicNativeTransactionAsync(
        CosmicQueryReadAccess access,CosmicNativeTransactionOptions options,CancellationToken token=default)
    {
        DataContext context=null;SqliteTransaction native=null;IDbContextTransaction enlisted=null;
        try
        {
            if(access==null||access.ActorId==Guid.Empty||options==null
                ||access.Authority.HasValue&&access.Authority.Value!=NativeProviderType.SQLLiteDBOASIS)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="Bind one SQLite actor and transaction bounds."};
            if(!IsProviderActivated)
                return new(){IsError=true,ErrorCount=1,ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before opening a held transaction."};
            var settings=new SqliteConnectionStringBuilder(_appDataContext.Database.GetDbConnection().ConnectionString);
            if(string.IsNullOrWhiteSpace(settings.DataSource)||settings.DataSource==":memory:"
                ||settings.Mode==SqliteOpenMode.Memory&&settings.Cache!=SqliteCacheMode.Shared)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SOURCE_UNSUPPORTED",
                    Message="A held SQLite owner requires the configured file or an explicitly named shared-memory store; private temporary databases cannot be cloned."};
            settings.DefaultTimeout=Math.Max(1,(int)Math.Ceiling(options.Duration.TotalSeconds));
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(options.Duration);
            context=new DataContext(settings.ConnectionString);
            await context.Database.OpenConnectionAsync(deadline.Token).ConfigureAwait(false);
            native=((SqliteConnection)context.Database.GetDbConnection()).BeginTransaction(IsolationLevel.Serializable,deferred:false);
            enlisted=context.Database.UseTransaction(native);
            var versioning=IsVersionControlEnabled;
            return new(new SqliteHeldTransactionCommands(this,_holonRepository,context,new HolonRepository(context,()=>versioning),
                native,enlisted,access,options));
        }
        catch(Exception cause)
        {
            foreach(var resource in new IAsyncDisposable[]{enlisted,native,context})
                if(resource!=null)try{await resource.DisposeAsync().ConfigureAwait(false);}
                catch(Exception cleanup){cause=new AggregateException(cause,cleanup);}
            return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException?"COSMIC_TRANSACTION_CANCELLED":"COSMIC_TRANSACTION_OPEN_FAILED",
                Message="The SQLite native transaction could not start.",Exception=cause};
        }
    }

    private sealed class SqliteHeldTransactionCommands:ICosmicNativeTransactionCommands,ICosmicQueryStorageProvider
    {
        private readonly SQLLiteDBOASIS provider;
        private readonly object sourceRepository;
        private readonly DataContext context;
        private readonly HolonRepository repository;
        private readonly SqliteTransaction native;
        private readonly IDbContextTransaction enlisted;
        private readonly CosmicNativeTransactionOptions options;
        private readonly System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
        private bool commitDispatched,completed,disposed;
        public CosmicQueryReadAccess Access {get;}
        public NativeProviderType Authority=>NativeProviderType.SQLLiteDBOASIS;
        public ICosmicQueryStorageProvider Reads=>this;
        NativeProviderType ICosmicQueryStorageProvider.QueryProviderType=>Authority;
        CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities=>CqlCapabilities;
        internal SqliteHeldTransactionCommands(SQLLiteDBOASIS provider,object sourceRepository,DataContext context,
            HolonRepository repository,SqliteTransaction native,IDbContextTransaction enlisted,CosmicQueryReadAccess access,
            CosmicNativeTransactionOptions options)
        {this.provider=provider;this.sourceRepository=sourceRepository;this.context=context;this.repository=repository;
            this.native=native;this.enlisted=enlisted;Access=access;this.options=options;}
        private TimeSpan Remaining()=>options.Duration-elapsed.Elapsed;
        private void RequireActive(bool requireSource=true)
        {
            if(disposed||completed||commitDispatched
                ||requireSource&&(!provider.IsProviderActivated||!ReferenceEquals(sourceRepository,provider._holonRepository)))
                throw new InvalidOperationException("The held SQLite native owner is no longer active on the captured storage instance.");
        }
        private CancellationTokenSource Bounded(CancellationToken token)
        {
            RequireActive();var remaining=Remaining();
            if(remaining<=TimeSpan.Zero)throw new OperationCanceledException("The held SQLite transaction deadline expired.",token);
            ((SqliteConnection)context.Database.GetDbConnection()).DefaultTimeout=Math.Max(1,(int)Math.Ceiling(remaining.TotalSeconds));
            var cancellation=CancellationTokenSource.CreateLinkedTokenSource(token);cancellation.CancelAfter(remaining);return cancellation;
        }
        public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            if(plan?.Query.ReadAccess==null||!plan.Query.ReadAccess.SamePolicy(Access)||maximumResults>options.MaximumRows)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="The held SQLite read changed its actor policy or bounds."};
            using var cancellation=Bounded(cancellationToken);
            return await repository.ExecuteCosmicQueryAsync(plan,maximumResults,cancellation.Token).ConfigureAwait(false);
        }
        public Task<OASISResult<CosmicStagedUnitOfWork>> StageUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,CancellationToken token)
        {
            using var cancellation=Bounded(token);
            if(request==null||request.Write.ActorId!=Access.ActorId||request.Write.Authority!=Authority)
                return Task.FromResult(new OASISResult<CosmicStagedUnitOfWork>{IsError=true,ErrorCount=1,
                    ErrorCode="COSMIC_TRANSACTION_SCOPE_INVALID",Message="The staged SQLite write changed its captured actor or authority."});
            using var budget=new SqliteCqlReadBudget((SqliteConnection)context.Database.GetDbConnection(),cancellation.Token,deadline:Remaining());
            budget.Check();var result=repository.ExecuteUnitOfWorkWithinOwner(request,cancellation.Token,native);budget.Check();
            if(result==null||result.IsError||result.IsWarning||!result.IsSaved||result.Result==null)
                return Task.FromResult(new OASISResult<CosmicStagedUnitOfWork>{IsError=true,ErrorCount=1,
                    ErrorCode=result?.ErrorCode??"COSMIC_TRANSACTION_STAGE_INCOMPLETE",
                    Message=result?.Message??"SQLite did not return a complete provisional outcome.",Exception=result?.Exception});
            return Task.FromResult(CosmicStagedUnitOfWork.CaptureProvisional(request.Write.OperationId,result.Result));
        }
        public async Task CommitAsync(CancellationToken token)
        {
            RequireActive(false);
            ((SqliteConnection)context.Database.GetDbConnection()).DefaultTimeout=Math.Max(1,(int)Math.Ceiling(Math.Max(0,Remaining().TotalSeconds)));
            commitDispatched=true;await native.CommitAsync(token).ConfigureAwait(false);completed=true;
        }
        public async Task RollbackAsync(CancellationToken token)
        {RequireActive(false);
            if(SQLitePCL.raw.sqlite3_get_autocommit(((SqliteConnection)context.Database.GetDbConnection()).Handle)==0)
                await native.RollbackAsync(token).ConfigureAwait(false);
            completed=true;}
        public async ValueTask DisposeAsync()
        {
            if(disposed)return;
            if(!completed&&!commitDispatched)
            {using var cleanup=new CancellationTokenSource(options.Duration);
                if(SQLitePCL.raw.sqlite3_get_autocommit(((SqliteConnection)context.Database.GetDbConnection()).Handle)==0)
                    await native.RollbackAsync(cleanup.Token).ConfigureAwait(false);
                completed=true;}
            await enlisted.DisposeAsync().ConfigureAwait(false);await native.DisposeAsync().ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);disposed=true;
        }
    }
}
