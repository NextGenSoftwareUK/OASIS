using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeConvertedExtremaReduceStorageAndReturnModelValues(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyConvertedExtremaJourney,nativeJourneySuffix:"-converted-extrema-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyConvertedExtremaJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var units=new CosmicValueConverter<int,long>(value=>checked((long)value*10),
            value=>value%10==0?checked((int)(value/10)):throw new ArgumentException("Invalid units."),
            CosmicValueConversionSemantics.Ordering);
        var codes=new CosmicValueConverter<ConvertedCode,string>(
            value=>"code:"+value.Value,
            value=>value.StartsWith("code:",StringComparison.Ordinal)
                ?new ConvertedCode{Value=value.Substring(5)}:throw new ArgumentException("Invalid code."),
            CosmicValueConversionSemantics.Ordering);
        var builder=new CosmicModelBuilder();
        builder.Entity<ConvertedReadingHolon>().Property(row=>row.Units).HasConversion(units).HasMetadataKey("ext_units");
        builder.Entity<ConvertedReadingHolon>().Property(row=>row.Code).HasConversion(codes).HasMetadataKey("ext_code");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;
        using var writer=CosmicSession.Create(manager,adapter,access,model).Result;
        var first=new ConvertedReadingHolon{Id=Guid.NewGuid(),Name="extrema Alpha",Units=2,
            Code=new ConvertedCode{Value="Alpha"},CreatedByAvatarId=actor,IsActive=true,IsNewHolon=true};
        var last=new ConvertedReadingHolon{Id=Guid.NewGuid(),Name="extrema Zulu",Units=7,
            Code=new ConvertedCode{Value="Zulu"},CreatedByAvatarId=actor,IsActive=true,IsNewHolon=true};
        owned.Add(first.Id);owned.Add(last.Id);
        Assert.IsFalse(writer.Add(first).IsError);Assert.IsFalse(writer.Add(last).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var saved=await Timed("create-extrema",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.AreEqual(20L,Convert.ToInt64(first.MetaData["ext_units"]));
        Assert.AreEqual(70L,Convert.ToInt64(last.MetaData["ext_units"]));
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var firstId=first.Id;var lastId=last.Id;
        var source=reader.Query<ConvertedReadingHolon>().Result.Where(row=>row.Id==firstId || row.Id==lastId);
        Assert.IsFalse(source.IsError,source.Message);
        var root=CosmicLinq.Create(manager,adapter,source.Result).Result;
        var minimum=await Timed("min-units",()=>root.ToCosmicMinAsync(row=>row.Units));
        Assert.IsFalse(minimum.IsError,minimum.Message+minimum.Exception);Assert.AreEqual(2,minimum.Result);
        var maximum=await Timed("max-units",()=>root.ToCosmicMaxAsync(row=>row.Units));
        Assert.IsFalse(maximum.IsError,maximum.Message+maximum.Exception);Assert.AreEqual(7,maximum.Result);
        var minCode=await Timed("min-code",()=>root.ToCosmicMinAsync(row=>row.Code));
        Assert.IsFalse(minCode.IsError,minCode.Message+minCode.Exception);Assert.AreEqual("Alpha",minCode.Result!.Value);
        var maxCode=await Timed("max-code",()=>root.ToCosmicMaxAsync(row=>row.Code));
        Assert.IsFalse(maxCode.IsError,maxCode.Message+maxCode.Exception);Assert.AreEqual("Zulu",maxCode.Result!.Value);
        var paged=await Timed("paged-extrema",()=>root.OrderByDescending(row=>row.Units).Take(1).ToCosmicMinAsync(row=>row.Units));
        Assert.IsFalse(paged.IsError,paged.Message+paged.Exception);Assert.AreEqual(7,paged.Result);
        var selected=await Timed("selected-converted-extrema",()=>root.Select(row=>row.Units).ToCosmicMinAsync());
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);Assert.AreEqual(2,selected.Result);
        var record=await Timed("record-converted-extrema",()=>root.Select(row=>new{row.Units,row.Code})
            .OrderBy(row=>row.Units).Skip(1).ToCosmicMaxAsync(row=>row.Code));
        Assert.IsFalse(record.IsError,record.Message+record.Exception);Assert.AreEqual("Zulu",record.Result!.Value);
        var set=await Timed("set-converted-extrema",()=>root.Union(root).Select(row=>row.Units).ToCosmicMaxAsync());
        Assert.IsFalse(set.IsError,set.Message+set.Exception);Assert.AreEqual(7,set.Result);
        var empty=await Timed("empty-converted-extrema",()=>root.Take(0).ToCosmicMinAsync(row=>row.Code));
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.IsNull(empty.Result);
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var hidden=foreign.Query<ConvertedReadingHolon>().Result.Where(row=>row.Id==firstId || row.Id==lastId);
        var foreignRoot=CosmicLinq.Create(manager,adapter,hidden.Result).Result;
        var forbidden=await Timed("foreign-extrema",()=>foreignRoot.ToCosmicMaxAsync(row=>row.Code));
        Assert.IsFalse(forbidden.IsError,forbidden.Message+forbidden.Exception);Assert.IsNull(forbidden.Result);
        var equalityBuilder=new CosmicModelBuilder();
        equalityBuilder.Entity<ConvertedReadingHolon>().Property(row=>row.Code)
            .HasConversion(new CosmicValueConverter<ConvertedCode,string>(
                value=>"code:"+value.Value,value=>new ConvertedCode{Value=value.Substring(5)}))
            .HasMetadataKey("ext_code");
        var equalityModel=equalityBuilder.Build();Assert.IsFalse(equalityModel.IsError,equalityModel.Message);
        using var equality=CosmicSession.Create(manager,adapter,access,equalityModel.Result).Result;
        var rejected=equality.Query<ConvertedReadingHolon>().Result.Min(row=>row.Code);
        Assert.IsTrue(rejected.IsError,"An equality-only converter cannot authorize an extremum.");
        Assert.AreEqual("CQL_SCALAR_SELECTION_UNSUPPORTED",rejected.ErrorCode);
        var invalidNumeric=source.Result.Sum(row=>row.Units);
        Assert.IsTrue(invalidNumeric.IsError,"Ordering does not authorize sums over a changed representation.");
        return new NativeFixtureJourneyProof(8,2,new Dictionary<string,bool>{
            ["selectedConvertedExtrema"]=true,["recordAndSetConvertedExtrema"]=true,
            ["nativeConvertedNumericExtrema"]=true,["nativeConvertedObjectExtrema"]=true,
            ["convertedExtremaAfterSourceWindow"]=true,["emptyAndCallerScopedExtrema"]=true,
            ["equalityOnlyExtremaRefused"]=true,["nonHomomorphicNumericReductionRefused"]=true});
    }
}
