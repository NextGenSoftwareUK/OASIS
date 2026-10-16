using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaCommonIdentityTests
{
    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void AvatarDetailTrackedQuestSurvivesStoredRoundTrip(bool tracked)
    {
        var source=new AvatarDetail {Id=Guid.NewGuid(),ActiveQuestId=tracked ? Guid.NewGuid():null,
            ActiveObjectiveId=tracked ? Guid.NewGuid():null};
        var dto=source.GetSolanaAvatarDetailDto();
        var loaded=((SolanaAvatarDetailDto)Read(typeof(SolanaAvatarDetailDto),OasisJson.Serialize(dto))).GetAvatarDetail();
        Assert.AreEqual(source.ActiveQuestId,loaded.ActiveQuestId);
        Assert.AreEqual(source.ActiveObjectiveId,loaded.ActiveObjectiveId);
    }

    private static IHolonBase Source(string kind) => kind switch {
        "avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
    private static SolanaBaseDto Stored(IHolonBase source) => source switch {
        IAvatar avatar=>avatar.GetSolanaAvatarDto(),IAvatarDetail detail=>detail.GetSolanaAvatarDetailDto(),
        IHolon holon=>holon.GetSolanaHolonDto(),_=>throw new InvalidOperationException()};
    private static IHolonBase Domain(SolanaBaseDto source) => source switch {
        SolanaAvatarDto avatar=>avatar.GetAvatar(),SolanaAvatarDetailDto detail=>detail.GetAvatarDetail(),
        SolanaHolonDto holon=>holon.GetHolon(),_=>throw new InvalidOperationException()};
    private static SolanaBaseDto Read(Type type,string json) => (SolanaBaseDto)typeof(SolanaRepository)
        .GetMethod("DeserializeStoredRecord",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(type).Invoke(null,new object[] {json})!;

    [DataTestMethod]
    [DataRow("avatar",true)] [DataRow("avatar",false)]
    [DataRow("detail",true)] [DataRow("detail",false)]
    [DataRow("holon",true)] [DataRow("holon",false)]
    public void EncryptedRecordRetainsCommonIdentity(string kind,bool visible)
    {
        var source=Source(kind);source.Id=Guid.NewGuid();source.VersionId=Guid.NewGuid();source.PreviousVersionId=Guid.NewGuid();
        source.IsPublic=visible;source.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        source.CreatedOASISType=new EnumValue<OASISType>(OASISType.OurWorld);
        var dto=Stored(source);var repository=new SolanaRepository(new Solnet.Wallet.Account(),null!,()=>Enumerable.Repeat((byte)23,32).ToArray());
        var encoded=(string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!
            .MakeGenericMethod(dto.GetType()).Invoke(repository,new object[] {dto})!;
        Assert.IsFalse(encoded.Contains(source.VersionId.ToString(),StringComparison.Ordinal));
        var decoded=(string)typeof(SolanaRepository).GetMethod("DecodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(repository,new object[] {System.Text.Encoding.UTF8.GetBytes(encoded)})!;
        var loaded=Domain(Read(dto.GetType(),decoded));
        Assert.AreEqual(source.VersionId,loaded.VersionId);Assert.AreEqual(source.IsPublic,loaded.IsPublic);
        Assert.AreEqual(source.CreatedProviderType.Value,loaded.CreatedProviderType?.Value);
        Assert.AreEqual(source.CreatedOASISType.Value,loaded.CreatedOASISType?.Value);
        Assert.AreEqual(source.Id,loaded.Id);Assert.AreEqual(source.PreviousVersionId,loaded.PreviousVersionId);
    }

    [DataTestMethod]
    [DataRow("avatar")] [DataRow("detail")] [DataRow("holon")]
    public void MissingLegacyFieldsStayPrivateWithoutInventedOrigin(string kind)
    {
        var source=Source(kind);source.Id=Guid.NewGuid();var dto=Stored(source);
        var json=JObject.Parse(OasisJson.Serialize(dto));
        foreach(var name in new[] {"VersionId","IsPublic","CreatedProviderType","CreatedOASISType"}) json.Remove(name);
        var loaded=Domain(Read(dto.GetType(),json.ToString()));
        Assert.AreEqual(source.Id,loaded.Id);Assert.AreEqual(Guid.Empty,loaded.VersionId);Assert.IsFalse(loaded.IsPublic);
        Assert.IsNull(loaded.CreatedProviderType);Assert.IsNull(loaded.CreatedOASISType);
    }
}
