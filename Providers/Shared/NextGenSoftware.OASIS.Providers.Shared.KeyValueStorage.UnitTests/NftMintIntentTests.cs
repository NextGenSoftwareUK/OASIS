using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class NftMintIntentTests
{
    [TestMethod]
    public void EveryInheritedContractPropertyIsCapturedWithoutDefaultReplacement()
    {
        var input = new MintWeb3NFTRequest();
        var properties = typeof(IMintWeb3NFTRequest).GetInterfaces().Append(typeof(IMintWeb3NFTRequest))
            .SelectMany(type => type.GetProperties()).GroupBy(property => property.Name).Select(group => group.First()).ToArray();
        foreach (var property in properties) property.SetValue(input, Example(property.PropertyType));
        var saved = NftMintIntent.Capture(input);
        foreach (var property in properties)
        {
            var original = property.GetValue(input); var copied = property.GetValue(saved);
            if (original is System.Collections.ICollection collection)
            {
                Assert.AreNotSame(original, copied, property.Name);
                CollectionAssert.AreEqual(collection.Cast<object>().ToArray(), ((System.Collections.ICollection)copied).Cast<object>().ToArray(), property.Name);
            }
            else Assert.AreEqual(original, copied, property.Name);
        }
    }

    [TestMethod]
    public void NestedMutationAndNullsCannotReplaceCapturedIntent()
    {
        var input = new MintWeb3NFTRequest { MetaData = new(StringComparer.OrdinalIgnoreCase) { ["Key"]="original" },
            Tags = new() { "original" }, Image = new byte[] { 1 }, Thumbnail = new byte[] { 2 }, WaitTillNFTMinted=null };
        var saved = NftMintIntent.Capture(input);
        input.MetaData["key"]="changed"; input.Tags[0]="changed"; input.Image[0]=9; input.Thumbnail[0]=9;
        Assert.AreEqual("original", saved.MetaData["KEY"]); Assert.AreEqual("original", saved.Tags[0]);
        Assert.AreEqual((byte)1, saved.Image[0]); Assert.AreEqual((byte)2, saved.Thumbnail[0]); Assert.IsNull(saved.WaitTillNFTMinted);
        var empty = NftMintIntent.Capture(new MintWeb3NFTRequest { MetaData=null, Tags=null, Image=null, Thumbnail=null });
        Assert.IsNull(empty.MetaData); Assert.IsNull(empty.Tags); Assert.IsNull(empty.Image); Assert.IsNull(empty.Thumbnail);
        Assert.ThrowsException<ArgumentNullException>(() => NftMintIntent.Capture(null));
    }

    private static object Example(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum) return Enum.GetValues(type).Cast<object>().Last();
        if (type==typeof(string)) return "captured";
        if (type==typeof(int)) return 7;
        if (type==typeof(bool)) return false;
        if (type==typeof(decimal)) return 1.23m;
        if (type==typeof(Guid)) return Guid.Parse("11111111-2222-3333-4444-555555555555");
        if (type==typeof(DateTime)) return DateTime.UnixEpoch;
        if (type==typeof(byte[])) return new byte[] {1,2};
        if (type==typeof(List<string>)) return new List<string> {"captured"};
        if (type==typeof(Dictionary<string,string>)) return new Dictionary<string,string> {{"key","captured"}};
        throw new InvalidOperationException("Add an explicit capture/test for new mint contract type: "+type);
    }
}
