using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // Reuse the canonical SQLite AST translator; index capability admission restricts its surface.
    internal static (string Predicate,string Ordering,List<SqliteParameter> Parameters) CompileChainIndexRows<T>(CosmicQuery<T> query) where T:IHolon
    {
        var translator=new CqlSqlTranslator(query.ReadAccess);
        return (translator.TranslateSource(query.Predicates),translator.TranslateOrdering(query.Orderings),translator.Parameters);
    }
    internal static string CompileChainIndexProjection(IReadOnlyList<CosmicProjectionColumn> columns,List<SqliteParameter> parameters)
        =>new CqlSqlTranslator("Holon",parameters,null){IndexedProjectionColumns=true}.TranslateProjection(columns);

    internal static object DecodeChainIndexProjection(object value,System.Type declared)=>DecodeProjectionCell(value,declared);

}
