using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

// Boundary tests only: captures the DTO passed to storage, not native-chain execution.
[TestClass]
public class SolanaAvatarUpdateTests
{
    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task UpdatesUseCurrentPayloadAndRejectMismatchedReferences(bool detail, bool mismatch)
    {
        var account = new Account();
        var provider = new SolanaOASIS("http://127.0.0.1:8899", account.PrivateKey.Key, account.PublicKey.Key);
        var identity = Guid.NewGuid();
        var existingId = mismatch ? Guid.NewGuid() : identity;
        var repository = new CapturingRepository { Existing = detail
            ? new SolanaAvatarDetailDto { Id = existingId, Version = 5, Karma = 1 }
            : new SolanaAvatarDto { Id = existingId, Version = 5, Email = "old@example.invalid" } };
        typeof(SolanaOASIS).GetField("_solanaRepository", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, repository);
        if (detail)
        {
            var avatar = new AvatarDetail { Id = identity, Karma = 999, XP = 321, Version = 1 };
            avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS] = "old-signature";
            var result = await provider.SaveAvatarDetailAsync(avatar);
            Assert.AreEqual(mismatch, result.IsError);
            if (!mismatch)
            {
                var submitted = (SolanaAvatarDetailDto)repository.Updated!;
                Assert.AreEqual(avatar.Karma, submitted.Karma);
                Assert.AreEqual(avatar.XP, submitted.XP);
                Assert.AreEqual("new-signature", avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
            }
        }
        else
        {
            var avatar = new Avatar { Id = identity, Email = "new@example.invalid", Version = 1 };
            avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS] = "old-signature";
            var result = await provider.SaveAvatarAsync(avatar);
            Assert.AreEqual(mismatch, result.IsError);
            if (!mismatch)
                Assert.AreEqual(avatar.Email, ((SolanaAvatarDto)repository.Updated!).Email);
        }
        if (mismatch) Assert.IsNull(repository.Updated);
        else
        {
            Assert.AreEqual(identity, repository.Updated!.Id);
            Assert.AreEqual(5, repository.Updated.Version, "repository owns incrementing persisted version");
        }
    }

    private sealed class CapturingRepository : ISolanaRepository
    {
        public SolanaBaseDto Existing = null!;
        public SolanaBaseDto? Updated;
        public Task<T> GetAsync<T>(string reference) where T : SolanaBaseDto, new() => Task.FromResult((T)Existing);
        public Task<string> UpdateAsync<T>(T entity) where T : SolanaBaseDto, new() { Updated = entity; return Task.FromResult("new-signature"); }
        public Task<string> CreateAsync<T>(T entity) where T : SolanaBaseDto, new() => throw new InvalidOperationException("Unexpected create");
        public Task<bool> DeleteAsync(string reference) => throw new InvalidOperationException("Unexpected delete");
    }
}
