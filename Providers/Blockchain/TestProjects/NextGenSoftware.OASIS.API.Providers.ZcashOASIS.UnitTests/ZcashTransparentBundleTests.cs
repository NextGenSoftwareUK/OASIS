using System;
using System.Buffers.Binary;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests;

[TestClass]
public sealed class ZcashTransparentBundleTests
{
    private static readonly byte[] Script = [0x6a, 0x03, 0xAA, 0xBB, 0xCC];

    // Builds an unsigned transaction: header fields, one input with an empty scriptSig, one P2PKH output, then a tail
    // standing in for the shielded bundles that must be preserved byte for byte.
    private static byte[] Transaction(uint header, int headerFieldBytes, byte[] tail)
    {
        var prefix = new byte[headerFieldBytes];
        BinaryPrimitives.WriteUInt32LittleEndian(prefix, header);
        for (var i = 4; i < headerFieldBytes; i++) prefix[i] = (byte)(0x10 + i);
        byte[] input = [.. Enumerable.Repeat((byte)0x11, 32), 0x01, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];
        byte[] p2pkh = [0x76, 0xA9, 0x14, .. Enumerable.Repeat((byte)0x22, 20), 0x88, 0xAC];
        byte[] output = [0x40, 0x42, 0x0F, 0, 0, 0, 0, 0, (byte)p2pkh.Length, .. p2pkh];
        return [.. prefix, 0x01, .. input, 0x01, .. output, .. tail];
    }

    private static void AssertAppended(byte[] original, int transparentStart, byte[] tail)
    {
        var patched = Convert.FromHexString(ZcashTransparentBundle.AppendNullDataOutput(Convert.ToHexString(original), Script));
        var outputsAt = transparentStart + 1 + 41;
        Assert.IsTrue(patched.AsSpan(0, outputsAt).SequenceEqual(original.AsSpan(0, outputsAt)), "header and inputs unchanged");
        Assert.AreEqual(2, patched[outputsAt], "output count incremented");
        var existing = original.AsSpan(outputsAt + 1, 8 + 1 + 25);
        Assert.IsTrue(patched.AsSpan(outputsAt + 1, existing.Length).SequenceEqual(existing), "existing output kept first");
        var appendedAt = outputsAt + 1 + existing.Length;
        Assert.IsTrue(patched.AsSpan(appendedAt, 8).ToArray().All(b => b == 0), "zero-value output");
        Assert.AreEqual(Script.Length, patched[appendedAt + 8]);
        Assert.IsTrue(patched.AsSpan(appendedAt + 9, Script.Length).SequenceEqual(Script), "OP_RETURN script");
        Assert.IsTrue(patched.AsSpan(appendedAt + 9 + Script.Length).SequenceEqual(tail), "shielded tail preserved");
        Assert.AreEqual(original.Length + 8 + 1 + Script.Length, patched.Length);
    }

    [TestMethod]
    public void AppendsToAVersion5Transaction()
    {
        byte[] tail = [0x05, 0x06, 0x07, 0x00, 0x00, 0x00];
        AssertAppended(Transaction(0x8000_0005, 20, tail), 20, tail);
    }

    [TestMethod]
    public void AppendsToAVersion4SaplingTransaction()
    {
        byte[] tail = [0x00, 0x00, 0x00, 0x00, 0x99, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        AssertAppended(Transaction(0x8000_0004, 8, tail), 8, tail);
    }

    [TestMethod]
    public void AppendsToALegacyTransaction()
    {
        byte[] tail = [0x00, 0x00, 0x00, 0x00];
        AssertAppended(Transaction(0x0000_0001, 4, tail), 4, tail);
    }

    [TestMethod]
    public void RejectsANonNullDataScript()
    {
        var hex = Convert.ToHexString(Transaction(0x8000_0005, 20, [0x00]));
        Assert.ThrowsException<ArgumentException>(() => ZcashTransparentBundle.AppendNullDataOutput(hex, [0x76, 0xA9]));
    }

    [TestMethod]
    public void RejectsATruncatedTransaction()
    {
        var full = Transaction(0x8000_0005, 20, []);
        var hex = Convert.ToHexString(full.AsSpan(0, full.Length - 10));
        Assert.ThrowsException<System.IO.InvalidDataException>(() => ZcashTransparentBundle.AppendNullDataOutput(hex, Script));
    }
}
