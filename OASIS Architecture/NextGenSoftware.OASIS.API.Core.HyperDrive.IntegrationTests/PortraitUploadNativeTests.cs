using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    // Actual compiled action boundary and persisted Mongo portrait; not production HTTP/Startup acceptance.
    [Fact]
    public async Task PortraitUploadBindsAllTargetsBeforeProviderActivationNative()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("Exact owned loopback Mongo fixture required.");
        await using var mongo = new MongoStore();
        await mongo.Activate();
        var ownerId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        await SeedRoutingStores([mongo.Provider], ownerId);
        await SeedRoutingStores([mongo.Provider], foreignId);
        var owner = await mongo.Provider.LoadAvatarAsync(ownerId);
        Assert.False(owner.IsError, owner.Message);
        Assert.NotNull(owner.Result);
        owner.Result.AvatarType.Value = AvatarType.User;
        var foreign = await mongo.Provider.LoadAvatarAsync(foreignId);
        Assert.False(foreign.IsError, foreign.Message);
        Assert.NotNull(foreign.Result);
        var routing = Routing([mongo.Provider], false, HyperDriveModes.Legacy);
        var manager = new AvatarManager(null, routing.OASISDNA, routing);
        var beforeOwner = await Read(mongo.Provider, ownerId);
        var beforeForeign = await Read(mongo.Provider, foreignId);

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env")))
            root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var project = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var artifact = Path.Combine(project, "bin", configuration, "net10.0", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build actual WEB4 first.");
        Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(
            Path.Combine(project, "Controllers/AvatarProfileController.cs")), "API artifact is stale.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var program = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Program", true)!;
            var field = program.GetField("_avatarManager", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.NotNull(field);
            var previous = field.GetValue(null);
            field.SetValue(null, manager);
            try
            {
                var type = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.AvatarProfileController", true)!;
                var controller = (ControllerBase)Activator.CreateInstance(type)!;
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
                controller.HttpContext.Items["Avatar"] = owner.Result;
                var actions = type.GetMethods().Where(method => method.Name == "UploadAvatarPortrait").ToArray();
                Assert.Equal(2, actions.Length);
                var plain = Assert.Single(actions.Where(method => method.GetParameters().Length == 1));
                var selected = Assert.Single(actions.Where(method => method.GetParameters().Length == 3));
                var requestType = plain.GetParameters()[0].ParameterType;
                const string image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl2nXcAAAAASUVORK5CYII=";
                object Request(int conflict = -1)
                {
                    var request = Activator.CreateInstance(requestType)!;
                    requestType.GetProperty("AvatarId")!.SetValue(request, conflict == 0 ? foreignId : ownerId);
                    requestType.GetProperty("Username")!.SetValue(request, conflict == 1 ? foreign.Result.Username : owner.Result.Username);
                    requestType.GetProperty("Email")!.SetValue(request, conflict == 2 ? foreign.Result.Email : owner.Result.Email);
                    requestType.GetProperty("ImageBase64")!.SetValue(request, image);
                    return request;
                }
                async Task<HttpResponseMessage> Invoke(MethodInfo method, object? request)
                {
                    object?[] args = method == plain ? [request] : [request, (ProviderType)int.MaxValue, false];
                    var pending = (Task)method.Invoke(controller, args)!;
                    await pending;
                    return (HttpResponseMessage)pending.GetType().GetProperty("Result")!.GetValue(pending)!;
                }
                foreach (var method in actions)
                {
                    for (var conflict = 0; conflict < 3; conflict++)
                    {
                        using var response = await Invoke(method, Request(conflict));
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                        var result = (OASISResult<bool>)response.GetType().GetProperty("Result")!.GetValue(response)!;
                        Assert.True(result.IsError);
                        Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
                    }
                    using var malformed = await Invoke(method, null);
                    Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
                }
                SameRevisions(beforeOwner, await Read(mongo.Provider, ownerId));
                SameRevisions(beforeForeign, await Read(mongo.Provider, foreignId));
                using (var accepted = await Invoke(plain, Request()))
                {
                    Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
                    var result = (OASISResult<bool>)accepted.GetType().GetProperty("Result")!.GetValue(accepted)!;
                    Assert.False(result.IsError, result.Message);
                    Assert.True(result.Result);
                    Assert.True(result.IsSaved);
                }
                var portrait = await mongo.Provider.LoadAvatarDetailAsync(ownerId);
                Assert.False(portrait.IsError, portrait.Message);
                Assert.Equal(image, portrait.Result.Portrait);
                var untouched = await mongo.Provider.LoadAvatarDetailAsync(foreignId);
                Assert.False(untouched.IsError, untouched.Message);
                Assert.True(string.IsNullOrEmpty(untouched.Result.Portrait));
                SameRevisions(beforeForeign, await Read(mongo.Provider, foreignId));
                controller.HttpContext.Items.Remove("Avatar");
                using var anonymous = await Invoke(plain, Request());
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            }
            finally { field.SetValue(null, previous); }
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
