using System;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class STARNETManagerBaseVisibilityTests
    {
        private static readonly Guid Owner = Guid.NewGuid();
        private static readonly Guid Other = Guid.NewGuid();

        private static ISTARNETHolon Holon(bool? publishedOnSTARNET, Guid creator, DateTime publishedOn)
        {
            var holon = new Mock<ISTARNETHolon>();

            if (publishedOnSTARNET == null)
            {
                holon.SetupGet(h => h.STARNETDNA).Returns((ISTARNETDNA)null);
                return holon.Object;
            }

            var dna = new Mock<ISTARNETDNA>();
            dna.SetupGet(d => d.PublishedOnSTARNET).Returns(publishedOnSTARNET.Value);
            dna.SetupGet(d => d.CreatedByAvatarId).Returns(creator);
            dna.SetupGet(d => d.PublishedOn).Returns(publishedOn);
            holon.SetupGet(h => h.STARNETDNA).Returns(dna.Object);
            return holon.Object;
        }

        [Fact]
        public void Anonymous_sees_only_holons_published_on_STARNET()
        {
            OAPPManager.IsVisibleToAvatar(Holon(true, Owner, DateTime.UtcNow), Guid.Empty).Should().BeTrue();
            OAPPManager.IsVisibleToAvatar(Holon(false, Owner, DateTime.UtcNow), Guid.Empty).Should().BeFalse();
            OAPPManager.IsVisibleToAvatar(Holon(false, Owner, DateTime.MinValue), Guid.Empty).Should().BeFalse();
        }

        [Fact]
        public void Anonymous_never_sees_holons_without_STARNETDNA_or_null()
        {
            OAPPManager.IsVisibleToAvatar(Holon(null, Owner, DateTime.MinValue), Guid.Empty).Should().BeFalse();
            OAPPManager.IsVisibleToAvatar(null, Guid.Empty).Should().BeFalse();
        }

        [Fact]
        public void Owner_sees_own_drafts()
        {
            OAPPManager.IsVisibleToAvatar(Holon(false, Owner, DateTime.MinValue), Owner).Should().BeTrue();
        }

        [Fact]
        public void Other_avatars_do_not_see_unpublished_drafts_but_see_published()
        {
            OAPPManager.IsVisibleToAvatar(Holon(false, Owner, DateTime.MinValue), Other).Should().BeFalse();
            OAPPManager.IsVisibleToAvatar(Holon(true, Owner, DateTime.UtcNow), Other).Should().BeTrue();
        }
    }
}
