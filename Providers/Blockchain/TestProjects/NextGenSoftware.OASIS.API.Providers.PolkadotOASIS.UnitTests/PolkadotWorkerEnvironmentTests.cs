using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class PolkadotWorkerEnvironmentTests
{
    [DataTestMethod]
    [DataRow("balance")]
    [DataRow("transactionStatus")]
    [DataRow("prepareRecordMutation")]
    public async Task SdkChildDoesNotReceiveParentStorageEncryptionKey(string operation)
    {
        const string variable = "OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY";
        var previousKey = Environment.GetEnvironmentVariable(variable);
        var previousBridge = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        try
        {
            Environment.SetEnvironmentVariable(variable, "synthetic-parent-only-key");
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "worker-environment-fixture.mjs"));
            using var provider = new PolkadotOASIS("http://127.0.0.1:1");
            var method = typeof(PolkadotOASIS).GetMethod("InvokePolkadotSdkAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pending = (Task<JsonElement>)method.Invoke(provider, new object?[] { new { operation }, CancellationToken.None, null })!;
            var response = await pending;
            Assert.IsFalse(response.GetProperty("storageKeyPresent").GetBoolean(), "The SDK worker must not inherit the parent's record encryption key.");
            Assert.AreEqual("synthetic-parent-only-key", Environment.GetEnvironmentVariable(variable), "Child isolation must not mutate parent configuration.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previousKey);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", previousBridge);
        }
    }
}
