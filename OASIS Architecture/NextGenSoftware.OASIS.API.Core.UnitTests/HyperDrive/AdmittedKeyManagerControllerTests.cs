using System.Reflection;
using System.Runtime.Loader;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

[CollectionDefinition("Admitted key manager controllers",DisableParallelization=true)]
public class AdmittedKeyManagerControllerCollection { }

[Collection("Admitted key manager controllers")]
public class AdmittedKeyManagerControllerTests
{
    [Theory]
    [InlineData("keys",false)] [InlineData("keys",true)]
    [InlineData("eosio",false)] [InlineData("eosio",true)]
    [InlineData("holochain",false)] [InlineData("holochain",true)]
    public async Task ActualControllerRejectsUnapprovedDnaBeforeBootActivation(string controller,bool missing)
    {
        var handle=new UsageExecutionHandle(new UsageOutboxOperation(),Mock.Of<IUsageOutboxStore>(),new Web4UsageLedgerOptions(),
                new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
        var policy=new PreparedStorageExecution(handle,"controller-acquisition",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>());
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Docker/oasis-dependency-versions.env")))root=root.Parent;
        Assert.NotNull(root);
        var project=Path.Combine(root!.FullName,"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var artifact=Path.Combine(project,"bin/Debug/net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact),"Build the retained WEB4 artifact first.");
        var name=controller switch {"keys"=>"KeysController","eosio"=>"EOSIOController",_=>"HolochainController"};
        Assert.True(File.GetLastWriteTimeUtc(artifact)>=File.GetLastWriteTimeUtc(Path.Combine(project,"Controllers",name+".cs")),"Controller artifact is stale.");
        var resolver=new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context,AssemblyName assemblyName){
            var path=resolver.ResolveAssemblyToPath(assemblyName);return path==null?null:context.LoadFromAssemblyPath(path);
        }
        AssemblyLoadContext.Default.Resolving+=Resolve;
        try{
            var api=AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var type=api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers."+name,true)!;
            var boot=Assembly.Load("NextGenSoftware.OASIS.OASISBootLoader").GetType("NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader",true)!;
            var dna=boot.GetProperty("OASISDNA",BindingFlags.Static|BindingFlags.Public)!;
            var prior=dna.GetValue(null);
            try{
                dna.SetValue(null,missing?null:new OASISDNA {OASIS=new(){HyperDriveMode=HyperDriveModes.Legacy}});
                await policy.RunAsync(()=>{
                    var wrapped=Assert.Throws<TargetInvocationException>(()=>type.GetProperty("KeyManager")!.GetValue(Activator.CreateInstance(type)));
                    var error=Assert.IsType<UsageProtocolException>(wrapped.InnerException);
                    Assert.Equal(PreparedStorageExecution.RejectedCode,error.Code);
                    Assert.Contains("requires HyperDrive V2",error.Message);
                    return Task.FromResult(0);
                });
            }finally{dna.SetValue(null,prior);}
        }finally{AssemblyLoadContext.Default.Resolving-=Resolve;}
    }
}
