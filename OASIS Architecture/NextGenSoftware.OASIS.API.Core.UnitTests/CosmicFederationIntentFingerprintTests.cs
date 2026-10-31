using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFederationIntentFingerprintTests
{
    private static readonly ProviderType[] Providers = { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };
    private static readonly CosmicQueryCapabilities Capabilities = new(
        Enum.GetValues<CosmicQueryFeatures>().Aggregate(CosmicQueryFeatures.None, (value, feature) => value | feature),
        new[] { typeof(string), typeof(bool), typeof(Guid), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(DateTime) },
        Enum.GetValues<CosmicQueryComparisonOperator>(), new CosmicQueryTraversalCapability(32, 10000));

    private static CosmicFederationPlan<Holon> Plan(CosmicQuery<Holon>? query = null,
        CosmicFederationOptions? options = null, CosmicFederationReadPolicy[]? policies = null, ProviderType[]? providers = null)
    {
        var targets = providers ?? Providers;
        var plan = CosmicFederationPlanner.Plan(query ?? CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),
            options ?? CosmicFederationOptions.Create().Result, targets[0],
            targets.Select(provider => new CosmicQueryTarget(provider, Capabilities)).ToArray(), policies!);
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    private static string Fingerprint(CosmicFederationPlan<Holon> plan)
    {
        var result = CosmicFederationIntentFingerprint.Create(plan);
        Assert.False(result.IsError, result.Message);
        Assert.Matches("^[0-9a-f]{64}$", result.Result);
        return result.Result;
    }

    [Fact]
    public void EquivalentSnapshotsBindIdenticallyAcrossInstancesAndCultures()
    {
        CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .Where(row => CosmicQueryMetadata.Value<decimal>(row, "price") > 1.25m).OrderBy(row => row.Name).Result;
        var expected = Fingerprint(Plan(Query()));
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(expected, Fingerprint(Plan(Query())));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void CompleteTargetCataloguesAndActorAreBoundRatherThanOnlyAuthorities()
    {
        var actor = Guid.NewGuid(); var one = Guid.NewGuid(); var two = Guid.NewGuid();
        CosmicFederationReadPolicy[] Policies(Guid principal, Guid[] records) => Providers.Select(provider =>
            new CosmicFederationReadPolicy(provider, CosmicQueryReadAccess.AuthoritativeRecords(principal, provider, records).Result)).ToArray();
        var original = Fingerprint(Plan(policies: Policies(actor, new[] { one, two })));
        Assert.Equal(original, Fingerprint(Plan(policies: Policies(actor, new[] { two, one }))));
        Assert.NotEqual(original, Fingerprint(Plan(policies: Policies(actor, new[] { one }))));
        Assert.NotEqual(original, Fingerprint(Plan(policies: Policies(Guid.NewGuid(), new[] { one, two }))));
        Assert.NotEqual(original, Fingerprint(Plan(policies: Policies(actor, Array.Empty<Guid>()))));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void EveryGlobalWindowAndResourceCeilingParticipates(int field)
    {
        var options = CosmicFederationOptions.Create(skip: field == 0 ? 1 : 0, take: field == 1 ? 99 : 100,
            maximumInputRows: field == 2 ? 9999 : 10000, maximumRowsPerProvider: field == 3 ? 999 : 1000,
            maximumInputBytes: field == 4 ? 16777215 : 16777216, maximumOutputBytes: field == 5 ? 1048575 : 1048576,
            maximumWork: field == 6 ? 999999 : 1000000, maximumProviderAttempts: field == 7 ? 63 : 64,
            maximumDurationMilliseconds: field == 8 ? 4999 : 5000,
            identityMode: field == 9 ? CosmicFederationIdentityMode.ProviderPriority : CosmicFederationIdentityMode.PreserveCopies,
            providerPriority: field == 9 ? Providers : null).Result;
        Assert.NotEqual(Fingerprint(Plan()), Fingerprint(Plan(options: options)));
    }

    [Fact]
    public void SourceWindowsOrderingAndMetadataPathSegmentsCannotAlias()
    {
        var root = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var ascending = root.OrderBy(row => row.Name).Result;
        Assert.NotEqual(Fingerprint(Plan(ascending)), Fingerprint(Plan(root.OrderByDescending(row => row.Name).Result)));
        Assert.NotEqual(Fingerprint(Plan(ascending)), Fingerprint(Plan(ascending.Skip(1).Take(2).Result)));
        var first = root.Where(row => CosmicQueryMetadata.Value<string>(row, "ab", "c") == "same").Result;
        var second = root.Where(row => CosmicQueryMetadata.Value<string>(row, "a", "bc") == "same").Result;
        Assert.NotEqual(Fingerprint(Plan(first)), Fingerprint(Plan(second)));
    }

    [Fact]
    public void ResolvedProviderOrderAndPriorityPreferenceAreBound()
    {
        Assert.NotEqual(Fingerprint(Plan()), Fingerprint(Plan(providers: Providers.Reverse().ToArray())));
        var options = CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority, providerPriority: Providers).Result;
        var reversed = CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority: Providers.Reverse().ToArray()).Result;
        Assert.NotEqual(Fingerprint(Plan(options: options)), Fingerprint(Plan(options: reversed)));
    }

    [Fact]
    public void DescendantBoundsAndPredicateKindsArePartOfTheIntent()
    {
        var root = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var first = root.WhereAnyDescendant(row => row.Name == "child", CosmicQueryTraversalOptions.Create(2, 10).Result).Result;
        var second = root.WhereAnyDescendant(row => row.Name == "child", CosmicQueryTraversalOptions.Create(3, 10).Result).Result;
        Assert.NotEqual(Fingerprint(Plan(first)), Fingerprint(Plan(second)));
        Assert.NotEqual(Fingerprint(Plan(root.Where(row => row.Name.Contains("x")).Result)),
            Fingerprint(Plan(root.Where(row => row.Name.StartsWith("x")).Result)));
        Assert.NotEqual(Fingerprint(Plan(root.Where(row => CosmicQueryMetadata.Exists(row, "x")).Result)),
            Fingerprint(Plan(root.Where(row => CosmicQueryMetadata.IsNull(row, "x")).Result)));
    }

    [Fact]
    public void CapturedLiteralsCannotDriftAfterCompilation()
    {
        var name = "before";
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Where(row => row.Name == name).Result;
        var plan = Plan(query); var original = Fingerprint(plan);
        name = "after";
        Assert.Equal(original, Fingerprint(plan));
        Assert.NotEqual(original, Fingerprint(Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Where(row => row.Name == name).Result)));
    }

    public sealed class ConstructorProbe : Holon
    {
        public static int Calls;
        public ConstructorProbe() { Interlocked.Increment(ref Calls); }
    }

    [Fact]
    public void FingerprintingNeverConstructsOrMaterializesAnEntity()
    {
        var before = ConstructorProbe.Calls;
        var plan = CosmicFederationPlanner.Plan(CosmicQuery<ConstructorProbe>.Create(), CosmicFederationOptions.Create().Result,
            Providers[0], new[] { new CosmicQueryTarget(Providers[0], Capabilities) });
        Assert.False(plan.IsError, plan.Message);
        Assert.False(CosmicFederationIntentFingerprint.Create(plan.Result).IsError);
        Assert.Equal(before, ConstructorProbe.Calls);
    }

    [Fact]
    public void EncodedSizeLimitRejectsInsteadOfPublishingAPrefixHash()
    {
        var providers = Enum.GetValues<ProviderType>().Where(provider => !CosmicQueryScope.Named(provider).IsError).Take(64).ToArray();
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var value = new string('x', CosmicQueryExpression.MaximumStringLength);
        for (var index = 0; index < CosmicQuery<Holon>.MaximumPredicates; index++)
        {
            var clause = source.Where(row => row.Name == value);
            Assert.False(clause.IsError, clause.Message);
            source = clause.Result;
        }
        var result = CosmicFederationIntentFingerprint.Create(Plan(source, providers: providers));
        Assert.Equal("CQL_FEDERATION_BINDING_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
    }
}
