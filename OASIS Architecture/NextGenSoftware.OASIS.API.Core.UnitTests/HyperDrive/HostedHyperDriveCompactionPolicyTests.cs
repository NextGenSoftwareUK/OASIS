using System;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class HostedHyperDriveCompactionPolicyTests
{
    [Fact]
    public void UsesSlowestActiveDeviceAsSafeSequence()
    {
        HostedHyperDriveCompactionPolicy.CalculateSafeSequence(100, new long[] { 90, 25, 70 })
            .Should().Be(25);
    }

    [Fact]
    public void UsesCurrentSequenceWhenThereAreNoActiveDevices()
    {
        HostedHyperDriveCompactionPolicy.CalculateSafeSequence(100, Array.Empty<long>())
            .Should().Be(100);
    }

    [Fact]
    public void DeviceAtZeroPreventsUnsafeCompaction()
    {
        HostedHyperDriveCompactionPolicy.CalculateSafeSequence(100, new long[] { 75, 0 })
            .Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RejectsImpossibleDeviceProgress(long sequence)
    {
        Action act = () => HostedHyperDriveCompactionPolicy.CalculateSafeSequence(100,
            new[] { sequence });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
