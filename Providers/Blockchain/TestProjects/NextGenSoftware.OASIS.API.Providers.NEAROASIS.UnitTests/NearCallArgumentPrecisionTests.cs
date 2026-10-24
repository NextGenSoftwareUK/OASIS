using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearCallArgumentPrecisionTests
{
    [DataTestMethod]
    [DataRow("",false)][DataRow("%%%",false)][DataRow("e30",false)]
    [DataRow("/w==",false)][DataRow("bm90LWpzb24=",false)][DataRow("e30=",true)]
    public async Task DecoderRequiresCanonicalBase64Utf8Json(string encoded,bool expected)
    {
        var sdkPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js"));
        Assert.IsTrue(File.Exists(sdkPath));
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE");
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE",sdkPath);
            var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","fixture","receiver.near","sender.near","controlled-key","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","call-arguments.mjs")})!;
            var view=(Task<JsonElement>)serviceType.GetMethod("ViewAsync")!.Invoke(service,new object?[]{"validate-arguments",new{encoded},null})!;
            Assert.AreEqual(expected,(await view).GetProperty("valid").GetBoolean());
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE",previous);}
    }

    [DataTestMethod]
    [DataRow("{\"value\":1}")]
    [DataRow("{\"value\":9223372036854775807}")]
    [DataRow("{\"value\":-9007199254740993}")]
    [DataRow("{\"value\":0.1234567890123456789012345678}")]
    public async Task RealSdkActionPreservesOriginalJsonArgumentBytes(string json)
    {
        using var document=JsonDocument.Parse(json);
        var arguments=JsonSerializer.SerializeToElement(document.RootElement);
        var sdkPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js"));
        Assert.IsTrue(File.Exists(sdkPath),"Reuse the existing pinned SDK; do not restore/download another copy.");
        var previousSdk=Environment.GetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE");
        var previousArgs=Environment.GetEnvironmentVariable("OASIS_NEAR_EXPECTED_ARGS_BASE64");
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE",sdkPath);
            Environment.SetEnvironmentVariable("OASIS_NEAR_EXPECTED_ARGS_BASE64",Convert.ToBase64String(Encoding.UTF8.GetBytes(arguments.GetRawText())));
            var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","fixture","receiver.near","sender.near","controlled-key","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","call-arguments.mjs")})!;
            var call=(Task<string>)serviceType.GetMethod("CallAsync")!.Invoke(service,new object?[]{"custom_method",arguments,null,"0",null,null})!;
            Assert.AreEqual("controlled-sdk-action-transaction",await call);
        }finally{
            Environment.SetEnvironmentVariable("OASIS_NEAR_REAL_SDK_MODULE",previousSdk);
            Environment.SetEnvironmentVariable("OASIS_NEAR_EXPECTED_ARGS_BASE64",previousArgs);
        }
    }
}
