using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public class ArweaveServiceReadTests
{
    private static ArweaveService Service() => new("", "http://127.0.0.1:1", "node",
        Path.Combine(AppContext.BaseDirectory, "read-boundary.cjs"));

    [TestMethod]
    public async Task GatewayFailureIsNotARecordMiss()
        => await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Service().GetTransactionDataAsync("failed"));

    [TestMethod]
    public async Task MalformedPayloadIsNotARecordMiss()
        => await Assert.ThrowsExceptionAsync<FormatException>(() => Service().GetTransactionDataAsync("malformed"));

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task MissingIdentityRejectsBeforeDispatch(string? id)
        => await Assert.ThrowsExceptionAsync<ArgumentException>(() => Service().GetTransactionDataAsync(id!));

    [TestMethod]
    public async Task AcknowledgedBytesRemainExact()
        => CollectionAssert.AreEqual(new byte[]{0, 1, 255}, await Service().GetTransactionDataAsync("valid"));
}
