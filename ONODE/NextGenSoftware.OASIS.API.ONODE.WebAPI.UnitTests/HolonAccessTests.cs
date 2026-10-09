using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests
{
    public class HolonAccessTests
    {
        private static readonly Guid Owner = Guid.NewGuid();
        private static readonly Guid Friend = Guid.NewGuid();
        private static readonly Guid Stranger = Guid.NewGuid();

        private static Holon HolonSharedWith(object sharedRaw) => new Holon
        {
            CreatedByAvatarId = Owner,
            MetaData = new Dictionary<string, object> { [HolonAccess.SharedAvatarIdsMetaKey] = sharedRaw },
        };

        [Fact]
        public void CanRead_AllowsCreatorPublicAndSharedAvatars_Only()
        {
            var holon = HolonSharedWith(JsonSerializer.Serialize(new[] { Friend }));

            HolonAccess.CanRead(holon, Owner).Should().BeTrue();
            HolonAccess.CanRead(holon, Friend).Should().BeTrue();
            HolonAccess.CanRead(holon, Stranger).Should().BeFalse();
            HolonAccess.CanRead(holon, Guid.Empty).Should().BeFalse();

            holon.IsPublic = true;
            HolonAccess.CanRead(holon, Stranger).Should().BeTrue();
        }

        [Fact]
        public void CanRead_WithoutShareMetadata_IsCreatorOnly()
        {
            var holon = new Holon { CreatedByAvatarId = Owner };

            HolonAccess.CanRead(holon, Owner).Should().BeTrue();
            HolonAccess.CanRead(holon, Friend).Should().BeFalse();
            HolonAccess.CanRead(null, Owner).Should().BeFalse();
        }

        public static IEnumerable<object[]> StoredShareFormats()
        {
            yield return new object[] { JsonSerializer.Serialize(new[] { Friend }) };              // JSON array string (ShareController)
            yield return new object[] { Friend.ToString() };                                      // single ID
            yield return new object[] { $"{Guid.NewGuid()}, {Friend}" };                          // legacy comma-separated
            yield return new object[] { new List<Guid> { Friend } };                              // in-memory collection
            yield return new object[] { JsonDocument.Parse(JsonSerializer.Serialize(new[] { Friend })).RootElement }; // provider JSON
            yield return new object[] { JsonDocument.Parse(JsonSerializer.Serialize(Friend.ToString())).RootElement };
        }

        [Theory]
        [MemberData(nameof(StoredShareFormats))]
        public void GetSharedAvatarIds_ReadsEveryStoredFormat(object stored)
        {
            HolonAccess.GetSharedAvatarIds(HolonSharedWith(stored)).Should().Contain(Friend);
        }

        [Fact]
        public void GetSharedAvatarIds_IgnoresGarbage()
        {
            HolonAccess.GetSharedAvatarIds(HolonSharedWith("not-a-guid, [broken")).Should().BeEmpty();
        }

        [Fact]
        public void RecordShares_WritesListAndPerRecipientIndex()
        {
            var holon = new Holon { CreatedByAvatarId = Owner };

            HolonAccess.RecordShares(holon, new[] { Friend, Stranger, Friend, Guid.Empty });

            HolonAccess.GetSharedAvatarIds(holon).Should().BeEquivalentTo(new[] { Friend, Stranger });
            holon.MetaData[HolonAccess.SharedWithIndexKey(Friend)].Should().Be(HolonAccess.SharedWithIndexValue);
            holon.MetaData[HolonAccess.SharedWithIndexKey(Stranger)].Should().Be(HolonAccess.SharedWithIndexValue);
        }

        [Fact]
        public async Task ShareAsync_RejectsAnonymousCaller_BeforeTouchingStorage()
        {
            var result = await HolonAccess.ShareAsync(null, Guid.NewGuid(), new[] { Friend }, caller: null);

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("Unauthorized");
        }

        [Fact]
        public async Task ShareAsync_RejectsEmptyRecipientList_BeforeTouchingStorage()
        {
            var caller = new Mock<IAvatar>();
            caller.SetupGet(a => a.Id).Returns(Owner);

            var result = await HolonAccess.ShareAsync(null, Guid.NewGuid(), new[] { Guid.Empty }, caller.Object);

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("At least one valid avatar id");
        }
    }
}
