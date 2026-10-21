using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        private static string OrderingMetadataValue(string alias,Type declared)
        {
            var value=MetadataColumn(alias,declared);
            return (Nullable.GetUnderlyingType(declared)??declared)==typeof(Guid)
                ? "lower(replace("+value+",'-','')) COLLATE BINARY" : value;
        }
        internal string TranslateOrdering(IReadOnlyList<CosmicQueryOrdering> orderings)
        {
            var keys=new List<string>();
            for(var index=0;index<orderings.Count;index++)
            {
                var ordering=orderings[index];var path=ordering.Path;
                var direction=ordering.Descending?" DESC":" ASC";
                if(path.IsMetadata)
                {
                    // Missing, explicit null, valid scalar, invalid domain. Invalid
                    // data never participates through a coerced scalar sort key.
                    var prefix="cqlsort"+index;
                    var rank="COALESCE(("+MetadataSubquery(path.Segments,alias=>
                        "CASE WHEN "+alias+".type = 'null' THEN 1 WHEN "+MetadataTypeGuard(path.ValueType,alias)+" THEN 2 ELSE 3 END",
                        _=>"1",prefix+"r")+" LIMIT 1),0)";
                    var value="("+MetadataSubquery(path.Segments,alias=>
                        "CASE WHEN "+MetadataTypeGuard(path.ValueType,alias)+" THEN "+OrderingMetadataValue(alias,path.ValueType)+" ELSE NULL END",
                        _=>"1",prefix+"v")+" LIMIT 1)";
                    keys.Add(rank+direction);keys.Add(value+direction);
                }
                else
                {
                    FieldOperand(path,"unused",(value,_)=> {
                        keys.Add("CASE WHEN "+value+" IS NULL THEN 1 ELSE 2 END"+direction);
                        keys.Add(value+direction);return "1";
                    });
                }
            }
            // Logical identity makes ties deterministic within this native query.
            keys.Add(CanonicalColumn("Id")+" COLLATE BINARY ASC");
            return string.Join(", ",keys);
        }
    }
}
