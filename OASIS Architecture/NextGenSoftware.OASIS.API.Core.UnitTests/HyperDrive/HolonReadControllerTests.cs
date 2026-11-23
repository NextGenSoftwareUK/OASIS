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
            var absent = Map(new OASISResult<IHolon> { ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "No Holon Found" });
            Assert.True(absent.IsError);
            Assert.Equal(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            Assert.Equal(OASISResultErrorCodes.RecordNotFound, Map(new OASISResult<IHolon>()).ErrorCode);
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
            var rejected = ResponseResult<Holon>(task.GetType().GetProperty("Result")!.GetValue(task));
            Assert.True(rejected.IsError);
            Assert.Contains("invalid-child-type", rejected.Message);
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
            var response = ResponseResult<IEnumerable<Holon>>(task.GetType().GetProperty("Result")!.GetValue(task));
            Assert.True(response.IsError);
            Assert.Contains(invalid, response.Message);
            if (parent)
            {
                var shorthand = type.GetMethod("LoadAllHolons", new[] { typeof(string) })!;
                var shortTask = Assert.IsAssignableFrom<Task>(shorthand.Invoke(controller, new object[] { invalid }));
                shortTask.GetAwaiter().GetResult();
                var shortResponse = ResponseResult<IEnumerable<Holon>>(shortTask.GetType().GetProperty("Result")!.GetValue(shortTask));
                Assert.True(shortResponse.IsError);
                Assert.Contains(invalid, shortResponse.Message);
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
            object Format(OASISResult<IEnumerable<IHolon>>? input) =>
                Assert.IsAssignableFrom<System.Net.Http.HttpResponseMessage>(method!.Invoke(null, new object?[] { input, detailed }));

            Assert.True(ResponseResult<IEnumerable<Holon>>(Format(null)).IsError);
            Assert.True(ResponseResult<IEnumerable<Holon>>(Format(new OASISResult<IEnumerable<IHolon>>())).IsError);
            var empty = Format(new OASISResult<IEnumerable<IHolon>> { Result = Array.Empty<IHolon>() });
            var emptyResult = ResponseResult<IEnumerable<Holon>>(empty);
            Assert.False(emptyResult.IsError);
            Assert.Empty(emptyResult.Result);
            Assert.Equal(detailed, Assert.IsType<bool>(empty.GetType().GetProperty("ShowDetailedSettings")!.GetValue(empty)));

            var failed = Format(new OASISResult<IEnumerable<IHolon>> {
                IsError = true, ErrorCode = "REAL_PROVIDER_ERROR", Message = "The storage read failed."
            });
            var failedResult = ResponseResult<IEnumerable<Holon>>(failed);
            Assert.True(failedResult.IsError);
            Assert.Equal("REAL_PROVIDER_ERROR", failedResult.ErrorCode);
            Assert.Equal("The storage read failed.", failedResult.Message);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, Assert.IsAssignableFrom<System.Net.Http.HttpResponseMessage>(failed).StatusCode);
            var withoutMessage = Format(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = null });
            Assert.True(ResponseResult<IEnumerable<Holon>>(withoutMessage).IsError);

            var id = Guid.NewGuid();
            var real = ResponseResult<IEnumerable<Holon>>(Format(new OASISResult<IEnumerable<IHolon>> {
                Result = new IHolon[] { new Holon { Id = id, Name = "Persisted holon" } }
            }));
            Assert.False(real.IsError);
            Assert.Equal(id, Assert.Single(real.Result).Id);
        });
    }

    private static OASISResult<T> ResponseResult<T>(object? response)
    {
        Assert.NotNull(response);
        Assert.Equal("NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.OASISHttpResponseMessage`1",
            response!.GetType().GetGenericTypeDefinition().FullName);
        var property = response.GetType().GetProperty("Result");
        Assert.NotNull(property);
        return Assert.IsType<OASISResult<T>>(property!.GetValue(response));
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
