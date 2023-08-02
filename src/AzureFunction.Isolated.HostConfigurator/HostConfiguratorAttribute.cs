namespace AzureFunction.Isolated.HostConfigurator;

/// <summary>
/// The assembly attribute used to specify which configurator type to invoke.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class HostConfiguratorAttribute : Attribute
{
    public HostConfiguratorAttribute(Type configuratorType)
    {
        ConfiguratorType = configuratorType ?? throw new ArgumentNullException(nameof(configuratorType));
    }

    /// <summary>
    /// Represents the configuration type that will be invoked during the host run and before the start of you (worker) process
    /// </summary>
    public Type ConfiguratorType { get; }
}
