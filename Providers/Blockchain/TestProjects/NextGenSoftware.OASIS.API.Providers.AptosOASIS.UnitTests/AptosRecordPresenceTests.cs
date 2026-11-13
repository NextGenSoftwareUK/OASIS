using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests;

[TestClass]
public class AptosRecordPresenceTests
{
    private static readonly MethodInfo Decoder = typeof(AptosOASIS).GetMethod("DecodeRecordPresence", BindingFlags.NonPublic | BindingFlags.Static)!;

    [TestMethod]
    public void OnlyOneNativeBooleanCanEstablishPresenceOrAbsence()
    {
        foreach (var value in new[] { false, true })
        {
            Assert.AreEqual(value, Decoder.Invoke(null, new object[] { new object[] { value } }));
            Assert.AreEqual(value, Decoder.Invoke(null, new object[] { new object[] { new JValue(value) } }));
        }
        foreach (var values in new object[][] { Array.Empty<object>(), new object[] { false, true },
            new object[] { "false" }, new object[] { 0 }, new object[] { JValue.CreateNull() }, new object[] { new JArray() } })
        {
            var error = Assert.ThrowsException<TargetInvocationException>(() => Decoder.Invoke(null, new object[] { values }));
            Assert.IsInstanceOfType(error.InnerException, typeof(InvalidDataException));
        }
    }
}
