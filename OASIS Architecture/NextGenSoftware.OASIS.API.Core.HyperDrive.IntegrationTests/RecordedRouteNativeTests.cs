using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task RecordedRoutesUseOwnedMongoConditionalVisibilityAndLazyGeometry()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        await using var mongo = new MongoStore();
        await mongo.Activate();
        var owner = Guid.NewGuid(); var reader = Guid.NewGuid();
        IOASISStorageProvider[] stores = [mongo.Provider];
        await SeedRoutingStores(stores, owner); await SeedRoutingStores(stores, reader);
        var routing = Routing(stores, false);
        Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(
            autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false),
            ProviderType.MongoDBOASIS).IsError);
        var ownerBefore = await mongo.ReadNative(owner); var readerBefore = await mongo.ReadNative(reader);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
            "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the recorded route case.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        { var path = resolver.ResolveAssemblyToPath(name); return path == null ? null : context.LoadFromAssemblyPath(path); }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var type = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.MapRoutesController", true)!;
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var id = Guid.NewGuid();
            var geometryId = (Guid)type.GetMethod("Geometry", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [id])!;
            object Model(string name, object value) => JsonSerializer.Deserialize(JsonSerializer.Serialize(value, json), type.GetNestedType(name)!, json)!;
            async Task<object> Call(Guid actor, string action, params object[] arguments)
            {
                var controller = (ControllerBase)Activator.CreateInstance(type)!;
                type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(controller,
                    new HolonManager(mongo.Provider, routing.OASISDNA, routing));
                var context = new DefaultHttpContext();
                var avatar = await mongo.Provider.LoadAvatarAsync(actor);
                Assert.False(avatar.IsError, avatar.Message); Assert.NotNull(avatar.Result);
                context.Items["Avatar"] = avatar.Result;
                var model = arguments.LastOrDefault();
                var operation = model?.GetType().GetProperty("OperationId")?.GetValue(model);
                if (operation != null) context.Request.Headers["X-COSMIC-Operation-Id"] = operation.ToString();
                controller.ControllerContext = new ControllerContext { HttpContext = context };
                var task = (Task)type.GetMethod(action)!.Invoke(controller, arguments)!;
                await task;
                return task.GetType().GetProperty("Result")!.GetValue(task)!;
            }
            async Task<JsonElement> Detail(Guid actor)
            {
                var result = (OASISResult<object>)await Call(actor, "Get", id);
                Assert.False(result.IsError, result.Message);
                return JsonSerializer.SerializeToElement(result.Result, json);
            }
            var created = (OASISResult<IEnumerable<IHolon>>)await Call(owner, "Create", Model("CreateRouteRequest", new
            {
                operationId = Guid.NewGuid(), providerType = "MongoDBOASIS",
                expectedRevisions = new Dictionary<Guid, Guid?> { [id] = null, [geometryId] = null },
                route = new { id, name = "Owned route", sourceApp = "Owned route verification", sourceRouteId = "walk-1", isPublic = false,
                    segments = new[] { new[] { new { latitude = 0d, longitude = 0d }, new { latitude = 0d, longitude = .001d } } } }
            }));
            Assert.False(created.IsError, created.Message); Assert.True(created.IsSaved); Assert.Equal(2, created.Result.Count());
            Assert.True(((OASISResult<object>)await Call(reader, "Get", id)).IsError);
            var first = await Detail(owner);
            Dictionary<Guid, Guid?> Revisions(JsonElement detail) => new()
            { [id] = detail.GetProperty("expectedRevisions").GetProperty(id.ToString("D")).GetGuid(),
              [geometryId] = detail.GetProperty("expectedRevisions").GetProperty(geometryId.ToString("D")).GetGuid() };
            Assert.NotEqual(first.GetProperty("header").GetProperty("versionId").GetGuid(), Revisions(first)[id]);
            var payload = first.GetProperty("geometry").GetProperty("metaData").GetProperty("payload").GetString();
            object Access(Dictionary<Guid, Guid?> versions, bool visible, string name) => Model("RouteAccessRequest", new
            { operationId = Guid.NewGuid(), providerType = "MongoDBOASIS", expectedRevisions = versions, isPublic = visible, name });
            var publicWrite = (OASISResult<IEnumerable<IHolon>>)await Call(owner, "UpdateAccess", id, Access(Revisions(first), true, "Owned route"));
            Assert.False(publicWrite.IsError, $"{publicWrite.ErrorCode}: {publicWrite.Message} | {publicWrite.Exception}"); Assert.True(publicWrite.IsSaved);
            var shared = await Detail(reader);
            Assert.True(((OASISResult<IEnumerable<IHolon>>)await Call(reader, "UpdateAccess", id, Access(Revisions(shared), false, "Foreign rename"))).IsError);
            var revoked = (OASISResult<IEnumerable<IHolon>>)await Call(owner, "UpdateAccess", id, Access(Revisions(shared), false, "Renamed route"));
            Assert.False(revoked.IsError, revoked.Message); Assert.True(revoked.IsSaved);
            Assert.True(((OASISResult<object>)await Call(reader, "Get", id)).IsError);
            Assert.True(((OASISResult<IEnumerable<IHolon>>)await Call(owner, "UpdateAccess", id, Access(Revisions(first), true, "Stale write"))).IsError);
            var final = await Detail(owner);
            Assert.False(final.GetProperty("header").GetProperty("isPublic").GetBoolean());
            Assert.False(final.GetProperty("geometry").GetProperty("isPublic").GetBoolean());
            Assert.Equal("Renamed route", final.GetProperty("header").GetProperty("name").GetString());
            Assert.Equal(payload, final.GetProperty("geometry").GetProperty("metaData").GetProperty("payload").GetString());
            var listed = (OASISResult<object>)await Call(owner, "List", 0, 50);
            Assert.False(listed.IsError, listed.Message);
            var page = JsonSerializer.SerializeToElement(listed.Result, json);
            var item = Assert.Single(page.GetProperty("items").EnumerateArray());
            Assert.Equal(id, item.GetProperty("id").GetGuid());
            using var summary = JsonDocument.Parse(item.GetProperty("metaData").GetProperty("payload").GetString()!);
            Assert.Equal(2, summary.RootElement.GetProperty("PointCount").GetInt32());
            Assert.False(summary.RootElement.TryGetProperty("Segments", out _));
            SameRevisions(ownerBefore, await mongo.ReadNative(owner)); SameRevisions(readerBefore, await mongo.ReadNative(reader));
            Console.WriteLine("RECORDED_ROUTE_NATIVE_PROOF: create/private/public/revoke/foreign-write/stale-revision/summary-only/immutable-geometry/avatar-revisions passed on owned Mongo.");
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
