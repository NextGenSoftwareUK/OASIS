using FluentAssertions;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Services;

/// <summary>
/// Tests for ShipmentStatusValidator - validates status transition rules
/// </summary>
public class ShipmentStatusValidatorTests
{
    [Fact]
    public void IsValidTransition_QuoteRequestedToQuoteProvided_ReturnsTrue()
    {
        // Arrange
        var currentStatus = ShipmentStatus.QuoteRequested;
        var newStatus = ShipmentStatus.QuoteProvided;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(currentStatus, newStatus);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidTransition_QuoteRequestedToDelivered_ReturnsFalse()
    {
        // Arrange
        var currentStatus = ShipmentStatus.QuoteRequested;
        var newStatus = ShipmentStatus.Delivered;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(currentStatus, newStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidTransition_SameStatus_ReturnsTrue()
    {
        // Arrange
        var status = ShipmentStatus.InTransit;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(status, status);

        // Assert
        result.Should().BeTrue("Same status transitions should be valid (idempotent)");
    }

    [Fact]
    public void IsValidTransition_DeliveredToInTransit_ReturnsFalse()
    {
        // Arrange
        var currentStatus = ShipmentStatus.Delivered;
        var newStatus = ShipmentStatus.InTransit;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(currentStatus, newStatus);

        // Assert
        result.Should().BeFalse("Delivered is a terminal state");
    }

    [Fact]
    public void IsValidTransition_CancelledToInTransit_ReturnsFalse()
    {
        // Arrange
        var currentStatus = ShipmentStatus.Cancelled;
        var newStatus = ShipmentStatus.InTransit;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(currentStatus, newStatus);

        // Assert
        result.Should().BeFalse("Cancelled is a terminal state");
    }

    [Fact]
    public void IsValidTransition_ErrorToQuoteRequested_ReturnsTrue()
    {
        // Arrange
        var currentStatus = ShipmentStatus.Error;
        var newStatus = ShipmentStatus.QuoteRequested;

        // Act
        var result = ShipmentStatusValidator.IsValidTransition(currentStatus, newStatus);

        // Assert
        result.Should().BeTrue("Error state allows retry transitions");
    }

    [Fact]
    public void GetValidNextStatuses_QuoteRequested_ReturnsExpectedStatuses()
    {
        // Arrange
        var currentStatus = ShipmentStatus.QuoteRequested;

        // Act
        var validStatuses = ShipmentStatusValidator.GetValidNextStatuses(currentStatus);

        // Assert
        validStatuses.Should().Contain(ShipmentStatus.QuoteProvided);
        validStatuses.Should().Contain(ShipmentStatus.Cancelled);
        validStatuses.Should().Contain(ShipmentStatus.Error);
        validStatuses.Should().NotContain(ShipmentStatus.Delivered);
    }

    [Fact]
    public void GetValidNextStatuses_Delivered_ReturnsEmpty()
    {
        // Arrange
        var currentStatus = ShipmentStatus.Delivered;

        // Act
        var validStatuses = ShipmentStatusValidator.GetValidNextStatuses(currentStatus);

        // Assert
        validStatuses.Should().BeEmpty("Terminal states have no valid next statuses");
    }

    [Fact]
    public void GetTransitionErrorMessage_InvalidTransition_ReturnsDescriptiveMessage()
    {
        // Arrange
        var currentStatus = ShipmentStatus.Delivered;
        var newStatus = ShipmentStatus.InTransit;

        // Act
        var message = ShipmentStatusValidator.GetTransitionErrorMessage(currentStatus, newStatus);

        // Assert
        message.Should().Contain("terminal state");
        message.Should().Contain("Delivered");
    }
}
