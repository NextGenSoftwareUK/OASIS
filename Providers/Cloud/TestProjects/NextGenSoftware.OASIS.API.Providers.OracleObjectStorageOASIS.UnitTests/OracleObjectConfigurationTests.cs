using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

[TestClass][DoNotParallelize]
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

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RealProfileFactoryConstructsWithoutActivationOrReplacementKeys(bool history)
    {
        const string variable="OASIS_OWNED_ORACLE_FACTORY_TEST_KEY";
        var previous=Environment.GetEnvironmentVariable(variable);
        var directory=Path.Combine(AppContext.BaseDirectory,"TestResults","OracleFactoryProfile");
        Directory.CreateDirectory(directory);
        var config=Path.Combine(directory,"config");var signingKey=Path.Combine(directory,"synthetic-key.pem");
        Assert.IsFalse(File.Exists(config));Assert.IsFalse(File.Exists(signingKey));
        using var rsa=RSA.Create(2048);
        var pem=rsa.ExportRSAPrivateKeyPem();
        var profile="[client]\nuser=ocid1.user.oc1..synthetic\ntenancy=ocid1.tenancy.oc1..synthetic\nfingerprint=aa:bb:cc\nregion=us-ashburn-1\nkey_file="+signingKey.Replace('\\','/')+"\n";
        try {
            await File.WriteAllTextAsync(signingKey,pem);await File.WriteAllTextAsync(config,profile);
            Environment.SetEnvironmentVariable(variable,null);
            var settings=Settings();settings.ConfigFilePath=config;settings.StorageEncryptionKeyEnvironmentVariable=variable;
            settings.IsVersionControlEnabled=history;
            using var provider=OracleObjectStorageProviderFactory.Create(settings);
            Assert.IsFalse(provider.IsProviderActivated);Assert.AreEqual(history,provider.IsVersionControlEnabled);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleObjectStorageOASIS,provider.ProviderType.Value);
            // Construction retains the configured variable name, not the caller's mutable settings object.
            settings.StorageEncryptionKeyEnvironmentVariable="invalid\0variable";
            var activated=await provider.ActivateProviderAsync();
            Assert.IsTrue(activated.IsError);Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsInstanceOfType<InvalidOperationException>(activated.Exception);
            StringAssert.Contains(activated.Exception.Message,"configured separately from the signing key");
            Assert.IsNull(Environment.GetEnvironmentVariable(variable));
            Assert.AreEqual(pem,await File.ReadAllTextAsync(signingKey));Assert.AreEqual(profile,await File.ReadAllTextAsync(config));
            provider.Dispose();Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        }finally{
            Environment.SetEnvironmentVariable(variable,previous);
            // Only exact owned synthetic fixtures; never an existing user's OCI profile or key.
            File.Delete(config);File.Delete(signingKey);
        }
    }
}
