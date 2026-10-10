using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Security;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests
{
    public class ApiSurfaceAuthorizationTests
    {
        private static HttpContext ContextFor(AvatarType? type)
        {
            var http = new DefaultHttpContext();
            if (type != null)
                http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { AvatarType = new EnumValue<AvatarType>(type.Value) };
            return http;
        }

        public class TestQuery
        {
            public string Secret() => "secret";
            public string Authenticate() => "token";
        }

        private static async Task<IExecutionResult> ExecuteAsync(string query, AvatarType? caller)
        {
            var accessor = new HttpContextAccessor { HttpContext = ContextFor(caller) };
            var executor = await new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(accessor)
                .AddGraphQL()
                .AddQueryType<TestQuery>()
                .UseField<OASISGraphQLAuthorizationMiddleware>()
                .BuildRequestExecutorAsync();
            return await executor.ExecuteAsync(query);
        }

        [Theory]
        [InlineData(null, "AUTH_NOT_AUTHENTICATED")]
        [InlineData(AvatarType.User, "AUTH_NOT_AUTHORIZED")]
        public async Task GraphQL_ProtectedField_NonWizard_IsRejected(AvatarType? caller, string code)
        {
            var result = (IOperationResult)await ExecuteAsync("{ secret }", caller);

            result.Errors.Should().ContainSingle(e => e.Code == code);
            result.ToJson().Should().NotContain("\"secret\":\"secret\"");
        }

        [Fact]
        public async Task GraphQL_ProtectedField_Wizard_IsAllowed()
        {
            var result = (IOperationResult)await ExecuteAsync("{ secret }", AvatarType.Wizard);

            result.Errors.Should().BeNullOrEmpty();
            result.ToJson().Should().Contain("\"secret\": \"secret\"");
        }

        [Fact]
        public async Task GraphQL_PublicField_Anonymous_IsAllowed()
        {
            var result = (IOperationResult)await ExecuteAsync("{ authenticate }", caller: null);

            result.Errors.Should().BeNullOrEmpty();
        }

        private sealed class TestCallContext : ServerCallContext
        {
            private readonly Dictionary<object, object> _userState = new();
            private readonly string _method;
            public TestCallContext(string method, HttpContext http) { _method = method; _userState["__HttpContext"] = http; }
            protected override string MethodCore => _method;
            protected override string HostCore => "localhost";
            protected override string PeerCore => "test";
            protected override DateTime DeadlineCore => DateTime.MaxValue;
            protected override Metadata RequestHeadersCore => new Metadata();
            protected override CancellationToken CancellationTokenCore => CancellationToken.None;
            protected override Metadata ResponseTrailersCore => new Metadata();
            protected override Status StatusCore { get; set; }
            protected override WriteOptions? WriteOptionsCore { get; set; }
            protected override AuthContext AuthContextCore => new AuthContext(null, new Dictionary<string, List<AuthProperty>>());
            protected override IDictionary<object, object> UserStateCore => _userState;
            protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
            protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        }

        private static Task<string> CallAsync(string method, AvatarType? caller)
            => new OASISGrpcAuthorizationInterceptor().UnaryServerHandler<string, string>(
                "req", new TestCallContext(method, ContextFor(caller)), (r, c) => Task.FromResult("ok"));

        [Theory]
        [InlineData(null, StatusCode.Unauthenticated)]
        [InlineData(AvatarType.User, StatusCode.PermissionDenied)]
        public async Task Grpc_ProtectedMethod_NonWizard_IsRejected(AvatarType? caller, StatusCode expected)
        {
            Func<Task> act = () => CallAsync("/oasis.web4.AvatarService/DeleteById", caller);

            (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(expected);
        }

        [Fact]
        public async Task Grpc_ProtectedMethod_Wizard_IsAllowed()
            => (await CallAsync("/oasis.web4.AvatarService/DeleteById", AvatarType.Wizard)).Should().Be("ok");

        [Fact]
        public async Task Grpc_PublicMethod_Anonymous_IsAllowed()
            => (await CallAsync("/oasis.web4.AvatarService/Authenticate", caller: null)).Should().Be("ok");
    }
}
