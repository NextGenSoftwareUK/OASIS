using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task MessagingHistoryAndSessionResumeUseActualSdkHttpAndOwnedMongo()
    {
        RequireOwnedFixtures();
        await using (var mongo = new MongoStore())
        {
            await mongo.Activate();
            IOASISStorageProvider[] stores = [mongo.Provider];
            var actor = Guid.NewGuid();
            var sender = Guid.NewGuid();
            await SeedRoutingStores(stores, actor);
            await SeedRoutingStores(stores, sender);
            var manager = Routing(stores, false);
            Assert.False(manager.PublishRoutingOverrides(new NextGenSoftware.OASIS.API.Core.Configuration.ProviderRoutingOverrides(
                autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false), ProviderType.MongoDBOASIS).IsError);
            T Create<T>() => (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                null, [mongo.Provider, manager.OASISDNA, manager], null)!;
            var messages = Create<MessagingManager>();
            var chat = Create<ChatManager>();
            var earlierSender = Guid.NewGuid();
            await SeedRoutingStores(stores, earlierSender);
            var oldestMessage = await messages.SendMessageToAvatarAsync(earlierSender, actor, "Older conversation beyond first page");
            Assert.False(oldestMessage.IsError, oldestMessage.Message);
            for (var i = 0; i < 51; i++)
            {
                var sent = await messages.SendMessageToAvatarAsync(sender, actor, "Owned inbox message " + i);
                Assert.False(sent.IsError, sent.Message);
                Assert.False(sent.IsWarning, sent.Message);
            }
            var started = await chat.StartNewChatSessionAsync([actor, sender], "Owned persisted resumable chat");
            Assert.False(started.IsError, started.Message);
            Assert.False(string.IsNullOrWhiteSpace(started.Result));
            for (var i = 0; i < 51; i++)
            {
                var sent = await chat.SendMessageAsync(started.Result, actor, "Owned chat message " + i);
                Assert.False(sent.IsError, sent.Message);
                Assert.False(sent.IsWarning, sent.Message);
            }
            var persistedMessages = Create<MessagingManager>();
            var baseline = await mongo.ReadNative(actor);
            var chatField = typeof(ChatManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var messagesField = typeof(MessagingManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previousChat = chatField.GetValue(null);
            var previousMessages = messagesField.GetValue(null);
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
            Assert.NotNull(root);
            var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
                "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
            Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the browser case.");
            var resolver = new AssemblyDependencyResolver(artifact);
            Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
            {
                var path = resolver.ResolveAssemblyToPath(name);
                return path == null ? null : context.LoadFromAssemblyPath(path);
            }
            AssemblyLoadContext.Default.Resolving += Resolve;
            try
            {
                chatField.SetValue(null, chat);
                messagesField.SetValue(null, persistedMessages);
                var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
                var controllers = new[] { "ChatController", "MessagingController" }
                    .Select(name => api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers." + name, true)!).ToArray();
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
                {
                    parts.ApplicationParts.Clear();
                    parts.ApplicationParts.Add(new AssemblyPart(api));
                    parts.FeatureProviders.Add(new ProfileControllerFeature(controllers));
                }).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()));
                builder.Services.AddAuthorization();
                await using var app = builder.Build();
                using var portal = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned portal required."));
                app.UseStaticFiles(new StaticFileOptions { FileProvider = portal });
                app.MapGet("/__messaging_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/messaging-browser-host.html")), "text/html"));
                var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
                var validation = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                    ValidateIssuer = true, ValidIssuer = "owned-portal-fixture", ValidateAudience = true,
                    ValidAudience = "owned-portal-http", ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        var bearer = context.Request.Headers.Authorization.ToString();
                        if (!bearer.StartsWith("Bearer ", StringComparison.Ordinal)) { context.Response.StatusCode = 401; return; }
                        try
                        {
                            var principal = new JwtSecurityTokenHandler().ValidateToken(bearer[7..], validation, out _);
                            var id = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                            var saved = await mongo.Provider.LoadAvatarAsync(id);
                            if (saved.IsError || saved.Result?.Id != id) { context.Response.StatusCode = 401; return; }
                            context.User = principal;
                            context.Items["Avatar"] = saved.Result;
                        }
                        catch (SecurityTokenException) { context.Response.StatusCode = 401; return; }
                    }
                    await next(context);
                });
                app.UseAuthorization();
                app.MapControllers();
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("owned-portal-fixture", "owned-portal-http",
                        [new Claim(ClaimTypes.NameIdentifier, actor.ToString())], expires: DateTime.UtcNow.AddMinutes(5),
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                    using var result = await ExchangeSdkRequest(new { baseUrl = address, token, avatarId = actor, recipientId = sender, olderRecipientId = earlierSender, sessionId = started.Result, scenario = "history-resume", providerType = "MongoDBOASIS" }, TimeSpan.FromMinutes(2));
                    Assert.False(result.RootElement.TryGetProperty("isError", out var error) && error.GetBoolean(),
                        result.RootElement.TryGetProperty("message", out var message) ? message.GetString() : "Actual popup verification failed.");
                    Assert.True(result.RootElement.GetProperty("messagingHistoryResumeVerified").GetBoolean());
                    Assert.Equal(started.Result, result.RootElement.GetProperty("sessionId").GetString());
                    var readback = await Create<MessagingManager>().GetMessagesAsync(actor, limit: 100);
                    Assert.False(readback.IsError, readback.Message);
                    Assert.Equal(52, readback.Result.Count);
                    Assert.All(readback.Result, row => Assert.False(row.IsRead));
                    var notifications = await Create<MessagingManager>().GetNotificationsAsync(actor, limit: 100);
                    Assert.False(notifications.IsError, notifications.Message);
                    Assert.Equal(52, notifications.Result.Count);
                    Assert.All(notifications.Result, row => Assert.False(row.IsRead));
                    var coldChat = Create<ChatManager>();
                    var history = await coldChat.GetChatHistoryAsync(started.Result, actor, limit: 100);
                    Assert.False(history.IsError, history.Message);
                    Assert.Equal(51, history.Result.Count);
                    Assert.Contains(history.Result, row => row.Content == "Owned chat message 0");
                    Assert.Contains(history.Result, row => row.Content == "Owned chat message 50");
                    var active = await coldChat.GetActiveSessionsAsync(actor);
                    Assert.False(active.IsError, active.Message);
                    Assert.Equal(started.Result, Assert.Single(active.Result).Id);
                    SameRevisions(baseline, await mongo.ReadNative(actor));
                    Console.WriteLine("Messaging pagination/resume native round trip verified: MongoDBOASIS; 52 messages/notifications and 51 chat messages unchanged; active session preserved after browser reload.");
                }
                finally { using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await app.StopAsync(stop.Token); }
            }
            finally
            {
                chatField.SetValue(null, previousChat);
                messagesField.SetValue(null, previousMessages);
                Assert.Same(previousChat, chatField.GetValue(null));
                Assert.Same(previousMessages, messagesField.GetValue(null));
                AssemblyLoadContext.Default.Resolving -= Resolve;
                Console.WriteLine("Owned Messaging/chat application managers restored.");
            }
        }
        Console.WriteLine("Owned Messaging/chat Mongo scope disposal and database absence verified.");
    }
}
