using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using AzureFunction.Isolated.HostConfigurator;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Diagnostics.CodeAnalysis;
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

        var assemblyPath = Path.Combine(applicationRootPath, targetAssemblyName);
        var resolver = new HostConfiguratorLoadContext(assemblyPath);
        var assembly = resolver.LoadFromAssemblyPath(assemblyPath);

        try
        {
            var attribute = assembly.GetCustomAttribute<HostConfiguratorAttribute>()
                ?? throw new InvalidOperationException($"Missing attribute HostConfiguratorAttribute on the loaded assembly {assembly.FullName}.");

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
        catch (Exception ex) when (ex is MissingMemberException or TargetInvocationException or InvalidCastException)
        {
            var message = @"Unable to call into user code, this may be due to dependencies version mismatch, 
                            make sure to reference the same version of the dependencies referenced by the host process, listed below.";
            var hostDependencies = GetHostDependenciesMessage(assembly);
            throw new InvalidOperationException(message + Environment.NewLine + hostDependencies, ex);
        }
    }

    private string GetHostDependenciesMessage(Assembly entryAssembly)
    {
        var assemblies = AssemblyLoadContext.Default.Assemblies.OrderBy(a => a.FullName).Select(a => a.GetName()).ToList();
        var sb = new StringBuilder();
        foreach (var assembly in assemblies)
        {
            sb.AppendLine(assembly.FullName);
        }

        if (entryAssembly is not null)
        {
            foreach (var dependency in entryAssembly.GetReferencedAssemblies())
            {
                if (HasAlreadyLoadedDifferentAssemblyVersion(assemblies, dependency, out var loadedAssembly))
                {
                    sb.AppendLine($"Check the reference to for {dependency.FullName} and try to use version {loadedAssembly.Version} that's already loaded by the core.");
                }
            }
        }

        return sb.ToString();
    }

    private bool HasAlreadyLoadedDifferentAssemblyVersion(List<AssemblyName> assemblies, AssemblyName dependency, [NotNullWhen(true)]out AssemblyName? loadedAssembly)
    {
        var alreadyLoaded = assemblies.FirstOrDefault(a => a.Name == dependency.Name);
        if (alreadyLoaded != null)
        {
            // FullName contains version and culture
            if (alreadyLoaded.FullName != dependency.FullName)
            {
                loadedAssembly = alreadyLoaded;
                return true;
            }
        }

        loadedAssembly = null;
        return false;
    }

    class HostConfiguratorLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public HostConfiguratorLoadContext(string pluginPath)
            : base("HostConfiguratorLoadContext", isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            foreach (var ignoredAssembly in Default.Assemblies)
            {
                var ignoredName = ignoredAssembly.GetName();

                if (AssemblyName.ReferenceMatchesDefinition(ignoredName, assemblyName))
                {
                    return null!;
                }
            }

            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, typeof(HostConfiguratorAttribute).Assembly.GetName()))
            {
                return null!;
            }

            var path = _resolver.ResolveAssemblyToPath(assemblyName);

            return path != null
                ? LoadFromAssemblyPath(path)
                : null!;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            return path != null
                ? LoadUnmanagedDllFromPath(path)
                : default;
        }
    }
}
