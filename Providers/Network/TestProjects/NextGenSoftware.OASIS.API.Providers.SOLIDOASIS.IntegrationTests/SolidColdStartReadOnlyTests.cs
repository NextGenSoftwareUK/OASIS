using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

/// <summary>Explicit retained-server diagnostic; never writes ACLs or records.</summary>
public class SolidColdStartReadOnlyTests
{
    [Fact]
    public async Task RetainedIssuerDiscoveryThenAuthenticatedAclRead()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_SOLID_COLD_READONLY_WORK")
            ??throw new InvalidOperationException("Use the retained diagnostic environment explicitly.");
        var root=new Uri("http://localhost:13000/");
        var timer=Stopwatch.StartNew();
        using var anonymous=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var discovery=await anonymous.GetAsync(new Uri(root,".well-known/openid-configuration"));
        Assert.True(discovery.IsSuccessStatusCode,$"Discovery status {(int)discovery.StatusCode}");
        using var configuration=JsonDocument.Parse(await discovery.Content.ReadAsStringAsync());
        Assert.Equal(root.AbsoluteUri,configuration.RootElement.GetProperty("issuer").GetString());
        Assert.Equal(new Uri(root,".oidc/token").AbsoluteUri,configuration.RootElement.GetProperty("token_endpoint").GetString());
        Console.WriteLine($"Discovery completed after {timer.ElapsedMilliseconds}ms.");
        var bytes=ProtectedData.Unprotect(await File.ReadAllBytesAsync(Path.Combine(work,"private-local-client.dpapi")),null,DataProtectionScope.CurrentUser);
        try{
            using var credentials=JsonDocument.Parse(bytes);
            using var handler=new SolidDpopClientCredentialsHandler(new Uri(root,"oasis-auth-local/"),new Uri(root,".oidc/token"),
                credentials.RootElement.GetProperty("id").GetString()!,credentials.RootElement.GetProperty("secret").GetString()!);
            using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)};
            using var response=await client.GetAsync(new Uri(root,"oasis-auth-local/.acl"));
            Assert.True(response.IsSuccessStatusCode,$"Authenticated ACL read status {(int)response.StatusCode}");
            Assert.False(string.IsNullOrWhiteSpace(await response.Content.ReadAsStringAsync()));
            Console.WriteLine($"Authenticated read completed after {timer.ElapsedMilliseconds}ms; no ACL or record mutation.");
        }finally{CryptographicOperations.ZeroMemory(bytes);}
    }
}
