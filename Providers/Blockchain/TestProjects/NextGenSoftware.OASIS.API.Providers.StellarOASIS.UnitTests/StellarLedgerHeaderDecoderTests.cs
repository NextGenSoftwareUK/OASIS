using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS.UnitTests;

[TestClass]
public sealed class StellarLedgerHeaderDecoderTests
{
    // Retained native Horizon headers and independently encoded Python struct vectors.
    private const string NativeJson = """[{"hash":"c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce","prev_hash":"4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768","sequence":65247993,"header_xdr":"AAAAHUzHXxlsMzvMMl/JDRpC6jkglRPnrRaWwYZlc9kKdNdoTjCNVOqHMrsnmJcg+6EgreyCuLzPXHltEH+IHs+MjOcAAAAAaucH9QAAAAAAAAABAAAAAF60jCXLtH/CB/4KI+zln4jBc1nQ2p1AsXCUSYH47yvBAAAAQPxzrzJSy0nzXXUEARyz7KU3gQ0FB4fXpFo1bqlKhl2aD4Ky2/j0ZQ+GYJFO+u5+70JMLLmNoE3cZFqJLASI4QrB4X9kxbbO4mML52LWT5bR/MJRhcrswSZJ39UEEgG+NnU7Qirq8CWbNLxKij5BKHr4mR9ZGlFjsuyiPtSqWwIYA+Oa+Q6iHrPseVthAABjr1Pzg0MAAAEWAAAAAG8bf/IAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29},{"hash":"4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768","prev_hash":"d898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9","sequence":65247992,"header_xdr":"AAAAHdiYuJQcTLLQwD1c9frG+xi1KIgQL9syg8Jrd5oDkFTZhE89mVOK81qOlgWKO587jbtvRoUpNqyd8R+g0/r0My4AAAAAaucH8AAAAAAAAAABAAAAAGLK4AK2p9ZUqsvAkvnk6pa4sXh8nzinkjgQOMq3wyDTAAAAQFu53TpZ/OQugUzuDeF45U5EPShWx8MrMoueLtKXRiwu666B/kaaQ8heBlJFsm53Fd46/M73CmmKUzEKm4lbfAZjJYliEGkk1W4ZXHqQ3A5muLH3U3eB+O4sLNuQdU0qyImKRNztpYy/KL+HxBqAoXFsoZLoiEQLjlROT4iEV2lBA+Oa+A6iHrPseVthAABjr1Pu/U4AAAEWAAAAAG8bf+gAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29},{"hash":"d898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9","prev_hash":"f261d3d88e7c45caf1c62e54b260a26dc83132c3e95137a589888ab7c557c5b5","sequence":65247991,"header_xdr":"AAAAHfJh09iOfEXK8cYuVLJgom3IMTLD6VE3pYmIirfFV8W1VcDGHXGP+uJnqNLEr83n+15eRKhwoQ7KJGJu+MHfkqIAAAAAaucH6wAAAAAAAAABAAAAAFaWt2CucvNouAYniCiCbacMYpTGlVK3TqKyoNF3RcOeAAAAQEhkphzTWe6ftm0BB4jfgsQG9bj9DximOhRmJdMzaDOJP/spgwRHFj6hHv3YpyykcM4pnHt6qk8XlROOBV2I4Au3W8bOo5MYRp08PFyzQrj281wQNm23idaNkIhcUOnUl1iBVoGJpvKWiRx9T8dqPapC77lzpPapv1JQGpriesGTA+Oa9w6iHrPseVthAABjr1PldnkAAAEWAAAAAG8bf+IAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29}]""";
    private const string SyntheticJson = """[{"hash":"27b87cb3be77de6b2244c6c02740c2eac6fa8bf72ce7d211ec1d425e9a8c7402","header_xdr":"AAAAHRERERERERERERERERERERERERERERERERERERERERERIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAAgxJul41QAAAAIAAAABeAAAAAAAAIAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7fH1+fwAAAAB3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d4iIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAHiQH//////////gAAAAAAAAAAAAAAA////////////////AAAAyAAAA+gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwAAAAEAAAAHAAAAAA==","offsets":{"upgradeCount":76,"upgradeLength":80,"upgradePadding":85,"upgrade2Length":88,"upgrade2Padding":220,"valueType":220,"headerExtension":460,"flags":464,"innerExtension":468}},{"hash":"c9d3b98a9cf3a7ed2d77133aab06a64ce75659a38ff5ee00b19ba31d973262e0","header_xdr":"AAAAHRERERERERERERERERERERERERERERERERERERERERERIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAAgxJul41QAAAAIAAAABeAAAAAAAAIAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7fH1+fwAAAAEAAAAAMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMAAAABeAAAAHd3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3iIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIgAAeJAf/////////+AAAAAAAAAAAAAAAD///////////////8AAADIAAAD6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQECAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAAAAAQAAAAcAAAAA","offsets":{"upgradeCount":76,"upgradeLength":80,"upgradePadding":85,"upgrade2Length":88,"upgrade2Padding":220,"valueType":220,"keyType":224,"signatureLength":260,"signaturePadding":265,"headerExtension":504,"flags":508,"innerExtension":512}},{"hash":"a5a9054ae74989b1e91561a40a35041f4bdde214108e6ce23436c3b483c780d0","header_xdr":"AAAAHRERERERERERERERERERERERERERERERERERERERERERIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAAgxJul41QAAAAIAAAABeAAAAAAAAIAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7fH1+fwAAAAJVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVWZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmAAAAHQAAAAAzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMwAAAEBEREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREd3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3eIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiAAB4kB//////////4AAAAAAAAAAAAAAAP///////////////wAAAMgAAAPoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMAAAABAAAABwAAAAA=","offsets":{"upgradeCount":76,"upgradeLength":80,"upgradePadding":85,"upgrade2Length":88,"upgrade2Padding":220,"valueType":220,"keyType":292,"signatureLength":328,"signaturePadding":396,"headerExtension":632,"flags":636,"innerExtension":640}},{"hash":"1f7de2e913f68812ee8965c9c64d947a65aea7274f100f91b4cfe14c62074f19","header_xdr":"AAAAHRERERERERERERERERERERERERERERERERERERERERERIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAAgxJul41QAAAAIAAAABeAAAAAAAAIAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7fH1+fwAAAAMAIAAAAAAAgwAAAAAzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMwAAAEBEREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREREd3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3eIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiAAB4kB//////////4AAAAAAAAAAAAAAAP///////////////wAAAMgAAAPoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMAAAABAAAABwAAAAA=","offsets":{"upgradeCount":76,"upgradeLength":80,"upgradePadding":85,"upgrade2Length":88,"upgrade2Padding":220,"valueType":220,"milliseconds":224,"keyType":232,"signatureLength":268,"signaturePadding":336,"headerExtension":572,"flags":576,"innerExtension":580}},{"hash":"0ba776b3d56304af98684e9eb453f2e57b4ec920044832d95eafc004cb9ae1a4","header_xdr":"AAAAHRERERERERERERERERERERERERERERERERERERERERERIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAAgxJul41QAAAAIAAAABeAAAAAAAAIAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7fH1+fwAAAAQAIAAAAAAAg1VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmZmYAAAAdAAAAADMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzAAAAQERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERERER3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d3d4iIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIiIAAHiQH//////////gAAAAAAAAAAAAAAA////////////////AAAAyAAAA+gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwAAAAEAAAAHAAAAAA==","offsets":{"upgradeCount":76,"upgradeLength":80,"upgradePadding":85,"upgrade2Length":88,"upgrade2Padding":220,"valueType":220,"milliseconds":224,"keyType":300,"signatureLength":336,"signaturePadding":404,"headerExtension":640,"flags":644,"innerExtension":648}}]""";

