using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using CanonicalHoloOasis = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HoloOasisConductorFactAttribute : FactAttribute
{
    public HoloOasisConductorFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_CQL_HOLOOASIS_NATIVE") != "1")
            Skip = "Requires the disposable official Holochain conductor and verified OASIS hApp.";
    }
}

public sealed class CqlHoloOasisConductorTests
{
    private const ProviderType Provider = ProviderType.HoloOASIS;
    private const string Zome = "oasis";
    private const string ReadFunction = "get_holon_by_id";
    private const string Prefix = "CQL_HOLOOASIS_QUERY:";

    [HoloOasisConductorFact]
    public async Task ReadsCurrentPublicHolonAndDoesNotExposeDeletedRevisions()
    {
        var adminEndpoint = Environment.GetEnvironmentVariable("HOLONET_LIVE_ADMIN_URI");
        Assert.True(Uri.TryCreate(adminEndpoint, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback,
            "A dedicated loopback conductor is required.");
        var admin = new FixtureAdmin(adminEndpoint!);
        var writer = new CanonicalHoloOasis(admin, null, useHoloNetwork: false);
        HoloOasisReadSource? reader = null;
        try
        {
            var writerActivation = await writer.ActivateProviderAsync();
            Assert.True(writerActivation.Result && !writerActivation.IsError, writerActivation.ErrorCode);
            var fixture = new Holon { Id = Guid.NewGuid(), Name = "Public CQL fixture", IsPublic = true };
            var saved = await writer.SaveHolonAsync(fixture, saveChildren: false);
            Assert.True(!saved.IsError && saved.Result != null, saved.ErrorCode);

            var writerApp = writer.HoloNETClientAppAgent;
            var dna = writerApp.ConvertHoloHashToBytes(writerApp.HoloNETDNA.DnaHash);
            var agent = writerApp.ConvertHoloHashToBytes(writerApp.HoloNETDNA.AgentPubKey);
            var appId = CanonicalHoloOasis.OASIS_HAPP_ID;
            var credentials = await admin.CreateReadGrantAsync(dna, agent);
            var token = await admin.IssueAppAuthenticationTokenAsync(appId, expirySeconds: 600);
            Assert.False(token.IsError, "The conductor must issue an app token for the read connection.");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(
                    new Dictionary<string, Guid[]> { [Provider.ToString()] = [fixture.Id] }),
                [Prefix + "Endpoint"] = writerApp.EndPoint.AbsoluteUri,
                [Prefix + "InstalledAppId"] = appId,
                [Prefix + "Zome"] = Zome,
                [Prefix + "ProbeId"] = fixture.Id.ToString("D"),
                [Prefix + "DnaHash"] = Convert.ToBase64String(dna),
                [Prefix + "AgentKey"] = Convert.ToBase64String(agent),
                [Prefix + "AppToken"] = Convert.ToBase64String(token.TokenIssued.token),
                [Prefix + "CapabilitySecret"] = Convert.ToBase64String(credentials.CapSecret),
                [Prefix + "SigningKey"] = Convert.ToBase64String(credentials.SigningKey),
                [Prefix + "SigningPrivateKey"] = Convert.ToBase64String(credentials.KeyPair.PrivateKey)
            }).Build();
            var services = new ServiceCollection();
            CqlPublicReadCatalogue.Enroll(services, configuration);
            CqlHoloOasisReadSourceRegistration.AddHoloOasisReadSource(services, configuration);
            using var container = services.BuildServiceProvider();
            reader = Assert.IsType<HoloOasisReadSource>(Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage);
            var probe = await reader.ActivateQuerySourceAsync(default);
            Assert.True(probe.Result && !probe.IsError, probe.ErrorCode);

            var actor = Guid.NewGuid();
            var access = Assert.Single(container.GetServices<CqlPublicReadCatalogue>()).Authorize(actor, Provider);
            Assert.False(access.IsError, access.ErrorCode);
            var fixtureId = fixture.Id;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
                .WithReadAccess(access.Result).Where(row => row.Id == fixtureId);
            Assert.False(query.IsError, query.ErrorCode);
            var plan = CosmicQueryPlanner.Plan(query.Result, Provider, [new(Provider, reader.QueryCapabilities)]);
            Assert.False(plan.IsError, plan.ErrorCode);
            var initial = await reader.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(initial.IsError, initial.ErrorCode);
            Assert.Equal(fixture.Id, Assert.Single(initial.Result).Id);

            writer.ConfigurePublicQuery(fixture.Id, new HashSet<Guid> { fixture.Id });
            var canonicalProbe = await writer.ActivateQuerySourceAsync(default);
            Assert.True(canonicalProbe.Result && !canonicalProbe.IsError, canonicalProbe.ErrorCode);
            var canonicalPlan = CosmicQueryPlanner.Plan(query.Result, Provider, [new(Provider, writer.QueryCapabilities)]);
            Assert.False(canonicalPlan.IsError, canonicalPlan.ErrorCode);
            var canonicalInitial = await writer.ExecuteCosmicQueryAsync(canonicalPlan.Result);
            Assert.False(canonicalInitial.IsError, canonicalInitial.ErrorCode);
            Assert.Equal(fixture.Id, Assert.Single(canonicalInitial.Result).Id);

            fixture.IsPublic = false;
            fixture.Name = "Private revision";
            Assert.False((await writer.SaveHolonAsync(fixture, saveChildren: false)).IsError);
            var privateRevision = await reader.ExecuteCosmicQueryAsync(plan.Result);
            Assert.True(privateRevision.IsError);
            Assert.Null(privateRevision.Result);
            var canonicalPrivate = await writer.ExecuteCosmicQueryAsync(canonicalPlan.Result);
            Assert.True(canonicalPrivate.IsError);
            Assert.Null(canonicalPrivate.Result);

            fixture.IsPublic = true;
            fixture.Name = "Republished revision";
            Assert.False((await writer.SaveHolonAsync(fixture, saveChildren: false)).IsError);
            var republished = await reader.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(republished.IsError, republished.ErrorCode);
            Assert.Equal(fixture.Name, Assert.Single(republished.Result).Name);

            Assert.False((await writer.DeleteHolonAsync(fixture.Id)).IsError);
            var deleted = await reader.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(deleted.IsError, deleted.ErrorCode);
            Assert.Empty(deleted.Result);
            var canonicalDeleted = await writer.ExecuteCosmicQueryAsync(canonicalPlan.Result);
            Assert.False(canonicalDeleted.IsError, canonicalDeleted.ErrorCode);
            Assert.Empty(canonicalDeleted.Result);
        }
        finally
        {
            if (reader != null)
                await reader.DeActivateProviderAsync();
            await writer.DeActivateProviderAsync();
        }
    }

    private sealed class FixtureAdmin(string endpoint) : HoloNETClientAdmin(new HoloNETDNA
    {
        HolochainConductorAdminURI = endpoint,
        AutoStartHolochainConductor = false,
        AutoShutdownHolochainConductor = false
    })
    {
        public async Task<SigningCredentials> CreateReadGrantAsync(byte[] dna, byte[] agent)
        {
            var cell = new[] { dna, agent };
            var key = $"{ConvertHoloHashToString(agent)}:{ConvertHoloHashToString(dna)}";
            var writerCredentials = _signingCredentialsForCell[key];
            try
            {
                var grant = await AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(cell,
                    CapGrantAccessType.Assigned, GrantedFunctionsType.Listed, [(Zome, ReadFunction)]);
                Assert.False(grant.IsError, "The conductor must acknowledge the function-scoped read grant.");
                return _signingCredentialsForCell[key];
            }
            finally
            {
                _signingCredentialsForCell[key] = writerCredentials;
            }
        }
    }
}
