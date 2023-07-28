using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AzureFunction.Isolated.HostConfigurator.Tests;

public class DelegatingWebjobsConfigurationStartupTests
{
    [Fact]
    public void DelegatingWebJobsConfigurationStartup_throws_with_null_ConfiguratorAssemblyName()
    {
        // Arrange
        var sut = new DelegatingWebJobsConfigurationStartup();
        var ctx = GetWebJobsBuilderContext();
        var builderMock = GetFakeBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", "");

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => sut.Configure(ctx, builderMock.Object));
    }

    [Fact]
    public void DelegatingWebJobsConfigurationStartup_throws_with_unexistent_assembly_name()
    {
        // Arrange
        var sut = new DelegatingWebJobsConfigurationStartup();
        var ctx = GetWebJobsBuilderContext();
        var builderMock = GetFakeBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", "notexisting.dll");

        // Act + Assert
        Assert.Throws<FileNotFoundException>(() => sut.Configure(ctx, builderMock.Object));
    }

    [Fact]
    public void DelegatingWebJobsConfigurationStartup_throws_when_assembly_is_missing_attribute()
    {
        // Arrange
        var sut = new DelegatingWebJobsConfigurationStartup();
        var ctx = GetWebJobsBuilderContext();
        var builderMock = GetFakeBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", GetType().Assembly.GetName().Name);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => sut.Configure(ctx, builderMock.Object));
    }

    [Fact]
    public void DelegatingWebJobsConfigurationStartup_throws_when_attribute_type_is_wrong()
    {
        // Arrange
        var sut = new DelegatingWebJobsConfigurationStartup();
        var ctx = GetWebJobsBuilderContext();
        var builderMock = GetFakeBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", AssemblyNameProviderWrong.AssemblyName);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => sut.Configure(ctx, builderMock.Object));
    }

    [Fact]
    public void DelegatingWebJobsConfigurationStartup_should_call_the_method()
    {
        // Arrange
        var sut = new DelegatingWebJobsConfigurationStartup();
        var ctx = GetWebJobsBuilderContext();
        var builderMock = GetFakeBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", AssemblyNameProviderRight.AssemblyName);

        // Act
        sut.Configure(ctx, builderMock.Object);

        // Assert
        Assert.True(MyConfiguration.Executed);
    }

    private Mock<IWebJobsConfigurationBuilder> GetFakeBuilder() => new();

    private WebJobsBuilderContext GetWebJobsBuilderContext() => new()
    {
        ApplicationRootPath = Environment.CurrentDirectory,
        Configuration = new ConfigurationBuilder().Build(),
        EnvironmentName = "Development"
    };


}
