using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public async Task HistoricalCandidateMergeRejectsConflictingPhysicalObservations(bool collection,bool differentRevision)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Holon{Name="current",Version=1};
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var observed=(await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
        if(differentRevision)CosmicConcurrency.SetProviderRevisionId(observed,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        else observed.Name="conflicting-content";
        var json=StoredRecordJson.Serialize<IHolon>(observed);
        using var control=new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("INSERT INTO CosmicHolonRevision(HolonId,ProviderRevisionId,Version,VersionId,RecordJson,RecordDigest,ArchivedAtUtc) VALUES({0},{1},1,{2},{3},{4},{5})",
            observed.Id.ToString("D"),CosmicConcurrency.GetProviderRevisionId(observed,ProviderType.SQLLiteDBOASIS)!.Value.ToString("D"),
            observed.VersionId.ToString("D"),json,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))),DateTime.UtcNow.ToString("O"));
        if(collection)
        {
            var result=await provider.LoadAllHolonsAsync(version:1,loadChildren:false);
            result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<InvalidDataException>();
            result.Exception.Message.Should().Contain(differentRevision ? "multiple native revisions":"different canonical content");
        }
        else
        {
            var result=await provider.LoadHolonAsync(source.Id,version:1,loadChildren:false);
            result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<InvalidDataException>();
            result.Exception.Message.Should().Contain(differentRevision ? "multiple native revisions":"different canonical content");
        }
    }

    private sealed class HistoricalRollover(Action write):DbConnectionInterceptor
    {
        private int _fired;
        public int Writes;
        public override void ConnectionClosed(DbConnection connection,ConnectionEndEventData eventData)
        {
            if(Interlocked.Exchange(ref _fired,1)!=0)return;
            write();Writes++;
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HistoricalReadDeduplicatesOneNativeRevisionAcrossConcurrentRollover(bool collection)
    {
        using var writer=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await writer.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Holon{Name="version-one",Version=1};
        (await writer.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var rollover=new HistoricalRollover(()=>{
            source.Name="version-two";source.Version=2;
            var committed=writer.SaveHolon(source);committed.IsError.Should().BeFalse(committed.Message+committed.Exception);
        });
        using var reader=new ReplicaProbeContext(TestConnectionString,rollover);
        var repository=new HolonRepository(reader);
        if(collection)
        {
            var result=repository.LoadAllHolons(loadChildren:false,version:1);
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            result.Result.Should().ContainSingle().Which.Name.Should().Be("version-one");
        }
        else
        {
            var result=repository.LoadHolon(source.Id,loadChildren:false,version:1);
            result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Name.Should().Be("version-one");
        }
        rollover.Writes.Should().Be(1);
        (await writer.LoadHolonAsync(source.Id,loadChildren:false)).Result.Name.Should().Be("version-two");
    }
}
