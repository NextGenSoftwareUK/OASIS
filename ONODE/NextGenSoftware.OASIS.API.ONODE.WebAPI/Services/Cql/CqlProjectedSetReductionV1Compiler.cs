using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Compiler
{
    public static CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> CompileProjectedSetReduction(
        CqlSetV1Request request,Guid actor,ProviderType? provider,CosmicQueryReadAccess access=null)
    {
        var query=CompileProjectedSetSource(request,actor,provider,access,true);
        if(request.ReductionWhere==null)return query;
        int filters=0,relationships=0;ValidateWhere(request.ReductionWhere,ref filters,ref relationships);
        if(relationships!=0)throw new ArgumentException("Selected reductions use visible aliases, without relationship predicates.");
        var row=Expression.Parameter(typeof(CosmicProjectionRecord),"row");
        Expression Condition(CqlWhereV1Predicate node)
        {
            if(node.Kind=="Filter")
            {
                var filter=node.Filter;
                if(filter.MetadataPath!=null||filter.Function!=null||string.IsNullOrEmpty(filter.Field))
                    throw new ArgumentException("Selected reduction filters name visible aliases; select computed values before reducing.");
                var position=query.Output.OutputAliases.IndexOf(filter.Field);
                if(position<0)throw new ArgumentException("Selected reduction filter alias is not visible.");
                var column=query.Output.Columns[query.Output.OutputColumnIndices[position]];
                Expression Term(CqlV1Operator operation,JsonElement value)=>CompileSelectedFilter(column.ValueType,filter.Field,operation,value,row).Body;
                if(filter.Operator==CqlV1Operator.In)
                {
                    if(filter.Value.ValueKind!=JsonValueKind.Array||filter.Value.GetArrayLength() is <1 or >100)
                        throw new ArgumentException("Selected membership requires one to100 exact literals.");
                    Expression members=null;
                    foreach(var value in filter.Value.EnumerateArray())members=members==null?Term(CqlV1Operator.Equal,value):Expression.OrElse(members,Term(CqlV1Operator.Equal,value));
                    return members;
                }
                return Term(filter.Operator,filter.Value);
            }
            if(node.Kind=="Not")return Expression.Not(Condition(node.Terms[0]));
            if(node.Kind is not ("And" or "Or"))throw new ArgumentException("Selected reduction conditions accept filters and Boolean groups only.");
            var expression=Condition(node.Terms[0]);
            foreach(var term in node.Terms.Skip(1))expression=node.Kind=="And"?Expression.AndAlso(expression,Condition(term)):Expression.OrElse(expression,Condition(term));
            return expression;
        }
        var predicate=Condition(request.ReductionWhere);
        if(request.Reduction=="All")predicate=Expression.Not(predicate);
        return RequiredSet(query.Where(Expression.Lambda<Func<CosmicProjectionRecord,bool>>(predicate,row)));
    }
}
