using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    internal const string JoinFetchLanguage = "SQLite SQL";

    /// <summary>
    /// A13 join fetch: the admitted root window (its predicate, read access, ordering and page come from
    /// the one root compiler in ExecuteCqlReadAsync) and its direct children in ONE SQL statement: a root
    /// CTE joined to Holon on ParentHolonId, with the child read-access predicate in the same statement.
    /// </summary>
    public async Task<OASISResult<CosmicJoinFetchPacket>> ExecuteCosmicJoinFetchAsync<TRoot, TChild>(
        CosmicJoinFetchPlan<TRoot, TChild> plan, CancellationToken cancellationToken) where TRoot : IHolon where TChild : IHolon
    {
        if (plan?.Roots == null || plan.Children == null)
            return new OASISResult<CosmicJoinFetchPacket> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_PLAN_INVALID", Message = "An admitted join-fetch plan is required." };
        var request = new SqliteJoinFetch(plan.Children.ReadAccess, plan.Children.Predicates, plan.MaximumChildren, JoinFetchColumns());
        var read = await ExecuteCqlReadAsync(plan.Roots, plan.MaximumRoots, cancellationToken, joinFetch: request).ConfigureAwait(false);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(read, new OASISResult<CosmicJoinFetchPacket>());
        if (!read.IsError)
            result.Result = new CosmicJoinFetchPacket(read.Result.Rows, request.Children, request.NativeCommands, JoinFetchLanguage, request.Command);
        return result;
    }

    private (IProperty Property, string Column)[] JoinFetchColumns()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(HolonModel))
            ?? throw new InvalidOperationException("The SQLite holon entity is not mapped.");
        var table = StoreObjectIdentifier.Table(entity.GetTableName(), entity.GetSchema());
        return entity.GetProperties().Where(property => !property.IsShadowProperty())
            .Select(property => (property, property.GetColumnName(table))).ToArray();
    }

    private sealed class SqliteJoinFetch
    {
        internal readonly CosmicQueryReadAccess ReadAccess;
        internal readonly IReadOnlyList<CosmicQueryPredicate> Predicates;
        internal readonly int MaximumChildren;
        internal readonly (IProperty Property, string Column)[] Columns;
        internal IReadOnlyList<IHolon> Children = Array.Empty<IHolon>();
        internal string Command;
        internal int NativeCommands;
        internal SqliteJoinFetch(CosmicQueryReadAccess readAccess, IReadOnlyList<CosmicQueryPredicate> predicates,
            int maximumChildren, (IProperty, string)[] columns)
        { ReadAccess = readAccess; Predicates = predicates; MaximumChildren = maximumChildren; Columns = columns; }

        /// <summary>One statement; parameters are shared with the root translator so names never collide.</summary>
        internal string Compile(CqlSqlTranslator roots, string rootPredicate, string rootOrdering)
        {
            var childTranslator = new CqlSqlTranslator("Holon", roots.Parameters, ReadAccess);
            var childPredicate = childTranslator.TranslateSource(Predicates);
            if (childTranslator.HasRelationship)
                throw new NotSupportedException("SQLite join fetch admits direct-child predicates without relationship traversal.");
            var childLimit = roots.Bind(MaximumChildren + 1);
            string Columns(string table) => string.Join(",", this.Columns.Select(column => "\"" + table + "\".\"" + column.Column + "\""));
            return "WITH \"JoinFetchRoots\" AS (SELECT \"Holon\".*,row_number() OVER (ORDER BY " + rootOrdering + ") AS \"JoinFetchOrdinal\" "
                + "FROM Holon WHERE " + rootPredicate + " ORDER BY " + rootOrdering + " LIMIT $limit OFFSET $offset) "
                + "SELECT 0 AS \"JoinFetchRole\",\"JoinFetchRoots\".\"JoinFetchOrdinal\" AS \"JoinFetchOrdinal\","
                + "lower(\"JoinFetchRoots\".\"Id\") AS \"JoinFetchKey\"," + Columns("JoinFetchRoots") + " FROM \"JoinFetchRoots\" "
                + "UNION ALL SELECT * FROM (SELECT 1,\"JoinFetchRoots\".\"JoinFetchOrdinal\",lower(\"Holon\".\"Id\")," + Columns("Holon")
                + " FROM \"JoinFetchRoots\" JOIN Holon ON \"Holon\".\"ParentHolonId\" = \"JoinFetchRoots\".\"Id\" COLLATE NOCASE"
                + " WHERE " + childPredicate + " ORDER BY 2,3 LIMIT " + childLimit + ") "
                + "ORDER BY 1,2,3";
        }
    }

    private async Task<IReadOnlyList<IHolon>> ReadJoinFetchAsync(SqliteConnection connection, string sql,
        IReadOnlyList<SqliteParameter> parameters, SqliteJoinFetch joinFetch, SqliteCqlReadBudget budget, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.SqliteType) { Value = parameter.Value });
        var roots = new List<IHolon>(); var children = new List<IHolon>(); long bytes = 0;
        budget.Check();
        joinFetch.NativeCommands++;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                budget.Check();
                var role = reader.GetInt64(0);
                var model = new HolonModel();
                for (var index = 0; index < joinFetch.Columns.Length; index++)
                    SetJoinFetchValue(model, joinFetch.Columns[index].Property, reader, index + 3);
                bytes += CqlRecordSize(model);
                if (bytes > SqliteCqlReadBudget.MaximumPayloadBytes)
                    throw new SqliteCqlResourceException("SQLite join fetch exceeds its streamed payload budget.");
                (role == 0 ? roots : role == 1 ? children : throw new System.IO.InvalidDataException("SQLite join fetch returned an unknown row role."))
                    .Add(GetHolonFromEntity(model));
            }
        }
        budget.Check();
        joinFetch.Children = children.AsReadOnly();
        return roots.AsReadOnly();
    }

    private static readonly MethodInfo JoinFetchFieldValue = typeof(SqliteDataReader).GetMethods()
        .Single(method => method.Name == nameof(SqliteDataReader.GetFieldValue) && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);

    // Materialize through EF's own mapping: provider type, value converter and field/property access.
    private static void SetJoinFetchValue(HolonModel model, IProperty property, SqliteDataReader reader, int ordinal)
    {
        object value = null;
        if (!reader.IsDBNull(ordinal))
        {
            var mapping = property.GetTypeMapping();
            var providerType = mapping.Converter?.ProviderClrType ?? mapping.ClrType;
            providerType = Nullable.GetUnderlyingType(providerType) ?? providerType;
            value = JoinFetchFieldValue.MakeGenericMethod(providerType).Invoke(reader, new object[] { ordinal });
            if (mapping.Converter != null) value = mapping.Converter.ConvertFromProvider(value);
        }
        else if (property.ClrType.IsValueType && Nullable.GetUnderlyingType(property.ClrType) == null) return;
        if (property.FieldInfo != null) property.FieldInfo.SetValue(model, value);
        else property.PropertyInfo.SetValue(model, value);
    }
}
