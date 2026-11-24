using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DatabricksOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DatabricksOASIS.UnitTests
{
    [TestClass]
    public class DatabricksOASISProviderTests
    {
        private DatabricksOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new DatabricksOASIS("https://dbc-abc.cloud.databricks.com", "TOKEN", "warehouse-id");

        [TestMethod]
        public void ProviderType_ShouldBeDatabricksOASIS()
            => Assert.AreEqual(ProviderType.DatabricksOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeDatabricksOASIS()
            => Assert.AreEqual("DatabricksOASIS", _provider.ProviderName);

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [DataTestMethod]
        [DataRow("", false)]
        [DataRow(",\"next_chunk_index\":1", true)]
        [DataRow(",\"external_links\":[]", true)]
        public void InlineResultRequiresTheCompletePayload(string extra, bool rejected)
        {
            using var response = System.Text.Json.JsonDocument.Parse(
                "{\"status\":{\"state\":\"SUCCEEDED\"},\"manifest\":{\"total_row_count\":1,\"truncated\":false}," +
                "\"result\":{\"data_array\":[[\"key\",\"{}\"]]" + extra + "}}");
            if (rejected)
                Assert.ThrowsException<System.IO.InvalidDataException>(() =>
                    DatabricksQuerySource.ReadInlineResult(response.RootElement, "key"));
            else
                Assert.AreEqual("{}", DatabricksQuerySource.ReadInlineResult(response.RootElement, "key"));
        }

        [DataTestMethod]
        [DataRow("SUCCEEDED", 0, "[]", false, false)]
        [DataRow("SUCCEEDED", 1, "[]", false, true)]
        [DataRow("SUCCEEDED", 2, "[[\"key\",\"{}\"],[\"key\",\"{}\"]]", false, true)]
        [DataRow("CANCELED", 0, "[]", false, true)]
        [DataRow("SUCCEEDED", 0, "[]", true, true)]
        public void InlineResultRequiresSuccessfulUntruncatedManifest(
            string state, int count, string rows, bool truncated, bool rejected)
        {
            using var data = System.Text.Json.JsonDocument.Parse(rows);
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = new { state },
                manifest = new { total_row_count = count, truncated },
                result = new { data_array = data.RootElement }
            });
            using var response = System.Text.Json.JsonDocument.Parse(payload);
            if (rejected)
                Assert.ThrowsException<System.IO.InvalidDataException>(() =>
                    DatabricksQuerySource.ReadInlineResult(response.RootElement, "key"));
            else
                Assert.IsNull(DatabricksQuerySource.ReadInlineResult(response.RootElement, "key"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
