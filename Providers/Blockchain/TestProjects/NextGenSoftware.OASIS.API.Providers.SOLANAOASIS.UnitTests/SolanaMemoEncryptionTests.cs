using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaMemoEncryptionTests
{
    private static string Encode(SolanaRepository repository, SolanaAvatarDto dto) => (string)typeof(SolanaRepository)
        .GetMethod("EncodeEntity", BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(SolanaAvatarDto))
        .Invoke(repository, new object[] { dto })!;
    private static string Decode(SolanaRepository repository, string envelope) => (string)typeof(SolanaRepository)
        .GetMethod("DecodeEntity", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(repository, new object[] { Encoding.UTF8.GetBytes(envelope) })!;

    [TestMethod]
    public void MemoCodecEncryptsWholeDtoAndReopensWithSamePersistedKey()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var writer = new SolanaRepository(null!, null!, () => (byte[])key.Clone());
        var dto = new SolanaAvatarDto { Id = Guid.NewGuid(), Email = "private@example.invalid", Password = "synthetic-password" };
        var envelope = Encode(writer, dto);
        StringAssert.StartsWith(envelope, "oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(envelope.Contains(dto.Email));
        Assert.IsFalse(envelope.Contains(dto.Password));
        Assert.AreNotEqual(envelope, Encode(writer, dto));
        var reader = new SolanaRepository(null!, null!, () => (byte[])key.Clone());
        var json = Decode(reader, envelope);
        StringAssert.Contains(json, dto.Email);
        StringAssert.Contains(json, dto.Password);
        var wrongReader = new SolanaRepository(null!, null!, () => RandomNumberGenerator.GetBytes(32));
        Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(() => Decode(wrongReader, envelope)).InnerException,
            typeof(CryptographicException));
    }

    [TestMethod]
    public void MemoCodecRejectsPlaintextAndModifiedCiphertext()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var repository = new SolanaRepository(null!, null!, () => (byte[])key.Clone());
        Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(() => Decode(repository, "{\"Email\":\"plain\"}")).InnerException,
            typeof(CryptographicException));
        var envelope = Encode(repository, new SolanaAvatarDto { Id = Guid.NewGuid() });
        var separator = envelope.IndexOf(':') + 1;
        var payload = Convert.FromBase64String(envelope[separator..]);
        payload[^1] ^= 1;
        Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(() => Decode(repository,
            envelope[..separator] + Convert.ToBase64String(payload))).InnerException, typeof(CryptographicException));
    }

    [TestMethod]
    public async Task MissingStorageKeyFailsBeforeRpcAccess()
    {
        var repository = new SolanaRepository(null!, null!, () => throw new InvalidOperationException("Storage key unavailable"));
        var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => repository.CreateAsync(new SolanaAvatarDto {Id=Guid.NewGuid()}));
        Assert.AreEqual("Storage key unavailable", error.Message);
    }
}
