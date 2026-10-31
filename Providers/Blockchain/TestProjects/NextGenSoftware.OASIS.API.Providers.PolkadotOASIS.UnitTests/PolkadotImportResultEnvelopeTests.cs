using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ProviderTestSupport;
namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;
[TestClass]
public sealed class PolkadotImportResultEnvelopeTests
{
    private sealed class Probe(OASISResult<IEnumerable<IHolon>> source):PolkadotOASIS("http://127.0.0.1:1","localnet","")
    {
        public int Calls,Activations;
        public bool Continued;
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;return Task.FromResult(new OASISResult<bool>(true));}
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {Calls++;Continued=continueOnError;Assert.IsTrue(saveChildren);Assert.IsTrue(recursive);Assert.AreEqual(0,maxChildDepth);Assert.AreEqual(0,curentChildDepth);return Task.FromResult(source);}
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)]
    public async Task ImportPreservesOriginalSaveEvidenceWithoutReenumeration(bool synchronous,int mode)
    {var source=ImportResultEnvelopeAssertions.Source(mode);using var provider=new Probe(source);await ImportResultEnvelopeAssertions.Verify(provider,source,synchronous);Assert.AreEqual(1,provider.Calls);Assert.AreEqual(0,provider.Activations);Assert.IsFalse(provider.Continued);Assert.IsFalse(provider.IsProviderActivated);}
}
