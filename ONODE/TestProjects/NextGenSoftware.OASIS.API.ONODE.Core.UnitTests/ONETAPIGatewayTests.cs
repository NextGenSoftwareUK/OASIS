using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for ONETAPIGateway: GenerateCacheKey threw NRE on a null parameters object (every
    /// call site passes it as nullable), and SelectEndpointAsync accepted an `endpoint` filter string but
    /// never actually consulted it, always falling through to plain round-robin regardless of which
    /// endpoint was requested.
    /// </summary>
    public class ONETAPIGatewayTests
    {
        private static void SetApiBridges(ONETAPIGateway gateway, Dictionary<string, APIBridge> bridges)
        {
            var field = typeof(ONETAPIGateway).GetField("_apiBridges", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(gateway, bridges);
        }

        private static string GenerateCacheKey(ONETAPIGateway gateway, string endpoint, object parameters, string networkType)
        {
            var method = typeof(ONETAPIGateway).GetMethod("GenerateCacheKey", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method!.Invoke(gateway, new object[] { endpoint, parameters, networkType })!;
        }

        [Fact]
        public void GenerateCacheKey_NullParameters_DoesNotThrow()
        {
            var gateway = new ONETAPIGateway(storageProvider: null);

            System.Action act = () => GenerateCacheKey(gateway, "getNodes", null, "web2");

            act.Should().NotThrow();
            GenerateCacheKey(gateway, "getNodes", null, "web2").Should().Contain("getNodes").And.Contain("web2");
        }

        [Fact]
        public async Task SelectEndpointAsync_EndpointMatchesOneBridge_PrefersTheMatchingBridgeOverRoundRobin()
        {
            var gateway = new ONETAPIGateway(storageProvider: null);

            var nftBridge = new APIBridge { Id = "nft-bridge", Name = "NFT Bridge", Status = "Active", IsActive = true, Endpoints = new List<string> { "https://api.example.com/nft" } };
            var walletBridge = new APIBridge { Id = "wallet-bridge", Name = "Wallet Bridge", Status = "Active", IsActive = true, Endpoints = new List<string> { "https://api.example.com/wallet" } };

            SetApiBridges(gateway, new Dictionary<string, APIBridge> { ["nft"] = nftBridge, ["wallet"] = walletBridge });

            var selected = await gateway.SelectEndpointAsync(bridge: null, endpoint: "nft");

            selected.BridgeId.Should().Be("nft-bridge", "the endpoint filter should select the bridge that actually advertises a matching endpoint, not whichever round-robin lands on");
            selected.Url.Should().Be("https://api.example.com/nft");
        }

        [Fact]
        public async Task SelectEndpointAsync_NoEndpointFilter_FallsBackToRoundRobinAcrossAllActiveBridges()
        {
            var gateway = new ONETAPIGateway(storageProvider: null);

            var bridgeA = new APIBridge { Id = "a", Name = "A", Status = "Active", IsActive = true, Endpoints = new List<string> { "https://api.example.com/a" } };
            SetApiBridges(gateway, new Dictionary<string, APIBridge> { ["a"] = bridgeA });

            var selected = await gateway.SelectEndpointAsync(bridge: null, endpoint: null);

            selected.BridgeId.Should().Be("a");
        }
    }

    /// <summary>
    /// Regression tests for the real RateLimiter backing ONETAPIGateway.CallUnifiedAPIAsync. Previously
    /// rate limiting was pure decoration - a dozen InitializeRateLimiting* methods logged policy/algorithm
    /// labels but no limiter object existed anywhere, so no request was ever actually throttled.
    /// </summary>
    public class RateLimiterTests
    {
        [Fact]
        public void TryAcquire_UnderLimit_AllowsRequests()
        {
            var limiter = new RateLimiter();

            for (int i = 0; i < 5; i++)
                limiter.TryAcquire("endpoint-a", maxRequests: 5, window: TimeSpan.FromMinutes(1)).Should().BeTrue();
        }

        [Fact]
        public void TryAcquire_AtLimit_RejectsFurtherRequests()
        {
            var limiter = new RateLimiter();

            for (int i = 0; i < 3; i++)
                limiter.TryAcquire("endpoint-b", maxRequests: 3, window: TimeSpan.FromMinutes(1)).Should().BeTrue();

            limiter.TryAcquire("endpoint-b", maxRequests: 3, window: TimeSpan.FromMinutes(1)).Should().BeFalse("the 4th request within the window exceeds the limit of 3");
        }

        [Fact]
        public void TryAcquire_DifferentKeys_AreLimitedIndependently()
        {
            var limiter = new RateLimiter();

            limiter.TryAcquire("endpoint-c", maxRequests: 1, window: TimeSpan.FromMinutes(1)).Should().BeTrue();
            limiter.TryAcquire("endpoint-c", maxRequests: 1, window: TimeSpan.FromMinutes(1)).Should().BeFalse();

            // A different key must not be affected by endpoint-c's exhausted limit.
            limiter.TryAcquire("endpoint-d", maxRequests: 1, window: TimeSpan.FromMinutes(1)).Should().BeTrue();
        }

        [Fact]
        public void TryAcquire_AfterWindowExpires_AllowsRequestsAgain()
        {
            var limiter = new RateLimiter();
            var tinyWindow = TimeSpan.FromMilliseconds(50);

            limiter.TryAcquire("endpoint-e", maxRequests: 1, window: tinyWindow).Should().BeTrue();
            limiter.TryAcquire("endpoint-e", maxRequests: 1, window: tinyWindow).Should().BeFalse();

            System.Threading.Thread.Sleep(100);

            limiter.TryAcquire("endpoint-e", maxRequests: 1, window: tinyWindow).Should().BeTrue("the old timestamp should have aged out of the window");
        }
    }

    /// <summary>
    /// Regression test for ONETAPIGateway's routing tree. BuildRoutingTreeAsync previously had nothing to
    /// do with routing at all - it logged a hardcoded ["v1","v2","v3","latest"] version list, a copy-paste
    /// artifact unrelated to APIRoute. It now builds a real NetworkType -> ordered route-key index.
    /// </summary>
    public class APIRouterRoutingTreeTests
    {
        [Fact]
        public async Task InitializeAsync_BuildsRealRoutingTreeGroupedByNetworkTypeOrderedByPriority()
        {
            var router = new APIRouter();
            var routes = new Dictionary<string, APIRoute>
            {
                ["web2-low"] = new APIRoute { NetworkType = "web2", Priority = 1 },
                ["web2-high"] = new APIRoute { NetworkType = "web2", Priority = 10 },
                ["web3-only"] = new APIRoute { NetworkType = "web3", Priority = 5 }
            };

            await router.InitializeAsync(routes);

            var web2Routes = router.GetRoutesForNetworkType("web2");
            web2Routes.Should().Equal("web2-high", "web2-low");

            router.GetRoutesForNetworkType("web3").Should().Equal("web3-only");
            router.GetRoutesForNetworkType("nonexistent").Should().BeEmpty();
        }
    }
}
