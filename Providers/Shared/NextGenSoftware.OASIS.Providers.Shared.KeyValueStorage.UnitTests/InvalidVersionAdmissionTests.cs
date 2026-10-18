using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class InvalidVersionAdmissionTests
{
    [DataTestMethod]
    [DataRow("LoadAvatar")] [DataRow("LoadAvatarByProviderKey")]
    [DataRow("LoadAvatarByUsername")] [DataRow("LoadAvatarByEmail")] [DataRow("LoadAllAvatars")]
    [DataRow("LoadAvatarDetail")] [DataRow("LoadAvatarDetailByUsername")]
    [DataRow("LoadAvatarDetailByEmail")] [DataRow("LoadAllAvatarDetails")]
    [DataRow("LoadHolon")] [DataRow("LoadHolonsForParent")] [DataRow("LoadAllHolons")]
    [DataRow("LoadHolonsByMetaData")] [DataRow("ExportAll")]
    [DataRow("ExportAllDataForAvatarById")] [DataRow("ExportAllDataForAvatarByUsername")]
    [DataRow("ExportAllDataForAvatarByEmail")]
    public async Task InvalidVersionRejectsBeforeAnyBackendOperation(string operation)
    {
        var methods=typeof(TestProvider).GetMethods().Where(method =>
            (method.Name==operation || method.Name==operation+"Async") &&
            method.GetParameters().Any(parameter=>parameter.Name=="version")).ToArray();
        Assert.IsTrue(methods.Length>=2,"Both sync and async contracts must be exercised.");
        foreach(var method in methods)
        foreach(var version in new[]{-1,int.MinValue})
        {
            var backend=new ProbeBackend();var provider=new TestProvider(backend);
            var args=method.GetParameters().Select(parameter=>Argument(parameter,version)).ToArray();
            var result=method.Invoke(provider,args);
            if(result is Task task){await task;result=task.GetType().GetProperty("Result")!.GetValue(task);}
            var label=method+" version="+version;
            Assert.AreEqual(0,backend.Calls,label+": invalid input must not issue even a paid read/list.");
            Assert.IsTrue((bool)result!.GetType().GetProperty("IsError")!.GetValue(result)!,label);
            Assert.IsInstanceOfType(result.GetType().GetProperty("Exception")!.GetValue(result),typeof(ArgumentOutOfRangeException),label);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.GetType().GetProperty("ErrorCode")!.GetValue(result),label);
        }
    }

    private static object Argument(ParameterInfo parameter,int version)
    {
        if(parameter.Name=="version")return version;
        if(parameter.HasDefaultValue)return parameter.DefaultValue!;
        if(parameter.ParameterType==typeof(Guid))return Guid.NewGuid();
        if(parameter.ParameterType==typeof(string))return Guid.NewGuid().ToString();
        if(parameter.ParameterType==typeof(Dictionary<string,string>))return new Dictionary<string,string>{{"key","value"}};
        if(parameter.ParameterType.IsEnum)return Enum.GetValues(parameter.ParameterType).GetValue(0)!;
        throw new InvalidOperationException("Unhandled required test parameter: "+parameter);
    }

    private sealed class ProbeBackend:IKeyValueBackend
    {
        internal int Calls;
        public Task<string> GetAsync(string key,CancellationToken cancellationToken=default){Calls++;return Task.FromResult<string>(null!);}
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken=default){Calls++;return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());}
        public Task PutAsync(string key,string value,CancellationToken cancellationToken=default){Calls++;return Task.CompletedTask;}
        public Task DeleteAsync(string key,CancellationToken cancellationToken=default){Calls++;return Task.CompletedTask;}
        public Task VerifyAsync(CancellationToken cancellationToken=default){Calls++;return Task.CompletedTask;}
    }
}
