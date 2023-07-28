namespace AzureFunction.Isolated.HostConfigurator.Tests;

public class AttributeTests
{
    [Fact]
    public void HostConfiguratorAttribute_throws_with_null_ctor_param()
    {
        // Act + Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new HostConfiguratorAttribute(null!));
        Assert.Equal("configuratorType", ex.ParamName);
    }

    [Fact]
    public void HostConfiguratorAttribute_sets_type()
    {
        // Act
        var attrib = new HostConfiguratorAttribute(typeof(int));

        // Assert
        Assert.Equal(typeof(int), attrib.ConfiguratorType);
    }
}
