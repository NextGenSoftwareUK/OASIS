using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests;

[TestClass]
public class AptosRecordPayloadTests
{
    private static readonly MethodInfo Decoder = typeof(AptosOASIS)
        .GetMethod("DecodeRecordPayload", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(Holon));

    [TestMethod]
    public void OfficialSdkVectorRepresentationsDecodeTheSameCanonicalRecord()
    {
        var original = new Holon { Id = Guid.NewGuid(), Name = "Espresso" };
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(original));
        foreach (var value in new object[] { bytes, "0x" + Convert.ToHexString(bytes), new JArray(bytes.Select(b => (int)b)) })
        {
            var row = (Holon)Decoder.Invoke(null, new object[] { new[] { value } })!;
            Assert.AreEqual(original.Id, row.Id);
            Assert.AreEqual(original.Name, row.Name);
        }
    }

    [TestMethod]
    public void MalformedPresentRecordsNeverBecomeAbsenceOrReplacementCharacters()
    {
        foreach (var values in new object[][] { Array.Empty<object>(), new object[] { new byte[0] },
            new object[] { true }, new object[] { new byte[] { 0xff } },
            new object[] { Encoding.UTF8.GetBytes("null") }, new object[] { "0xnothex" },
            new object[] { new byte[] { 1 }, new byte[] { 2 } } })
        {
            var error = Assert.ThrowsException<TargetInvocationException>(() => Decoder.Invoke(null, new object[] { values }));
            Assert.IsNotNull(error.InnerException);
        }
    }
}
