using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS.UnitTests
{
    /// <summary>
    /// Records saved through EclipseOASIS belong to Eclipse: the composed SVM storage engine must stamp and read
    /// <see cref="ProviderType.EclipseOASIS"/> in ProviderUniqueStorageKey, never SolanaOASIS. The storage
    /// repository boundary is captured so no RPC endpoint is contacted.
    /// </summary>
    [TestClass]
    public class EclipseOASISStorageKeyTests
    {
        private static (EclipseOASIS Provider, CapturingRepository Repository) Compose()
        {
            var account = new Account();
            var provider = new EclipseOASIS("http://127.0.0.1:1/", account.PrivateKey.Key, account.PublicKey.Key);
            var repository = new CapturingRepository();
            var storage = (SolanaOASIS)typeof(EclipseOASIS).GetField("_solana", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
            typeof(SolanaOASIS).GetField("_solanaRepository", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(storage, repository);
            typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(storage, true);
            return (provider, repository);
        }

        [DataTestMethod]
        [DataRow(false)][DataRow(true)]
        public async Task NewHolonSavedThroughEclipseCarriesTheEclipseKey(bool synchronous)
        {
            var (provider, repository) = Compose();
            var holon = new Holon { Id = Guid.NewGuid(), Name = "eclipse-key" };
            var saved = synchronous ? provider.SaveHolon(holon, saveChildren: false) : await provider.SaveHolonAsync(holon, saveChildren: false);

            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.AreEqual(1, repository.Creates);
            Assert.AreEqual("created-signature", saved.Result.ProviderUniqueStorageKey[ProviderType.EclipseOASIS]);
            Assert.IsFalse(saved.Result.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        }

        [DataTestMethod]
        [DataRow(false)][DataRow(true)]
        public async Task AvatarUpdateThroughEclipseReadsAndStampsTheEclipseKey(bool synchronous)
        {
            var (provider, repository) = Compose();
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "eclipse", Email = "eclipse@example.invalid" };
            avatar.ProviderUniqueStorageKey[ProviderType.EclipseOASIS] = "old-signature";
            repository.Existing = new SolanaAvatarDto { Id = avatar.Id, Version = 1 };

            var saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);

            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.AreEqual("old-signature", repository.ReadReference);
            Assert.AreEqual(1, repository.Updates);
            Assert.AreEqual(0, repository.Creates);
            Assert.AreEqual("updated-signature", avatar.ProviderUniqueStorageKey[ProviderType.EclipseOASIS]);
            Assert.IsFalse(avatar.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        }

        private sealed class CapturingRepository : ISolanaRepository
        {
            public SolanaBaseDto? Existing;
            public string? ReadReference;
            public int Creates, Updates;
            public Task<T> GetAsync<T>(string reference) where T : SolanaBaseDto, new()
            {
                ReadReference = reference;
                return Task.FromResult((T)(Existing ?? throw new InvalidOperationException("Unexpected read")));
            }
            public Task<IReadOnlyList<(T Record, string Signature)>> GetAllAsync<T>(int version = 0) where T : SolanaBaseDto, new()
                => Task.FromResult<IReadOnlyList<(T Record, string Signature)>>(Array.Empty<(T, string)>());
            public Task<string> CreateAsync<T>(T entity) where T : SolanaBaseDto, new() { Creates++; return Task.FromResult("created-signature"); }
            public Task<string> UpdateAsync<T>(T entity) where T : SolanaBaseDto, new() { Updates++; return Task.FromResult("updated-signature"); }
            public Task<bool> DeleteAsync(string reference) => throw new InvalidOperationException("Unexpected delete");
            public Task<string> CreateTombstoneAsync(string reference) => throw new InvalidOperationException("Unexpected tombstone");
        }
    }
}
