using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[Collection("GraphQL boot configuration")]
public class GrpcConditionalWriteHttpTests
{
    [Theory]
    [InlineData(false, "COSMIC_WRITE_ACTOR_REQUIRED", false)]
    [InlineData(true, "COSMIC_WRITE_INTENT_INVALID", false)]
    [InlineData(false, "COSMIC_WRITE_ACTOR_REQUIRED", true)]
    [InlineData(true, "COSMIC_EXPECTED_REVISION_REQUIRED", true)]
    public async Task ShippingGrpcSaveRejectsUnadmittedIntentOverRealHttp2(bool authenticated, string expectedCode, bool deletion)
    {
        var previous = OASISBootLoader.OASISBootLoader.OASISDNA;
        try
        {
            OASISBootLoader.OASISBootLoader.OASISDNA = new OASISDNA
                { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
                endpoint => endpoint.Protocols = HttpProtocols.Http2));
            builder.Services.AddGrpc();
            await using var app = builder.Build();
            app.Use(async (context, next) =>
            {
                if (authenticated)
                    context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid() };
                await next(context);
            });
            app.MapGrpcService<DataGrpcService>();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
                var wire = new SaveHolonGrpcRequest { HolonJson = "{\"name\":\"bounded create\"}",
                    OperationId = authenticated ? "" : Guid.NewGuid().ToString("D") };
                var payload = deletion
                    ? new DeleteHolonGrpcRequest { Id = Guid.NewGuid().ToString("D"), OperationId = Guid.NewGuid().ToString("D") }.ToByteArray()
                    : wire.ToByteArray();
                var frame = new byte[payload.Length + 5];
                BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length);
                payload.CopyTo(frame, 5);
                using var request = new HttpRequestMessage(HttpMethod.Post, deletion ? "/oasis.web4.DataService/DeleteHolon" : "/oasis.web4.DataService/SaveHolon")
                {
                    Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new ByteArrayContent(frame)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
                request.Headers.TryAddWithoutValidation("TE", "trailers");

                using var response = await client.SendAsync(request);
                var responseBytes = await response.Content.ReadAsByteArrayAsync();

                Assert.Equal(HttpVersion.Version20, response.Version);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(responseBytes.Length >= 5);
                Assert.Equal(0, responseBytes[0]);
                Assert.Equal(responseBytes.Length - 5, BinaryPrimitives.ReadInt32BigEndian(responseBytes.AsSpan(1, 4)));
                if (deletion)
                {
                    var result = OASISDataResponse.Parser.ParseFrom(responseBytes, 5, responseBytes.Length - 5);
                    Assert.True(result.IsError);
                    Assert.Equal(expectedCode, result.ErrorCode);
                    Assert.False(result.IsDeleted);
                }
                else
                {
                    var result = HolonResponse.Parser.ParseFrom(responseBytes, 5, responseBytes.Length - 5);
                    Assert.True(result.IsError);
                    Assert.Equal(expectedCode, result.ErrorCode);
                    Assert.False(result.IsSaved);
                    Assert.Empty(result.HolonJson);
                }
            }
            finally { await app.StopAsync(); }
        }
        finally { OASISBootLoader.OASISBootLoader.OASISDNA = previous; }
    }
}
