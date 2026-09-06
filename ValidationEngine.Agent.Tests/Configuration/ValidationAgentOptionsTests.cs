using ValidationEngine.Agent.Configuration;

namespace ValidationEngine.Agent.Tests.Configuration;

public class ValidationAgentOptionsTests
{
    [Fact]
    public void GetActiveByPurpose_ReturnsOnlyActiveProvidersMatchingPurpose()
    {
        var options = new ValidationAgentOptions
        {
            Providers =
            [
                new ModelProviderOptions { Name = "A", Type = "AzureOpenAI", Purpose = "Validation", IsActive = true },
                new ModelProviderOptions { Name = "B", Type = "AzureOpenAI", Purpose = "Validation", IsActive = false },
                new ModelProviderOptions { Name = "C", Type = "AzureOpenAI", Purpose = "Explain", IsActive = true }
            ]
        };

        var active = options.GetActiveByPurpose(AgentPurpose.Validation);

        Assert.Single(active);
        Assert.Equal("A", active[0].Name);
    }

    [Fact]
    public void GetActiveByPurpose_SupportsMultipleActiveProvidersForSamePurpose()
    {
        var options = new ValidationAgentOptions
        {
            Providers =
            [
                new ModelProviderOptions { Name = "A", Type = "AzureOpenAI", Purpose = "Validation", IsActive = true },
                new ModelProviderOptions { Name = "B", Type = "AzureOpenAI", Purpose = "Validation", IsActive = true }
            ]
        };

        var active = options.GetActiveByPurpose(AgentPurpose.Validation);

        Assert.Equal(2, active.Count);
        Assert.Contains(active, p => p.Name == "A");
        Assert.Contains(active, p => p.Name == "B");
    }

    [Fact]
    public void GetActiveByPurpose_ReturnsEmpty_WhenNoProvidersConfigured()
    {
        var options = new ValidationAgentOptions();

        var active = options.GetActiveByPurpose(AgentPurpose.Validation);

        Assert.Empty(active);
    }

    [Fact]
    public void GetActiveByPurpose_IsCaseInsensitiveOnPurpose()
    {
        var options = new ValidationAgentOptions
        {
            Providers =
            [
                new ModelProviderOptions { Name = "A", Type = "AzureOpenAI", Purpose = "validation", IsActive = true }
            ]
        };

        var active = options.GetActiveByPurpose(AgentPurpose.Validation);

        Assert.Single(active);
    }
}
