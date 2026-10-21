using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

/// <summary>CSS client-credentials authentication with per-request RFC9449 ES256 DPoP proofs.
/// Explicit endpoints only; no implicit redirects or replay of resource requests.</summary>
public sealed class SolidDpopClientCredentialsHandler : DelegatingHandler
{
    private readonly Uri _podRoot,_tokenEndpoint;
    private readonly string _clientId,_clientSecret;
    private readonly HttpClient _tokens;
    private readonly ECDsa _key;
    private readonly SemaphoreSlim _tokenGate=new(1,1);
    private readonly object _signingGate=new();
    private string _accessToken;
    private string _issuerNonce,_resourceNonce;
    private DateTimeOffset _refreshAt;
    private bool _disposed;

    public SolidDpopClientCredentialsHandler(Uri podRoot,Uri tokenEndpoint,string clientId,string clientSecret,
        HttpMessageHandler resourceTransport=null,HttpMessageHandler tokenTransport=null)
        :base(WithoutRedirects(resourceTransport??new HttpClientHandler()))
    {
        _podRoot=SolidPrivateRecordCodec.NormalizePodRoot(podRoot??throw new ArgumentNullException(nameof(podRoot)));
        ArgumentNullException.ThrowIfNull(tokenEndpoint);
        if(!tokenEndpoint.IsAbsoluteUri||!string.IsNullOrEmpty(tokenEndpoint.UserInfo)||!string.IsNullOrEmpty(tokenEndpoint.Fragment)
            ||(tokenEndpoint.Scheme!="https"&&!(tokenEndpoint.Scheme=="http"&&tokenEndpoint.IsLoopback)))
            throw new ArgumentException("Explicit HTTPS (or loopback HTTP) token endpoint required.",nameof(tokenEndpoint));
        if(string.IsNullOrWhiteSpace(clientId)||string.IsNullOrWhiteSpace(clientSecret))throw new ArgumentException("Explicit Solid client credentials required.");
        _tokenEndpoint=tokenEndpoint;_clientId=clientId;_clientSecret=clientSecret;
        _tokens=new HttpClient(WithoutRedirects(tokenTransport??new HttpClientHandler())){Timeout=TimeSpan.FromSeconds(30)};
        // Do not allocate a native signing key for configuration rejected by this constructor.
        _key=ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static HttpMessageHandler WithoutRedirects(HttpMessageHandler handler)
    {
        if(handler is HttpClientHandler http)http.AllowAutoRedirect=false;
        if(handler is SocketsHttpHandler sockets)sockets.AllowAutoRedirect=false;
        return handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        var target=request.RequestUri;
        if(target==null||!target.IsAbsoluteUri||!_podRoot.IsBaseOf(target)||!string.IsNullOrEmpty(target.UserInfo)||!string.IsNullOrEmpty(target.Fragment))
            throw new InvalidOperationException("DPoP credentials may only be sent within the configured Pod root.");
        if(request.Headers.Authorization!=null||request.Headers.Contains("DPoP"))
            throw new InvalidOperationException("DPoP handler owns authentication headers; conflicting credentials refused.");
        var token=await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization=new AuthenticationHeaderValue("DPoP",token);
        request.Headers.Add("DPoP",CreateProof(target,request.Method,token,Volatile.Read(ref _resourceNonce)));
        // Never repeat a possibly mutating resource request, including on401/nonce challenge.
        var response=await base.SendAsync(request,cancellationToken).ConfigureAwait(false);
        try
        {
            var nonce=ReadNonce(response);
            if(nonce!=null)Volatile.Write(ref _resourceNonce,nonce);
            // HTTP 401 rejects the credentials sent on this request. CSS can return only
            // Bearer scope="openid webid", without an invalid_token error parameter.
            // Return the original failure; obtain credentials only for the next explicit call.
            if(response.StatusCode==HttpStatusCode.Unauthorized)
                // A late rejection for an older token must not clear a newer cached token.
                Interlocked.CompareExchange(ref _accessToken,null,token);
            return response;
        }
        catch{response.Dispose();throw;}
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cached=Volatile.Read(ref _accessToken);
            if(cached!=null&&DateTimeOffset.UtcNow<_refreshAt)return cached;
            // RFC9449 token-endpoint nonce challenge: at most one answered challenge,
            // before any resource request. This never retries a Pod mutation.
            for(var challenge=0;challenge<2;challenge++)
            {
            using var request=new HttpRequestMessage(HttpMethod.Post,_tokenEndpoint);
            var requestedAt=DateTimeOffset.UtcNow;
            request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(
                Uri.EscapeDataString(_clientId)+":"+Uri.EscapeDataString(_clientSecret))));
            request.Headers.Add("DPoP",CreateProof(_tokenEndpoint,HttpMethod.Post,null,_issuerNonce));
            request.Content=new FormUrlEncodedContent(new Dictionary<string,string>{{"grant_type","client_credentials"},{"scope","webid"}});
            using var response=await _tokens.SendAsync(request,cancellationToken).ConfigureAwait(false);
            var nonce=ReadNonce(response);
            var body=await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if(nonce!=null&&(response.StatusCode==HttpStatusCode.BadRequest||response.StatusCode==HttpStatusCode.Unauthorized))
            {
                using var error=JsonDocument.Parse(body);
                if(error.RootElement.TryGetProperty("error",out var reason)&&reason.GetString()=="use_dpop_nonce"&&challenge==0)
                {_issuerNonce=nonce;continue;}
            }
            response.EnsureSuccessStatusCode();
            if(nonce!=null)_issuerNonce=nonce;
            using var json=JsonDocument.Parse(body);
            var root=json.RootElement;
            if(!root.TryGetProperty("token_type",out var type)||!string.Equals(type.GetString(),"DPoP",StringComparison.OrdinalIgnoreCase)
                ||!root.TryGetProperty("access_token",out var access)||access.ValueKind!=JsonValueKind.String
                ||!root.TryGetProperty("expires_in",out var lifetime)||!lifetime.TryGetInt32(out var seconds)||seconds<=0)
                throw new InvalidDataException("Solid token response requires a DPoP access token and positive lifetime.");
            var token=access.GetString();
            if(string.IsNullOrWhiteSpace(token)||System.Linq.Enumerable.Any(token,c=>c<=32||c>=127))throw new InvalidDataException("Invalid DPoP access-token encoding.");
            if(DateTimeOffset.UtcNow>=requestedAt.AddSeconds(seconds))throw new InvalidDataException("Solid token expired before resource dispatch.");
            _refreshAt=requestedAt.AddSeconds(seconds-Math.Min(30,seconds/10));
            _accessToken=token;return token;
            }
            throw new InvalidDataException("Solid issuer nonce challenge was not resolved.");
        }
        finally{_tokenGate.Release();}
    }

    private static string ReadNonce(HttpResponseMessage response)
    {
        if(!response.Headers.TryGetValues("DPoP-Nonce",out var values))return null;
        var nonces=values.ToArray();
        if(nonces.Length!=1||string.IsNullOrWhiteSpace(nonces[0])||nonces[0].Length>1024||nonces[0].Any(c=>c<=32||c>=127))
            throw new InvalidDataException("Solid server returned an invalid or ambiguous DPoP nonce.");
        return nonces[0];
    }

    private string CreateProof(Uri uri,HttpMethod method,string token,string nonce=null)
    {
        lock(_signingGate)
        {
            var publicKey=_key.ExportParameters(false);
            var header=new{typ="dpop+jwt",alg="ES256",jwk=new{kty="EC",crv="P-256",x=Base64Url(publicKey.Q.X),y=Base64Url(publicKey.Q.Y)}};
            var claims=new Dictionary<string,object>{{"jti",Guid.NewGuid().ToString("N")},{"htm",method.Method},
                {"htu",new UriBuilder(uri){Query="",Fragment=""}.Uri.AbsoluteUri},{"iat",DateTimeOffset.UtcNow.ToUnixTimeSeconds()}};
            if(token!=null)claims["ath"]=Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
            if(nonce!=null)claims["nonce"]=nonce;
            var input=Base64Url(JsonSerializer.SerializeToUtf8Bytes(header))+"."+Base64Url(JsonSerializer.SerializeToUtf8Bytes(claims));
            return input+"."+Base64Url(_key.SignData(Encoding.ASCII.GetBytes(input),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }
    }

    private static string Base64Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');

    protected override void Dispose(bool disposing)
    {
        if(disposing&&!_disposed){_disposed=true;_tokens.Dispose();_key.Dispose();_tokenGate.Dispose();_accessToken=null;}
        base.Dispose(disposing);
    }
}
