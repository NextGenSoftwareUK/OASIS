using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nethereum.Signer;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.zkPassOASIS;

public sealed record ZkPassEvmProof(
    [property: JsonPropertyName("taskId")] string TaskId,
    [property: JsonPropertyName("uHash")] string UserHash,
    [property: JsonPropertyName("publicFieldsHash")] string PublicFieldsHash,
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("allocatorAddress")] string AllocatorAddress,
    [property: JsonPropertyName("allocatorSignature")] string AllocatorSignature,
    [property: JsonPropertyName("validatorAddress")] string ValidatorAddress,
    [property: JsonPropertyName("validatorSignature")] string ValidatorSignature);

/// <summary>Verifies recipient-bound zkPass TransGate EVM attestations.</summary>
public sealed class zkPassOASIS : OASISProvider
{
    private const string OfficialAllocator = "0x19a567b3b212a5b35bA0E3B600FbEd5c2eE9083d";
    private static readonly BigInteger HalfCurveOrder = new(
        Convert.FromHexString("7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF5D576E7357A4501DDFE92F46681B20A0"),
        isUnsigned: true, isBigEndian: true);
    private readonly string _trustedAllocator;

    public zkPassOASIS(string trustedAllocator = OfficialAllocator)
    {
        _trustedAllocator = Address(trustedAllocator);
        ProviderName = nameof(zkPassOASIS);
        ProviderDescription = "zkPass TransGate recipient-bound EVM attestation verifier";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.zkPassOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        IsProviderActivated = true;
        return new OASISResult<bool> { Result = true };
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => Task.FromResult(ActivateProvider());

    public override OASISResult<bool> DeActivateProvider()
    {
        IsProviderActivated = false;
        return new OASISResult<bool> { Result = true };
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync() => Task.FromResult(DeActivateProvider());

    /// <summary>Checks signatures for the server-selected schema and expected recipient.</summary>
    public OASISResult<bool> Verify(ZkPassEvmProof proof, string expectedSchemaId, string expectedRecipient)
    {
        if (!IsProviderActivated)
            return Failure("Activate the zkPass verifier before checking an attestation.");

        try
        {
            ArgumentNullException.ThrowIfNull(proof);
            var recipient = Address(expectedRecipient);
            var validator = Address(proof.ValidatorAddress);
            if (!SameAddress(recipient, Address(proof.Recipient)) ||
                !SameAddress(_trustedAllocator, Address(proof.AllocatorAddress)))
                return Failure("The zkPass recipient or allocator does not match the verification policy.");

            var task = TextWord(proof.TaskId);
            var schema = TextWord(expectedSchemaId);
            var allocatorMessage = task.Concat(schema).Concat(AddressWord(validator)).ToArray();
            if (!SameAddress(_trustedAllocator, Recover(allocatorMessage, proof.AllocatorSignature)))
                return Failure("The zkPass allocator signature is invalid.");

            var validatorMessage = task.Concat(schema)
                .Concat(Hex(proof.UserHash, 32))
                .Concat(Hex(proof.PublicFieldsHash, 32))
                .Concat(AddressWord(recipient)).ToArray();
            if (!SameAddress(validator, Recover(validatorMessage, proof.ValidatorSignature)))
                return Failure("The zkPass validator signature is invalid.");

            return new OASISResult<bool> { Result = true };
        }
        catch (ArgumentException)
        {
            return Failure("The zkPass attestation contains malformed verification fields.");
        }
        catch (FormatException)
        {
            return Failure("The zkPass attestation contains invalid hexadecimal fields.");
        }
    }

    private static string Recover(byte[] encodedMessage, string signature)
    {
        var bytes = Hex(signature, 65);
        var s = new BigInteger(bytes.AsSpan(32, 32), isUnsigned: true, isBigEndian: true);
        if (bytes[64] is not (27 or 28) || s.IsZero || s > HalfCurveOrder)
            throw new ArgumentException("Non-canonical Ethereum signature.");

        var hash = Sha3Keccack.Current.CalculateHash(encodedMessage);
        return new EthereumMessageSigner().EcRecover(hash, signature);
    }

    private static byte[] TextWord(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 32 || value.Any(c => c < '!' || c > '~'))
            throw new ArgumentException("Expected a printable TransGate identifier of at most 32 ASCII bytes.");

        var word = new byte[32];
        Encoding.ASCII.GetBytes(value).CopyTo(word, 0);
        return word;
    }

    private static byte[] AddressWord(string value)
    {
        var word = new byte[32];
        Hex(value, 20).CopyTo(word, 12);
        return word;
    }

    private static string Address(string value)
    {
        if (Hex(value, 20).All(b => b == 0))
            throw new ArgumentException("The zero address cannot sign or receive a zkPass attestation.");
        return value;
    }

    private static byte[] Hex(string value, int bytes)
    {
        if (value is null || value.Length != 2 + 2 * bytes || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Unexpected length for a hexadecimal verification field.");
        return Convert.FromHexString(value.AsSpan(2));
    }

    private static bool SameAddress(string expected, string actual) =>
        string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);

    private static OASISResult<bool> Failure(string message) =>
        new() { IsError = true, Result = false, Message = message };
}
