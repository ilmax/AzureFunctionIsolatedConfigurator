namespace AzureFunction.Isolated.HostConfigurator.Tests;

public class AttributeTests
{
    [Fact]
    public void HostConfigratoAttribute_throws_with_null_ctor_param()
    {
        // Act + Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new HostConfiguratorAttribute(null!));
        Assert.Equal("configuratorType", ex.ParamName);
    }

    [Fact]
    public void HostConfigratoAttribute_sets_type()
    {
        // Act
        var attrib = new HostConfiguratorAttribute(typeof(int));

        // Assert
        Assert.Equal(typeof(int), attrib.ConfiguratorType);
    }

    
    [Fact]
    public void HostConfigratoAttributeOfT_sets_type()
    {
        // Act
        var attrib = new HostConfiguratorAttribute<MySampleConfigurator>();

        // Assert
        Assert.Equal(typeof(MySampleConfigurator), attrib.ConfiguratorType);
    }
}