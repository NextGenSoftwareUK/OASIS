using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class MetadataAdmissionReader(CosmicChainQuerySource selected):ICosmicChainQueryIndexScalarReader
    {
        public int Calls;
        public CosmicChainQuerySource Source=>selected;
        public CosmicQueryCapabilities IndexedRowCapabilities=>new CosmicQueryCapabilities(
            Capabilities.Features|CosmicQueryFeatures.Metadata|CosmicQueryFeatures.MetadataExists|CosmicQueryFeatures.MetadataNull,
            Capabilities.CanonicalFields.ToArray(),new[]{new CosmicQueryMetadataCapability(typeof(string),
                new[]{CosmicQueryComparisonOperator.Equal},supportsOrdering:true)}).WithMetadataRoots(new[]{"Status"});
        public CosmicScalarCapabilities IndexedScalarCapabilities=>new(new[]{CosmicScalarOperation.Count,CosmicScalarOperation.LongCount,CosmicScalarOperation.Any});
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {Calls++;throw new InvalidOperationException("Preparation must not read storage/source.");}
        public Task<OASISResult<CosmicChainIndexCardinality>> QueryCardinalityAsync<T,TResult>(CosmicScalarPlan<T,TResult> plan,
            ICosmicChainQuerySourceObserver observer,CancellationToken cancellationToken=default) where T:IHolon
        {Calls++;throw new InvalidOperationException("Preparation must not execute a scalar.");}
    }

    [Theory]
    [InlineData("rows")] [InlineData("LongCount")] [InlineData("Any")]
    [InlineData("private")] [InlineData("case")] [InlineData("envelope")]
    public void Schema3Web4AdmissionFreezesQueryableMetadataAndRejectsUnenrolledRootsWithoutIo(string mode)
    {
        var selected=new CosmicChainQuerySource(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,
            source.IndexId,3,source.CatalogueHash,source.CoverageStartSlot);
        var reader=new MetadataAdmissionReader(selected);
        var tariffs=new CosmicIndexedReadTariffPolicy(selected,100,CosmicIndexedReadTariffPolicy.Required.Select(
            op=>new CosmicIndexedReadTariff(op,0.001m,"metadata-preparation")));
        Assert.NotEqual(Tariffs().BindingHash,tariffs.BindingHash); // Same index identity, distinct schema reservation domain.
        var binding=new CqlIndexedV1Binding(reader,Mock.Of<ICosmicChainQuerySourceObserver>(),tariffs,position,10,
            TimeSpan.FromMinutes(1),CosmicQueryReadAccess.OwnerOrPublic(actor).Result,Manager());
        var root=mode=="private"?"private":mode=="case"?"status":mode=="envelope"?"__oasis_enc__":"Status";
        var filter=new CqlV1Filter{MetadataPath=new[]{root,"category"},Scalar=CqlV1Scalar.String,
            Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement("coffee")};
        var rowRequest=new CqlIndexedV1Request{Alias="metadata-demo",Query=new(){OrderBy="Id",Take=1,Filters=new[]{filter}}};
        var scalarRequest=new CqlIndexedScalarV1Request{Alias="metadata-demo",Query=new(){Operation=mode=="Any"?"Any":"LongCount",
            OrderBy="Id",Filters=new[]{filter}}};
        var registry=new BindingRegistry(binding,()=>filter.MetadataPath[0]="changed-after-capture");
        var prepared=mode is "LongCount" or "Any"
            ? CqlIndexedV1Preparation.Prepare(actor,scalarRequest,registry)
            : CqlIndexedV1Preparation.Prepare(actor,rowRequest,registry);
        if(mode is "private" or "case" or "envelope")Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",prepared.ErrorCode);
        else
        {
            Assert.False(prepared.IsError,prepared.Message);
            Assert.Equal(100,prepared.Result.MaximumUnits);
            Assert.Same(tariffs,prepared.Result.Binding.Tariffs);
            Assert.False(mode=="rows"?prepared.Result.Matches(rowRequest):prepared.Result.Matches(scalarRequest));
            filter.MetadataPath[0]="Status";
            Assert.True(mode=="rows"?prepared.Result.Matches(rowRequest):prepared.Result.Matches(scalarRequest));
            if(mode=="rows")Assert.False(prepared.Result.IsScalar);
            if(mode=="LongCount")Assert.Null(prepared.Result.LongCountPlan.Query.Source.Limit);
            if(mode=="Any")Assert.NotNull(prepared.Result.AnyPlan);
        }
        Assert.Equal(0,reader.Calls);Assert.Equal(1,registry.Calls);
    }
    [Fact]
    public void IndexedTariffsDoNotEnrollHistoryOnlyOrUnknownSchemas()
    {
        foreach(var schema in new[]{1,4})
        {
            var selected=new CosmicChainQuerySource(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,
                source.IndexId,schema,source.CatalogueHash,source.CoverageStartSlot);
            Assert.Throws<ArgumentException>(()=>new CosmicIndexedReadTariffPolicy(selected,100,
                CosmicIndexedReadTariffPolicy.Required.Select(op=>new CosmicIndexedReadTariff(op,0.001m,"metadata-test"))));
        }
    }

    private sealed class MetadataWitnessReader(Reader inner,string mode):ICosmicChainQueryIndexProjectionReader
    {
        public int Calls;
        public CosmicChainQuerySource Source=>inner.Source;
        public CosmicQueryCapabilities IndexedRowCapabilities=>inner.IndexedRowCapabilities;
        public CosmicProjectionCapabilities IndexedProjectionCapabilities=>mode=="wildcard"
            ?new(Array.Empty<CosmicProjectionFieldCapability>(),new[]{typeof(string)})
            :new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),new[]{typeof(string)}).WithMetadataRoots(new[]{"Status"});
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,ICosmicChainQuerySourceObserver observer,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon=>throw new InvalidOperationException("Selected dispatch only.");
        public Task<OASISResult<CosmicChainIndexProjectionRows>> QueryProjectionAsync<T,TResult>(CosmicProjectionPlan<T,TResult> plan,
            ICosmicChainQuerySourceObserver observer,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;
            return Task.FromResult(new OASISResult<CosmicChainIndexProjectionRows>(new(inner.Result.Result,
                new[]{new CosmicProjectionRow(mode=="forged"?"forged":"coffee")})){IsLoaded=true});
        }
    }
    public sealed class MetadataWitnessDto
    {
        public static int Constructed;
        public MetadataWitnessDto(){Constructed++;}
        public string Category{get;set;}="";
    }

    [Theory]
    [InlineData("valid",null)] [InlineData("forged","CQL_SOURCE_RECORD_MISMATCH")]
    [InlineData("wildcard","CQL_CAPABILITY_UNSUPPORTED")]
    public async Task IndexedSelectedMetadataRequiresExplicitRootsAndProofBeforeDtoConstruction(string mode,string? error)
    {
        var fixture=Fixture("rows");
        fixture.Row.MetaData=new(){["Status"]=new Dictionary<string,object>{["category"]="coffee"}};
        var reader=new MetadataWitnessReader(fixture.Reader,mode);
        var selected=Plan().Query.Select(row=>new MetadataWitnessDto{
            Category=CosmicQueryMetadata.Value<string>(row,"Status","category")}).Result;
        var planned=CosmicProjectionPlanner.Plan(selected,source.SourceProvider,new[]{
            new CosmicProjectionTarget(source.SourceProvider,reader.IndexedRowCapabilities,
                new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),new[]{typeof(string)}).WithMetadataRoots(new[]{"Status"}),source)});
        Assert.False(planned.IsError,planned.Message);
        MetadataWitnessDto.Constructed=0;
        var result=await CosmicIndexedProjectionExecutor.ExecuteAsync(planned.Result,reader,Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal(error,result.ErrorCode);Assert.Equal(error==null?1:0,MetadataWitnessDto.Constructed);
        Assert.Equal(mode=="wildcard"?0:1,reader.Calls);
        if(error==null)Assert.Equal("coffee",result.Result.Rows.Single().Category);
    }

    [Fact]
    public void IndexedMetadataSelectionWithoutExplicitRootsRejectsDuringPlanning()
    {
        var selected=Plan().Query.Select(row=>CosmicQueryMetadata.Value<string>(row,"Status","category")).Result;
        var planned=CosmicProjectionPlanner.Plan(selected,source.SourceProvider,new[]{new CosmicProjectionTarget(
            source.SourceProvider,Capabilities,new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),new[]{typeof(string)}),source)});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",planned.ErrorCode);
    }

}