    private static StellarLedgerHeaderDecoder.Header Decode(byte[] bytes, uint sequence=123456)
        => StellarLedgerHeaderDecoder.Decode(Convert.ToBase64String(bytes),
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),sequence);

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void RetainedNativeHeadersBindSequenceAndParentToContent(int index)
    {
        using var json=JsonDocument.Parse(NativeJson);
        var row=json.RootElement[index];
        var header=StellarLedgerHeaderDecoder.Decode(row.GetProperty("header_xdr").GetString()!,
            row.GetProperty("hash").GetString()!,row.GetProperty("sequence").GetUInt32());
        Assert.AreEqual(29u,header.ProtocolVersion);
        Assert.AreEqual(row.GetProperty("prev_hash").GetString(),header.ParentHash);
        Assert.AreEqual(row.GetProperty("sequence").GetUInt32(),header.Sequence);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)]
    public void KnownUnionsDecodeWholeHeaderWithExactIntegerPrecision(int kind)
    {
        using var json=JsonDocument.Parse(SyntheticJson);
        var row=json.RootElement[kind];
        var header=StellarLedgerHeaderDecoder.Decode(row.GetProperty("header_xdr").GetString()!,row.GetProperty("hash").GetString()!,123456);
        Assert.AreEqual((uint)kind,header.ValueType);
        Assert.AreEqual(long.MaxValue,header.TotalStroops);
        Assert.AreEqual(long.MinValue,header.FeePool);
        Assert.AreEqual(ulong.MaxValue,header.IdPool);
        Assert.AreEqual(uint.MaxValue,header.BaseFee);
        Assert.AreEqual(7u,header.Flags);
        Assert.AreEqual(new string('1',64),header.ParentHash);
        Assert.AreEqual(kind>=3 ? (ulong?)9007199254741123UL : null,header.CloseTimeMilliseconds);
    }

    [DataTestMethod]
    [DataRow("upgradeCount",0,7u)]
    [DataRow("upgradeLength",0,129u)]
    [DataRow("signatureLength",1,65u)]
    [DataRow("valueType",0,5u)]
    [DataRow("keyType",1,1u)]
    [DataRow("headerExtension",0,2u)]
    [DataRow("innerExtension",0,1u)]
    [DataRow("flags",0,8u)]
    public void InvalidStructureRejectedEvenWithMatchingNativeHash(string field,int vector,uint replacement)
    {
        using var json=JsonDocument.Parse(SyntheticJson);
        var row=json.RootElement[vector];
        var bytes=Convert.FromBase64String(row.GetProperty("header_xdr").GetString()!);
        int offset=row.GetProperty("offsets").GetProperty(field).GetInt32();
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset),replacement);
        Assert.ThrowsException<InvalidDataException>(()=>Decode(bytes));
    }

    [TestMethod]
    public void NonzeroOpaquePaddingRejectedWithMatchingHash()
    {
        using var json=JsonDocument.Parse(SyntheticJson);
        var row=json.RootElement[0];
        var bytes=Convert.FromBase64String(row.GetProperty("header_xdr").GetString()!);
        bytes[row.GetProperty("offsets").GetProperty("upgradePadding").GetInt32()]=1;
        Assert.ThrowsException<InvalidDataException>(()=>Decode(bytes));
    }

    [TestMethod]
    public void MillisecondValueMustAgreeWithSeconds()
    {
        using var json=JsonDocument.Parse(SyntheticJson);
        var row=json.RootElement[3];
        var bytes=Convert.FromBase64String(row.GetProperty("header_xdr").GetString()!);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(row.GetProperty("offsets").GetProperty("milliseconds").GetInt32()),1);
        Assert.ThrowsException<InvalidDataException>(()=>Decode(bytes));
    }

    [TestMethod]
    public void EveryNativeTruncationAndTrailingDataRejected()
    {
        using var json=JsonDocument.Parse(NativeJson);
        var row=json.RootElement[0];
        var bytes=Convert.FromBase64String(row.GetProperty("header_xdr").GetString()!);
        uint sequence=row.GetProperty("sequence").GetUInt32();
        for(int size=0;size<bytes.Length;size++)
        {
            var truncated=bytes[..size];
            Assert.ThrowsException<InvalidDataException>(()=>Decode(truncated,sequence),"Truncation at "+size);
        }
        Assert.ThrowsException<InvalidDataException>(()=>Decode(bytes.Concat(new byte[4]).ToArray(),sequence));
    }

    [TestMethod]
    public void WrongIdentitySequenceAndNoncanonicalBase64Rejected()
    {
        using var json=JsonDocument.Parse(NativeJson);
        var row=json.RootElement[0];string xdr=row.GetProperty("header_xdr").GetString()!;
        string hash=row.GetProperty("hash").GetString()!;uint sequence=row.GetProperty("sequence").GetUInt32();
        Assert.ThrowsException<InvalidDataException>(()=>StellarLedgerHeaderDecoder.Decode(xdr,new string('0',64),sequence));
        Assert.ThrowsException<InvalidDataException>(()=>StellarLedgerHeaderDecoder.Decode(xdr,hash,sequence+1));
        Assert.ThrowsException<InvalidDataException>(()=>StellarLedgerHeaderDecoder.Decode(xdr+"\n",hash,sequence));
        Assert.ThrowsException<InvalidDataException>(()=>Decode(new byte[4097],sequence));
    }
}
