using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearSdkEncodingTests
{
    [TestMethod]
    public async Task MalformedUtf8CannotBecomeAReplacementCharacterInSuccessfulSdkData()
    {
        var service=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var fixture=Path.Combine(AppContext.BaseDirectory,"Fixtures","utf8-response.mjs");
        var instance=Activator.CreateInstance(service,new object[]{"http://127.0.0.1:1","unused","unused","","",
            Environment.GetEnvironmentVariable("OASIS_NEAR_NODE")??"node",fixture})!;
        var operation=(Task<JsonElement>)service.GetMethod("ViewAsync")!.Invoke(instance,new object[]{"malformed",new{value="unused"},"unused"})!;
        await Assert.ThrowsExceptionAsync<DecoderFallbackException>(async()=>await operation);
    }

    [DataTestMethod]
    [DataRow("plain ASCII")][DataRow("Café")][DataRow("Tree 🌳")][DataRow("東京")]
    public async Task SdkProtocolIsUtf8RegardlessOfHostConsoleCodePage(string value)
    {
        var previous=Console.OutputEncoding;
        try
        {
            Console.OutputEncoding=Encoding.Latin1;
            var service=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var fixture=Path.Combine(AppContext.BaseDirectory,"Fixtures","utf8-response.mjs");
            Assert.IsTrue(File.Exists(fixture));
            var instance=Activator.CreateInstance(service,new object[]{"http://127.0.0.1:1","unused","unused","","",
                Environment.GetEnvironmentVariable("OASIS_NEAR_NODE")??"node",fixture})!;
            var operation=(Task<JsonElement>)service.GetMethod("ViewAsync")!.Invoke(instance,new object[]{"echo",new{value},"unused"})!;
            var response=await operation;
            Assert.AreEqual(value,response.GetProperty("value").GetString());
        }
        finally{Console.OutputEncoding=previous;}
    }
}
