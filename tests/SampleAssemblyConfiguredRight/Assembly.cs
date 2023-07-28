using AzureFunction.Isolated.HostConfigurator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: HostConfigurator(typeof(MyConfiguration))]

public static class AssemblyNameProviderRight
{
    public static string AssemblyName => typeof(AssemblyNameProviderRight).Assembly.GetName().Name!;
}

public class MyConfiguration : IWebJobsConfigurationStartup
{
    public MyConfiguration()
    {
        Executed = false;
    }

    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        Executed = true;
    }

    public static bool Executed { get; private set; }
}