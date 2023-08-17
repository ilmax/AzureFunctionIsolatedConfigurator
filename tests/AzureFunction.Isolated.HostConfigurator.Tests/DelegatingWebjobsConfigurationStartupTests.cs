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
        var ex = Assert.Throws<InvalidOperationException>(() => sut.Configure(ctx, builderMock.Object));
        Assert.StartsWith("Dependency resolution failed for component", ex.Message);
        Assert.Contains("notexisting.dll", ex.Message);
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
        var builder = new MyConfigBuilder();
        Environment.SetEnvironmentVariable("ConfiguratorAssemblyName", AssemblyNameProviderRight.AssemblyName);

        // Act
        sut.Configure(ctx, builder);

        // Assert
        Assert.Equal(1, builder.ConfigurationBuilder.Sources.Count);
    }

    private Mock<IWebJobsConfigurationBuilder> GetFakeBuilder() => new();

    private WebJobsBuilderContext GetWebJobsBuilderContext() => new()
    {
        ApplicationRootPath = Environment.CurrentDirectory,
        Configuration = new ConfigurationBuilder().Build(),
        EnvironmentName = "Development"
    };

    class MyConfigBuilder : IWebJobsConfigurationBuilder
    {
        readonly ConfigurationBuilder _builder = new();

        public IConfigurationBuilder ConfigurationBuilder => _builder;
    }
}
