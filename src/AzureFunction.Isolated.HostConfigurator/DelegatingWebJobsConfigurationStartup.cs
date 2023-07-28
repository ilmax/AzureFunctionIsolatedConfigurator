using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using AzureFunction.Isolated.HostConfigurator;
using System.Diagnostics;
using System.Reflection;

[assembly: WebJobsStartup(typeof(DelegatingWebJobsConfigurationStartup))]
[assembly: ExtensionInformation("AzureFunction.Isolated.HostConfigurator", "1.0.0", true)]

namespace AzureFunction.Isolated.HostConfigurator;

public class DelegatingWebJobsConfigurationStartup : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {

//#if DEBUG
//        Debugger.Launch();
//#endif

        var tmpConfig = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "host.json"), optional: true)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var targetAssemblyName = tmpConfig.GetValue<string>("ConfiguratorAssemblyName");
        if (string.IsNullOrWhiteSpace(targetAssemblyName))
        {
            throw new InvalidOperationException("ConfiguratorAssemblyName has not bees specified, ensure to add it to either the host.json file, the appsettings.json file or as an environment variable.");
        }
        if (!targetAssemblyName.EndsWith(".dll"))
        {
            targetAssemblyName = $"{targetAssemblyName}.dll";
        }
        Assembly assembly = Assembly.LoadFrom(Path.Combine(context.ApplicationRootPath, targetAssemblyName));

        var attribute = assembly.GetCustomAttribute<HostConfiguratorAttribute>();
        if (attribute is null)
        {
            throw new InvalidOperationException($"Missing attribute HostConfiguratorAttribute on the assembly {targetAssemblyName}.");
        }

        var instance = Activator.CreateInstance(attribute.ConfiguratorType);
        if (instance is IWebJobsConfigurationStartup startup)
        {
            startup.Configure(context, builder);
        }
        else
        {
            throw new InvalidOperationException($"Type {attribute.ConfiguratorType.AssemblyQualifiedName} does not implement IWebJobsConfigurationStartup.");
        }
    }
}