using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Source=NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.GoogleCloudPublicMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GooglePublicProfileConfigurationTests
{
    private static string Valid()=>JsonSerializer.Serialize(new{schema=1,bucket="public-test-bucket",keys=new[]{"one"},owner=Guid.NewGuid()});

    [DataTestMethod]
    [DataRow("credential")][DataRow("billing")][DataRow("endpoint")][DataRow("duplicate")]
    [DataRow("escaped-duplicate")][DataRow("schema")][DataRow("array")][DataRow("key-type")][DataRow("owner")][DataRow("oversize")]
    public void InvalidOrCredentialBearingConfigurationCannotCreateAProfile(string scenario)
    {
        var json=Valid();
        json=scenario switch {
            "credential"=>json[..^1]+",\"credentials\":\"not-a-secret\"}",
            "billing"=>json[..^1]+",\"userProject\":\"billing-project\"}",
            "endpoint"=>json[..^1]+",\"endpoint\":\"https://unexpected.invalid\"}",
            "duplicate"=>json[..^1]+",\"schema\":1}",
            "escaped-duplicate"=>json[..^1]+",\"\\u0073chema\":1}",
            "schema"=>json.Replace("\"schema\":1","\"schema\":2"),
            "array"=>"[]",
            "key-type"=>json.Replace("[\"one\"]","[1]"),
            "owner"=>json.Replace("\"owner\":\"","\"owner\":\"not-a-guid-"),
            _=>new string('é',5000)};
        Assert.ThrowsException<ArgumentException>(()=>Source.FromJson(json));
    }

    [TestMethod]
    public void ValidConfigurationCreatesOnlyAnInactiveAnonymousProfile()
    {
        using var source=Source.FromJson(Valid());
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.GoogleCloudOASIS,source.QueryProviderType);
        var constructor=source.GetType().GetConstructors().Single();
        Assert.AreEqual(3,constructor.GetParameters().Length);
        Assert.IsFalse(constructor.GetParameters().Any(parameter=>parameter.ParameterType==typeof(Google.Cloud.Storage.V1.StorageClient)));
    }
}
