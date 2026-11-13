using Moq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class GeoNFTBatchPolicyTests
{
    private sealed class CountedHistory(string json)
    {
        public int Reads;
        public override string ToString(){Reads++;return json;}
    }
    [Fact]
    public void BatchDecodesEachAvatarHistoryOnceWhilePreservingGlobalAndPlayerCounts()
    {
        var actor=Guid.NewGuid();var now=DateTime.UtcNow;
        var ids=Enumerable.Range(0,36).Select(_=>Guid.NewGuid()).ToArray();
        var rows=ids.Select(id=>{var nft=new Mock<IWeb4GeoSpatialNFT>();nft.SetupGet(p=>p.Id).Returns(id);nft.SetupGet(p=>p.AllowOtherPlayersToAlsoCollect).Returns(true);nft.SetupGet(p=>p.GlobalSpawnQuantity).Returns(5);nft.SetupGet(p=>p.PlayerSpawnQuantity).Returns(3);return nft.Object;}).ToArray();
        var own=new CountedHistory(JsonConvert.SerializeObject(ids.ToDictionary(id=>id,id=>new{Count=2,LastCollectedUtc=now.AddHours(-1)})));
        var other=new CountedHistory(JsonConvert.SerializeObject(ids.ToDictionary(id=>id,id=>new{Count=1,LastCollectedUtc=now.AddHours(-2)})));
        IAvatarDetail Avatar(Guid id,CountedHistory history){var avatar=new Mock<IAvatarDetail>();avatar.SetupGet(p=>p.Id).Returns(id);avatar.SetupGet(p=>p.MetaData).Returns(new Dictionary<string,object>{{"GeoNFT.CollectionHistory.v1",history}});return avatar.Object;}
        var statuses=GeoNFTCollectionPolicy.EvaluateForAvatarBatch(rows,actor,new[]{Avatar(actor,own),Avatar(Guid.NewGuid(),other)},now);
        Assert.Equal(36,statuses.Count);Assert.Equal(1,own.Reads);Assert.Equal(1,other.Reads);
        Assert.All(statuses,s=>{Assert.True(s.CanCollect);Assert.Equal(2,s.PlayerCollectionCount);Assert.Equal(3,s.GlobalCollectionCount);});
    }
}
