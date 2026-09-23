using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;
using Xunit;

public sealed class ReconciliationTests
{
    private static readonly string Avatar = Guid.NewGuid().ToString("D");
    private static readonly TimeProvider ClosedPeriod = new TestTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
    private static UsageExternalReceipt Receipt() => new()
    {
        Source = "provider", UserId = Avatar, Month = "2026-09", OperationId = Guid.NewGuid().ToString("D"),
        ConsumingService = "WEB6", Provider = "test-provider", ProviderRequestId = "req-1", ExternalId = "receipt-1",
        AmountUsd = 0.012345m, EvidenceSha256 = new string('a', 64), Actor = "auditor"
    };

    [Theory]
    [InlineData("2026-09", true)] [InlineData("2026-13", false)] [InlineData("2026-9", false)]
    [InlineData("", false)] [InlineData("2026-09-01", false)] [InlineData(null, false)]
    public void MonthIsExact(string month, bool expected) => Assert.Equal(expected, UsageReconciliationMath.IsMonth(month));

    [Fact]
    public void DecimalAuditPreservesPrecisionAndChecksOverflow()
    {
        var a = new UsageLedgerTotals(1, 20, 0, 0.012345m, 0);
        var b = new UsageLedgerTotals(0, -20, 12, -0.012345m, 0.009876m);
        Assert.Equal(new UsageLedgerTotals(1, 0, 12, 0, 0.009876m), a + b);
        Assert.Throws<OverflowException>(() => new UsageLedgerTotals(long.MaxValue, 0, 0, 0, 0) + a);
    }

    [Theory]
    [InlineData("negative")] [InlineData("avatar")] [InlineData("service")] [InlineData("operation")]
    [InlineData("provider")] [InlineData("request")] [InlineData("sha")] [InlineData("actor")]
    public void InvalidEvidenceRejected(string field)
    {
        var r = Receipt();
        switch (field)
        {
            case "negative": r.AmountUsd = -1; break;
            case "avatar": r.UserId = "forged"; break;
            case "service": r.ConsumingService = "WEB4"; break;
            case "operation": r.OperationId = "1"; break;
            case "provider": r.Provider = ""; break;
            case "request": r.ProviderRequestId = ""; break;
            case "sha": r.EvidenceSha256 = new string('z', 64); break;
            case "actor": r.Actor = ""; break;
        }
        Assert.Throws<ArgumentException>(r.Validate);
    }

