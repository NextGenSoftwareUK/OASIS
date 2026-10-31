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
    [DataTestMethod][DataRow("dynamo")][DataRow("aws")][DataRow("s3")]
    public void StorageProvidersAreDiscoverableAsCloudWithoutActivation(string kind)
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider provider;
        IDisposable client;
        if(kind=="s3"){
            var native=new Amazon.S3.AmazonS3Client(new BasicAWSCredentials("owned","owned"),new Amazon.S3.AmazonS3Config{ServiceURL="http://127.0.0.1:1",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0});client=native;
            provider=new NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS(native,"synthetic-bucket",()=>new byte[32]);
        }else{
            var nativeProvider=kind=="aws"?(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS)new AWSOASIS("us-east-1","owned","owned","http://127.0.0.1:1"):new NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:1");
            provider=nativeProvider;client=(AmazonDynamoDBClient)typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        }
        try{
            var manager=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,new OASISDNA());Assert.IsTrue(manager.RegisterProvider(provider));
            Assert.AreSame(provider,manager.GetStorageProviders().Single());Assert.AreSame(provider,manager.GetCloudProviders().Single());
            Assert.IsTrue(manager.IsProviderCloud(provider.ProviderType.Value));Assert.IsFalse(provider.IsProviderActivated);
        }finally{(provider as IDisposable)?.Dispose();client.Dispose();}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void HistoryOptionRoundTripsThroughDnaAndProductionFactory(bool enabled)
    {
        var settings=new AWSOASISProviderSettings{Region="us-east-1",AccessKey="owned",SecretKey="owned",ServiceUrl="http://127.0.0.1:1",IsVersionControlEnabled=enabled};
        var restored=JsonConvert.DeserializeObject<AWSOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=CloudProviderFactory.CreateAws(restored,null!);using var client=Client(provider);
        Assert.AreEqual(enabled,provider.IsVersionControlEnabled);Assert.AreEqual(0,client.Config.MaxErrorRetry);
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow("Amazon.SecurityToken.AmazonSecurityTokenServiceClient, AWSSDK.SecurityToken")]
    [DataRow("Amazon.SSO.AmazonSSOClient, AWSSDK.SSO")]
    [DataRow("Amazon.SSOOIDC.AmazonSSOOIDCClient, AWSSDK.SSOOIDC")]
    public void OptionalCredentialSdkClientsAreAvailableWithoutNetwork(string typeName)
    {
        var type=Type.GetType(typeName,throwOnError:false);
        Assert.IsNotNull(type,"Configured credential paths must not fail because their official SDK assembly is absent.");
        Assert.IsTrue(typeof(AmazonServiceClient).IsAssignableFrom(type));
        var constructor=type.GetConstructor(new[] {typeof(AWSCredentials),typeof(Amazon.RegionEndpoint)});
        Assert.IsNotNull(constructor);
        using var client=(AmazonServiceClient)constructor.Invoke(new object[] {new BasicAWSCredentials("OwnedSdkOnly","owned-synthetic-secret"),Amazon.RegionEndpoint.USEast1});
        Assert.IsNotNull(client.Config);
    }

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
