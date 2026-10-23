using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using Provider = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Full provider/official SDK path over controlled in-memory HTTP.
// No real bucket, network service, durable database or cloud acceptance claim.
[TestClass]
public class AwsS3EncryptedLifecycleTests
{
    private sealed class Store : IDisposable
    {
        public readonly Dictionary<string, string> Objects = new(StringComparer.Ordinal);
        public readonly AmazonS3Client Client;
        private readonly FakeService _http;
        public int Requests, Pages;
        private static string Revision(string value) => "\"" + Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant() + "\"";
        private static HttpResponseMessage Failure(HttpStatusCode status, string code) => new(status)
        { Content = new StringContent($"<Error><Code>{code}</Code><Message>controlled</Message></Error>", Encoding.UTF8, "application/xml") };
        public Store()
        {
            _http = new FakeService((request, body) =>
            {
                lock (Objects)
                {
                    Assert.IsTrue(++Requests <= 2000, "Bounded controlled request budget exceeded.");
                    Assert.AreEqual("s3.us-east-1.amazonaws.com", request.RequestUri.Host);
                    Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ", StringComparison.Ordinal));
                    var path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath).TrimStart('/');
                    if (request.Method == HttpMethod.Head && path.TrimEnd('/') == "test-bucket") return FakeService.Status(HttpStatusCode.OK);
                    if (path.TrimEnd('/') == "test-bucket")
                    {
                        Assert.AreEqual(HttpMethod.Get, request.Method);
                        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                        Assert.AreEqual("2", query["list-type"]); Pages++;
                        var all = Objects.Keys.Where(k => k.StartsWith(query["prefix"] ?? "", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
                        var offset = int.TryParse(query["continuation-token"], out var parsed) ? parsed : 0;
                        var page = all.Skip(offset).Take(2).ToArray(); var more = offset + page.Length < all.Length;
                        var xml = "<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name>"
                            + $"<IsTruncated>{more.ToString().ToLowerInvariant()}</IsTruncated>"
                            + string.Concat(page.Select(k => $"<Contents><Key>{SecurityElement.Escape(k)}</Key><Size>{Encoding.UTF8.GetByteCount(Objects[k])}</Size></Contents>"))
                            + (more ? $"<NextContinuationToken>{offset + page.Length}</NextContinuationToken>" : "") + "</ListBucketResult>";
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml, Encoding.UTF8, "application/xml") };
                    }
                    Assert.IsTrue(path.StartsWith("test-bucket/scope/", StringComparison.Ordinal));
                    var key = path["test-bucket/".Length..];
                    var present = Objects.TryGetValue(key, out var existing);
                    var match = request.Headers.TryGetValues("If-Match", out var values) ? values.Single() : null;
                    if (match != null && (!present || Revision(existing) != match)) return Failure(HttpStatusCode.PreconditionFailed, "PreconditionFailed");
                    if (request.Headers.TryGetValues("If-None-Match", out var none) && none.Single() == "*" && present)
                        return Failure(HttpStatusCode.PreconditionFailed, "PreconditionFailed");
                    if (request.Method == HttpMethod.Put)
                    {
                        var value = TigrisS3ProtocolTests.DecodeAwsChunked(request, body);
                        var checksum = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
                        Assert.AreEqual(checksum, request.Headers.GetValues("x-amz-checksum-sha256").Single());
                        Objects[key] = value;
                        var response = FakeService.Status(HttpStatusCode.OK);
                        response.Headers.TryAddWithoutValidation("ETag", Revision(value));
                        response.Headers.TryAddWithoutValidation("x-amz-checksum-sha256", checksum);
                        return response;
                    }
                    if (request.Method == HttpMethod.Delete) { Objects.Remove(key); return FakeService.Status(HttpStatusCode.NoContent); }
                    Assert.AreEqual(HttpMethod.Get, request.Method);
                    if (!present) return Failure(HttpStatusCode.NotFound, "NoSuchKey");
                    var get = FakeService.Text(existing); get.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(existing);
                    get.Headers.TryAddWithoutValidation("ETag", Revision(existing)); return get;
                }
            });
            Client = new AmazonS3Client(new BasicAWSCredentials("synthetic-key", "synthetic-secret"), new AmazonS3Config
            {
                ServiceURL = "https://s3.us-east-1.amazonaws.com", AuthenticationRegion = "us-east-1", ForcePathStyle = true,
                MaxErrorRetry = 0, HttpClientFactory = new HandlerHttpClientFactory(_http)
            });
        }
        public Provider Open(byte[] key) => new(Client, "test-bucket", () => key.ToArray(), "scope/") { IsVersionControlEnabled = true };
        public void AssertPrivate(params string[] plaintext)
        {
            Assert.IsTrue(Objects.Count > 0);
            foreach (var item in Objects)
            {
                Assert.IsTrue(item.Key.StartsWith("scope/oasis/private/v1/", StringComparison.Ordinal));
                foreach (var text in plaintext) { Assert.IsFalse(item.Key.Contains(text, StringComparison.Ordinal)); Assert.IsFalse(item.Value.Contains(text, StringComparison.Ordinal)); }
            }
        }
        public void Dispose() { Client.Dispose(); _http.Dispose(); Objects.Clear(); }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Holon_history_survives_provider_recreation_and_stays_encrypted(bool sync)
    {
        using var store = new Store(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var id = Guid.NewGuid();
            using (var provider = store.Open(key))
            {
                var active = await provider.ActivateProviderAsync(); Assert.IsFalse(active.IsError, active.Message + active.Exception);
                var holon = new Holon { Id = id, Name = "private-original-name", MetaData = new() { ["private-field"] = "private-value" } };
                var first = sync ? provider.SaveHolon(holon) : await provider.SaveHolonAsync(holon);
                Assert.IsFalse(first.IsError, first.Message + first.Exception); Assert.AreEqual(1, holon.Version);
                holon.Name = "private-updated-name";
                var second = sync ? provider.SaveHolon(holon) : await provider.SaveHolonAsync(holon);
                Assert.IsFalse(second.IsError, second.Message + second.Exception); Assert.AreEqual(2, holon.Version);
                store.AssertPrivate(id.ToString(), "private-original-name", "private-updated-name", "private-field", "private-value");
            }
            using (var fresh = store.Open(key))
            {
                var active = await fresh.ActivateProviderAsync(); Assert.IsFalse(active.IsError, active.Message + active.Exception);
                var latest = sync ? fresh.LoadHolon(id, loadChildren: false) : await fresh.LoadHolonAsync(id, loadChildren: false);
                Assert.IsFalse(latest.IsError, latest.Message + latest.Exception); Assert.AreEqual("private-updated-name", latest.Result.Name);
                var old = sync ? fresh.LoadHolon(id, loadChildren: false, version: 1) : await fresh.LoadHolonAsync(id, loadChildren: false, version: 1);
                Assert.IsFalse(old.IsError, old.Message + old.Exception); Assert.AreEqual("private-original-name", old.Result.Name);
                var selected = sync ? fresh.LoadHolonsByMetaData("private-field", "private-value", loadChildren: false)
                    : await fresh.LoadHolonsByMetaDataAsync("private-field", "private-value", loadChildren: false);
                Assert.IsFalse(selected.IsError, selected.Message + selected.Exception); Assert.AreEqual(id, selected.Result.Single().Id);
                Assert.IsTrue(store.Pages > 1, "Encrypted query must exercise paginated object enumeration.");
                var deleted = sync ? fresh.DeleteHolon(id) : await fresh.DeleteHolonAsync(id);
                Assert.IsFalse(deleted.IsError, deleted.Message + deleted.Exception);
                Assert.IsTrue((await fresh.LoadHolonAsync(id, loadChildren: false)).IsError);
            }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Avatar_identity_detail_inventory_and_delete_are_coherent(bool sync)
    {
        using var store = new Store(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = store.Open(key);
            var active = await provider.ActivateProviderAsync(); Assert.IsFalse(active.IsError, active.Message + active.Exception);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "private-s3-user", Email = "private-s3@example.invalid", IsActive = true };
            var saved = sync ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            var detail = new AvatarDetail { Id = avatar.Id, Karma = 29, XP = 321 };
            detail.Inventory.Add(new InventoryItem { Id = Guid.NewGuid(), Name = "private-s3-inventory" });
            var details = sync ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(details.IsError, details.Message + details.Exception);
            var byName = sync ? provider.LoadAvatarByUsername(avatar.Username) : await provider.LoadAvatarByUsernameAsync(avatar.Username);
            var byEmail = sync ? provider.LoadAvatarByEmail(avatar.Email) : await provider.LoadAvatarByEmailAsync(avatar.Email);
            Assert.IsFalse(byName.IsError, byName.Message + byName.Exception); Assert.AreEqual(avatar.Id, byName.Result.Id);
            Assert.IsFalse(byEmail.IsError, byEmail.Message + byEmail.Exception); Assert.AreEqual(avatar.Id, byEmail.Result.Id);
            var wrongDelete = sync ? provider.DeleteAvatar(avatar.Username, softDelete: false) : await provider.DeleteAvatarAsync(avatar.Username, softDelete: false);
            Assert.IsTrue(wrongDelete.IsError); Assert.IsInstanceOfType<ArgumentException>(wrongDelete.Exception);
            var read = await provider.LoadAvatarDetailAsync(avatar.Id);
            Assert.IsFalse(read.IsError, read.Message + read.Exception); Assert.AreEqual(321L, read.Result.XP); Assert.AreEqual(29L, read.Result.Karma);
            Assert.AreEqual("private-s3-inventory", read.Result.Inventory.Single().Name);
            store.AssertPrivate(avatar.Id.ToString(), avatar.Username, avatar.Email, "private-s3-inventory");
            var soft = sync ? provider.DeleteAvatar(avatar.Id, softDelete: true) : await provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Assert.IsFalse(soft.IsError, soft.Message + soft.Exception);
            var deletedAvatar = await provider.LoadAvatarAsync(avatar.Id); var deletedDetail = await provider.LoadAvatarDetailAsync(avatar.Id);
            Assert.IsFalse(deletedAvatar.IsError, deletedAvatar.Message); Assert.IsFalse(deletedDetail.IsError, deletedDetail.Message);
            Assert.IsTrue(deletedAvatar.Result.IsDeleted); Assert.IsTrue(deletedDetail.Result.IsDeleted);
            Assert.AreEqual(deletedAvatar.Result.DeletedDate, deletedDetail.Result.DeletedDate);
            var hard = sync ? provider.DeleteAvatar(avatar.Id, softDelete: false) : await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Assert.IsFalse(hard.IsError, hard.Message + hard.Exception);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id)).IsError);
            Assert.IsTrue((await provider.LoadAvatarDetailAsync(avatar.Id)).IsError);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [TestMethod]
    public async Task Wrong_persisted_key_cannot_read_or_mutate_existing_records()
    {
        using var store = new Store(); var key = RandomNumberGenerator.GetBytes(32); var wrong = RandomNumberGenerator.GetBytes(32);
        try
        {
            var id = Guid.NewGuid();
            using (var owner = store.Open(key))
            {
                Assert.IsFalse((await owner.ActivateProviderAsync()).IsError);
                var saved = await owner.SaveHolonAsync(new Holon { Id = id, Name = "private-owner-record" });
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            }
            var before = store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
            using (var other = store.Open(wrong))
            {
                Assert.IsTrue((await other.ActivateProviderAsync()).IsError);
            }
            CollectionAssert.AreEqual(before, store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray());
            using var reader = store.Open(key);
            Assert.IsFalse((await reader.ActivateProviderAsync()).IsError);
            var read = await reader.LoadHolonAsync(id, loadChildren: false);
            Assert.IsFalse(read.IsError, read.Message + read.Exception); Assert.AreEqual("private-owner-record", read.Result.Name);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(wrong); }
    }
}
