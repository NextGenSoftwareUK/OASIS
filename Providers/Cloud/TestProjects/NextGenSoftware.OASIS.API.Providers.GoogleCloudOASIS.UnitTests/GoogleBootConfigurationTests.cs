using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleBootConfigurationTests
{
    [TestMethod]
    public void ProductionFactoryPreservesServiceSettingsWithoutSdkActivation()
    {
        var settings=new GoogleCloudOASISProviderSettings{ProjectId="owned-project",BucketName="owned-bucket",CredentialsPath="unused-owned-credentials.json",
            FirestoreDatabaseId="owned-db",BigQueryDatasetId="owned-data",EnableStorage=false,EnableFirestore=true,EnableBigQuery=false};
        var restored=JsonConvert.DeserializeObject<GoogleCloudOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=CloudProviderFactory.CreateGoogle(restored);
        object? Field(string name)=>typeof(GoogleCloudOASIS).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider);
        Assert.IsFalse(provider.IsProviderActivated);Assert.AreEqual(settings.ProjectId,Field("_projectId"));Assert.AreEqual(settings.BucketName,Field("_bucketName"));
        Assert.AreEqual(settings.CredentialsPath,Field("_credentialsPath"));Assert.AreEqual(settings.FirestoreDatabaseId,Field("_firestoreDatabaseId"));Assert.AreEqual(settings.BigQueryDatasetId,Field("_bigQueryDatasetId"));
        Assert.AreEqual(false,Field("_enableStorage"));Assert.AreEqual(true,Field("_enableFirestore"));Assert.AreEqual(false,Field("_enableBigQuery"));
        Assert.IsNull(Field("_storageClient"));Assert.IsNull(Field("_firestoreDb"));Assert.IsNull(Field("_bigQueryClient"));
    }

    [TestMethod]
    public void MissingDnaSectionCannotSilentlyConstructDefaultCloudAccount()
        =>Assert.ThrowsException<ArgumentNullException>(()=>CloudProviderFactory.CreateGoogle(null!));
}
