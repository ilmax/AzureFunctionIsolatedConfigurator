using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using AzureFunction.Isolated.HostConfigurator;
using System.Reflection;
using System.Runtime.Loader;
#if DEBUG
using System.Diagnostics;
#endif

[assembly: WebJobsStartup(typeof(DelegatingWebJobsConfigurationStartup))]
[assembly: ExtensionInformation("AzureFunction.Isolated.HostConfigurator", ProjectConstants.Version, true)]

namespace AzureFunction.Isolated.HostConfigurator;

public class DelegatingWebJobsConfigurationStartup : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
#if DEBUG
        Debugger.Launch();
#endif

        var applicationRootPath = context.ApplicationRootPath;

        var tmpConfig = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(applicationRootPath, "host.json"), optional: true)
            .AddJsonFile(Path.Combine(applicationRootPath, "appsettings.json"), optional: true)
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

        using var resolver = new AssemblyResolver(applicationRootPath);
        Assembly assembly = Assembly.LoadFrom(Path.Combine(applicationRootPath, targetAssemblyName));

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

    class AssemblyResolver : IDisposable
    {
        private readonly string _functionPath;

        public AssemblyResolver(string functionPath)
        {
            _functionPath = functionPath;
            AssemblyLoadContext.Default.Resolving += OnResolvingAssembly;
        }

        private Assembly? OnResolvingAssembly(AssemblyLoadContext context, AssemblyName name)
        {
            var assemblyPath = Path.Combine(_functionPath, $"{name.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }

        public void Dispose()
        {
            AssemblyLoadContext.Default.Resolving -= OnResolvingAssembly;
        }
    }
}
