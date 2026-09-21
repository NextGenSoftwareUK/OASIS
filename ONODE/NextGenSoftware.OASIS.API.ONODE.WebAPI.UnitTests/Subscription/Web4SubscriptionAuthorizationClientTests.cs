using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription
{
    public class Web4SubscriptionAuthorizationClientTests
    {
        [Fact]
        public async Task AuthorizeRequest_ForwardsBearerTokenAndConsumerToWeb4()
        {
            HttpRequestMessage? captured = null;
            string? body = null;
            var handler = new StubHandler(async request =>
            {
                captured = request;
                body = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"allowed\":true,\"statusCode\":200,\"planId\":\"enterprise\",\"limit\":-1,\"remaining\":-1}")
                };
            });
            var client = new Web4SubscriptionAuthorizationClient(new HttpClient(handler), "https://web4.example/");

            var result = await client.AuthorizeRequestAsync("jwt-value", "WEB8");

            captured!.RequestUri!.ToString().Should().Be("https://web4.example/api/subscription/authorize-request");
            captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
            captured.Headers.Authorization.Parameter.Should().Be("jwt-value");
            body.Should().Contain("WEB8");
            result.Allowed.Should().BeTrue();
            result.PlanId.Should().Be("enterprise");
            result.Limit.Should().Be(-1);
        }

        [Fact]
        public async Task AuthorizeRequest_Web4Failure_IsSurfaced()
        {
            var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("authority unavailable")
            }));
            var client = new Web4SubscriptionAuthorizationClient(new HttpClient(handler), "https://web4.example");

            Func<Task> action = () => client.AuthorizeRequestAsync("jwt-value", "WEB10");

            await action.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task UsageAuthorizeAndSettle_UseOperationIdAndBearer()
        {
            var requests = new System.Collections.Generic.List<(string Path, string Body, string Bearer)>();
            var handler = new StubHandler(async request =>
            {
                requests.Add((request.RequestUri!.AbsolutePath, await request.Content!.ReadAsStringAsync(), request.Headers.Authorization!.Parameter!));
                string json = request.RequestUri.AbsolutePath.EndsWith("authorize")
                    ? "{\"allowed\":true,\"statusCode\":200,\"operationId\":\"11111111-1111-1111-1111-111111111111\"}"
                    : "{\"settled\":true,\"alreadySettled\":false,\"operationId\":\"11111111-1111-1111-1111-111111111111\"}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            });
            var client = new Web4SubscriptionAuthorizationClient(new HttpClient(handler), "https://web4.example");
            const string op = "11111111-1111-1111-1111-111111111111";

            var auth = await client.AuthorizeUsageAsync("jwt", new UsageAuthorizationRequest { OperationId=op, ConsumingService="WEB6", Endpoint="POST /v1/complete" });
            var settle = await client.SettleUsageAsync("jwt", new UsageSettlementRequest { OperationId=op, Outcome="succeeded", PromptTokens=10, CompletionTokens=5 });

            auth.OperationId.Should().Be(op);
            settle.Settled.Should().BeTrue();
            requests.Should().OnlyContain(x => x.Bearer == "jwt" && x.Body.Contains(op));
            requests.Select(x => x.Path).Should().Equal("/api/subscription/usage/authorize", "/api/subscription/usage/settle");
        }

        [Fact]
        public async Task GetUsage_DoesNotSendCallerPlanOrKarma()
        {
            HttpRequestMessage? captured = null;
            var client = new Web4SubscriptionAuthorizationClient(new HttpClient(new StubHandler(request =>
            {
                captured = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"planId\":\"gold\",\"karma\":123}") });
            })), "https://web4.example");
            var result = await client.GetUsageAsync("jwt");
            captured!.Method.Should().Be(HttpMethod.Get);
            captured.RequestUri!.PathAndQuery.Should().Be("/api/subscription/usage/current");
            result.PlanId.Should().Be("gold");
            result.Karma.Should().Be(123);
        }

        [Fact]
        public async Task AuthorizeUsage_PolicyDenialIsReturnedAsDecision()
        {
            var client = new Web4SubscriptionAuthorizationClient(new HttpClient(new StubHandler(_ =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("{\"allowed\":false,\"statusCode\":429,\"code\":\"DAILY_CALL_LIMIT_EXCEEDED\",\"message\":\"limit\"}")
                }))), "https://web4.example");

            var result = await client.AuthorizeUsageAsync("jwt", new UsageAuthorizationRequest
            {
                OperationId=Guid.NewGuid().ToString(), ConsumingService="WEB6", Endpoint="POST /v1/complete", MeterCategory="completion"
            });

            result.Allowed.Should().BeFalse();
            result.StatusCode.Should().Be(429);
            result.Code.Should().Be("DAILY_CALL_LIMIT_EXCEEDED");
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
            public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _handler(request);
        }
    }
}
