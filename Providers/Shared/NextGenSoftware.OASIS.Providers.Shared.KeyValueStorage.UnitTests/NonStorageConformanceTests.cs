using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NonStorageConformanceTests
{
    [DataTestMethod] [DataRow("healthy")] [DataRow("unacknowledged")] [DataRow("paid-or-write")]
    [DataRow("no-roundtrip")] [DataRow("secret-url")] [DataRow("wrong-name")] [DataRow("advertised-storage")]
    public async Task CapabilityGateNeverSubstitutesStorageOrUnconfirmedServiceSuccess(string mode)
    {
        var backend=new InMemoryBackend();var p=new TestProvider(backend);
        p.ProviderCapabilities=new(){new EnumValue<ProviderCategory>(mode=="advertised-storage"?ProviderCategory.Storage:ProviderCategory.Map)};
        var probes=0;
        var r=await NonStorageConformance.RunAsync(p,mode=="wrong-name"?"wrong-provider":p.ProviderName,"controlled",()=> {
            probes++;return Task.FromResult(new NonStorageConformance.Probe(mode=="secret-url"?"https://example.invalid/?key=synthetic":"example.invalid","read-metadata",mode=="no-roundtrip"?0:1,mode!="paid-or-write",mode!="unacknowledged"));
        },_=>Task.CompletedTask);
        Assert.AreEqual(mode=="healthy",r.Succeeded,r.CurrentCase);
        if(mode is "wrong-name" or "advertised-storage")Assert.AreEqual(0,probes);
        Assert.AreEqual(0,backend.Items.Count,"Capability gate must never write storage records.");
        if(r.Succeeded)Assert.AreEqual(5,r.Passed.Count);
        if(mode=="secret-url")Assert.IsNull(r.Observation,"Rejected URL material must never enter a report.");
    }
}
