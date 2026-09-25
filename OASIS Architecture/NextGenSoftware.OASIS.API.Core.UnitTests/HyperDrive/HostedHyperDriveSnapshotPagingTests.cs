using System;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class HostedHyperDriveSnapshotPagingTests
{
    [Fact]
    public void UsesMaterializedPageSizeForEveryPage()
    {
        var window = HostedHyperDriveSnapshotPaging.GetWindow(25, 10, 2);
        window.Offset.Should().Be(20);
        window.PageSize.Should().Be(10);
        HostedHyperDriveSnapshotPaging.IsComplete(25, window.Offset, 5).Should().BeTrue();
    }

    [Fact]
    public void AllowsOneEmptySnapshotPage()
    {
        var window = HostedHyperDriveSnapshotPaging.GetWindow(0, 10, 0);
        window.Offset.Should().Be(0);
        HostedHyperDriveSnapshotPaging.IsComplete(0, 0, 0).Should().BeTrue();
    }

    [Theory]
    [InlineData(20, 10, 2)]
    [InlineData(1, 10, 1)]
    public void RejectsPageStartingAtOrBeyondNonEmptySnapshotEnd(int count, int size, int page)
    {
        Action act = () => HostedHyperDriveSnapshotPaging.GetWindow(count, size, page);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
