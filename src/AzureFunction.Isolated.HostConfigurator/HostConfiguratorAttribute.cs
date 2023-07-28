using Microsoft.Azure.WebJobs.Hosting;
using System.ComponentModel;

namespace AzureFunction.Isolated.HostConfigurator;

#if NET7_0_OR_GREATER
/// <summary>
/// The assembly attribute used to specify which configurator type to invoke.
/// </summary>
/// <typeparam name="TConfigurator">The type you want to invoke to configure the host application, has to implement <see cref="IWebJobsConfigurationStartup"/>.</typeparam>
public sealed class HostConfiguratorAttribute<TConfigurator> : HostConfiguratorAttribute where TConfigurator : IWebJobsConfigurationStartup, new()
{
    public HostConfiguratorAttribute()
        : base(typeof(TConfigurator))
    { }
}
#endif

/// <summary>
/// The assembly attribute used to specify which configurator type to invoke.
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public class HostConfiguratorAttribute : Attribute
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