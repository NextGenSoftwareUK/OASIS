using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weaviate.Client.Grpc.Protobuf.V1;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.UnitTests;

[TestClass]
public class WeaviateQuerySourceTests
{
    [TestMethod]
    public async Task QueryActivationUsesTheOfficialSearchContractOnTheSuppliedChannel()
    {
        var id = Guid.Parse("12345678-1234-4234-8234-123456789abc");
        using var transport = new ObservedTransport(id);
        using var channel = GrpcChannel.ForAddress("https://weaviate.example.test", new GrpcChannelOptions
        {
            HttpHandler = transport, MaxReceiveMessageSize = 4 * 1024 * 1024
        });
        using var rest = new HttpClient();
        var provider = new WeaviateQuerySource(rest, "https://weaviate.example.test",
            channel.CreateCallInvoker(), "controlled-reader", id);
        var result = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
        Assert.AreEqual(1, transport.Calls);
    }

    private sealed class ObservedTransport(Guid id) : HttpMessageHandler
    {
        internal int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/weaviate.v1.Weaviate/Search", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("Bearer controlled-reader", request.Headers.GetValues("authorization").Single());
            var bytes = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            Assert.AreEqual((byte)0, bytes[0]);
            Assert.AreEqual(bytes.Length - 5, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4)));
            var native = SearchRequest.Parser.ParseFrom(bytes, 5, bytes.Length - 5);
            Assert.AreEqual("OasisHolon", native.Collection);
            Assert.AreEqual(2u, native.Limit);
            Assert.IsTrue(native.Uses127Api && native.Metadata.Uuid);
            Assert.AreEqual("_id", native.Filters.Target.Property);
            Assert.AreEqual(Filters.Types.Operator.Equal, native.Filters.Operator);
            Assert.AreEqual(id.ToString("D"), native.Filters.ValueText);
            CollectionAssert.AreEqual(new[] { "dataJson", "isDeleted" }, native.Properties.NonRefProperties.ToArray());

            var fields = new Properties();
            fields.Fields.Add("dataJson", new Value { TextValue = JsonSerializer.Serialize(new { id, isPublic = true }) });
            fields.Fields.Add("isDeleted", new Value { BoolValue = false });
            var reply = new SearchReply { Results = { new SearchResult
            {
                Metadata = new MetadataResult { IdAsBytes = ByteString.CopyFrom(id.ToByteArray(bigEndian: true)) },
                Properties = new PropertiesResult { NonRefProps = fields }
            } } };
            var payload = reply.ToByteArray();
            var frame = new byte[payload.Length + 5];
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length);
            payload.CopyTo(frame, 5);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Version = HttpVersion.Version20, Content = new ByteArrayContent(frame)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
            response.TrailingHeaders.Add("grpc-status", "0");
            return response;
        }
    }
}
