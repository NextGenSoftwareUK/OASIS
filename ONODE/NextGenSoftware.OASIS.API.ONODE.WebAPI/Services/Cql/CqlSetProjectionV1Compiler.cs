using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    public static CosmicSetProjectionQuery<Holon,CosmicProjectionRecord> CompileSetProjection(
        CqlSetV1Request request,Guid actor,ProviderType? provider,CosmicQueryReadAccess access=null)
    {
        if(request?.OutputColumns==null || request.OutputColumns.Length is <1 or >32 || request.IncludeMetadata)
            throw new ArgumentException("Late set selection requires one to32 explicit output columns and no metadata expansion.");
        var source=CompileEntitySetSource(request,actor,provider,access);
        var selected=RequiredSet(source.SelectColumns(request.OutputColumns.Select(ProjectionColumn).ToArray()));
        return request.SelectedStages==null?selected:RequiredSet(selected.WithSelectedPipeline(
            ApplyCompositionStages(selected.Selection,request.SelectedStages)));
    }
}
