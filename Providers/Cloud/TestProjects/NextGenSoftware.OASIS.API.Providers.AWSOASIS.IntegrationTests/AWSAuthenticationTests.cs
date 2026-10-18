using System.Reflection;
using System.Collections.Concurrent;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task OfficialSessionAndDefaultCredentialsSignRealLocalStorageRequests(bool defaults)
    {
        const string endpoint="http://127.0.0.1:18000";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        foreach(var type in new[] {"Amazon.SecurityToken.AmazonSecurityTokenServiceClient, AWSSDK.SecurityToken",
            "Amazon.SSO.AmazonSSOClient, AWSSDK.SSO","Amazon.SSOOIDC.AmazonSSOOIDCClient, AWSSDK.SSOOIDC"})
            Assert.IsNotNull(Type.GetType(type,throwOnError:false),"Credential SDK assemblies must be present in the actual provider test host.");
        var variables=new[] {"AWS_ACCESS_KEY_ID","AWS_SECRET_ACCESS_KEY","AWS_SESSION_TOKEN","AWS_PROFILE","AWS_DEFAULT_PROFILE","AWS_EC2_METADATA_DISABLED"};
        var previous=variables.ToDictionary(name=>name,Environment.GetEnvironmentVariable);
        AWSOASIS? provider=null;
        try
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID","OwnedEnvAccess");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY","owned-env-secret");
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN","owned-env-token");
            Environment.SetEnvironmentVariable("AWS_PROFILE",null);Environment.SetEnvironmentVariable("AWS_DEFAULT_PROFILE",null);
            Environment.SetEnvironmentVariable("AWS_EC2_METADATA_DISABLED","true");FallbackCredentialsFactory.Reset();
            provider=CloudProviderFactory.CreateAws(new AWSOASISProviderSettings {Region="us-east-1",ServiceUrl=endpoint,
                UseDefaultCredentials=defaults,AccessKey=defaults ? null!:"OwnedSessionAccess",SecretKey=defaults ? null!:"owned-session-secret",
                SessionToken=defaults ? null!:"owned-session-token"},null!);
            var client=(AmazonDynamoDBClient)typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS)
                .GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
            var tokens=new ConcurrentBag<(string Token,string Authorization)>();
            void Observe(AmazonDynamoDBClient current)
            {
                var pipeline=(RuntimePipeline)typeof(AmazonServiceClient).GetProperty("RuntimePipeline",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(current)!;
                pipeline.AddHandlerAfter<Signer>(new SignedTokenObserver(tokens));
            }
            Observe(client);
            var activated=await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError,activated.Exception?.ToString() ?? activated.Message);
            var row=new Holon {Id=Guid.NewGuid(),Name="owned credential-mode roundtrip"};
            Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
            Assert.AreEqual(row.Name,(await provider.LoadHolonAsync(row.Id,loadChildren:false)).Result.Name);
            Assert.IsTrue(tokens.Count>0,"Official SDK must attach the session token to actual local requests.");
            var beforeReactivation=tokens.Count;
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Observe((AmazonDynamoDBClient)typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS)
                .GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!);
            Assert.AreEqual(row.Name,(await provider.LoadHolonAsync(row.Id,loadChildren:false)).Result.Name);
            Assert.IsFalse((await provider.DeleteHolonAsync(row.Id)).IsError);
            Assert.IsTrue(tokens.Count>beforeReactivation);
            Assert.IsTrue(tokens.All(value=>value.Token==(defaults ? "owned-env-token":"owned-session-token")));
            Assert.IsTrue(tokens.All(value=>value.Authorization.Contains("x-amz-security-token",StringComparison.OrdinalIgnoreCase)
                && value.Authorization.Contains("Credential="+(defaults ? "OwnedEnvAccess":"OwnedSessionAccess")+"/",StringComparison.Ordinal)));
        }
        finally
        {
            if(provider!=null) await provider.DeActivateProviderAsync();
            foreach(var entry in previous) Environment.SetEnvironmentVariable(entry.Key,entry.Value);
            FallbackCredentialsFactory.Reset();
        }
    }

    private sealed class SignedTokenObserver(ConcurrentBag<(string Token,string Authorization)> tokens):PipelineHandler
    {
        private void Observe(IExecutionContext context)
        {
            var headers=context.RequestContext.Request.Headers;
            string? Header(string name)=>headers.FirstOrDefault(pair=>string.Equals(pair.Key,name,StringComparison.OrdinalIgnoreCase)).Value;
            var token=Header("X-Amz-Security-Token");
            if(token!=null) tokens.Add((token,Header("Authorization")??string.Empty));
        }
        public override void InvokeSync(IExecutionContext context) {Observe(context);base.InvokeSync(context);}
        public override Task<T> InvokeAsync<T>(IExecutionContext context) {Observe(context);return base.InvokeAsync<T>(context);}
    }
}
