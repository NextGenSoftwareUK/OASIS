using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class NativeProjectionProbe
    {
        public static int Constructors;
        public NativeProjectionProbe(){Constructors++;}
        public string Name {get;set;}="";
        public long Precision {get;set;}
        public ulong Unsigned {get;set;}
        public decimal Amount {get;set;}
    }
    private sealed record NativeProjectionRecord(string Name, long Precision)
    {
        public int Revision { get; init; }
    }

    private sealed record NestedProjectionEnvelope(NativeProjectionRecord Detail)
    {
        public string Label { get; init; } = "";
    }

    private static Holon CreateEntityProjectionFixture(Guid[] ids,Guid actor,Guid other)
    {
        var root=new Holon{Id=ids[0],Name="protected source root",Description="Case",CreatedByAvatarId=actor,IsPublic=true,Version=1,
            MetaData=new(){["nullableJoinFlag"]=true,["nullableJoinGuid"]=ids[0],["i64"]=9007199254740993L,["narrow"]=1,["u32"]=uint.MaxValue,["distinctHolonType"]=(int)HolonType.Avatar,["distinctProviderType"]=(int)ProviderType.MongoDBOASIS,["distinctUtc"]=new DateTime(638700000000000001L,DateTimeKind.Utc),["u64"]=ulong.MaxValue,["decimal"]=1.0000000000000000000000000001m,["float"]=1f+1.1920929e-7f,["double"]=1d+2.220446049250313e-16d,["presentNull"]=null,["text"]="Café 😀"}};
        root.MetaData["catalog.v1"] = "literal";
        root.MetaData["catalog"] = new Dictionary<string, object> { ["v1"] = "nested" };
        root.Children.Add(new Holon{Id=ids[1],ParentHolonId=root.Id,Name="visible match",Description="Case",CreatedByAvatarId=actor,IsPublic=true,Version=1,
            MetaData=new(){["nullableJoinFlag"]=false,["nullableJoinGuid"]=ids[1],["i64"]=9007199254740992L,["narrow"]=2,["u32"]=uint.MaxValue-1,["float"]=1f,["double"]=1d,["distinctHolonType"]=(int)HolonType.Quest,["distinctProviderType"]=(int)ProviderType.Neo4jOASIS,["u64"]=ulong.MaxValue-1,["decimal"]=1.00m,["distinctUtc"]=new DateTime(638700000000000002L,DateTimeKind.Utc)}});
        var branch=new Holon{Id=ids[2],ParentHolonId=root.Id,Name="private branch",Description=null,CreatedByAvatarId=other,Version=1};
        branch.Children.Add(new Holon{Id=ids[3],ParentHolonId=branch.Id,Name="grandchild one",Description="case",CreatedByAvatarId=other,IsPublic=true,Version=1,
            MetaData=new(){["i64"]=9007199254740993L,["narrow"]=1,["u32"]=uint.MaxValue,["float"]=1f+1.1920929e-7f,["double"]=1d+2.220446049250313e-16d,["distinctHolonType"]=(int)HolonType.Avatar,["distinctProviderType"]=(int)ProviderType.MongoDBOASIS,["u64"]=ulong.MaxValue,["decimal"]=1.0000000000000000000000000001m,["distinctUtc"]=new DateTime(638700000000000001L,DateTimeKind.Utc)}});
        branch.Children.Add(new Holon{Id=ids[4],ParentHolonId=branch.Id,Name="grandchild two",Description=null,CreatedByAvatarId=other,IsPublic=true,Version=1,
            MetaData=new(){["decimal"]=1.000m}});
        root.Children.Add(branch);return root;
    }

    private static async Task VerifyEntityProjection(ICosmicQueryEntityProjectionStorageProvider adapter,Guid rootId,List<object> checks)
    {
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicProjectionExecution<TResult>>> Read<T,TResult>(
            CosmicEntityProjectionQuery<T,TResult> query,CosmicEntityCompositionOptions? options=null,int maximumBytes=1048576) where T:IHolon
        {
            var plan=CosmicEntityProjectionPlanner.Plan(query,adapter.QueryProviderType,
                new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities),adapter.EntityCompositionCapabilities,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteEntityProjectionQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter},10,maximumBytes);
        }
        void Passed(string name)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entityProjection=name});
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(3).Result;
        var projection=source.Compose().Result.Reverse().Result.Take(2).Result.Select(row=>row.Name).Result;
        var selected=await Read(projection);Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        CollectionAssert.AreEqual(new[]{"visible match","protected source root"},selected.Result.Providers.Single().Rows.ToArray());Passed("native selection after reversal/window");
        var rootSource=CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result.Skip(0).Result;
        NativeProjectionProbe.Constructors=0;
        var typed=rootSource.Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64"),
            Unsigned=CosmicQueryMetadata.Value<ulong>(row,"u64"),Amount=CosmicQueryMetadata.Value<decimal>(row,"decimal")}).Result;
        var native=await Read(typed);Assert.IsFalse(native.IsError,native.Message+native.Exception);
        var dto=native.Result.Providers.Single().Rows.Single();Assert.AreEqual(9007199254740993L,dto.Precision);
        Assert.AreEqual(ulong.MaxValue,dto.Unsigned);Assert.AreEqual(1.0000000000000000000000000001m,dto.Amount);
        Assert.AreEqual(1,NativeProjectionProbe.Constructors);Passed("exact native cells without source entity construction");
        var nested = rootSource.Select(row => new NestedProjectionEnvelope(
            new NativeProjectionRecord(row.Name, CosmicQueryMetadata.Value<long>(row, "i64")) { Revision = row.Version })
            { Label = row.Name });
        Assert.IsFalse(nested.IsError, nested.Message);
        var nestedResult = await Read(nested.Result);
        Assert.IsFalse(nestedResult.IsError, nestedResult.Message + nestedResult.Exception);
        var envelope = nestedResult.Result.Providers.Single().Rows.Single();
        Assert.AreEqual("protected source root", envelope.Label);
        Assert.AreEqual(envelope.Label, envelope.Detail.Name);
        Assert.AreEqual(9007199254740993L, envelope.Detail.Precision);
        Assert.AreEqual(1, envelope.Detail.Revision);
        Passed("nested DTO from exact native scalar cells");
        var firstSource=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(1).Result;
        var excluded=await Read(firstSource.Compose().Result.Where(row=>row.Name=="protected source root").Result.Select(row=>row.Name).Result);
        Assert.IsFalse(excluded.IsError,excluded.Message+excluded.Exception);Assert.AreEqual(0,excluded.Result.Providers.Single().Rows.Count);Passed("selection cannot move filter before source page");
        excluded=await Read(source.Compose().Result.Reverse().Result.Take(1).Result.Where(row=>row.Name=="protected source root").Result.Select(row=>row.Name).Result);
        Assert.IsFalse(excluded.IsError,excluded.Message+excluded.Exception);Assert.AreEqual(0,excluded.Result.Providers.Single().Rows.Count);Passed("selection cannot move filter before stage page");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var root=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result;
        var protectedQuery=root.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result.Take(0).Result.Select(row=>row.Name).Result;
        var failed=await Read(protectedQuery);Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);Passed("native projection protects preceding scopes");
        var empty=root.Skip(0).Result.Take(0).Result.Select(row=>row.Name).Result;
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var limited=await Read(empty,options);Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);Passed("projection source/work budget before empty result");
        }
        var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var secured=CosmicQuery<Holon>.Create(scope).WithReadAccess(policy).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result
            .Where(row=>row.Children.Any(child=>child.Name=="private branch")).Result.Select(row=>row.Name).Result;
        var invisible=await Read(secured);Assert.IsFalse(invisible.IsError,invisible.Message+invisible.Exception);
        Assert.AreEqual(0,invisible.Result.Providers.Single().Rows.Count);Passed("projection preserves relationship visibility");
        NativeProjectionProbe.Constructors=0;
        var invalid=root.Skip(0).Result.Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).Result;
        var rejected=await Read(invalid);Assert.AreEqual("CQL_DATA_INVALID",rejected.ErrorCode,rejected.Message+rejected.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("invalid native cells precede DTO construction");
        var payload=await Read(projection,maximumBytes:1);Assert.AreEqual("CQL_QUERY_LIMIT",payload.ErrorCode,payload.Message+payload.Exception);Passed("projection output payload bound");
    }

    private static async Task VerifyStandardEntityProjection(ICosmicQueryEntityProjectionStorageProvider adapter,Guid rootId,List<object> checks)
    {
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Result;
        var query=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result.OrderBy(row=>row.Name);
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),standardEntityProjection=label});
        var rows=await query.Take(3).Where(row=>row.Version>=1).Reverse().Take(2).Select(row=>row.Name).ToCosmicListAsync();
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);
        CollectionAssert.AreEqual(new[]{"visible match","protected source root"},rows.Result.ToArray());Passed("standard Select preserves staged order and pages");
        var empty=query.Take(1).Where(row=>row.Name=="protected source root").Select(row=>row.Name).ToArray();
        Assert.AreEqual(0,empty.Length);Passed("standard Select cannot move filter before source page");
        var rootSource=CosmicLinq.Create(NativeManager(adapter),adapter,CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Result).Result;
        var desired="not-matched-at-construction";
        NativeProjectionProbe.Constructors=0;
        var deferred=rootSource.Take(1).Where(row=>row.Name==desired)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64")});
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);desired="protected source root";
        var selected=await deferred.ToCosmicListAsync();Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        Assert.AreEqual(9007199254740993L,selected.Result.Single().Precision);Assert.AreEqual(1,NativeProjectionProbe.Constructors);
        var recordResult = await rootSource.Take(1)
            .Select(row => new NativeProjectionRecord(row.Name, CosmicQueryMetadata.Value<long>(row, "i64")))
            .ToCosmicListAsync();
        Assert.IsFalse(recordResult.IsError, recordResult.Message + recordResult.Exception);
        Assert.AreEqual(new NativeProjectionRecord("protected source root", 9007199254740993L),
            recordResult.Result.Single());
        Passed("standard Select binds positional record constructor arguments without losing Int64 precision");

        var initializedRecord = await rootSource.Take(1)
            .Select(row => new NativeProjectionRecord(row.Name, CosmicQueryMetadata.Value<long>(row, "i64"))
            {
                Revision = row.Version
            }).ToCosmicListAsync();
        Assert.IsFalse(initializedRecord.IsError, initializedRecord.Message + initializedRecord.Exception);
        Assert.AreEqual(new NativeProjectionRecord("protected source root", 9007199254740993L) { Revision = 1 },
            initializedRecord.Result.Single());
        Passed("standard Select materializes constructor arguments before public property assignments");

        var assignedFields = await rootSource.Take(1)
            .Select(row => new NativeFieldProjection
            {
                Name = row.Name,
                Precision = CosmicQueryMetadata.Value<long>(row, "i64")
            }).ToCosmicListAsync();
        Assert.IsFalse(assignedFields.IsError, assignedFields.Message + assignedFields.Exception);
        var fieldRow = assignedFields.Result.Single();
        Assert.AreEqual("protected source root", fieldRow.Name);
        Assert.AreEqual(9007199254740993L, fieldRow.Precision);
        Passed("standard Select materializes public fields with exact Int64 precision");

        var constructorFields = await rootSource.Take(1)
            .Select(row => new NativeConstructorFieldProjection(row.Name, CosmicQueryMetadata.Value<long>(row, "i64")))
            .ToCosmicListAsync();
        Assert.IsFalse(constructorFields.IsError, constructorFields.Message + constructorFields.Exception);
        var constructorFieldRow = constructorFields.Result.Single();
        Assert.AreEqual("protected source root", constructorFieldRow.Name);
        Assert.AreEqual(9007199254740993L, constructorFieldRow.Precision);
        Passed("standard Select binds constructor arguments to readonly public fields");

        var opaqueConstructor = await rootSource.Take(1)
            .Select(row => new NativeOpaqueConstructorProjection(row.Name, CosmicQueryMetadata.Value<long>(row, "i64")))
            .ToCosmicListAsync();
        Assert.IsFalse(opaqueConstructor.IsError, opaqueConstructor.Message + opaqueConstructor.Exception);
        var opaqueRow = opaqueConstructor.Result.Single();
        Assert.AreEqual("prefix:protected source root", opaqueRow.Label);
        Assert.AreEqual(9007199254740993L, opaqueRow.Sequence);
        Passed("standard Select materializes opaque constructor inputs after native validation");

        var transformed = await rootSource.Take(1)
            .Select(row => new NativeTransformingConstructorProjection(row.Name))
            .ToCosmicListAsync();
        Assert.IsFalse(transformed.IsError, transformed.Message + transformed.Exception);
        Assert.AreEqual("prefix:protected source root", transformed.Result.Single().Name);

        var explicitProjection = CosmicQuery<NativeCountWithoutConstructor>.Create(scope)
            .Select(row => new NativeTransformingConstructorProjection(row.Name));
        Assert.IsFalse(explicitProjection.IsError, explicitProjection.Message);
        var unsafeFilter = explicitProjection.Result.Where(row => row.Name == "protected source root");
        Assert.IsTrue(unsafeFilter.IsError, "A constructor argument is not proof of the materialized member value.");
        Passed("selected member composition rejects unsafe constructor argument substitution");

        desired="not-matched-on-second-read";Assert.AreEqual(0,deferred.ToArray().Length);Passed("deferred selector rebinds without application source construction or eager DTOs");
        NativeProjectionProbe.Constructors=0;
        var invalid=await rootSource.Take(1).Where(row=>row.Name=="protected source root")
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).ToCosmicListAsync();
        Assert.AreEqual("CQL_DATA_INVALID",invalid.ErrorCode,invalid.Message+invalid.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("standard Select rejects invalid native cells before DTOs");
    }
    private sealed class NativeConstructorFieldProjection(string name, long precision)
    {
        public readonly string Name = name;
        public readonly long Precision = precision;
    }

    private sealed class NativeTransformingConstructorProjection(string name)
    {
        public string Name { get; } = "prefix:" + name;
    }

    private sealed class NativeOpaqueConstructorProjection(string sourceName, long sourceValue)
    {
        public string Label { get; } = "prefix:" + sourceName;
        public long Sequence { get; } = sourceValue;
    }

    private sealed class NativeFieldProjection
    {
        public NativeFieldProjection() { }

        public string Name;
        public long Precision;
    }

}
