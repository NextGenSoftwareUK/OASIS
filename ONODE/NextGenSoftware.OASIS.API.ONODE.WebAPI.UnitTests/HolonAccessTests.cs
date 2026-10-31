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
        public void NeedsShareIndex_TrueOnlyForSharesMissingIndexFlags()
        {
            var legacy = HolonSharedWith(JsonSerializer.Serialize(new[] { Friend }));
            HolonAccess.NeedsShareIndex(legacy).Should().BeTrue();

            HolonAccess.RecordShares(legacy, HolonAccess.GetSharedAvatarIds(legacy));
            HolonAccess.NeedsShareIndex(legacy).Should().BeFalse();

            HolonAccess.NeedsShareIndex(new Holon { CreatedByAvatarId = Owner }).Should().BeFalse();
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
        private static string K(Guid id) => HolonPermissionSet.Key(id);

        [Fact]
        public void CanRead_HonoursPerHolonPermissionGrants()
        {
            var holon = new Holon { CreatedByAvatarId = Owner };
            holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty
                .With(b => b.SetGrant(K(Friend), HolonPermission.Read | HolonPermission.Write)).Encode().Result;

            HolonAccess.CanRead(holon, Friend).Should().BeTrue();
            HolonAccess.CanRead(holon, Stranger).Should().BeFalse();
            HolonAccess.GetPermissions(holon).Grants[K(Friend)].Should().Be(HolonPermission.Read | HolonPermission.Write);
        }

        [Fact]
        public void Project_ShowsOnlyTheFieldsAnAvatarMayRead()
        {
            var holon = new Holon { CreatedByAvatarId = Owner, Name = "Dave", ParentHolonId = Owner };
            holon.MetaData["username"] = "dave";
            holon.MetaData["email"] = "dave@example.com";
            holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty
                .With(b => b.SetField(HolonPermissionSet.Public, "metadata.username", HolonPermission.Read)).Encode().Result;

            HolonAccess.CanRead(holon, Stranger).Should().BeFalse();
            var shown = HolonAccess.Project(holon, Stranger);
            shown.MetaData.Should().ContainKey("username").And.NotContainKey("email");
            shown.Name.Should().BeNullOrEmpty();
            HolonAccess.Project(holon, Owner).Should().BeSameAs(holon);
        }

        [Fact]
        public void RebuildReadIndex_FlagsEveryWholeHolonReader_IncludingGroupsAndPublic_AndDropsStaleFlags()
        {
            var group = Guid.NewGuid();
            var holon = new Holon { CreatedByAvatarId = Owner };
            holon.MetaData[HolonAccess.SharedWithIndexKey(Stranger)] = "true"; // stale
            holon.MetaData[HolonAccess.SharedAvatarIdsMetaKey] = JsonSerializer.Serialize(new[] { Friend });
            holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b => b
                .SetGrant(K(group), HolonPermission.Read).SetGrant(HolonPermissionSet.Public, HolonPermission.Read)
                .SetGrant(K(Stranger), HolonPermission.Write)
                .SetField(K(Stranger), "name", HolonPermission.Read)).Encode().Result;

            HolonAccess.RebuildReadIndex(holon);

            holon.MetaData.Should().ContainKey(HolonAccess.SharedWithIndexKey(Friend));
            holon.MetaData.Should().ContainKey(HolonAccess.SharedWithIndexKey(group));
            holon.MetaData.Should().ContainKey(HolonAccess.SharedWithPublicKey);
            holon.MetaData.Should().NotContainKey(HolonAccess.SharedWithIndexKey(Stranger)); // write-only and field-only never flag
        }

        [Fact]
        public async Task SetPermissionsAsync_RejectsAnonymousCaller_BeforeTouchingStorage()
        {
            var result = await HolonAccess.SetPermissionsAsync(null, Guid.NewGuid(),
                new HolonPermissionChange { Grants = new Dictionary<string, HolonPermission> { [K(Friend)] = HolonPermission.Read } }, caller: null);

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("Unauthorized");
        }

        [Theory]
        [InlineData("not-a-grantee")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public async Task SetPermissionsAsync_RejectsAnInvalidGrantee(string grantee)
        {
            var caller = new Mock<IAvatar>();
            caller.SetupGet(a => a.Id).Returns(Owner);

            var result = await HolonAccess.SetPermissionsAsync(null, Guid.NewGuid(),
                new HolonPermissionChange { Grants = new Dictionary<string, HolonPermission> { [grantee] = HolonPermission.Read } }, caller.Object);

            result.ErrorCode.Should().Be("COSMIC_PERMISSIONS_INPUT_INVALID");
        }

        [Fact]
        public async Task SetPermissionsAsync_RejectsManageOnAField()
        {
            var caller = new Mock<IAvatar>();
            caller.SetupGet(a => a.Id).Returns(Owner);
            var change = new HolonPermissionChange
            {
                Fields = new Dictionary<string, IReadOnlyDictionary<string, HolonPermission>>
                {
                    [HolonPermissionSet.Public] = new Dictionary<string, HolonPermission> { ["name"] = HolonPermission.Manage },
                },
            };

            var result = await HolonAccess.SetPermissionsAsync(null, Guid.NewGuid(), change, caller.Object);

            result.ErrorCode.Should().Be("COSMIC_PERMISSIONS_INPUT_INVALID");
        }

        [Fact]
        public async Task CreateGroupAsync_RequiresASignedInCallerAndAName()
        {
            var result = await HolonAccess.CreateGroupAsync(null, "  ", new[] { Friend }, caller: null);
            result.ErrorCode.Should().Be("COSMIC_GROUP_INPUT_INVALID");
        }

        [Fact]
        public async Task SetGroupMembersAsync_NeedsSomethingToChange()
        {
            var caller = new Mock<IAvatar>();
            caller.SetupGet(a => a.Id).Returns(Owner);
            var result = await HolonAccess.SetGroupMembersAsync(null, Guid.NewGuid(), null, null, caller.Object);
            result.ErrorCode.Should().Be("COSMIC_GROUP_INPUT_INVALID");
        }
    }
}
