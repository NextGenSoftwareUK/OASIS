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
    public sealed class DiagnosticHolon:Holon
    {
        public string? Label {get;set;}
        public decimal Amount {get;set;}
        public string? Unmapped {get;set;}
        public string? AlsoUnmapped {get;set;}
    }

    // O17: design-time model validation, model description and stored-data diagnostics.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeModelDiagnosticsDescribeValidateAndScanStoredData(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyModelDiagnosticsJourney,nativeJourneySuffix:"-model-diagnostics-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyModelDiagnosticsJourney(
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

        // 1. Validate reports every problem at once; Build refuses with the same count and first message.
        var broken=new CosmicModelBuilder();
        broken.Entity<DiagnosticHolon>().Property(row=>row.Unmapped).IsRequired();
        broken.Entity<DiagnosticHolon>().Property(row=>row.AlsoUnmapped).HasMaxLength(4);
        var diagnostics=broken.Validate();
        Assert.IsFalse(diagnostics.IsValid);Assert.AreEqual(2,diagnostics.Issues.Count);
        CollectionAssert.AreEquivalent(new[]{"Unmapped","AlsoUnmapped"},diagnostics.Issues.Select(issue=>issue.Property).ToArray());
        Assert.IsTrue(diagnostics.Issues.All(issue=>issue.Code=="COSMIC_MODEL_CONSTRAINT_UNMAPPED" && issue.EntityType==typeof(DiagnosticHolon)));
        var refused=broken.Build();
        Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_MODEL_INVALID",refused.ErrorCode);
        Assert.AreEqual(2,refused.ErrorCount);Assert.AreEqual(diagnostics.Issues[0].Message,refused.Message);
        Assert.IsFalse(new CosmicModelBuilder().Validate().IsValid);
        checks++;

        // 2. A permissive model writes rows natively, including values a later constraint will reject.
        var looseBuilder=new CosmicModelBuilder();
        looseBuilder.Entity<DiagnosticHolon>().Property(row=>row.Label).HasMetadataKey("diag_label");
        looseBuilder.Entity<DiagnosticHolon>().Property(row=>row.Amount).HasMetadataKey("diag_amount");
        Assert.IsTrue(looseBuilder.Validate().IsValid);
        var loose=looseBuilder.Build().Result;
        DiagnosticHolon New(string name,string? label,decimal amount)
        {
            var row=new DiagnosticHolon{Id=Guid.NewGuid(),Name=name,Label=label,Amount=amount,
                CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var valid=New("valid","ok",12.5m);var tooLong=New("too long","much-too-long",1m);
        var missing=New("missing",null,2m);var precise=New("precise","ok",1.2345m);
        using(var writer=CosmicSession.Create(manager,adapter,access,loose).Result)
        {
            foreach(var row in new[]{valid,tooLong,missing,precise})Assert.IsFalse(writer.Add(row).IsError);
            var saved=await Timed("diagnostics-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
            var none=await Timed("diagnostics-loose-scan",()=>writer.FindModelViolationsAsync<DiagnosticHolon>());
            Assert.IsFalse(none.IsError,none.Message+none.Exception);Assert.AreEqual(0,none.Result.Count);
        }
        checks++;

        // 3. Describe and ToDebugString expose storage keys, constraints and shadow properties.
        var strictBuilder=new CosmicModelBuilder();
        strictBuilder.Entity<DiagnosticHolon>().Property(row=>row.Label).HasMetadataKey("diag_label");
        strictBuilder.Entity<DiagnosticHolon>().Property(row=>row.Amount).HasMetadataKey("diag_amount");
        strictBuilder.Entity<DiagnosticHolon>().Property(row=>row.Label).IsRequired().HasMaxLength(5);
        strictBuilder.Entity<DiagnosticHolon>().Property(row=>row.Amount).HasPrecision(6,2);
        strictBuilder.Entity<DiagnosticHolon>().ShadowProperty<string>("Audit").HasMaxLength(10).HasMetadataKey("diag_audit");
        Assert.IsTrue(strictBuilder.Validate().IsValid);
        var strict=strictBuilder.Build();Assert.IsFalse(strict.IsError,strict.Message);
        var description=strict.Result.Describe().Single();
        Assert.AreEqual(typeof(DiagnosticHolon),description.EntityType);
        var label=description.Properties.Single(property=>property.Name=="Label");
        Assert.AreEqual("diag_label",label.StorageKey);Assert.IsTrue(label.IsRequired);Assert.AreEqual(5,label.MaximumLength);
        var amount=description.Properties.Single(property=>property.Name=="Amount");
        Assert.AreEqual(6,amount.Precision);Assert.AreEqual(2,amount.Scale);
        var audit=description.Properties.Single(property=>property.IsShadow);
        Assert.AreEqual("diag_audit",audit.StorageKey);Assert.AreEqual(10,audit.MaximumLength);
        var debug=strict.Result.ToDebugString();
        StringAssert.Contains(debug,"Property Label String -> diag_label required max 5");
        StringAssert.Contains(debug,"Property Amount Decimal -> diag_amount decimal(6,2)");
        StringAssert.Contains(debug,"Shadow Audit String -> diag_audit max 10");
        Assert.AreEqual(debug,strict.Result.ToDebugString());
        checks++;

        // 4. A stricter model scans the native store and reports exactly the drifted records.
        using var scanner=CosmicSession.Create(manager,adapter,access,strict.Result).Result;
        var scan=await Timed("diagnostics-strict-scan",()=>scanner.FindModelViolationsAsync<DiagnosticHolon>());
        Assert.IsFalse(scan.IsError,scan.Message+scan.Exception);
        var byId=scan.Result.GroupBy(row=>row.Id).ToDictionary(group=>group.Key,group=>group.Select(row=>row.Problem).ToArray());
        Assert.AreEqual(3,byId.Count);Assert.IsFalse(byId.ContainsKey(valid.Id));
        CollectionAssert.AreEqual(new[]{"Label exceeds its maximum length."},byId[tooLong.Id]);
        CollectionAssert.AreEqual(new[]{"Label is required."},byId[missing.Id]);
        CollectionAssert.AreEqual(new[]{"Amount exceeds its decimal precision or scale."},byId[precise.Id]);
        checks++;

        // 5. Save-time validation uses the same rules: a fresh drifted write is refused.
        var rejected=New("rejected","rejected-label",1m);
        Assert.IsFalse(scanner.Add(rejected).IsError);
        var refusedWrite=await Timed("diagnostics-refused-write",()=>scanner.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refusedWrite.IsError);StringAssert.Contains(refusedWrite.Message,"Label exceeds its maximum length.");
        scanner.Detach(rejected);owned.Remove(rejected.Id);
        checks++;

        // 6. The scan is read-only: stored revisions and values are unchanged after scanning.
        using var verifier=CosmicSession.Create(manager,adapter,access,loose).Result;
        var after=await Timed("diagnostics-after-read",()=>verifier.ReadAsync(verifier.Query<DiagnosticHolon>().Result,trackResults:false));
        Assert.IsFalse(after.IsError,after.Message+after.Exception);Assert.AreEqual(4,after.Result.Count);
        Assert.AreEqual("much-too-long",after.Result.Single(row=>row.Id==tooLong.Id).Label);
        Assert.AreEqual(1,after.Result.Single(row=>row.Id==tooLong.Id).Version);
        checks++;

        // 7. Diagnostics respect the caller: another actor's scan sees no rows; a model-less session is refused.
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,strict.Result).Result;
        var hidden=await Timed("diagnostics-foreign-scan",()=>foreign.FindModelViolationsAsync<DiagnosticHolon>());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        using var plain=CosmicSession.Create(manager,adapter,access).Result;
        var noModel=await plain.FindModelViolationsAsync<DiagnosticHolon>();
        Assert.IsTrue(noModel.IsError);Assert.AreEqual("COSMIC_SESSION_MODEL_REQUIRED",noModel.ErrorCode);
        checks++;

        // 8. The scan honours its bound: one row maximum returns at most that row's problems.
        var bounded=await Timed("diagnostics-bounded-scan",()=>scanner.FindModelViolationsAsync<DiagnosticHolon>(1));
        Assert.IsFalse(bounded.IsError,bounded.Message+bounded.Exception);
        Assert.IsTrue(bounded.Result.Select(row=>row.Id).Distinct().Count()<=1);
        checks++;

        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["validateReportsAllIssues"]=true,["permissiveModelWritesNatively"]=true,
            ["describeAndDebugStringExposeMapping"]=true,["strictScanReportsDriftedRecords"]=true,
            ["saveValidationSharesRules"]=true,["scanIsReadOnly"]=true,
            ["diagnosticsRespectCallerAndModel"]=true,["scanHonoursBound"]=true});
    }
}
