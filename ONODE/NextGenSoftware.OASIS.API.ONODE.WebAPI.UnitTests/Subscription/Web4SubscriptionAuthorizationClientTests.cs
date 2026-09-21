using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
            public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _handler(request);
        }
    }
}
