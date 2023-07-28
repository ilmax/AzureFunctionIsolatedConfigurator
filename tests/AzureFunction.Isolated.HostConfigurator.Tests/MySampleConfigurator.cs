using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

namespace AzureFunction.Isolated.HostConfigurator.Tests;

internal class MySampleConfigurator : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        throw new NotImplementedException();
    }
}