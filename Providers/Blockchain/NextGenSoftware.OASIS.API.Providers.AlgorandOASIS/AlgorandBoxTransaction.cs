extern alias AlgorandCrypto;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using AlgorandCrypto::Org.BouncyCastle.Crypto.Digests;
using AlgorandCrypto::Org.BouncyCastle.Crypto.Parameters;
using AlgorandCrypto::Org.BouncyCastle.Crypto.Signers;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

internal static class AlgorandBoxTransaction
{
    internal static string ApplicationAddress(ulong applicationId)
    {
        var material = new byte[13];
        "appID"u8.CopyTo(material);
        BinaryPrimitives.WriteUInt64BigEndian(material.AsSpan(5), applicationId);
        var address = Digest(material);
        return Base32([..address, ..Digest(address).AsSpan(28)]);
    }

    internal sealed record Call(byte[][] Arguments, byte[][] Boxes);

    internal static (byte[] Signed, string Id) Sign(
        AlgorandRecordBoxSettings settings, string genesisId, ulong round,
        ulong minimumFee, ulong feePerByte, Call[] calls)
    {
        var key = new Ed25519PrivateKeyParameters(settings.WriterSeed ??
            throw new InvalidOperationException("No writer has been configured."), 0);
        var sender = key.GeneratePublicKey().GetEncoded();
        var address = Base32([..sender, ..Digest(sender).AsSpan(28)]);
        if (address != settings.WriterAddress)
            throw new InvalidOperationException("The signing key does not match the enrolled Algorand writer.");

        var fees = new ulong[calls.Length];
        var group = calls.Length > 1 ? new byte[32] : null;
        for (var index = 0; index < calls.Length; index++)
        {
            var feeBound = Encode(settings, genesisId, round, settings.MaximumFee, sender, calls[index], group);
            // The signed envelope contains its map header, keys and one 64-byte signature.
            var fee = Math.Max(minimumFee, checked(feePerByte * (ulong)(feeBound.Length + 75)));
            if (fee == 0 || fee > settings.MaximumFee)
                throw new InvalidOperationException("Algod's current fee exceeds the configured signing limit.");
            fees[index] = fee;
        }
        if (group != null)
        {
            using var transactionGroup = new MemoryStream();
            transactionGroup.Write("TG"u8);
            transactionGroup.WriteByte(0x81);
            WriteString(transactionGroup, "txlist");
            transactionGroup.WriteByte((byte)(0x90 + calls.Length));
            for (var index = 0; index < calls.Length; index++)
            {
                var raw = Encode(settings, genesisId, round, fees[index], sender, calls[index], null);
                WriteBytes(transactionGroup, Digest([(byte)'T', (byte)'X', ..raw]));
            }
            group = Digest(transactionGroup.ToArray());
        }

        var signer = new Ed25519Signer();
        using var signed = new MemoryStream();
        string? firstId = null;
        for (var index = 0; index < calls.Length; index++)
        {
            var raw = Encode(settings, genesisId, round, fees[index], sender, calls[index], group);
            byte[] message = [(byte)'T', (byte)'X', ..raw];
            firstId ??= Base32(Digest(message));
            signer.Init(true, key);
            signer.BlockUpdate(message, 0, message.Length);
            signed.WriteByte(0x82);
            WriteString(signed, "sig");
            WriteBytes(signed, signer.GenerateSignature());
            WriteString(signed, "txn");
            signed.Write(raw);
        }
        return (signed.ToArray(), firstId ?? throw new ArgumentException("At least one call is required.", nameof(calls)));
    }

    private static byte[] Encode(AlgorandRecordBoxSettings settings, string genesisId, ulong round,
        ulong fee, byte[] sender, Call call, byte[]? group)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(group == null ? (byte)0x8a : (byte)0x8b);
        WriteString(stream, "apaa");
        stream.WriteByte((byte)(0x90 + call.Arguments.Length));
        foreach (var argument in call.Arguments)
            WriteBytes(stream, argument);
        WriteString(stream, "apbx");
        stream.WriteByte((byte)(0x90 + call.Boxes.Length));
        foreach (var box in call.Boxes)
        {
            stream.WriteByte(0x81);
            WriteString(stream, "n");
            WriteBytes(stream, box);
        }
        WriteString(stream, "apid");
        WriteUInt(stream, settings.ApplicationId);
        WriteString(stream, "fee");
        WriteUInt(stream, fee);
        WriteString(stream, "fv");
        WriteUInt(stream, checked(round + 1));
        WriteString(stream, "gen");
        WriteString(stream, genesisId);
        WriteString(stream, "gh");
        WriteBytes(stream, Convert.FromBase64String(settings.GenesisHash));
        if (group != null)
        {
            WriteString(stream, "grp");
            WriteBytes(stream, group);
        }
        WriteString(stream, "lv");
        WriteUInt(stream, checked(round + 30));
        WriteString(stream, "snd");
        WriteBytes(stream, sender);
        WriteString(stream, "type");
        WriteString(stream, "appl");
        return stream.ToArray();
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length <= 31)
            stream.WriteByte((byte)(0xa0 + bytes.Length));
        else
        {
            stream.WriteByte(0xd9);
            stream.WriteByte(checked((byte)bytes.Length));
        }
        stream.Write(bytes);
    }

    private static void WriteBytes(Stream stream, byte[] bytes)
    {
        if (bytes.Length <= byte.MaxValue)
        {
            stream.WriteByte(0xc4);
            stream.WriteByte((byte)bytes.Length);
        }
        else
        {
            stream.WriteByte(0xc5);
            Span<byte> length = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)bytes.Length));
            stream.Write(length);
        }
        stream.Write(bytes);
    }

    private static void WriteUInt(Stream stream, ulong value)
    {
        if (value <= 127)
        {
            stream.WriteByte((byte)value);
            return;
        }
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        var length = value <= byte.MaxValue ? 1 : value <= ushort.MaxValue ? 2 : value <= uint.MaxValue ? 4 : 8;
        stream.WriteByte(length switch { 1 => (byte)0xcc, 2 => (byte)0xcd, 4 => (byte)0xce, _ => (byte)0xcf });
        stream.Write(bytes[(8 - length)..]);
    }

    private static byte[] Digest(byte[] value)
    {
        var digest = new Sha512tDigest(256);
        digest.BlockUpdate(value, 0, value.Length);
        var result = new byte[32];
        digest.DoFinal(result, 0);
        return result;
    }

    private static string Base32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((bytes.Length * 8 + 4) / 5);
        uint bits = 0;
        var available = 0;
        foreach (var value in bytes)
        {
            bits = (bits << 8) | value;
            available += 8;
            while (available >= 5)
            {
                available -= 5;
                output.Append(alphabet[(int)(bits >> available) & 31]);
            }
        }
        if (available != 0)
            output.Append(alphabet[(int)(bits << (5 - available)) & 31]);
        return output.ToString();
    }
}
