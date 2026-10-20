using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleGraphCallerAdmissionTests
{
    private sealed class ControlledAcknowledgement : OracleDBOASIS
    {
        private readonly Action _afterFirst;
        public int Writes {get;private set;}
        public ControlledAcknowledgement(Action afterFirst):base("NotUsed=true")=>_afterFirst=afterFirst;
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,
            int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {
            Writes++;await Task.Yield();
            holon.Version=1;holon.VersionId=Guid.NewGuid();
            if(Writes==1)_afterFirst();
            return new(){Result=holon,IsSaved=true};
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task EditedLaterCallerIsRejectedBeforeAnotherGraphWrite(bool synchronous)
    {
        var first=new Holon{Id=Guid.NewGuid(),Name="first"};
        var second=new Holon{Id=Guid.NewGuid(),Name="second"};
        var provider=new ControlledAcknowledgement(()=>second.Name="newer caller edit");
        var result=synchronous ? provider.SaveHolons(new[]{first,second}) :await provider.SaveHolonsAsync(new[]{first,second});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(1,provider.Writes,"A changed later caller must not dispatch its stale snapshot.");
        Assert.AreEqual("newer caller edit",second.Name);Assert.AreEqual(0,second.Version);
        Assert.AreEqual(1,first.Version);
        var partial=result.Exception as OraclePartialMutationException;Assert.IsNotNull(partial);
        CollectionAssert.AreEqual(new[]{first.Id},partial.AcknowledgedRecordIds.ToArray());
    }
}
