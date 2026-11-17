using System.Reflection;
using System.Runtime.Loader;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

[Collection("Admitted key manager controllers")]
public class HolonReadControllerTests
{
    [Fact]
    public void CompiledSingleReadPreservesProviderFailureAndExactHolon()
    {
        WithController(type =>
        {
            var method = type.GetMethod("MapHolonRead", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            OASISResult<Holon> Map(OASISResult<IHolon>? result) =>
                Assert.IsType<OASISResult<Holon>>(method!.Invoke(null, new object?[] { result }));
            Assert.True(Map(null).IsError);
            Assert.True(Map(new OASISResult<IHolon>()).IsError);
            var failure = Map(new OASISResult<IHolon> {
                IsError = true, ErrorCode = "REAL_SINGLE_READ_ERROR", Message = "Storage lookup failed."
            });
            Assert.True(failure.IsError);
            Assert.Equal("REAL_SINGLE_READ_ERROR", failure.ErrorCode);
            Assert.Equal("Storage lookup failed.", failure.Message);
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Real lookup" };
            var success = Map(new OASISResult<IHolon> { Result = holon });
            Assert.False(success.IsError);
            Assert.Same(holon, success.Result);

            var requestType = type.Assembly.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.LoadHolonRequest", true)!;
            var request = Activator.CreateInstance(requestType)!;
            requestType.GetProperty("ChildHolonType")!.SetValue(request, "invalid-child-type");
            var action = type.GetMethod("LoadHolon", new[] { requestType })!;
            var task = Assert.IsAssignableFrom<Task>(action.Invoke(Activator.CreateInstance(type), new[] { request }));
            task.GetAwaiter().GetResult();
            var rejected = Assert.IsType<OASISHttpResponseMessage<Holon>>(task.GetType().GetProperty("Result")!.GetValue(task));
            Assert.True(rejected.Result.IsError);
            Assert.Contains("invalid-child-type", rejected.Result.Message);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompiledControllerRejectsInvalidParentAndChildTypesBeforeRouting(bool parent)
    {
        WithController(type =>
        {
            const string invalid = "not-a-real-holon-type";
            var requestType = type.Assembly.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.LoadAllHolonsRequest", true)!;
            var request = Activator.CreateInstance(requestType)!;
            requestType.GetProperty("HolonType")!.SetValue(request, parent ? invalid : "All");
            requestType.GetProperty("ChildHolonType")!.SetValue(request, parent ? "All" : invalid);
            var controller = Activator.CreateInstance(type);
            var action = type.GetMethod("LoadAllHolons", new[] { requestType })!;
            var task = Assert.IsAssignableFrom<Task>(action.Invoke(controller, new[] { request }));
            task.GetAwaiter().GetResult();
            var response = Assert.IsType<OASISHttpResponseMessage<IEnumerable<Holon>>>(task.GetType().GetProperty("Result")!.GetValue(task));
            Assert.True(response.Result.IsError);
            Assert.Contains(invalid, response.Result.Message);
            if (parent)
            {
                var shorthand = type.GetMethod("LoadAllHolons", new[] { typeof(string) })!;
                var shortTask = Assert.IsAssignableFrom<Task>(shorthand.Invoke(controller, new object[] { invalid }));
                shortTask.GetAwaiter().GetResult();
                var shortResponse = Assert.IsType<OASISHttpResponseMessage<IEnumerable<Holon>>>(shortTask.GetType().GetProperty("Result")!.GetValue(shortTask));
                Assert.True(shortResponse.Result.IsError);
                Assert.Contains(invalid, shortResponse.Result.Message);
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompiledControllerPreservesErrorsEmptyAndRealHolons(bool detailed)
    {
        WithController(type =>
        {
            var method = type.GetMethod("FormatHolonListRead", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            OASISHttpResponseMessage<IEnumerable<Holon>> Format(OASISResult<IEnumerable<IHolon>>? input) =>
                Assert.IsType<OASISHttpResponseMessage<IEnumerable<Holon>>>(method!.Invoke(null, new object?[] { input, detailed }));

            Assert.True(Format(null).Result.IsError);
            Assert.True(Format(new OASISResult<IEnumerable<IHolon>>()).Result.IsError);
            var empty = Format(new OASISResult<IEnumerable<IHolon>> { Result = Array.Empty<IHolon>() });
            Assert.False(empty.Result.IsError);
            Assert.Empty(empty.Result.Result);
            Assert.Equal(detailed, empty.ShowDetailedSettings);

            var failed = Format(new OASISResult<IEnumerable<IHolon>> {
                IsError = true, ErrorCode = "REAL_PROVIDER_ERROR", Message = "The storage read failed."
            });
            Assert.True(failed.Result.IsError);
            Assert.Equal("REAL_PROVIDER_ERROR", failed.Result.ErrorCode);
            Assert.Equal("The storage read failed.", failed.Result.Message);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, failed.StatusCode);
            var withoutMessage = Format(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = null });
            Assert.True(withoutMessage.Result.IsError);

            var id = Guid.NewGuid();
            var real = Format(new OASISResult<IEnumerable<IHolon>> {
                Result = new IHolon[] { new Holon { Id = id, Name = "Persisted holon" } }
            });
            Assert.False(real.Result.IsError);
            Assert.Equal(id, Assert.Single(real.Result.Result).Id);
        });
    }

    private static void WithController(Action<Type> check)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var project = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var configuration = typeof(HolonReadControllerTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        Assert.Contains(configuration, new[] { "Debug", "Release" });
        var artifact = Path.Combine(project, "bin", configuration!, "net10.0", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the retained WEB4 artifact first.");
        foreach (var source in new[] { "Controllers/DataController.Load.cs", "Helpers/HttpResponseHelper.cs" })
            Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(Path.Combine(project, source)), "WEB4 artifact is stale.");
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
            check(api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.DataController", true)!);
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
