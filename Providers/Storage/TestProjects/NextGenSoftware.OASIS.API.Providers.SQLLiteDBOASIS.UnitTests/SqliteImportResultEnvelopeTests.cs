using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ProviderTestSupport;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass][DoNotParallelize]
public sealed class SqliteImportResultEnvelopeTests
{
    private const string Memory="Data Source=owned-import-envelope;Mode=Memory;Cache=Shared;Pooling=False";
    private sealed class Probe(string connection,OASISResult<IEnumerable<IHolon>> source):SQLLiteDBOASIS(connection)
    {
        public int Saves;
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {Saves++;Assert.IsTrue(saveChildren);Assert.IsTrue(recursive);Assert.AreEqual(0,maxChildDepth);Assert.AreEqual(0,curentChildDepth);Assert.IsFalse(continueOnError);return Task.FromResult(source);}
    }
    private sealed class FailedActivation:SQLLiteDBOASIS
    {
        public int Activations,Saves;
        public readonly OASISResult<bool> Original=new(){IsError=true,ErrorCode="OWNED_ACTIVATION_DENIED",Message="original activation",DetailedMessage="original before dispatch detail",
            Exception=new IOException("owned activation refusal"),ErrorCount=1,WarningCount=2,IsWarning=true,MetaData=new(){{"original-owner","synthetic-activation"}}};
        public FailedActivation():base(Memory){}
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;return Task.FromResult(Original);}
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {Saves++;throw new InvalidOperationException("Save must not follow failed activation.");}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ActivationFailureRetainsOriginalEnvelopeWithoutSaving(bool synchronous)
    {
        using var provider=new FailedActivation();
        var result=synchronous?provider.Import(new[]{new Holon()}):await provider.ImportAsync(new[]{new Holon()});var original=provider.Original;
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.AreSame(original.Exception,result.Exception);Assert.AreEqual(original.ErrorCode,result.ErrorCode);
        Assert.AreEqual(original.DetailedMessage,result.DetailedMessage);Assert.AreEqual(original.ErrorCount,result.ErrorCount);Assert.AreEqual(original.WarningCount,result.WarningCount);
        Assert.AreEqual(original.IsWarning,result.IsWarning);Assert.AreSame(original.MetaData,result.MetaData);Assert.AreEqual(1,provider.Activations);Assert.AreEqual(0,provider.Saves);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NullImportRejectsBeforeActivationOrSave(bool synchronous)
    {
        using var provider=new FailedActivation();var result=synchronous?provider.Import(null!):await provider.ImportAsync(null!);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentNullException));
        Assert.AreEqual(0,provider.Activations);Assert.AreEqual(0,provider.Saves);
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)]
    public async Task ImportPreservesSaveEvidenceAndUsesOnePublicWriter(bool synchronous,int mode)
    {
        await using var keeper=new SqliteConnection(Memory);await keeper.OpenAsync();
        var source=ImportResultEnvelopeAssertions.Source(mode);using var provider=new Probe(Memory,source);
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Exception?.ToString());
        await ImportResultEnvelopeAssertions.Verify(provider,source,synchronous);Assert.AreEqual(1,provider.Saves);
        using var control=new DataContext(Memory);Assert.AreEqual(0,await control.Holons.CountAsync(),"Import must not bypass the selected public save implementation.");
    }

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("Native")]
    public async Task NativeImportReportsActualGraphAcknowledgementsAndPublishesIds(bool synchronous)
    {
        await using var keeper=new SqliteConnection(Memory);await keeper.OpenAsync();
        using(var version=keeper.CreateCommand()){version.CommandText="SELECT sqlite_version()";Console.WriteLine("SQLite native version: "+await version.ExecuteScalarAsync());}
        using var provider=new SQLLiteDBOASIS(Memory);
        var owner=Guid.NewGuid();var root=new Holon{Name="owned native import",CreatedByAvatarId=owner,IsActive=true};
        var child=new Holon{Name="owned child",CreatedByAvatarId=owner,IsActive=true};
        var grandchild=new Holon{Name="owned grandchild",CreatedByAvatarId=owner,IsActive=true};
        root.Children.Add(child);child.Children.Add(grandchild);
        var result=synchronous?provider.Import(new[]{root}):await provider.ImportAsync(new[]{root});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);Assert.AreEqual(3,result.SavedCount);
        Assert.IsTrue(provider.IsProviderActivated);Assert.AreNotEqual(Guid.Empty,root.Id);Assert.AreNotEqual(Guid.Empty,child.Id);Assert.AreNotEqual(Guid.Empty,grandchild.Id);
        Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(child.Id,grandchild.ParentHolonId);
        using var control=new DataContext(Memory);Assert.AreEqual(3,await control.Holons.CountAsync());
        foreach(var row in new[]{root,child,grandchild}){
            var loaded=await provider.LoadHolonAsync(row.Id,loadChildren:false);
            Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(row.Id,loaded.Result.Id);Assert.AreEqual(owner,loaded.Result.CreatedByAvatarId);
        }
    }
}
