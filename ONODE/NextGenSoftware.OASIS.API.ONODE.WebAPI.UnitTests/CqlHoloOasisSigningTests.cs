using System.Security.Cryptography;
using Chaos.NaCl;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlHoloOasisSigningTests
{
    private const string Zome = "oasis";
    private const string ReadFunction = "get_holon_by_id";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanonicalHolonMappingPreservesVisibility(bool isPublic)
    {
        var original = new Holon { Id = Guid.NewGuid(), IsPublic = isPublic };
        var native = DataHelper.ConvertHolonToHoloOASISHolon(original, new HcHolon());
        Assert.Equal(isPublic, native.IsPublic);
        Assert.Equal(isPublic, DataHelper.ConvertHcHolonToHolon(native).IsPublic);
    }

    [Theory]
    [InlineData("function")]
    [InlineData("zome")]
    [InlineData("dna")]
    [InlineData("agent")]
    public async Task RejectsCallsOutsideTheGrantedScope(string mismatch)
    {
        var call = CreateCall();
        var signer = new HoloOasisReadSigner(call.CellIdDnaHash, call.CellIdAgentPubKey,
            Zome, new SigningCredentials());
        switch (mismatch)
        {
            case "function": call.FnName = "create_holon"; break;
            case "zome": call.ZomeName = "other"; break;
            case "dna": call.CellIdDnaHash[3] ^= 1; break;
            case "agent": call.CellIdAgentPubKey[3] ^= 1; break;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await signer.SignZomeCallAsync(call); });
    }

    [Fact]
    public async Task SignsTheGrantedReadWithTheHoloNetWireFormat()
    {
        var call = CreateCall();
        Ed25519.KeyPairFromSeed(out var publicKey, out var privateKey, new byte[32]);
        var credentials = new SigningCredentials
        {
            CapSecret = new byte[64],
            SigningKey = HoloHash(publicKey, [0x84, 0x20, 0x24]),
            KeyPair = new KeyPair { PublicKey = publicKey, PrivateKey = privateKey }
        };
        var signer = new HoloOasisReadSigner(call.CellIdDnaHash, call.CellIdAgentPubKey, Zome, credentials);

        var signed = await signer.SignZomeCallAsync(call);

        Assert.True(Ed25519.Verify(signed.signature, SHA512.HashData(signed.bytes), publicKey));
    }

    private static ZomeCallToSign CreateCall() => new()
    {
        CellIdDnaHash = HoloHash(new byte[32], [0x84, 0x2d, 0x24]),
        CellIdAgentPubKey = HoloHash(new byte[32], [0x84, 0x20, 0x24]),
        ZomeName = Zome,
        FnName = ReadFunction,
        Payload = [0xc0],
        Nonce = new byte[32],
        ExpiresAt = 1
    };

    private static byte[] HoloHash(byte[] body, byte[] prefix)
    {
        var hash = new byte[39];
        prefix.CopyTo(hash, 0);
        body.CopyTo(hash, prefix.Length);
        return hash;
    }
}
