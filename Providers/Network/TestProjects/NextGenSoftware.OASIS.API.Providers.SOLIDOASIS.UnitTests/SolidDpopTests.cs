using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidDpopTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly Uri TokenEndpoint=new("https://issuer.invalid/token");
    private sealed class TokenTransport(string mode="valid"):HttpMessageHandler
    {
        public int Calls;public string? PublicX;
        public TaskCompletionSource<bool>? Gate;
        public TaskCompletionSource<bool> Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Assert.Equal(TokenEndpoint,request.RequestUri);Assert.Equal(HttpMethod.Post,request.Method);
            Entered.TrySetResult(true);if(Gate!=null)await Gate.Task.WaitAsync(token);
            Assert.Equal("Basic",request.Headers.Authorization!.Scheme);
            Assert.Equal("client%3Aid:secret%20value",Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
            Assert.Equal("grant_type=client_credentials&scope=webid",await request.Content!.ReadAsStringAsync(token));
            var proof=Verify(request,TokenEndpoint,HttpMethod.Post,null);PublicX=proof.X;
            if(mode is "nonce" or "nonceForever")
            {
                Assert.Equal(Calls==1?null:"issuer-nonce",proof.Nonce);
                if(Calls==1||mode=="nonceForever")
                {
                    var challenge=new HttpResponseMessage(HttpStatusCode.BadRequest){Content=new StringContent("{\"error\":\"use_dpop_nonce\"}",Encoding.UTF8,"application/json")};
                    challenge.Headers.Add("DPoP-Nonce","issuer-nonce");return challenge;
                }
            }
            var body=mode switch
            {
                "bearer"=>"{\"access_token\":\"owned-token\",\"token_type\":\"Bearer\",\"expires_in\":3600}",
                "expired"=>"{\"access_token\":\"owned-token\",\"token_type\":\"DPoP\",\"expires_in\":0}",
                "missing"=>"{}",
                _=>"{\"access_token\":\"owned-token\",\"token_type\":\"DPoP\",\"expires_in\":3600}"
            };
            return new HttpResponseMessage(mode=="rejected"?HttpStatusCode.Unauthorized:HttpStatusCode.OK){Content=new StringContent(body,Encoding.UTF8,"application/json")};
        }
    }
    private sealed class Resources(TokenTransport issuer,bool challenge=false):HttpMessageHandler
    {
        public int Calls;public System.Collections.Concurrent.ConcurrentDictionary<string,byte> ProofIds=new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Interlocked.Increment(ref Calls);Assert.Equal("DPoP",request.Headers.Authorization!.Scheme);Assert.Equal("owned-token",request.Headers.Authorization.Parameter);
            var proof=Verify(request,new Uri(request.RequestUri!.GetLeftPart(UriPartial.Path)),request.Method,"owned-token");
            Assert.Equal(issuer.PublicX,proof.X);Assert.True(ProofIds.TryAdd(proof.Jti,0));
            Assert.Equal(challenge&&Calls>1?"resource-nonce":null,proof.Nonce);
            var response=new HttpResponseMessage(challenge&&Calls==1?HttpStatusCode.Unauthorized:HttpStatusCode.OK){Content=new StringContent("")};
            if(challenge)response.Headers.Add("DPoP-Nonce","resource-nonce");
            return Task.FromResult(response);
        }
    }
    private static (string X,string Jti,string? Nonce) Verify(HttpRequestMessage request,Uri target,HttpMethod method,string? token)
    {
        var parts=Assert.Single(request.Headers.GetValues("DPoP")).Split('.');Assert.Equal(3,parts.Length);
        using var header=JsonDocument.Parse(Decode(parts[0]));using var claims=JsonDocument.Parse(Decode(parts[1]));
        var h=header.RootElement;var c=claims.RootElement;var jwk=h.GetProperty("jwk");
        Assert.Equal("dpop+jwt",h.GetProperty("typ").GetString());Assert.Equal("ES256",h.GetProperty("alg").GetString());Assert.False(jwk.TryGetProperty("d",out _));
        Assert.Equal("EC",jwk.GetProperty("kty").GetString());Assert.Equal("P-256",jwk.GetProperty("crv").GetString());
        using var verifier=ECDsa.Create(new ECParameters{Curve=ECCurve.NamedCurves.nistP256,Q=new ECPoint{X=Decode(jwk.GetProperty("x").GetString()!),Y=Decode(jwk.GetProperty("y").GetString()!)}});
        Assert.True(verifier.VerifyData(Encoding.ASCII.GetBytes(parts[0]+"."+parts[1]),Decode(parts[2]),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        Assert.Equal(target.AbsoluteUri,c.GetProperty("htu").GetString());Assert.Equal(method.Method,c.GetProperty("htm").GetString());
        Assert.InRange(Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-c.GetProperty("iat").GetInt64()),0,10);
        if(token==null)Assert.False(c.TryGetProperty("ath",out _));
        else Assert.Equal(SHA256.HashData(Encoding.ASCII.GetBytes(token)),Decode(c.GetProperty("ath").GetString()!));
        return(jwk.GetProperty("x").GetString()!,c.GetProperty("jti").GetString()!,c.TryGetProperty("nonce",out var nonce)?nonce.GetString():null);
    }
    private static byte[] Decode(string value)=>Convert.FromBase64String(value.Replace('-','+').Replace('_','/').PadRight((value.Length+3)/4*4,'='));

    [Fact]
    public async Task ConcurrentRequestsShareOneTokenButHaveDistinctSignedBoundProofs()
    {
        var issuer=new TokenTransport{Gate=new(TaskCreationOptions.RunContinuationsAsynchronously)};var resources=new Resources(issuer);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        var pending=Task.WhenAll(Enumerable.Range(0,4).Select(i=>client.GetAsync(new Uri(Root,"record?query="+i))));
        await issuer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.Equal(1,issuer.Calls);Assert.Equal(0,resources.Calls);
        issuer.Gate.SetResult(true);var results=await pending;
        foreach(var result in results){Assert.Equal(HttpStatusCode.OK,result.StatusCode);result.Dispose();}
        Assert.Equal(1,issuer.Calls);Assert.Equal(4,resources.Calls);Assert.Equal(4,resources.ProofIds.Count);
    }
    [Theory][InlineData("bearer")][InlineData("expired")][InlineData("missing")][InlineData("rejected")]
    public async Task BadTokenResponsesNeverDispatchResourceRequest(string mode)
    {
        var issuer=new TokenTransport(mode);var resources=new Resources(issuer);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        await Assert.ThrowsAnyAsync<Exception>(()=>client.GetAsync(Root));Assert.Equal(1,issuer.Calls);Assert.Equal(0,resources.Calls);
    }
    [Theory][InlineData("https://evil.invalid/private/")][InlineData("https://pod.invalid/outside/")]
    public async Task OutOfScopeRequestsDoNotObtainOrDiscloseTokens(string target)
    {
        var issuer=new TokenTransport();var resources=new Resources(issuer);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>client.GetAsync(target));Assert.Equal(0,issuer.Calls);Assert.Equal(0,resources.Calls);
    }
    [Fact]
    public async Task UnauthorizedResourceResponseDoesNotReplayMutation()
    {
        var issuer=new TokenTransport();var resources=new Resources(issuer,true);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        using var result=await client.PutAsync(new Uri(Root,"record"),new StringContent("owned mutation"));
        Assert.Equal(HttpStatusCode.Unauthorized,result.StatusCode);Assert.Equal(1,resources.Calls);Assert.Equal(1,issuer.Calls);
    }

    [Fact]
    public async Task IssuerNonceIsAnsweredOnceBeforeResourceDispatch()
    {
        var issuer=new TokenTransport("nonce");var resources=new Resources(issuer);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        using var result=await client.GetAsync(Root);
        Assert.Equal(HttpStatusCode.OK,result.StatusCode);Assert.Equal(2,issuer.Calls);Assert.Equal(1,resources.Calls);
    }

    [Fact]
    public async Task RepeatedIssuerNonceChallengeStopsWithoutResourceDispatch()
    {
        var issuer=new TokenTransport("nonceForever");var resources=new Resources(issuer);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        await Assert.ThrowsAsync<HttpRequestException>(()=>client.GetAsync(Root));
        Assert.Equal(2,issuer.Calls);Assert.Equal(0,resources.Calls);
    }

    [Fact]
    public async Task ResourceNonceIsUsedOnlyOnNextExplicitRequestNotByReplayingWrite()
    {
        var issuer=new TokenTransport();var resources=new Resources(issuer,true);
        using var client=new HttpClient(new SolidDpopClientCredentialsHandler(Root,TokenEndpoint,"client:id","secret value",resources,issuer));
        using var rejected=await client.PutAsync(new Uri(Root,"record"),new StringContent("one mutation"));
        Assert.Equal(HttpStatusCode.Unauthorized,rejected.StatusCode);Assert.Equal(1,resources.Calls);
        using var next=await client.GetAsync(new Uri(Root,"record"));
        Assert.Equal(HttpStatusCode.OK,next.StatusCode);Assert.Equal(2,resources.Calls);Assert.Equal(1,issuer.Calls);
    }
}
