using System.Security.Cryptography;
using System.Text;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false,false,false)][InlineData(false,false,true)]
    [InlineData(false,true,false)][InlineData(false,true,true)]
    [InlineData(true,false,false)][InlineData(true,false,true)]
    [InlineData(true,true,false)][InlineData(true,true,true)]
    public async Task KarmaProjectionUsesAcknowledgedTotalWithoutPostCommitRead(bool sync, bool remove, bool facade)
    {
        var actor = Guid.NewGuid();
        var ledgerId = new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"settings:{actor}:karma")));
        var detail = new AvatarDetail { Id = actor, Karma = 1000, XP = 222 };
        var stored = new Dictionary<Guid,string> { [ledgerId] = StoredRecordJson.Serialize(new Holon {
            Id = ledgerId, Name = $"settings_avatar_{actor}_karma", CreatedByAvatarId = actor,
            MetaData = new() { ["totalKarma"] = 1000L, ["history"] = new List<Dictionary<string,object>>() }
        }) };
        var ledgerWrites = 0; var deniedPostCommitReads = 0; var detailWrites = 0;
        var provider = CreateActiveProvider(ProviderType.OracleDBOASIS,"acknowledged-total-control");
        DeclareLeafChildReadContract(provider);
        OASISResult<IHolon> Read(Guid id)
        {
            if (id == ledgerId && ledgerWrites > 0)
            {
                deniedPostCommitReads++;
                return new() { IsError = true, Exception = new IOException("owned postcommit read outage"), Message = "owned read outage" };
            }
            return new(LeafSettingsObservation(id, stored.TryGetValue(id,out var json)
                ? StoredRecordJson.Deserialize<Holon>(json) : new Holon { Id = id }));
        }
        OASISResult<IHolon> Save(IHolon row)
        {
            stored[row.Id] = StoredRecordJson.Serialize(row);
            if (row.Id == ledgerId) ledgerWrites++;
            return new(StoredRecordJson.Deserialize<Holon>(stored[row.Id])) { IsSaved = true, SavedCount = 1 };
        }
        provider.Setup(p=>p.LoadHolon(It.IsAny<Guid>(),true,true,0,true,false,0))
            .Returns((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        provider.Setup(p=>p.LoadHolonAsync(It.IsAny<Guid>(),true,true,0,true,false,0))
            .ReturnsAsync((Guid id,bool a,bool b,int c,bool d,bool e,int f)=>Read(id));
        foreach (var children in new[] { false,true })
        {
            provider.Setup(p=>p.SaveHolon(It.IsAny<IHolon>(),children,true,0,true,false))
                .Returns((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row));
            provider.Setup(p=>p.SaveHolonAsync(It.IsAny<IHolon>(),children,true,0,true,false))
                .ReturnsAsync((IHolon row,bool a,bool b,int c,bool d,bool e)=>Save(row));
        }
        provider.Setup(p=>p.LoadAvatarDetail(actor,0)).Returns(()=>new OASISResult<IAvatarDetail>(detail));
        provider.Setup(p=>p.SaveAvatarDetail(It.IsAny<IAvatarDetail>())).Returns((IAvatarDetail row)=> {
            detailWrites++; return new OASISResult<IAvatarDetail>(row) { IsSaved = true, SavedCount = 1 };
        });
        var dna = CreateDna(HyperDriveModes.V2);
        var providers = new ProviderManager(null,dna) { IsAutoFailOverEnabled=false, IsAutoReplicationEnabled=false, IsAutoLoadBalanceEnabled=false };
        providers.RegisterProvider(provider.Object); providers.SetAndActivateCurrentStorageProvider(provider.Object);
        OASISResult<KarmaAkashicRecord> result;
        if (facade)
        {
            var manager = new AvatarManager(provider.Object,dna,providers);
            result = sync
                ? remove ? manager.RemoveKarmaFromAvatar(actor,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","ack total")
                    : manager.AddKarmaToAvatar(actor,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","ack total")
                : new(remove ? await manager.RemoveKarmaFromAvatarAsync(actor,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","ack total")
                    : await manager.AddKarmaToAvatarAsync(actor,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","ack total"));
        }
        else
        {
            var manager = new KarmaManager(provider.Object,dna,providers);
            result = sync
                ? remove ? manager.RemoveKarmaFromAvatar(actor,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","ack total")
                    : manager.AddKarmaToAvatar(actor,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","ack total")
                : remove ? await manager.RemoveKarmaFromAvatarAsync(actor,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","ack total")
                    : await manager.AddKarmaToAvatarAsync(actor,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","ack total");
        }
        Assert.False(result.IsError,result.Message+" "+result.Exception); Assert.NotNull(result.Result);
        Assert.Equal(remove?500L:1500L,result.Result.TotalKarma);
        Assert.Equal(result.Result.TotalKarma,detail.Karma); Assert.Equal(222L,detail.XP);
        Assert.Single(detail.KarmaAkashicRecords); Assert.Equal(1,ledgerWrites); Assert.Equal(1,detailWrites);
        Assert.Equal(0,deniedPostCommitReads);
        Assert.Equal(remove?500L:1500L,Convert.ToInt64(StoredRecordJson.Deserialize<Holon>(stored[ledgerId]).MetaData["totalKarma"]));
    }
}
