using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleGraphCallerAdmissionTests
{
    private sealed class ControlledAcknowledgement : OracleDBOASIS
    {
        private readonly Action _afterFirst;
        public int Writes {get;private set;}
        public ControlledAcknowledgement(Action afterFirst):base("NotUsed=true")=>_afterFirst=afterFirst;
        protected override async Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            var holon=write.Snapshot;
            Writes++;await Task.Yield();
            holon.Version=1;holon.VersionId=Guid.NewGuid();
            if(Writes==1)_afterFirst();
            return new(){Result=holon,IsSaved=true};
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task AcknowledgedCallerEditPreservesReconciliationAndStopsDescendants(bool synchronous)
    {
        var first=new Holon{Id=Guid.NewGuid(),Name="first",IsNewHolon=true};
        var child=new Holon{Name="not dispatched"};first.Children.Add(child);
        var provider=new ControlledAcknowledgement(()=>first.MetaData["concurrent caller edit"]="keep");
        var result=synchronous?provider.SaveHolons(new[]{first}):await provider.SaveHolonsAsync(new[]{first});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(1,provider.Writes,"Never dispatch descendants after failed acknowledged publication.");
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        var evidence=result.Exception as OracleLifecycleReconciliationException;Assert.IsNotNull(evidence);
        Assert.AreEqual(first.Id,evidence.RecordId);
        Assert.AreEqual("keep",first.MetaData["concurrent caller edit"]);
        Assert.AreEqual(0,first.Version);Assert.AreEqual(Guid.Empty,first.VersionId);Assert.IsTrue(first.IsNewHolon);
        Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,child.ParentHolonId);
        Assert.IsFalse(first.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.OracleDBOASIS)==true);
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
