using System;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Compiler
{
    public static CosmicSetQuery<Holon> CompileSetReduction(CqlSetV1Request request,Guid actor,
        ProviderType? provider,CosmicQueryReadAccess access=null)
    {
        var query=CompileEntitySetSource(request,actor,provider,access,reduction:true);
        if(request.ReductionWhere!=null)
        {
            int filters=0,relationships=0;ValidateWhere(request.ReductionWhere,ref filters,ref relationships);
            var row=Expression.Parameter(typeof(Holon),"row");
            var condition=WhereExpression(request.ReductionWhere,row);
            if(request.Reduction=="All")condition=Expression.Not(condition);
            query=RequiredSet(query.Where(Expression.Lambda<Func<Holon,bool>>(condition,row)));
        }
        return query;
    }
}
