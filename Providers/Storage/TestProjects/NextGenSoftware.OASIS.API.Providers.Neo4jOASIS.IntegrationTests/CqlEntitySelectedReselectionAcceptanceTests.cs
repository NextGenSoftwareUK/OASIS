using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class NativeReselectionProbe
    {
        public static int Constructors;
        public NativeReselectionProbe(){Constructors++;}
        public string Label {get;set;}="";
    }

    private static async Task VerifyEntitySelectedReselection(ICosmicQueryEntityProjectionStorageProvider adapter,
        Guid rootId,List<object> checks)
    {
        var source=CosmicQuery<NativeCountWithoutConstructor>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId).Result;
        var root=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result;
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entityReselection=label});

        // Final-only selection still validates all retained native columns.
        NativeProjectionProbe.Constructors=0;
        var first=root.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64")});
        var expression=first.Select(row=>row.Name).Expression;
        var nonGeneric=(IQueryable<string>)first.Provider.CreateQuery(expression);
        Assert.AreEqual("protected source root",nonGeneric.ToArray().Single());
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("non-generic final reselection without intermediate or source constructors");

        // Selected predicates retain their original columns across repeated Select.
        long threshold=9007199254740992L;
        NativeReselectionProbe.Constructors=0;
        var repeated=first.Where(row=>row.Precision>threshold).OrderBy(row=>row.Name).Take(1)
            .Select(row=>row.Name).Select(name=>new NativeReselectionProbe{Label=name})
            .Where(row=>row.Label=="protected source root").Reverse().Take(1);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Assert.AreEqual(0,NativeReselectionProbe.Constructors);
        var result=await repeated.ToCosmicListAsync();Assert.IsFalse(result.IsError,result.Message+result.Exception);
        Assert.AreEqual("protected source root",result.Result.Single().Label);
        Assert.AreEqual(1,NativeReselectionProbe.Constructors);Assert.AreEqual(0,NativeProjectionProbe.Constructors);
        threshold=9007199254740993L;Assert.AreEqual(0,repeated.ToArray().Length);
        Assert.AreEqual(1,NativeReselectionProbe.Constructors);Passed("repeated selected shapes preserve native stages and per-read closure binding");

        // A later shape/window cannot discard the invalid required input column.
        NativeProjectionProbe.Constructors=0;NativeReselectionProbe.Constructors=0;
        var invalid=await root.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")})
            .Select(row=>row.Name).Select(name=>new NativeReselectionProbe{Label=name}).Take(0).ToCosmicListAsync();
        Assert.AreEqual("CQL_DATA_INVALID",invalid.ErrorCode,invalid.Message+invalid.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Assert.AreEqual(0,NativeReselectionProbe.Constructors);
        Passed("dropped invalid native cells remain protected before empty final output");
    }
}