    private static JsonDocument Invoice(string status = "paid", string currency = "usd", string customer = "cus_test", string basis = StripeUsageEvidenceReader.CostBasis) =>
        JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            id = "in_test", status, currency, customer, subtotal = 123,
            metadata = new { oasis_avatar_id = Avatar, oasis_usage_month = "2026-09", oasis_usage_cost_basis = basis }
        }));

    [Fact]
    public void StripePaymentStatusDoesNotChangeImmutableAccountingEvidence()
    {
        using var open = Invoice("open"); using var paid = Invoice();
        var a = StripeUsageEvidenceReader.ParseFinalizedInvoice(open.RootElement, "cus_test", Avatar, "2026-09", "worker");
        var b = StripeUsageEvidenceReader.ParseFinalizedInvoice(paid.RootElement, "cus_test", Avatar, "2026-09", "admin");
        Assert.True(UsageReconciliationMath.SameReceipt(a, b)); Assert.Equal(1.23m, a.AmountUsd);
    }

    [Theory]
    [InlineData("draft", "usd", "cus_test", StripeUsageEvidenceReader.CostBasis)]
    [InlineData("void", "usd", "cus_test", StripeUsageEvidenceReader.CostBasis)]
    [InlineData("paid", "eur", "cus_test", StripeUsageEvidenceReader.CostBasis)]
    [InlineData("paid", "usd", "cus_other", StripeUsageEvidenceReader.CostBasis)]
    [InlineData("paid", "usd", "cus_test", "plan-fee")]
    public void StripeEvidenceBoundToFinalizedUsageCost(string status, string currency, string customer, string basis)
    {
        using var invoice = Invoice(status, currency, customer, basis);
        Assert.Throws<InvalidOperationException>(() => StripeUsageEvidenceReader.ParseFinalizedInvoice(invoice.RootElement, "cus_test", Avatar, "2026-09", "worker"));
    }

    private static Dictionary<string, object> CacheMeasurement(string operationId) => new()
    {
        ["Provider"] = "WEB6-cache", ["Model"] = "semantic-cache", ["ProviderRequestId"] = operationId + ":cache",
        ["ClientRequestId"] = operationId + ":cache", ["CostSource"] = "semantic-cache", ["PricingCatalogueVersion"] = "web6-cache-v1",
        ["PromptTokens"] = 0L, ["CompletionTokens"] = 0L, ["Units"] = 0L, ["ActualCostUsd"] = 0m
    };

    [Theory]
    [InlineData("Provider", "external-provider")] [InlineData("Model", "other-model")]
    [InlineData("ProviderRequestId", "other-request")] [InlineData("ClientRequestId", "other-request")]
    [InlineData("CostSource", "other-source")] [InlineData("PricingCatalogueVersion", "other-version")]
    [InlineData("PromptTokens", 1L)] [InlineData("CompletionTokens", 1L)] [InlineData("Units", 1L)] [InlineData("ActualCostUsd", 1L)]
    public void CacheLabelCannotHideExternalOrNonzeroMeasurements(string field, object value)
    {
        string operation = Guid.NewGuid().ToString("D");
        var receipt = CacheMeasurement(operation); receipt[field] = value;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(receipt));
        Assert.False(UsageReconciliationMath.IsInternalReceipt(json.RootElement, "WEB6", operation, "succeeded"));
    }

    [Theory]
    [InlineData("WEB5")] [InlineData("WEB6")] [InlineData("WEB7")] [InlineData("WEB8")] [InlineData("WEB9")] [InlineData("WEB10")]
    public void IncludedServiceAndKnownUnexecutedReceiptsNeedNoVendorInvoice(string service)
    {
        string operation = Guid.NewGuid().ToString("D");
        var receipt = new Dictionary<string, object>
        {
            ["Provider"] = service, ["ProviderRequestId"] = operation, ["PromptTokens"] = 0L, ["CompletionTokens"] = 0L,
            ["Units"] = 1L, ["ActualCostUsd"] = 0m, ["CostSource"] = "service-catalogue", ["PricingCatalogueVersion"] = "included-request-v1"
        };
        using var included = JsonDocument.Parse(JsonSerializer.Serialize(receipt));
        Assert.True(UsageReconciliationMath.IsInternalReceipt(included.RootElement, service, operation, "succeeded"));
        receipt["CostSource"] = "not-executed"; receipt["PricingCatalogueVersion"] = "no-provider-execution-v1"; receipt["Units"] = 0L;
        using var rejected = JsonDocument.Parse(JsonSerializer.Serialize(receipt));
        Assert.True(UsageReconciliationMath.IsInternalReceipt(rejected.RootElement, service, operation, "failed"));
        Assert.True(UsageReconciliationMath.IsInternalReceipt(rejected.RootElement, service, operation, "cancelled"));
        Assert.False(UsageReconciliationMath.IsInternalReceipt(rejected.RootElement, service, operation, "succeeded"));
    }

    [MongoFact]
    public async Task ExactCacheReceiptBalancesWithoutPhantomProviderEvidence()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name); var store = new MongoUsageReconciler(client, name, ClosedPeriod); await store.InitializeAsync(default);
            string operationId = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument
            {
                { "UserId", Avatar }, { "Month", "2026-09" }, { "Day", "2026-09-22" },
                { "RequestsDelta", 1L }, { "ReservedUnitsDelta", 0L }, { "SettledUnitsDelta", 0L },
                { "ReservedCostDeltaUsd", new Decimal128(0m) }, { "SettledCostDeltaUsd", new Decimal128(0m) }
            });
            foreach (var (type, period) in new[] { ("month", "2026-09"), ("day", "2026-09-22") })
                await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                {
                    { "_id", Avatar + ":" + type + ":" + period }, { "UserId", Avatar }, { "PeriodType", type }, { "Period", period },
                    { "Requests", 1L }, { "ReservedUnits", 0L }, { "SettledUnits", 0L },
                    { "ReservedCostUsd", new Decimal128(0m) }, { "SettledCostUsd", new Decimal128(0m) }
                });
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", operationId }, { "UserId", Avatar }, { "ConsumingService", "WEB6" }, { "Status", "settled" }, { "Outcome", "succeeded" },
                { "ProviderReceiptsJson", JsonSerializer.Serialize(new[] { CacheMeasurement(operationId) }) },
                { "AuthorizedAtUtc", new DateTime(2026,9,22,0,0,0,DateTimeKind.Utc) }
            });
            var report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(report.Balanced); Assert.True(report.ProviderEvidenceComplete);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(CacheMeasurement(operationId)));
            Assert.False(UsageReconciliationMath.IsInternalReceipt(json.RootElement, "WEB5", operationId, "succeeded"));
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [MongoFact]
    public async Task ReceiptConcurrencyAndConflictAreDurable()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string database = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var stores = Enumerable.Range(0, 12).Select(_ => new MongoUsageReconciler(client, database)).ToArray();
            await stores[0].InitializeAsync(default);
            var original = Receipt();
            string json = JsonSerializer.Serialize(original);
            await Task.WhenAll(stores.Select(s => s.RecordReceiptAsync(JsonSerializer.Deserialize<UsageExternalReceipt>(json), default)));
            var receipts = client.GetDatabase(database).GetCollection<UsageExternalReceipt>("subscription_usage_external_receipts");
            Assert.Equal(1, await receipts.CountDocumentsAsync(FilterDefinition<UsageExternalReceipt>.Empty));
            original.AmountUsd++;
            await Assert.ThrowsAsync<InvalidOperationException>(() => stores[0].RecordReceiptAsync(original, default));
            var report = await stores[0].ReconcileAsync(Avatar, "2026-09", default);
            Assert.Contains(report.Findings, x => x.Code == "ORPHAN_PROVIDER_RECEIPT");
            Assert.False(report.Balanced);
        }
        finally { await client.DropDatabaseAsync(database); }
    }

    [MongoFact]
    public async Task MultiProviderReceiptsAndStripeMustIndependentlyBalance()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name); var store = new MongoUsageReconciler(client, name, ClosedPeriod); await store.InitializeAsync(default);
            string operationId = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument
            {
                { "UserId", Avatar }, { "Month", "2026-09" }, { "Day", "2026-09-22" },
                { "RequestsDelta", 1L }, { "ReservedUnitsDelta", 0L }, { "SettledUnitsDelta", 5L },
                { "ReservedCostDeltaUsd", new Decimal128(0m) }, { "SettledCostDeltaUsd", new Decimal128(0.03m) }
            });
            foreach (var (type, period) in new[] { ("month", "2026-09"), ("day", "2026-09-22") })
                await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                {
                    { "_id", Avatar + ":" + type + ":" + period }, { "UserId", Avatar }, { "PeriodType", type }, { "Period", period },
                    { "Requests", 1L }, { "ReservedUnits", 0L }, { "SettledUnits", 5L },
                    { "ReservedCostUsd", new Decimal128(0m) }, { "SettledCostUsd", new Decimal128(0.03m) }
                });
            string receiptsJson = JsonSerializer.Serialize(new[]
            {
                new { Provider="provider-a", ProviderRequestId="a1", CostSource="provider", ActualCostUsd=0.01m },
                new { Provider="provider-b", ProviderRequestId="b1", CostSource="provider", ActualCostUsd=0.02m }
            });
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", operationId }, { "UserId", Avatar }, { "ConsumingService", "WEB6" }, { "Status", "settled" },
                { "ProviderReceiptsJson", receiptsJson }, { "AuthorizedAtUtc", new DateTime(2026,9,22,0,0,0,DateTimeKind.Utc) }
            });
            var report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.Equal(2, report.Findings.Count(x => x.Code == "MISSING_PROVIDER_RECEIPT"));
            Assert.False(report.ProviderEvidenceComplete); Assert.False(report.StripeEvidenceComplete);
            foreach (var (provider, request, amount) in new[] { ("provider-a", "a1", 0.01m), ("provider-b", "b1", 0.02m) })
            {
                var r = Receipt(); r.OperationId = operationId; r.Provider = provider; r.ProviderRequestId = request; r.AmountUsd = amount;
                await store.RecordReceiptAsync(r, default);
            }
            await store.RecordReceiptAsync(new UsageExternalReceipt { Source="stripe", UserId=Avatar, Month="2026-09", ExternalId="in_usage",
                AmountUsd=0.03m, EvidenceSha256=new string('b',64), Actor="stripe-worker" }, default);
            report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(report.Balanced);
            string expiredId = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", expiredId }, { "UserId", Avatar }, { "Status", "expired" },
                { "AuthorizedAtUtc", new DateTime(2026,9,22,0,0,0,DateTimeKind.Utc) }
            });
            var impossible = Receipt(); impossible.OperationId=expiredId; impossible.ExternalId="unexpected";
            await store.RecordReceiptAsync(impossible, default);
            report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.Contains(report.Findings, x => x.Code == "PROVIDER_WORK_AFTER_UNUSED_EXPIRY");
            Assert.False(report.Balanced);
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [MongoFact]
    public async Task DetectsProjectionDriftMissingEvidenceAndUnsettledExposure()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name);
            var store = new MongoUsageReconciler(client, name); await store.InitializeAsync(default);
            await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument
            {
                { "UserId", Avatar }, { "Month", "2026-09" }, { "Day", "2026-09-22" },
                { "RequestsDelta", 1L }, { "ReservedUnitsDelta", 10L }, { "SettledUnitsDelta", 0L },
                { "ReservedCostDeltaUsd", new Decimal128(0.2m) }, { "SettledCostDeltaUsd", new Decimal128(0m) }
            });
            var report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.Equal(2, report.Findings.Count(x => x.Code == "MISSING_BUCKET"));
            foreach (var (type, period) in new[] { ("month", "2026-09"), ("day", "2026-09-22") })
                await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                {
                    { "_id", Avatar + ":" + type + ":" + period }, { "UserId", Avatar }, { "PeriodType", type }, { "Period", period },
                    { "Requests", 1L }, { "ReservedUnits", 10L }, { "SettledUnits", 0L },
                    { "ReservedCostUsd", new Decimal128(0.2m) }, { "SettledCostUsd", new Decimal128(0m) }
                });
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", Guid.NewGuid().ToString("D") }, { "UserId", Avatar }, { "Status", "recovery-required" },
                { "AuthorizedAtUtc", new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc) }
            });
            report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.DoesNotContain(report.Findings, x => x.Code == "LEDGER_PROJECTION_DRIFT" || x.Code == "MISSING_BUCKET");
            Assert.Equal(1, report.UnsettledOperations); Assert.False(report.Balanced);
            Assert.False(report.ProviderEvidenceComplete);
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").UpdateOneAsync(new BsonDocument("PeriodType", "month"), new BsonDocument("$inc", new BsonDocument("Requests", 1L)));
            report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.Contains(report.Findings, x => x.Code == "LEDGER_PROJECTION_DRIFT");
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [MongoFact]
    public async Task ActiveLeaseIsPendingUntilPersistedExpiryAndMissingExpiryIsActionable()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name);
            var time = new TestTimeProvider(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
            var store = new MongoUsageReconciler(client, name, time); await store.InitializeAsync(default);
            string operationId = Guid.NewGuid().ToString("D");
            await SeedPeriodAsync(db, 1, 10, 0.02m, 0m);
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", operationId }, { "UserId", Avatar }, { "Status", "executing" },
                { "AuthorizedAtUtc", time.Now.UtcDateTime }, { "ExpiresAtUtc", time.Now.AddMinutes(5).UtcDateTime }
            });
            var pending = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.False(pending.Balanced); Assert.False(pending.ActionRequired); Assert.True(pending.AwaitingPeriodClose);
            Assert.False(pending.ProviderEvidenceComplete); Assert.False(pending.StripeEvidenceComplete); Assert.Equal(1, pending.UnsettledOperations);
            time.Now = time.Now.AddMinutes(5);
            var expired = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(expired.ActionRequired); Assert.Contains(expired.Findings, x => x.Code == "UNSETTLED_OPERATION");
            await db.GetCollection<BsonDocument>("subscription_usage_events").UpdateOneAsync(new BsonDocument("OperationId", operationId),
                new BsonDocument("$unset", new BsonDocument("ExpiresAtUtc", "")));
            var invalid = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(invalid.ActionRequired); Assert.Contains(invalid.Findings, x => x.Code == "INVALID_OPERATION_EXPIRY");
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [MongoFact]
    public async Task InvoiceAbsenceIsPendingUntilUtcMonthCloses()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name);
            var time = new TestTimeProvider(new DateTimeOffset(2026, 9, 30, 23, 59, 59, TimeSpan.Zero));
            var store = new MongoUsageReconciler(client, name, time); await store.InitializeAsync(default);
            await SeedPeriodAsync(db, 1, 0, 0m, 0.05m);
            var pending = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(pending.AwaitingPeriodClose); Assert.False(pending.Balanced); Assert.False(pending.ActionRequired);
            Assert.False(pending.StripeEvidenceComplete); Assert.Empty(pending.Findings);
            time.Now = time.Now.AddSeconds(1);
            var closed = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.False(closed.AwaitingPeriodClose); Assert.True(closed.ActionRequired);
            Assert.Contains(closed.Findings, x => x.Code == "MISSING_STRIPE_RECEIPT");
            await store.RecordReceiptAsync(new UsageExternalReceipt { Source="stripe", UserId=Avatar, Month="2026-09", ExternalId="in_period",
                AmountUsd=0.05m, EvidenceSha256=new string('b',64), Actor="stripe-worker" }, default);
            Assert.True((await store.ReconcileAsync(Avatar, "2026-09", default)).Balanced);
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    private static async Task SeedPeriodAsync(IMongoDatabase db, long requests, long reservedTokens, decimal reservedCost, decimal settledCost)
    {
        await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument
        {
            { "UserId", Avatar }, { "Month", "2026-09" }, { "Day", "2026-09-22" },
            { "RequestsDelta", requests }, { "ReservedUnitsDelta", reservedTokens }, { "SettledUnitsDelta", 0L },
            { "ReservedCostDeltaUsd", new Decimal128(reservedCost) }, { "SettledCostDeltaUsd", new Decimal128(settledCost) }
        });
        foreach (var (type, period) in new[] { ("month", "2026-09"), ("day", "2026-09-22") })
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
            {
                { "_id", Avatar + ":" + type + ":" + period }, { "UserId", Avatar }, { "PeriodType", type }, { "Period", period },
                { "Requests", requests }, { "ReservedUnits", reservedTokens }, { "SettledUnits", 0L },
                { "ReservedCostUsd", new Decimal128(reservedCost) }, { "SettledCostUsd", new Decimal128(settledCost) }
            });
    }

    [MongoFact]
    public async Task WorkDiscoveryUnionsAuditBucketsAndOrphanEvidenceWithDistinctBoundedPages()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name); var store = new MongoUsageReconciler(client, name, ClosedPeriod);
            await store.InitializeAsync(default);
            await SeedPeriodAsync(db, 1, 0, 0m, 0m);
            // Losing the month projection must not remove its immutable audit from the scan.
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").DeleteOneAsync(new BsonDocument("PeriodType", "month"));
            var orphan = Receipt(); orphan.UserId = Guid.NewGuid().ToString("D");
            await store.RecordReceiptAsync(orphan, default);
            var stripe = new UsageExternalReceipt { Source="stripe", UserId=orphan.UserId, Month="2026-10", ExternalId="in_orphan",
                AmountUsd=0.01m, EvidenceSha256=new string('b',64), Actor="stripe-worker" };
            await store.RecordReceiptAsync(stripe, default);
            string bucketOnly = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument {
                { "_id", bucketOnly + ":month:2026-09" }, { "UserId", bucketOnly }, { "PeriodType", "month" }, { "Period", "2026-09" }
            });
            // A duplicate source key must not consume a result slot or skip a later key.
            await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument {
                { "UserId", orphan.UserId }, { "Month", "2026-10" }, { "Day", "2026-10-01" }
            });
            var expected = new[] { Avatar + ":month:2026-09", orphan.UserId + ":month:2026-09", orphan.UserId + ":month:2026-10", bucketOnly + ":month:2026-09" }
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var first = await store.GetWorkAsync("", 2, default);
            var second = await store.GetWorkAsync(first[^1].Id, 2, default);
            var end = await store.GetWorkAsync(second[^1].Id, 2, default);
            Assert.Equal(expected, first.Concat(second).Select(x => x.Id)); Assert.Empty(end);
            Assert.Contains((await store.ReconcileAsync(Avatar, "2026-09", default)).Findings, x => x.Code == "MISSING_BUCKET");
            Assert.Contains((await store.ReconcileAsync(orphan.UserId, "2026-09", default)).Findings, x => x.Code == "ORPHAN_PROVIDER_RECEIPT");
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [MongoFact]
    public async Task ProviderCorrectionsAffectOnlyTheirBoundReceiptAndBillingAdjustmentsAffectOnlyInvoiceTotals()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name); var store = new MongoUsageReconciler(client, name, ClosedPeriod);
            await store.InitializeAsync(default); await SeedPeriodAsync(db, 1, 0, 0m, 0.05m);
            string operation = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", operation }, { "UserId", Avatar }, { "Status", "settled" }, { "Outcome", "succeeded" }, { "ConsumingService", "WEB6" },
                { "AuthorizedAtUtc", new DateTime(2026,9,22,0,0,0,DateTimeKind.Utc) },
                { "ProviderReceiptsJson", JsonSerializer.Serialize(new[] {
                    new { Provider="provider-a", ProviderRequestId="a1", ActualCostUsd=0.01m },
                    new { Provider="provider-b", ProviderRequestId="b1", ActualCostUsd=0.04m }
                }) }
            });
            BsonDocument Correction(string kind, string provider, string request, decimal amount) => new()
            {
                { "UserId", Avatar }, { "Month", "2026-09" }, { "Day", "2026-09-22" }, { "OperationId", operation },
                { "Kind", "correction" }, { "CorrectionKind", kind }, { "Provider", provider }, { "ProviderRequestId", request },
                { "RequestsDelta", 0L }, { "ReservedUnitsDelta", 0L }, { "SettledUnitsDelta", 0L },
                { "ReservedCostDeltaUsd", new Decimal128(0m) }, { "SettledCostDeltaUsd", new Decimal128(amount) }
            };
            await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertManyAsync(new[] {
                Correction("provider-measurement", "provider-a", "a1", 0.02m), Correction("billing-adjustment", "", "", -0.01m)
            });
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").UpdateManyAsync(FilterDefinition<BsonDocument>.Empty,
                new BsonDocument("$inc", new BsonDocument("SettledCostUsd", new Decimal128(0.01m))));
            foreach (var (provider, request, cost) in new[] { ("provider-a", "a1", 0.03m), ("provider-b", "b1", 0.04m) })
            {
                var evidence = Receipt(); evidence.OperationId=operation; evidence.Provider=provider; evidence.ProviderRequestId=request; evidence.AmountUsd=cost;
                await store.RecordReceiptAsync(evidence, default);
            }
            await store.RecordReceiptAsync(new UsageExternalReceipt { Source="stripe", UserId=Avatar, Month="2026-09", ExternalId="in_corrected",
                AmountUsd=0.06m, EvidenceSha256=new string('b',64), Actor="stripe-worker" }, default);
            var report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.True(report.Balanced); Assert.Empty(report.Findings);
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [MongoFact]
    public async Task OpeningBalanceCannotFabricateCompleteHistoricalProviderEvidence()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string name = "usage_reconciliation_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = client.GetDatabase(name); var store = new MongoUsageReconciler(client, name, ClosedPeriod);
            await store.InitializeAsync(default); await SeedPeriodAsync(db, 1, 0, 0m, 0m);
            string operation = Guid.NewGuid().ToString("D"), sourceHash = new string('a', 64);
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "OperationId", operation }, { "UserId", Avatar }, { "Status", "settled" }, { "ConsumingService", "WEB6" },
                { "AuthorizedAtUtc", new DateTime(2026,9,22,0,0,0,DateTimeKind.Utc) },
                { "ProviderReceiptsJson", "[]" }, { "SettlementFingerprint", "legacy:" + sourceHash }
            });
            var report = await store.ReconcileAsync(Avatar, "2026-09", default);
            Assert.False(report.Balanced); Assert.False(report.ProviderEvidenceComplete); Assert.True(report.ActionRequired);
            Assert.Contains(report.Findings, x => x.Code == "HISTORICAL_PROVIDER_EVIDENCE_REQUIRED" && x.Reference == operation + ":legacy:" + sourceHash);
        }
        finally { await client.DropDatabaseAsync(name); }
    }
}

public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")))
            Skip = "Requires SUBSCRIPTION_TEST_MONGODB_URI pointing to an isolated MongoDB replica set.";
    }
}
