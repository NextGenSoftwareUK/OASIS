using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.FastlyOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class FastlyQueryTransportTests
{
    private const string Key = "oasis/holon/aaaaaaaa222242228222222222222222";
    private const string Store = "teachingStore";

    [TestMethod]
    public async Task QueryTransportUsesOneExactNativeKeyAndRejectsOtherOperations()
    {
        using var service = new FakeService((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("api.fastly.com", request.RequestUri.Host);
            Assert.AreEqual($"/resources/stores/kv/{Store}/keys/{Uri.EscapeDataString(Key)}", request.RequestUri.AbsolutePath);
            Assert.AreEqual("fixture-token", request.Headers.GetValues("Fastly-Key").Single());
            return FakeService.Text("fixture-value");
        });
        using var backend = new FastlyKvBackend("fixture-token", Store, service, queryOnly: true);
        Assert.AreEqual("fixture-value", await backend.GetAsync(Key));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.GetAsync("oasis/avatar/foreign"));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => backend.PutAsync(Key, "changed"));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => backend.DeleteAsync(Key));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => backend.ListKeysAsync("oasis/"));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => backend.VerifyAsync());
        Assert.AreEqual(1, service.Requests.Count);
    }

    [TestMethod]
    public async Task MissingAndDeniedNativeReadsHaveDifferentOutcomes()
    {
        var status = HttpStatusCode.NotFound;
        using var service = new FakeService((_, _) => FakeService.Status(status));
        using var backend = new FastlyKvBackend("fixture-token", Store, service, queryOnly: true);
        Assert.IsNull(await backend.GetAsync(Key));
        status = HttpStatusCode.Forbidden;
        var denied = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => backend.GetAsync(Key));
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [TestMethod]
    public async Task OversizedNativeItemFailsBeforeMaterialization()
    {
        using var service = new FakeService((_, _) => FakeService.Text(new string(' ', 1024 * 1024 + 1)));
        using var backend = new FastlyKvBackend("fixture-token", Store, service, queryOnly: true);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.GetAsync(Key));
    }
}
