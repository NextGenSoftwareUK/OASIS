using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class AwsBootConfigurationTests
{
    private static AmazonDynamoDBClient Client(AWSOASIS provider) => (AmazonDynamoDBClient)
        typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;

    [TestMethod]
    public void ExplicitSessionTokenReachesOfficialSdkCredentials()
    {
        var settings=new AWSOASISProviderSettings {Region="us-east-1",AccessKey="owned-session-access",SecretKey="owned-session-secret",
            SessionToken="owned-session-token",ServiceUrl="http://127.0.0.1:1"};
        var restored=JsonConvert.DeserializeObject<AWSOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=CloudProviderFactory.CreateAws(restored,null!);using var client=Client(provider);
        var credentials=(AWSCredentials)typeof(AmazonServiceClient).GetProperty("Credentials",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(client)!;
        Assert.IsInstanceOfType<SessionAWSCredentials>(credentials);
        Assert.AreEqual("owned-session-token",credentials.GetCredentials().Token);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public void OptedInDefaultChainUsesOfficialEnvironmentSessionCredentials()
    {
        var variables=new[] {"AWS_ACCESS_KEY_ID","AWS_SECRET_ACCESS_KEY","AWS_SESSION_TOKEN","AWS_PROFILE","AWS_DEFAULT_PROFILE","AWS_EC2_METADATA_DISABLED"};
        var previous=variables.ToDictionary(name=>name,Environment.GetEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID","owned-env-access");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY","owned-env-secret");
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN","owned-env-token");
            Environment.SetEnvironmentVariable("AWS_PROFILE",null);Environment.SetEnvironmentVariable("AWS_DEFAULT_PROFILE",null);
            Environment.SetEnvironmentVariable("AWS_EC2_METADATA_DISABLED","true");
            FallbackCredentialsFactory.Reset();
            var provider=CloudProviderFactory.CreateAws(new() {Region="us-east-1",UseDefaultCredentials=true,ServiceUrl="http://127.0.0.1:1"},null!);
            using var client=Client(provider);
            var credentials=(AWSCredentials)typeof(AmazonServiceClient).GetProperty("Credentials",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(client)!;
            Assert.AreEqual("owned-env-access",credentials.GetCredentials().AccessKey);
            Assert.AreEqual("owned-env-token",credentials.GetCredentials().Token);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally {foreach(var entry in previous) Environment.SetEnvironmentVariable(entry.Key,entry.Value);FallbackCredentialsFactory.Reset();}
    }

    [TestMethod]
    public void MixedDefaultAndExplicitCredentialsCannotSilentlyChooseOne()
        => Assert.ThrowsException<ArgumentException>(()=>CloudProviderFactory.CreateAws(new() {Region="us-east-1",
            UseDefaultCredentials=true,AccessKey="owned-explicit",SecretKey="owned-explicit",ServiceUrl="http://127.0.0.1:1"},null!));

    [TestMethod]
    public void ProductionFactoryUsesDnaRegionAndOverrideWithoutActivation()
    {
        var settings=new AWSOASISProviderSettings{Region="eu-west-1",AccessKey="owned-local",SecretKey="owned-local",ServiceUrl="http://127.0.0.1:1"};
        var restored=JsonConvert.DeserializeObject<AWSOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=CloudProviderFactory.CreateAws(restored,"http://127.0.0.1:18000");
        Assert.IsFalse(provider.IsProviderActivated);
        var client=(AmazonDynamoDBClient)typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        try {Assert.AreEqual("http://127.0.0.1:18000/",new Uri(client.Config.ServiceURL).AbsoluteUri);Assert.AreEqual("eu-west-1",client.Config.AuthenticationRegion);}
        finally {client.Dispose();}
    }

    [DataTestMethod]
    [DataRow("region")] [DataRow("access")] [DataRow("secret")]
    public void MissingExplicitConfigurationCannotProvisionOrGuessCredentials(string missing)
        =>Assert.ThrowsException<ArgumentException>(()=>CloudProviderFactory.CreateAws(new(){Region=missing=="region" ? "":"us-east-1",
            AccessKey=missing=="access" ? "":"owned-local",SecretKey=missing=="secret" ? "":"owned-local"},null!));
}
