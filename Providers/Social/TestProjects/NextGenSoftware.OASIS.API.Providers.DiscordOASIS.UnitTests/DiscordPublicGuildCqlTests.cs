using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.DiscordOASIS.UnitTests;

[TestClass]
public sealed class DiscordPublicGuildCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Guilds = { "267624335836053506", "1235157165589794909", "212250894228652034" };

    [TestMethod]
    public async Task WrongGuildCannotActivateThePublicReader()
    {
        using var transport = new InviteTransport("999999999999999999");
        using var source = new DiscordPublicGuildQuerySource(Owner, Guilds, Guilds[0], transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, transport.Requests);
    }

    [TestMethod]
    public async Task ExactGuildProjectionOmitsOtherInviteFields()
    {
        using var transport = new InviteTransport(Guilds[0]);
        using var source = new DiscordPublicGuildQuerySource(Owner, Guilds, Guilds[0], transport);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        var ids = new[] { DiscordPublicGuildQuerySource.LogicalId(Owner, Guilds[0]) };
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, ids));
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        var guild = result.Result.Single();
        Assert.AreEqual(ids[0], guild.Id);
        Assert.AreEqual("Teaching Guild", guild.Name);
        Assert.AreEqual(Guilds[0], guild.MetaData["DiscordGuildId"]);
        Assert.IsFalse(guild.MetaData.ContainsKey("inviter"));
        Assert.IsFalse(guild.MetaData.ContainsKey("channel"));
        Assert.IsFalse(guild.MetaData.ContainsKey("instant_invite"));
        Assert.AreEqual(2, transport.Requests);
        var outside = await source.ExecuteCosmicQueryAsync(Plan(source, new[] { Guid.NewGuid() }));
        Assert.IsTrue(outside.IsError);
        Assert.AreEqual(2, transport.Requests);
    }

    [TestMethod, TestCategory("DiscordPublicGuildNative")]
    public async Task OfficialInviteApiReturnsExactThreeAndTwoPublicGuilds()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_DISCORD_PUBLIC_GUILD_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public guild reads explicitly.");
            return;
        }

        using var source = new DiscordPublicGuildQuerySource(Owner, Guilds, Guilds[0]);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Guilds.Take(count).ToArray();
            var ids = selected.Select(guild => DiscordPublicGuildQuerySource.LogicalId(Owner, guild)).ToArray();
            var result = await source.ExecuteCosmicQueryAsync(Plan(source, ids));
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row =>
                row.ProviderUniqueStorageKey[ProviderType.DiscordOASIS]).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == Owner));
        }
    }

    private static CosmicQueryPlan<Holon> Plan(DiscordPublicGuildQuerySource source, Guid[] ids)
    {
        const ProviderType provider = ProviderType.DiscordOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    private sealed class InviteTransport(string guildId) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://discord.com/api/v10/invites/python?with_counts=false", request.RequestUri!.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                type = 0, code = "python", guild = new { id = guildId, name = "Teaching Guild" },
                inviter = new { id = "fixture" }, channel = new { name = "fixture" }, instant_invite = "fixture"
            }) });
        }
    }
}
