﻿using AppConfigSample;
using AzureFunction.Isolated.HostConfigurator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: HostConfigurator(typeof(StartupExtension))]

namespace AppConfigSample;

internal class StartupExtension : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        // Please note the usage of the context.ApplicationRootPath to get the path to the application root, where we can find our configuration files
        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: false, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();
    }
}
