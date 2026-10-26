using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ProviderTestSupport;
namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public sealed class ImportResultEnvelopeTests
{
    private sealed class Probe(OASISResult<IEnumerable<IHolon>> source):KeyValueStorageProviderBase(new InMemoryBackend())
    {
        public int Calls;
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {Calls++;Assert.IsTrue(saveChildren);Assert.IsTrue(recursive);Assert.AreEqual(0,maxChildDepth);Assert.AreEqual(0,curentChildDepth);Assert.IsFalse(continueOnError);return Task.FromResult(source);}
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)]
    public async Task ImportPreservesOriginalSaveEvidenceWithoutReenumeration(bool synchronous,int mode)
    {var source=ImportResultEnvelopeAssertions.Source(mode);var provider=new Probe(source);await ImportResultEnvelopeAssertions.Verify(provider,source,synchronous);Assert.AreEqual(1,provider.Calls);Assert.IsFalse(provider.IsProviderActivated);}
}
