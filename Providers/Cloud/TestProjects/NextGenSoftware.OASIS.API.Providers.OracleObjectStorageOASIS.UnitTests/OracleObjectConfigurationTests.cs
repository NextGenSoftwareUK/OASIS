using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

[TestClass]
public class OracleObjectConfigurationTests
{
    private static OracleObjectStorageOASISSettings Settings()=>new(){ConfigFilePath="must-not-be-read.ini",ProfileName="client",RegionId="us-ashburn-1",
        NamespaceName="namespace",BucketName="bucket",KeyPrefix="client/",StorageEncryptionKeyEnvironmentVariable="OWNED_TEST_KEY",IsVersionControlEnabled=true};

    [DataTestMethod][DataRow("ConfigFilePath")][DataRow("ProfileName")][DataRow("RegionId")][DataRow("NamespaceName")]
    [DataRow("BucketName")][DataRow("KeyPrefix")][DataRow("StorageEncryptionKeyEnvironmentVariable")]
    public void MissingSettingIsRejectedBeforeSdkProfileAccess(string property)
    {
        var settings=Settings();typeof(OracleObjectStorageOASISSettings).GetProperty(property)!.SetValue(settings,"");
        Assert.ThrowsException<ArgumentException>(()=>OracleObjectStorageProviderFactory.Create(settings));
    }

    [TestMethod]
    public void SqlOverrideCannotSilentlyChangeObjectAuthentication()
        =>Assert.ThrowsException<ArgumentException>(()=>OracleObjectStorageProviderFactory.Create(Settings(),"not-an-object-profile"));

    [TestMethod]
    public void DnaRoundtripRetainsIndependentOracleServiceSettings()
    {
        var original=new StorageProviderSettings{OracleDBOASIS=new(){ConnectionString="synthetic-db"},OracleObjectStorageOASIS=Settings()};
        var copy=JsonSerializer.Deserialize<StorageProviderSettings>(JsonSerializer.Serialize(original))!;
        Assert.AreEqual("synthetic-db",copy.OracleDBOASIS.ConnectionString);
        Assert.AreEqual("client",copy.OracleObjectStorageOASIS.ProfileName);Assert.AreEqual("bucket",copy.OracleObjectStorageOASIS.BucketName);
        Assert.AreEqual("client/",copy.OracleObjectStorageOASIS.KeyPrefix);Assert.IsTrue(copy.OracleObjectStorageOASIS.IsVersionControlEnabled);
        Assert.AreEqual("OWNED_TEST_KEY",copy.OracleObjectStorageOASIS.StorageEncryptionKeyEnvironmentVariable);
    }
}
