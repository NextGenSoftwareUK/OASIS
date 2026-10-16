using System.Reflection;
using Amazon.DynamoDBv2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
public sealed class AwsBootConfigurationTests
{
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
