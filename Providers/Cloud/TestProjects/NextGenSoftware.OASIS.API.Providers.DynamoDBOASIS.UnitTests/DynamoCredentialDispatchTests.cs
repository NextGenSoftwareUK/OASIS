using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoCredentialDispatchTests
{
    private sealed class NoDispatchClient():AmazonDynamoDBClient(new BasicAWSCredentials("synthetic","synthetic"),Amazon.RegionEndpoint.USEast1)
    {
        internal int Scans;
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
        {Scans++;throw new InvalidOperationException("Historical credentials must reject before native dispatch.");}
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InterfaceWalletCredentialHistoryRejectsBeforeSdkScan(bool privateKey,bool synchronous)
    {
        var provider=new DynamoDBOASIS("synthetic","synthetic","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        using var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new NoDispatchClient();field.SetValue(provider,client);
        IOASISStorageProvider storage=provider;
        var result=synchronous
            ?privateKey?storage.LoadAvatarByPrivateKey("synthetic-selector",1):storage.LoadAvatarByPublicKey("synthetic-selector",1)
            :privateKey?await storage.LoadAvatarByPrivateKeyAsync("synthetic-selector",1):await storage.LoadAvatarByPublicKeyAsync("synthetic-selector",1);
        Assert.IsTrue(result.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));
        Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.AreEqual(0,client.Scans);
    }
    [DataTestMethod]
    [DataRow("LoadAvatarByPublicKey")]
    [DataRow("LoadAvatarByPublicKeyAsync")]
    [DataRow("LoadAvatarByPrivateKey")]
    [DataRow("LoadAvatarByPrivateKeyAsync")]
    public void StorageInterfaceMustDispatchWalletCredentialsToCurrentAuthoritySelector(string name)
    {
        var map=typeof(DynamoDBOASIS).GetInterfaceMap(typeof(IOASISStorageProvider));
        var index=Array.FindIndex(map.InterfaceMethods,method=>method.Name==name);
        Assert.IsTrue(index>=0);
        Assert.AreEqual(typeof(DynamoDBOASIS),map.TargetMethods[index].DeclaringType,
            "Wallet credentials must use the provider's current-authority selector, not inherited default history-capable selection.");
    }
}
