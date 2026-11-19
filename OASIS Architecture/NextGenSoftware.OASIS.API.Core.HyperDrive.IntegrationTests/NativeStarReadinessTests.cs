using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

[CollectionDefinition("Native runtime state", DisableParallelization = true)]
public sealed class NativeRuntimeStateCollection { }

[Collection("Native runtime state")]
public sealed class NativeStarReadinessTests
{
    // A booted shared host cannot make a newly constructed native API instance ready.
    // This exercises compiled native clients without booting a provider or contacting a backend.
    [Fact]
    public void FreshNativeStarDoesNotInheritGlobalRuntimeReadiness()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env")))
            root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(NativeStarReadinessTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var project = Path.Combine(root!.FullName, "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI");
        var artifact = Path.Combine(project, "bin", configuration, "net10.0", "NextGenSoftware.OASIS.STAR.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB5 API and native dependency first.");
        var nativeSource = Path.Combine(root.FullName,
            "Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint/APIs/WEB5 STAR API/STARAPI.cs");
        Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(nativeSource),
            "The WEB5/native artifact is older than the changed readiness source.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var native = AssemblyLoadContext.Default.LoadFromAssemblyName(
                new AssemblyName("NextGenSoftware.OASIS.API.Native.Integrated.EndPoint"));
            var star = native.GetType("NextGenSoftware.OASIS.API.Native.EndPoint.STARAPI", true)!;
            var ctor = Assert.Single(star.GetConstructors().Where(value => value.GetParameters().Length == 2));
            var parameters = ctor.GetParameters();
            object Client() => ctor.Invoke([
                Activator.CreateInstance(parameters[0].ParameterType)!,
                Activator.CreateInstance(parameters[1].ParameterType)!
            ]);
            var first = Client();
            var second = Client();
            var state = star.GetProperty("IsOASISBooted")!;
            var quests = star.GetProperty("Quests")!;
            var loader = AssemblyLoadContext.Default.LoadFromAssemblyName(
                new AssemblyName("NextGenSoftware.OASIS.OASISBootLoader"));
            var loaderType = loader.GetType("NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader", true)!;
            var shared = loaderType.GetProperty("IsOASISBooted", BindingFlags.Public | BindingFlags.Static)!;
            var setter = shared.GetSetMethod(true)!;
            var previous = (bool)shared.GetValue(null)!;
            try
            {
                setter.Invoke(null, [true]);
                Assert.False((bool)state.GetValue(first)!);
                Assert.False((bool)state.GetValue(second)!);
                var unavailable = Assert.Throws<TargetInvocationException>(() => quests.GetValue(first));
                Assert.Equal("OASISException", unavailable.InnerException!.GetType().Name);
                Assert.Contains("not booted", unavailable.InnerException.Message, StringComparison.OrdinalIgnoreCase);

                state.SetValue(first, true);
                Assert.True((bool)state.GetValue(first)!);
                Assert.False((bool)state.GetValue(second)!);
                setter.Invoke(null, [false]);
                Assert.True((bool)state.GetValue(first)!);
                Assert.False((bool)state.GetValue(second)!);
            }
            finally { setter.Invoke(null, [previous]); }
            Assert.Equal(previous, (bool)shared.GetValue(null)!);
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
