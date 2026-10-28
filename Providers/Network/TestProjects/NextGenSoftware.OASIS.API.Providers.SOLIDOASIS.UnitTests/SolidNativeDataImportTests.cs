using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests
{
    /// <summary>The SOLID native reader against a scripted Pod: containers listed, resources paged by path, RDF turned into fields.</summary>
    public class SolidNativeDataImportTests
    {
        private const string Pod = "https://pod.example";

        private sealed class ScriptedPod : HttpMessageHandler
        {
            public readonly Dictionary<string, (string Type, string Body)> Resources = new Dictionary<string, (string, string)>();
            public readonly List<string> Requests = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri.AbsoluteUri;
                Requests.Add(path);
                if (!Resources.TryGetValue(path, out var found))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(found.Body)) };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(found.Type);
                return Task.FromResult(response);
            }
        }

        private static ScriptedPod People()
        {
            var pod = new ScriptedPod();
            pod.Resources[Pod + "/public/people/"] = ("text/turtle", $@"
                @prefix ldp: <http://www.w3.org/ns/ldp#> .
                <{Pod}/public/people/> ldp:contains <{Pod}/public/people/grace.ttl>, <{Pod}/public/people/ada.ttl>, <{Pod}/public/people/logo.png>, <{Pod}/public/people/archive/> .");
            pod.Resources[Pod + "/public/people/ada.ttl"] = ("text/turtle", $@"
                @prefix foaf: <http://xmlns.com/foaf/0.1/> . @prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
                <{Pod}/public/people/ada.ttl> foaf:name ""Ada"" ; foaf:age ""36""^^xsd:integer ; foaf:score ""4.5""^^xsd:decimal ;
                    foaf:member true ; foaf:homepage <https://ada.example/> .");
            pod.Resources[Pod + "/public/people/grace.ttl"] = ("text/turtle", $@"
                @prefix foaf: <http://xmlns.com/foaf/0.1/> .
                <{Pod}/public/people/grace.ttl> foaf:name ""Grace"" .");
            pod.Resources[Pod + "/public/people/logo.png"] = ("image/png", "PNGDATA");
            return pod;
        }

        private static SOLIDOASIS Provider(ScriptedPod pod, string containers = "public/people") =>
            new SOLIDOASIS(Pod, "", null, pod) { NativeImportContainers = containers };

        [Fact]
        public async Task ListsConfiguredContainersCountingOnlyResources()
        {
            var sources = await Provider(People()).ListNativeDataSourcesAsync();
            Assert.False(sources.IsError, sources.Message);
            var source = Assert.Single(sources.Result);
            Assert.Equal("public/people/", source.Name);
            Assert.Equal(3, source.EstimatedRecordCount); // two turtle files and the png; the sub-container is not a record
        }

        [Fact]
        public async Task PagesResourcesInPathOrderWithTypedFields()
        {
            var provider = Provider(People());
            var first = await provider.ReadNativeRecordsAsync("public/people/", null, 2);
            Assert.False(first.IsError, first.Message);
            Assert.Equal(new[] { "public/people/ada.ttl", "public/people/grace.ttl" }, first.Result.Records.Select(record => record.NativeKey));
            Assert.Equal("public/people/grace.ttl", first.Result.ContinuationToken);

            var ada = first.Result.Records[0].Fields;
            Assert.Equal("Ada", ada["name"]);
            Assert.Equal(36L, ada["age"]);
            Assert.Equal(4.5, ada["score"]);
            Assert.Equal(true, ada["member"]);
            Assert.Equal("https://ada.example/", ada["homepage"]);

            var second = await provider.ReadNativeRecordsAsync("public/people/", first.Result.ContinuationToken, 2);
            var logo = Assert.Single(second.Result.Records);
            Assert.Equal("public/people/logo.png", logo.NativeKey);
            Assert.Equal("image/png", logo.Fields["contentType"]);
            Assert.Equal(7L, logo.Fields["size"]);
            Assert.Null(second.Result.ContinuationToken);
        }

        [Fact]
        public async Task OasisOwnTreeTraversalAndUnlistedContainersAreRefused()
        {
            var pod = People();
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider(pod, "oasis/holons").ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider(pod, "public/../oasis").ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider(pod, null).ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider(pod).ReadNativeRecordsAsync("private/", null, 5)).ErrorCode);
            Assert.DoesNotContain(pod.Requests, request => request.Contains("/oasis/"));
        }

        [Fact]
        public async Task OversizedResourceFailsInsteadOfBeingTruncated()
        {
            var pod = People();
            pod.Resources[Pod + "/public/people/ada.ttl"] = ("text/turtle", new string('x', 1024 * 1024 + 1));
            var result = await Provider(pod).ReadNativeRecordsAsync("public/people/", null, 1);
            Assert.True(result.IsError);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", result.ErrorCode);
        }
    }
}
