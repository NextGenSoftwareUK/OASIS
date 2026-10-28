using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class RedisStateComparisonTests
{
    [DataTestMethod]
    [DataRow("")][DataRow("value")][DataRow("🌳\0é")]
    public void ObservationPreservesExactUtf8State(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var revision = RedisStateComparison.Observe("key:🌳", bytes);
        CollectionAssert.AreEqual(bytes, RedisStateComparison.Expected("key:🌳", revision));
        Assert.AreEqual("redis-state-v1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("key:🌳")) + ":" + Convert.ToBase64String(bytes), revision);
    }

    [TestMethod]
    public void ObservationCannotBeReusedForAnotherKey()
    {
        var revision = RedisStateComparison.Observe("a", new byte[] { 1 });
        Assert.ThrowsException<ArgumentException>(() => RedisStateComparison.Expected("b", revision));
    }

    [TestMethod]
    public void AlternateBase64EncodingIsRejected()
    {
        var revision = RedisStateComparison.Observe("a", new byte[] { 1 });
        Assert.ThrowsException<ArgumentException>(() => RedisStateComparison.Expected("a", revision + "\n"));
    }

    [TestMethod]
    public void RedisGlobMetacharactersAreLiteralInPrefixQueries()
        => Assert.AreEqual(@"oasis/\[a\]\?\*\\*", RedisStateComparison.PrefixPattern(@"oasis/[a]?*\"));
}
