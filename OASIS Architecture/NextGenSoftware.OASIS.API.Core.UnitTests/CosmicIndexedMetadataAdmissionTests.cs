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

}
