using Azure.Identity;
using AzureFunction.Isolated.HostConfigurator;
using KeyVaultSample;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: HostConfigurator(typeof(StartupExtension))]

namespace KeyVaultSample;

internal class StartupExtension : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder
            .AddAzureKeyVault(new Uri("https://rnhb-kv-bsprio-dev.vault.azure.net/"), new DefaultAzureCredential());
    }
}
