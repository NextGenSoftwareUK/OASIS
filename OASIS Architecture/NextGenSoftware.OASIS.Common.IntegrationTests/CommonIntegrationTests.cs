using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.Common.IntegrationTests;

public class CommonIntegrationTests
{
    [Fact]
    public async Task OASISResult_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var result = new OASISResult<object>();
        
        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task OASISResult_Should_Handle_Error_State()
    {
        // Arrange & Act
        var result = new OASISResult<object>();
        result.IsError = true;
        result.Message = "Test error message";
        
        // Assert
        result.IsError.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task OASISResult_Should_Handle_Success_State()
    {
        // Arrange & Act
        var testData = new { Name = "Test", Value = 123 };
        var result = new OASISResult<object>(testData);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task OASISResult_Should_Handle_Exception()
    {
        // Arrange & Act
        var result = new OASISResult<object>();
        var exception = new Exception("Test exception");
        result.Exception = exception;
        
        // Assert
        result.Exception.Should().NotBeNull();
        result.Exception.Message.Should().Be("Test exception");
    }

    [Fact]
    public async Task OASISResult_Should_Support_Generic_Types()
    {
        // Arrange & Act
        var stringResult = new OASISResult<string>("test");
        var intResult = new OASISResult<int>(42);
        var boolResult = new OASISResult<bool>(true);
        
        // Assert
        stringResult.Result.Should().Be("test");
        intResult.Result.Should().Be(42);
        boolResult.Result.Should().BeTrue();
    }

    [Fact]
    public async Task OASISResult_Should_Support_Collection_Types()
    {
        // Arrange & Act
        var list = new List<string> { "item1", "item2", "item3" };
        var result = new OASISResult<List<string>>(list);
        
        // Assert
        result.Result.Should().NotBeNull();
        result.Result.Should().HaveCount(3);
        result.Result.Should().Contain("item1");
        result.Result.Should().Contain("item2");
        result.Result.Should().Contain("item3");
    }

    [Fact]
    public async Task OASISResult_Should_Support_Complex_Objects()
    {
        // Arrange
        var complexObject = new
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Properties = new Dictionary<string, object>
            {
                { "Key1", "Value1" },
                { "Key2", 123 },
                { "Key3", true }
            }
        };
        
        // Act
        var result = new OASISResult<object>(complexObject);
        
        // Assert
        result.Result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task OASISResult_Should_Handle_Null_Values()
    {
        // Arrange & Act
        var result = new OASISResult<object>(null);
        
        // Assert
        result.Result.Should().BeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task OASISResult_Should_Support_Chaining()
    {
        // Arrange & Act
        var result = new OASISResult<string>("initial")
            .SetResult("updated")
            .SetMessage("Success message");
        
        // Assert
        result.Result.Should().Be("updated");
        result.Message.Should().Be("Success message");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task OASISResult_Should_Support_Error_Chaining()
    {
        // Arrange & Act
        var result = new OASISResult<string>()
            .SetError("Error occurred")
            .SetException(new Exception("Test exception"));
        
        // Assert
        result.IsError.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Error occurred");
        result.Exception.Should().NotBeNull();
    }
}
