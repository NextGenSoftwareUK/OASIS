#nullable enable
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public class Web4SettlementControllerTests
{
    [Fact]
    public async Task CompiledControllerEnforcesProtocolAndServiceIdentityOnBothHttpRoutes()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Docker/oasis-dependency-versions.env")))root=root.Parent;
        Assert.NotNull(root);
        var project=Path.Combine(root!.FullName,"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var artifact=Path.Combine(project,"bin/Debug/net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact),"Build the existing WEB4 output before testing its real controller; no substitute controller is used.");
        Assert.True(File.GetLastWriteTimeUtc(artifact)>=File.GetLastWriteTimeUtc(Path.Combine(project,"Controllers/SubscriptionController.cs")),"WEB4 controller artifact is stale.");
        var resolver=new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context,AssemblyName name)
        {
            var path=resolver.ResolveAssemblyToPath(name);
            return path==null?null:context.LoadFromAssemblyPath(path);
        }
        AssemblyLoadContext.Default.Resolving+=Resolve;
        try{
            var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var type=assembly.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController",true)!;
            var contract=assembly.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.ISubscriptionService",true)!;
            var service=DispatchProxy.Create(contract,typeof(SettlementService));var observed=(SettlementService)service;
            const string key="fixture-service-secret-at-least-32-bytes";
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"SUBSCRIPTION_SERVICE_KEY_WEB6",key}}).Build();
            using var host=new HostBuilder().ConfigureWebHost(web=>web.UseTestServer().ConfigureServices(services=>{
                services.AddLogging();services.AddSingleton<IControllerActivator>(new Activator(type,config,service));
                services.AddControllers().ConfigureApplicationPartManager(parts=>{
                    parts.ApplicationParts.Clear();parts.FeatureProviders.Clear();parts.FeatureProviders.Add(new ControllerOnly(type));
                });
            }).Configure(app=>{app.UseRouting();app.UseEndpoints(endpoints=>endpoints.MapControllers());})).Build();
            await host.StartAsync();using var client=host.GetTestClient();
            foreach(var native in new[]{false,true})foreach(var nativeRoute in new[]{false,true})foreach(var authenticated in new[]{false,true}){
                var id=Guid.NewGuid();var receipt=new UsageProviderReceipt {Provider="SuiOASIS",ProviderRequestId=id.ToString("D"),
                    Units=1,CostSource="test-only",PricingCatalogueVersion="fixture-v1"};
                if(native)receipt.NativeFeeValuations=new(){new(id,receipt.Provider,receipt.ProviderRequestId,"MIST","0",0,"test-only-price","v1",new DateTime(2026,10,17,0,0,0,DateTimeKind.Utc))};
                var payload=new UsageSettlementRequest {OperationId=id.ToString("D"),UserId=Guid.NewGuid().ToString("D"),ConsumingService="WEB6",
                    Outcome="succeeded",Provider=receipt.Provider,ProviderRequestId=receipt.ProviderRequestId,ActualCostUsd=0,Units=1,
                    CostSource=receipt.CostSource,PricingCatalogueVersion=receipt.PricingCatalogueVersion,ProviderReceipts=new(){receipt}};
                using var request=new HttpRequestMessage(HttpMethod.Post,nativeRoute?UsageSettlementProtocol.NativePath:UsageSettlementProtocol.OrdinaryPath){
                    Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json")};
                if(authenticated){request.Headers.Add("X-OASIS-Service","WEB6");request.Headers.Add("X-OASIS-Service-Key",key);}
                var before=observed.Calls;using var response=await client.SendAsync(request);
                Assert.Equal(!authenticated?HttpStatusCode.Unauthorized:native!=nativeRoute?HttpStatusCode.BadRequest:HttpStatusCode.OK,response.StatusCode);
                Assert.Equal(before+(authenticated&&native==nativeRoute?1:0),observed.Calls);
            }
        }finally{AssemblyLoadContext.Default.Resolving-=Resolve;}
    }
    public class SettlementService:DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Assert.Equal("SettleUsageAsync",method!.Name);Calls++;
            var operation=args![1]!.GetType().GetProperty("OperationId")!.GetValue(args[1]);
            var resultType=method!.ReturnType.GenericTypeArguments.Single();
            var result=JsonSerializer.Deserialize(JsonSerializer.Serialize(new{Settled=true,OperationId=operation}),resultType);
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null,new[]{result});
        }
    }
    private sealed class ControllerOnly(Type type):IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts,ControllerFeature feature)=>feature.Controllers.Add(type.GetTypeInfo());
    }
    private sealed class Activator(Type type,IConfiguration config,object service):IControllerActivator
    {
        // Bind the real public constructor by arity: configuration, the settlement service, and null for every optional dependency.
        public object Create(ControllerContext context)
        {
            var ctor=type.GetConstructors().Single(c=>c.GetParameters().Length>=2);
            var args=new object?[ctor.GetParameters().Length]; args[0]=config; args[1]=service;
            return ctor.Invoke(args);
        }
        public void Release(ControllerContext context,object controller){}
    }
}
