using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

public class SolidPrecisionFixtureReconciliationTests
{
    [Fact]
    public async Task ReconcileOnlyKnownAcknowledgedPrecisionFixture()
    {
        const string work="C:/Source/OASIS/TestResults/SolidCurrent";
        const string journalHash="C27B587D5A64DDFB69C997AF127D8ED1623E5B554CC7A7D6130A0966C7C68E7E";
        var journal=Path.Combine(work,"private-native-pending.json");
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        Assert.Equal("1A557AD1260E5F8E239D27CA2DE7C89BDD0622E21765AF3D6D66AAD5C108CB97",Hash(Path.Combine(work,"solid-native-current.trx")));
        Assert.Equal(journalHash,Hash(journal));
        var recovery=Path.Combine(work,"precision-recovery.pending.json");Assert.False(File.Exists(recovery));
        var proof=Path.Combine(work,"precision-recovery-verified.json");Assert.False(File.Exists(proof));
        var id=Guid.Parse("8ce1f32b-eda9-4ec7-9cb6-352ab0fa3514");
        var root=new Uri("http://localhost:13000/oasis-auth-local/");var resource=new Uri(root,$"oasis/holons/{id}.ttl");
        using(var intent=JsonDocument.Parse(File.ReadAllBytes(journal))){
            Assert.Equal(root.AbsoluteUri,intent.RootElement.GetProperty("root").GetString());
            Assert.Equal(resource.AbsoluteUri,intent.RootElement.GetProperty("resource").GetString());
            Assert.Equal("before-private-native-write",intent.RootElement.GetProperty("state").GetString());
        }
        var secret=ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(work,"private-local-client.dpapi")),null,DataProtectionScope.CurrentUser);
        var key=ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(work,"storage-key.dpapi")),null,DataProtectionScope.CurrentUser);
        try{
            using var credentials=JsonDocument.Parse(secret);
            using var client=new HttpClient(new SolidDpopClientCredentialsHandler(root,new Uri("http://localhost:13000/.oidc/token"),
                credentials.RootElement.GetProperty("id").GetString()!,credentials.RootElement.GetProperty("secret").GetString()!)){Timeout=TimeSpan.FromSeconds(20)};
            using var observed=await client.GetAsync(resource);Assert.Equal(HttpStatusCode.OK,observed.StatusCode);
            var etag=observed.Headers.ETag;Assert.NotNull(etag);Assert.False(etag.IsWeak);
            var turtle=await observed.Content.ReadAsStringAsync();
            using var codec=new SolidPrivateRecordCodec(root,key);
            var record=codec.Decode<IHolon>(turtle,id);
            Assert.Equal(id,record.Id);Assert.Equal(2,record.Version);Assert.Equal("private-dpop-updated",record.Name);Assert.False(record.IsDeleted);
            Assert.Equal(0.1234567890123456789012345678m,decimal.Parse(record.MetaData["exact_decimal"].ToString()!,System.Globalization.CultureInfo.InvariantCulture));
            var prior=await codec.DecodeVersionAsync<IHolon>(turtle,id,1);Assert.Equal("private-dpop-canary",prior.Name);Assert.Equal(id,prior.Id);
            var payloadHash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(turtle)));
            async Task Write(string path,object value){
                using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
                await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));stream.Flush(true);
            }
            await Write(recovery,new{resource=resource.AbsoluteUri,etag=etag.ToString(),payloadHash,state="before-exact-conditional-cleanup"});
            using var deletion=new HttpRequestMessage(HttpMethod.Delete,resource);deletion.Headers.IfMatch.Add(etag);
            using var removed=await client.SendAsync(deletion);Assert.True(removed.IsSuccessStatusCode,$"Conditional cleanup status {(int)removed.StatusCode}; retain original recovery intent.");
            using var absent=await client.GetAsync(resource);Assert.Equal(HttpStatusCode.NotFound,absent.StatusCode);
            Assert.Equal(journalHash,Hash(journal));
            await Write(proof,new{resource=resource.AbsoluteUri,payloadHash,etag=etag.ToString(),conditionalDelete=true,absenceVerified=true});
            File.Delete(recovery);File.Delete(journal);
        }finally{CryptographicOperations.ZeroMemory(secret);CryptographicOperations.ZeroMemory(key);}
    }
}
