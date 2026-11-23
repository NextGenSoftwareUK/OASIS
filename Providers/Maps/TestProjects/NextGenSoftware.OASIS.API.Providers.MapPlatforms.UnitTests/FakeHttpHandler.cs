using System.Net;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

/// <summary>Records each request and answers with canned responses in order.</summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body, string MediaType)> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> RequestBodies { get; } = new();

    public FakeHttpHandler Respond(HttpStatusCode status, string body, string mediaType = "application/json")
    {
        _responses.Enqueue((status, body, mediaType));
        return this;
    }

    public HttpClient Client() => new HttpClient(this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0) throw new InvalidOperationException("No canned response for " + request.RequestUri);
        var (status, body, mediaType) = _responses.Dequeue();
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
    }
}
