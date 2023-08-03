using AzureFunction.Isolated.HostConfigurator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: HostConfigurator(typeof(MyConfiguration))]

public static class AssemblyNameProviderRight
{
    public static string AssemblyName => typeof(AssemblyNameProviderRight).Assembly.GetName().Name!;
}

public class MyConfiguration : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder.AddInMemoryCollection();
    }
}
