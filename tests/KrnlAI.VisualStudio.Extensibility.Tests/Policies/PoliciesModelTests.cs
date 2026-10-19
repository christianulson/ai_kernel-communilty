using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Policies;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Policies;

public sealed class PoliciesModelTests
{
    [Fact]
    public void AddPolicy_ShouldAppendPolicy()
    {
        var model = new PoliciesModel();

        model.AddPolicy("R01 safety rule");

        model.Policies.Should().ContainSingle().Which.Name.Should().Be("R01 safety rule");
    }
}