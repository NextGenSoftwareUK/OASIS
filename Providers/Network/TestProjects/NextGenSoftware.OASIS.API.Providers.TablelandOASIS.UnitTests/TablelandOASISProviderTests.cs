using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TablelandOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class TablelandOASISProviderTests
    {
        private TablelandOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = TablelandOASISTestFactory.Create();

        [TestMethod]
        public void ProviderType_ShouldBeSet()
            => Assert.AreNotEqual(ProviderType.None, _provider.ProviderType.Value);

        [TestMethod]
        public void ProviderName_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderName));

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderCategory_ShouldBeSet()
            => Assert.IsNotNull(_provider.ProviderCategory);

        [DataTestMethod]
        [DataRow("200 OK", true)]
        [DataRow("302 Found", false)]
        public async Task ActivationUsesOnlyConfiguredGateway(string status, bool expectedActive)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var gateway = new TcpListener(IPAddress.Loopback, 0);
            gateway.Start();
            var address = $"http://{gateway.LocalEndpoint}";
            var provider = new TablelandOASIS(address);
            var activation = provider.ActivateProviderAsync();

            using (var connection = await gateway.AcceptTcpClientAsync(deadline.Token))
            using (var stream = connection.GetStream())
            using (var request = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                Assert.AreEqual("GET /api/v1/health HTTP/1.1", await request.ReadLineAsync(deadline.Token));
                while (await request.ReadLineAsync(deadline.Token) is { Length: > 0 }) { }
                await stream.WriteAsync(Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 {status}\r\nLocation: {address}/redirected\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"),
                    deadline.Token);
            }

            var result = await activation.WaitAsync(deadline.Token);
            Assert.AreEqual(expectedActive, result.Result, result.Message);
            Assert.AreEqual(!expectedActive, result.IsError, result.Message);
            Assert.AreEqual(expectedActive, provider.IsProviderActivated);
            Assert.IsFalse(gateway.Pending(), "The gateway redirect must not be followed.");

            await provider.DeActivateProviderAsync();
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
