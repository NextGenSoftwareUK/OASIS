using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Native SQL records an explicit traversal outcome. The read owner checks it
// before publishing any rows, retaining typed errors rather than parsing SQLite
// exception messages or returning a truncated graph as a negative match.
internal sealed class SqliteCqlTraversalGuard : IDisposable
{
    internal const string FunctionName = "cosmic_cql_traversal_guard";
    internal const string ValidationFunctionName = "cosmic_cql_membership_valid";
    private readonly SqliteConnection _connection;
    private readonly Action _checkBudget;
    private int _failure;
    private Exception _budgetFailure;
    private string _lastOwner;
    private string _lastManifest;

    internal SqliteCqlTraversalGuard(SqliteConnection connection, Action checkBudget)
    {
        _connection = connection;
        _checkBudget = checkBudget;
        connection.CreateFunction<int, int>(FunctionName, code =>
        {
            if (_failure == 0) _failure = code;
            return 0;
        }, isDeterministic: false);
        connection.CreateFunction<string, string, int>(ValidationFunctionName, (manifest, owner) =>
        {
            try
            {
                _checkBudget();
                if (_lastOwner == owner && _lastManifest == manifest) return 1;
                var parsed = Core.Serialization.HolonChildRoleManifestJson.Deserialize(manifest);
                if (!Guid.TryParseExact(owner, "D", out var id) || parsed.ParentId != id)
                    throw new InvalidDataException("Canonical child membership belongs to another parent.");
                _checkBudget();
                // Cache one parent observation only; memory cannot grow with roots.
                _lastOwner = owner; _lastManifest = manifest;
                return 1;
            }
            catch (InvalidDataException)
            {
                if (_failure == 0) _failure = 3;
                return 0;
            }
            catch (Exception cause) when (cause is OperationCanceledException ||
                cause is SqliteCqlDeadlineException || cause is SqliteCqlResourceException)
            {
                // SQLite wraps thrown callbacks as generic SQL errors. Defer the
                // original typed outcome to the owning reader before publication.
                _budgetFailure ??= cause;
                return 0;
            }
        }, isDeterministic: false);
    }

    internal void Check()
    {
        if (_budgetFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_budgetFailure).Throw();
        if (_failure == 1) throw new InvalidDataException("SQLite descendant traversal encountered a cycle.");
        if (_failure == 2) throw new SqliteCqlResourceException("SQLite descendant traversal exceeds its visited-node budget.");
        if (_failure == 3) throw new InvalidDataException("SQLite relationship query encountered invalid canonical membership data.");
    }

    public void Dispose()
    {
        _connection.CreateFunction<int, int>(FunctionName, (Func<int, int>)null);
        _connection.CreateFunction<string, string, int>(ValidationFunctionName, (Func<string, string, int>)null);
    }
}
