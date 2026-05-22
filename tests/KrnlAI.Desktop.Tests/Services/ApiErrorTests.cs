using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class ApiErrorTests
{
    [Fact]
    public void Classify_401_ShouldReturnAuthError()
    {
        var error = ApiErrorClassifier.Classify(401, "Unauthorized");
        Assert.IsType<KrnlAiAuthenticationError>(error);
        Assert.Equal("Unauthorized", error.Message);
    }

    [Fact]
    public void Classify_404_ShouldReturnNotFoundError()
    {
        var error = ApiErrorClassifier.Classify(404, "Not found");
        Assert.IsType<KrnlAiNotFoundError>(error);
    }

    [Fact]
    public void Classify_422_ShouldReturnValidationError()
    {
        var error = ApiErrorClassifier.Classify(422, "Invalid");
        Assert.IsType<KrnlAiValidationError>(error);
    }

    [Fact]
    public void Classify_500_ShouldReturnServerError()
    {
        var error = ApiErrorClassifier.Classify(500, "Server error");
        Assert.IsType<KrnlAiServerError>(error);
        var serverError = (KrnlAiServerError)error;
        Assert.Equal(500, serverError.StatusCode);
    }

    [Fact]
    public void ShouldRetry_ServerError_ShouldReturnTrue()
    {
        var error = new KrnlAiServerError("fail");
        Assert.True(ApiErrorClassifier.ShouldRetry(error));
    }

    [Fact]
    public void ShouldRetry_ValidationError_ShouldReturnFalse()
    {
        var error = new KrnlAiValidationError("invalid");
        Assert.False(ApiErrorClassifier.ShouldRetry(error));
    }

    [Fact]
    public void GetBackoffDelay_Attempt0_ShouldBe1Second()
    {
        var delay = ApiErrorClassifier.GetBackoffDelay(0);
        Assert.Equal(1000, delay.TotalMilliseconds);
    }

    [Fact]
    public void GetBackoffDelay_Attempt3_ShouldBe8Seconds()
    {
        var delay = ApiErrorClassifier.GetBackoffDelay(3);
        Assert.Equal(8000, delay.TotalMilliseconds);
    }
}
