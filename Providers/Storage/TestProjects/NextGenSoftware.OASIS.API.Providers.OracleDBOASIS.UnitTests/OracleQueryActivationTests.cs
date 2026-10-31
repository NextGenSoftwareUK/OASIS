using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleQueryActivationTests
{
    private sealed class Source : OracleDBOASIS
    {
        public int ProvisionAttempts;
        public Source() : base("not an Oracle connection string") { }
        public void MarkActive() => IsProviderActivated = true;
        protected override Task ProvisionStorageSchemaAsync(OracleConnection connection)
        {
            ProvisionAttempts++;
            throw new InvalidOperationException("Read-only activation must never provision.");
        }
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CancellationAndInvalidSourceNeverProvisionOrLeaveStaleActivation(bool cancel)
    {
        var source = new Source();
        source.MarkActive();
        using var cancellation = new CancellationTokenSource();
        if (cancel) cancellation.Cancel();

        var result = await ((ICosmicReadOnlyQueryActivation)source).ActivateQuerySourceAsync(cancellation.Token);

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.AreEqual(cancel ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(0, source.ProvisionAttempts);
        Assert.IsNotNull(result.Exception);
    }
}
