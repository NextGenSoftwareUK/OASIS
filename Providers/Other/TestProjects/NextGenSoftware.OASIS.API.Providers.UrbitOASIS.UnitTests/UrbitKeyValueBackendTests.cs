using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests
{
    /// <summary>Offline checks of the Eyre key encoding and configuration errors. The storage contract itself
    /// runs against a real fake ship in UrbitNativeStorageConformanceTests.</summary>
    [TestClass]
    public class UrbitKeyValueBackendTests
    {
        [TestMethod]
        public void Hex_IsLowercaseUtf8_ThatTheAgentDecodes()
        {
            Assert.AreEqual("6f617369732f412f31", UrbitKeyValueBackend.Hex("oasis/A/1"));
            Assert.AreEqual("68c3a9", UrbitKeyValueBackend.Hex("hé"));
        }

        [TestMethod]
        public void Store_WithSlash_IsRejected()
            => Assert.ThrowsException<ArgumentException>(() => new UrbitOASIS("http://127.0.0.1:1", "code", "a/b"));

        [TestMethod]
        public async Task Activate_WithoutLoginCode_FailsWithReason()
        {
            var provider = new UrbitOASIS("http://127.0.0.1:1");
            var result = await provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "+code");
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
