using System.Data.Common;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using SqliteContext = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context.DataContext;
using SqliteRepository = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.HolonRepository;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using NeoProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed class RecoveryLostSqliteCommit(Action committed, Exception failure) : DbTransactionInterceptor
    {
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            committed();
            throw failure;
        }
    }
    private sealed class RecoverySqliteContext(string connection, IInterceptor interceptor) : SqliteContext(connection)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
            options.AddInterceptors(interceptor);
        }
    }
    public class RecoveryMongoCommitProxy : DispatchProxy
    {
        public object Target = null!;
        public Exception Failure = null!;
        public Action Committed = null!;
        public static T Wrap<T>(T target, Exception failure, Action committed) where T : class
        {
            var proxy = Create<T, RecoveryMongoCommitProxy>();
            var probe = (RecoveryMongoCommitProxy)(object)proxy;
            probe.Target = target; probe.Failure = failure; probe.Committed = committed;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            object? result;
            try { result = method!.Invoke(Target, args); }
            catch (TargetInvocationException cause) when (cause.InnerException != null)
            { ExceptionDispatchInfo.Capture(cause.InnerException).Throw(); throw; }
            if (result is Task<IClientSessionHandle> session) return WrapSession(session);
            if (Target is IClientSessionHandle && method!.Name == nameof(IClientSessionHandle.CommitTransactionAsync))
                return LoseAcknowledgement((Task)result!);
            return result;
        }
        private async Task<IClientSessionHandle> WrapSession(Task<IClientSessionHandle> pending)
            => Wrap(await pending, Failure, Committed);
        private async Task LoseAcknowledgement(Task commit)
        {
            await commit;
            Committed();
            throw Failure;
        }
    }

    private static async Task<(OASISResult<IEnumerable<IHolon>> Result, int Commits)> ExecuteWithLostCommitAcknowledgement(
        ProviderType type, IOASISStorageProvider provider, ConditionalHolonWriteRequest original, string sqlitePath)
    {
        var commits = 0;
        var failure = new IOException("Owned acknowledgement lost after real native commit.");
        if (type == ProviderType.MongoDBOASIS)
        {
            var context = ((MongoProvider)provider).Database;
            var healthy = context.MongoClient;
            context.MongoClient = RecoveryMongoCommitProxy.Wrap(healthy, failure, () => commits++);
            try
            {
                var result = await ((IConditionalHolonStorageProvider)provider).ExecuteConditionalHolonWriteAsync(original);
                return (result, commits);
            }
            finally { context.MongoClient = healthy; }
        }
        if (type == ProviderType.Neo4jOASIS)
        {
            var field = typeof(NeoProvider).GetField("_driver", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var healthy = (IDriver)field.GetValue(provider)!;
            field.SetValue(provider, Neo4jCommitFaultProxy.Wrap(healthy, failure, () => commits++));
            try
            {
                var result = await ((IConditionalHolonStorageProvider)provider).ExecuteConditionalHolonWriteAsync(original);
                return (result, commits);
            }
            finally { field.SetValue(provider, healthy); }
        }
        if (type == ProviderType.SQLLiteDBOASIS)
        {
            if (!File.Exists(sqlitePath)) throw new InvalidOperationException("Preserve the existing owned SQLite authority.");
            using var context = new RecoverySqliteContext("Data Source=" + sqlitePath + ";Pooling=False",
                new RecoveryLostSqliteCommit(() => commits++, failure));
            var repository = new SqliteRepository(context, () => ((IOASISDBStorageProvider)provider).IsVersionControlEnabled);
            var result = await repository.ExecuteConditionalHolonWriteAsync(original);
            return (result, commits);
        }
        throw new InvalidOperationException("No owned acknowledgement-loss injector exists for this authority.");
    }
}
