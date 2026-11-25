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
    public sealed class DateTimeDomainHolon:Holon
    {
        public string? Tag {get;set;}
        public DateOnly Day {get;set;}
        public TimeOnly Clock {get;set;}
        public DateTimeOffset Stamp {get;set;}
        public TimeSpan Duration {get;set;}
        public DateOnly? OptionalDay {get;set;}
        public TimeOnly? OptionalClock {get;set;}
        public DateTimeOffset? OptionalStamp {get;set;}
        public TimeSpan? OptionalDuration {get;set;}
    }

    // Q30: DateOnly/TimeOnly/DateTimeOffset/TimeSpan as distinct native CLR domains.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeDateTimeDomainsRoundtripFilterAndOrder(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyDateTimeDomainsJourney,nativeJourneySuffix:"-date-time-domains-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyDateTimeDomainsJourney(
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
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var checks=0;
        var tag="q30-"+Guid.NewGuid().ToString("N");

        var builder=new CosmicModelBuilder();
        var entity=builder.Entity<DateTimeDomainHolon>();
        entity.Property(row=>row.Tag).HasMetadataKey("dt_tag");
        entity.Property(row=>row.Day).HasMetadataKey("dt_day");
        entity.Property(row=>row.Clock).HasMetadataKey("dt_clock");
        entity.Property(row=>row.Stamp).HasMetadataKey("dt_stamp");
        entity.Property(row=>row.Duration).HasMetadataKey("dt_duration");
        entity.Property(row=>row.OptionalDay).HasMetadataKey("dt_optional_day");
        entity.Property(row=>row.OptionalClock).HasMetadataKey("dt_optional_clock");
        entity.Property(row=>row.OptionalStamp).HasMetadataKey("dt_optional_stamp");
        entity.Property(row=>row.OptionalDuration).HasMetadataKey("dt_optional_duration");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;

        // Same instant 2026-03-01T12:00Z expressed with three offsets, plus distinct earlier/later instants.
        var instant=new DateTimeOffset(2026,3,1,12,0,0,TimeSpan.Zero);
        DateTimeDomainHolon New(string name,DateOnly day,TimeOnly clock,DateTimeOffset stamp,TimeSpan duration)
        {
            var row=new DateTimeDomainHolon{Id=Guid.NewGuid(),Name=name,Tag=tag,Day=day,Clock=clock,Stamp=stamp,Duration=duration,
                CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var rows=new[]{
            New("a",new DateOnly(2024,2,29),new TimeOnly(0,0,0),new DateTimeOffset(2026,3,1,17,30,0,TimeSpan.FromHours(5.5)),TimeSpan.FromTicks(-1)),
            New("b",new DateOnly(2026,1,1),new TimeOnly(9,15,30,250).Add(TimeSpan.FromTicks(7)),instant.ToOffset(TimeSpan.FromHours(-8)),TimeSpan.Zero),
            New("c",new DateOnly(2026,11,25),new TimeOnly(23,59,59).Add(TimeSpan.FromTicks(9999999)),instant,new TimeSpan(1,2,3,4,5)),
            New("d",new DateOnly(1,1,1),new TimeOnly(12,0),new DateTimeOffset(2026,3,1,11,59,59,TimeSpan.FromHours(-14)).AddTicks(9999999),TimeSpan.FromDays(-3)),
            New("e",new DateOnly(9999,12,31),new TimeOnly(12,0,0).Add(TimeSpan.FromTicks(1)),new DateTimeOffset(2026,3,1,12,0,0,TimeSpan.FromHours(14)),TimeSpan.MaxValue)};
        // Row d: 11:59:59.9999999-14:00 is 2026-03-02T01:59:59.9999999Z (later). Row e: 12:00+14:00 is 2026-02-28T22:00Z (earlier).
        rows[1].OptionalDay=new DateOnly(2026,6,15);rows[1].OptionalClock=new TimeOnly(6,30);
        rows[1].OptionalStamp=instant.ToOffset(TimeSpan.FromMinutes(345));rows[1].OptionalDuration=TimeSpan.FromMinutes(90);
        using var writer=CosmicSession.Create(manager,adapter,access,model).Result;
        foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
        var created=await Timed("date-time-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);

        // 1. Canonical stored encodings: exact order-preserving integers, offset kept in the DateTimeOffset value.
        Assert.AreEqual(20260101L,Convert.ToInt64(rows[1].MetaData["dt_day"]));
        Assert.AreEqual(91530_2500007L,Convert.ToInt64(rows[1].MetaData["dt_clock"]));
        Assert.AreEqual(0L,Convert.ToInt64(rows[1].MetaData["dt_duration"]));
        Assert.AreEqual(202603011200000000000m*10000m+(840-480),Convert.ToDecimal(rows[1].MetaData["dt_stamp"]));
        checks++;

        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var root=reader.Query<DateTimeDomainHolon>().Result.Where(row=>row.Tag==tag).Result;

        // 2. Exact round-trip of every domain, including offsets, extremes and nulls.
        var all=await Timed("date-time-roundtrip",()=>reader.ReadAsync(root.OrderBy(row=>row.Name).Result,trackResults:false));
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.AreEqual(5,all.Result.Count);
        for(var index=0;index<rows.Length;index++)
        {
            var expected=rows[index];var actual=all.Result[index];
            Assert.AreEqual(expected.Name,actual.Name);
            Assert.AreEqual(expected.Day,actual.Day);Assert.AreEqual(expected.Clock,actual.Clock);
            Assert.AreEqual(expected.Stamp.Ticks,actual.Stamp.Ticks);Assert.AreEqual(expected.Stamp.Offset,actual.Stamp.Offset);
            Assert.AreEqual(expected.Duration,actual.Duration);
            Assert.AreEqual(expected.OptionalDay,actual.OptionalDay);Assert.AreEqual(expected.OptionalClock,actual.OptionalClock);
            Assert.AreEqual(expected.OptionalStamp?.Ticks,actual.OptionalStamp?.Ticks);Assert.AreEqual(expected.OptionalStamp?.Offset,actual.OptionalStamp?.Offset);
            Assert.AreEqual(expected.OptionalDuration,actual.OptionalDuration);
        }
        checks++;

        async Task<string> Read(string label,CosmicQuery<DateTimeDomainHolon> query)
        {
            var result=await Timed(label,()=>reader.ReadAsync(query,trackResults:false));
            Assert.IsFalse(result.IsError,label+": "+result.Message+result.Exception);
            return string.Concat(result.Result.Select(row=>row.Name));
        }
        CosmicQuery<DateTimeDomainHolon> Where(System.Linq.Expressions.Expression<Func<DateTimeDomainHolon,bool>> predicate)
        {
            var query=root.Where(predicate);Assert.IsFalse(query.IsError,query.Message);
            var ordered=query.Result.OrderBy(row=>row.Name);Assert.IsFalse(ordered.IsError,ordered.Message);return ordered.Result;
        }
        CosmicQuery<DateTimeDomainHolon> Order<TKey>(System.Linq.Expressions.Expression<Func<DateTimeDomainHolon,TKey>> key,bool descending=false)
        {
            var ordered=descending?root.OrderByDescending(key):root.OrderBy(key);
            Assert.IsFalse(ordered.IsError,ordered.Message);return ordered.Result;
        }

        // 3. DateOnly predicates and ordering.
        var leap=new DateOnly(2024,2,29);var newYear=new DateOnly(2026,1,1);
        Assert.AreEqual("b",await Read("dateonly-eq",Where(row=>row.Day==newYear)));
        Assert.AreEqual("acde",await Read("dateonly-ne",Where(row=>row.Day!=newYear)));
        Assert.AreEqual("ad",await Read("dateonly-lt",Where(row=>row.Day<newYear)));
        Assert.AreEqual("abd",await Read("dateonly-le",Where(row=>row.Day<=newYear)));
        Assert.AreEqual("bce",await Read("dateonly-gt",Where(row=>row.Day>leap)));
        Assert.AreEqual("abce",await Read("dateonly-ge",Where(row=>row.Day>=leap)));
        Assert.AreEqual("dabce",await Read("dateonly-order",Order(row=>row.Day)));
        Assert.AreEqual("ecbad",await Read("dateonly-order-desc",Order(row=>row.Day,true)));
        checks++;

        // 4. TimeOnly predicates and ordering (100ns precision).
        var noon=new TimeOnly(12,0);
        Assert.AreEqual("d",await Read("timeonly-eq",Where(row=>row.Clock==noon)));
        Assert.AreEqual("ab",await Read("timeonly-lt",Where(row=>row.Clock<noon)));
        Assert.AreEqual("abd",await Read("timeonly-le",Where(row=>row.Clock<=noon)));
        Assert.AreEqual("ce",await Read("timeonly-gt",Where(row=>row.Clock>noon)));
        Assert.AreEqual("abdec",await Read("timeonly-order",Order(row=>row.Clock)));
        checks++;

        // 5. TimeSpan predicates and ordering, including negative and extreme durations.
        var zero=TimeSpan.Zero;
        Assert.AreEqual("b",await Read("timespan-eq",Where(row=>row.Duration==zero)));
        Assert.AreEqual("ad",await Read("timespan-lt",Where(row=>row.Duration<zero)));
        Assert.AreEqual("ce",await Read("timespan-gt",Where(row=>row.Duration>zero)));
        Assert.AreEqual("acde",await Read("timespan-ne",Where(row=>row.Duration!=zero)));
        Assert.AreEqual("dabce",await Read("timespan-order",Order(row=>row.Duration)));
        checks++;

        // 6. DateTimeOffset compares by instant: three offsets of the same instant are equal; ordering is by instant.
        var asTokyo=instant.ToOffset(TimeSpan.FromHours(9));
        Assert.AreEqual("abc",await Read("offset-eq-same-instant",Where(row=>row.Stamp==asTokyo)));
        Assert.AreEqual("de",await Read("offset-ne",Where(row=>row.Stamp!=asTokyo)));
        Assert.AreEqual("e",await Read("offset-lt",Where(row=>row.Stamp<asTokyo)));
        Assert.AreEqual("abce",await Read("offset-le",Where(row=>row.Stamp<=asTokyo)));
        Assert.AreEqual("d",await Read("offset-gt",Where(row=>row.Stamp>asTokyo)));
        Assert.AreEqual("abcd",await Read("offset-ge",Where(row=>row.Stamp>=instant)));
        var order=await Read("offset-order",Order(row=>row.Stamp));
        Assert.AreEqual('e',order[0]);Assert.AreEqual('d',order[4]);
        CollectionAssert.AreEquivalent(new[]{'a','b','c'},order.Substring(1,3).ToCharArray());
        // Ties at one instant order by stored offset (-08:00 < +00:00 < +05:30), identically on every store.
        Assert.AreEqual("ebcad",order);
        checks++;

        // 7. Nullable domains: null tests, lifted comparisons and CLR null inequality.
        Assert.AreEqual("b",await Read("nullable-hasvalue",Where(row=>row.OptionalDay!=null)));
        Assert.AreEqual("acde",await Read("nullable-null",Where(row=>row.OptionalStamp==null)));
        var june=new DateOnly(2026,6,15);var dawn=new TimeOnly(6,0);var hour=TimeSpan.FromHours(1);
        Assert.AreEqual("b",await Read("nullable-dateonly",Where(row=>row.OptionalDay>=june)));
        Assert.AreEqual("b",await Read("nullable-timeonly",Where(row=>row.OptionalClock>dawn)));
        Assert.AreEqual("b",await Read("nullable-timespan",Where(row=>row.OptionalDuration>hour)));
        Assert.AreEqual("b",await Read("nullable-offset-eq",Where(row=>row.OptionalStamp==asTokyo)));
        Assert.AreEqual("acde",await Read("nullable-offset-ne",Where(row=>row.OptionalStamp!=asTokyo)));
        checks++;

        // 8. Explicit refusals: no client evaluation of date parts, offset-sensitive membership or field comparisons.
        var partQuery=root.Where(row=>row.Day.Year==2026);
        Assert.IsTrue(partQuery.IsError,"DateOnly parts are not part of the native contract.");
        var stamps=new[]{asTokyo};
        var membership=root.Where(row=>stamps.Contains(row.Stamp));
        Assert.IsTrue(membership.IsError,"DateTimeOffset membership must refuse rather than compare offsets.");
        var fields=root.Where(row=>row.Stamp<row.OptionalStamp);
        Assert.IsTrue(fields.IsError,"DateTimeOffset field comparisons must refuse.");
        var addMonths=root.Where(row=>row.Stamp.AddMonths(1)>instant);
        Assert.IsTrue(addMonths.IsError,"Non-linear DateTimeOffset arithmetic must refuse.");
        checks++;

        // 9. Caller visibility: another actor sees nothing through the same native predicates.
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var foreignQuery=foreign.Query<DateTimeDomainHolon>().Result.Where(row=>row.Tag==tag && row.Stamp==asTokyo && row.Day>=leap);
        Assert.IsFalse(foreignQuery.IsError,foreignQuery.Message);
        var hidden=await Timed("date-time-foreign",()=>foreign.ReadAsync(foreignQuery.Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count);
        checks++;

        // 10. Update round-trips a changed offset for the same instant and the new value is found natively.
        rows[2].Stamp=instant.ToOffset(TimeSpan.FromHours(-3));rows[2].OptionalDuration=TimeSpan.FromTicks(1);
        var updated=await Timed("date-time-update",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var tick=TimeSpan.FromTicks(1);
        var reread=await Timed("date-time-update-read",()=>reader.ReadAsync(
            Where(row=>row.OptionalDuration==tick && row.Stamp==instant),trackResults:false));
        Assert.IsFalse(reread.IsError,reread.Message+reread.Exception);Assert.AreEqual(1,reread.Result.Count);
        Assert.AreEqual(TimeSpan.FromHours(-3),reread.Result[0].Stamp.Offset);Assert.AreEqual(instant,reread.Result[0].Stamp);
        checks++;

        return new NativeFixtureJourneyProof(checks,2,new Dictionary<string,bool>{
            ["canonicalOrderPreservingEncodings"]=true,["exactRoundTripAllDomains"]=true,
            ["dateOnlyNativeRangeAndOrder"]=true,["timeOnlyNativeRangeAndOrder"]=true,["timeSpanNativeRangeAndOrder"]=true,
            ["dateTimeOffsetComparesByInstantPreservesOffset"]=true,["nullableDomainsNative"]=true,
            ["unsupportedOperationsRefused"]=true,["callerVisibilityPreserved"]=true,["updatedOffsetRoundTrips"]=true});
    }
}
