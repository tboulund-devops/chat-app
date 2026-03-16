using Application.Common.Results;

namespace Unit;

public class ResultTests
{
    [Fact]
    public void Success_ShouldHaveSuccessStatus_AndIsSuccessTrue()
    {
        var result = Result.Success();
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Failure_ShouldHaveFailureStatus_AndIsSuccessFalse()
    {
        var result = Result.Failure("something went wrong");
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal("something went wrong", result.Message);
    }

    [Fact]
    public void Success_WithCustomMessage_ShouldPreserveMessage()
    {
        var result = Result.Success("custom message");
        Assert.Equal("custom message", result.Message);
    }

    [Fact]
    public void GenericSuccess_ShouldCarryDto_AndBeSuccessful()
    {
        var result = Result<string>.Success("payload");
        Assert.True(result.IsSuccess);
        Assert.Equal("payload", result.Dto);
    }

    [Fact]
    public void GenericFailure_ShouldHaveNullDto_AndCorrectStatus()
    {
        var result = Result<string>.Failure("error");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Dto);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public void GenericFailure_WithExplicitStatus_ShouldUseProvidedStatus()
    {
        var result = Result<string>.Failure("unauthorized", ResultStatus.Unauthorized);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Dto);
    }
}