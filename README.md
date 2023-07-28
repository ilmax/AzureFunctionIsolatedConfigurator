# Problem
The new dotnet-isolated hosting model of Azure Functions doesn't allow you to configure the connection for triggers or binding the same way it does in the In-Process model. This is a problem when you want to use a connection string from Key Vault, for example.
The problem is that the host process needs the connection before it calls into your code (i.e. before your `Program.cs` is called) so you only have a couple of ways to configure it:
- Use an environment variable
- Use a local.settings.json file

Both of these options may not be sufficient to cover your scenario so this package helps you solve that.

## How it works
Azure Functions using the dotnet-isolated hosting model will load the extensions that your project references and will initialize the extensions before trying to resolve the connection strings.

This step happens inside the `AddScriptHost` method of the `ScriptHostBuilderExtensions` when the host calls the `HasExternalConfigurationStartups()` method. (the code I'm talking about here is in the [azure-function-host](https://github.com/Azure/azure-functions-host) project on GitHub [here](https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/ScriptHostBuilderExtensions.cs).

So this package essentially hooks into that functionality to allow you to customize the host configuration before it's used'.

This package implements a WebJob extension that will delegate the configuration to a type that you specify in your project using the assembly attribute `[HostConfiguratorAttribute(typeof(TheTypeToBeInvoked))]`.
You need to specify a type that implements the interface `IWebJobsConfigurationStartup` from the package `Microsoft.Azure.WebJobs` in the attribute, this type will then be invoked by this extension allowing you to add additional configuration sources to the host configuration.

> If you are targeting .net 7.0, you can use the generic version of the attribute `[HostConfiguratorAttribute<T>]` which will allow you to specify the type of the configurator directly in the attribute.

## Usage
1. Install the package `AzureFunction.Isolated.HostConfigurator` into your Azure Functions dotnet-isolated project.
```
dotnet add package AzureFunction.Isolated.HostConfigurator
```
2. Create a class and implement the interface `IWebJobsConfigurationStartup` from the package `Microsoft.Azure.WebJobs` in your project.
```csharp
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
```

3. Add the attribute `[HostConfiguratorAttribute(typeof(TheTypeToBeInvoked))]` to your project.

```csharp
[assembly: HostConfigurator<StartupExtension>]
```

## Caveats
If you need to call `AddJsonFile('appsettings.json')`, make sure to call `Path.Combine(context.ApplicationRootPath, "appsettings.json")` because the host will not be running from the same directory your code is running from.

For now, your assembly will be loaded twice, once in the host process and once by the Azure Functions host, so if you have any static initialization code, it may potentially be called twice. This is a known issue and will be fixed in the future potentially using the `AssemblyLoadContext` functionality. A simple workaround may be to define a standalone assembly that contains the configuration code instead of adding the type in your functions code directly.

## Samples
Check the [/samples](https://github.com/ilmax/AzureFunctionIsolatedConfigurator/tree/main/samples/FunctionApp1) folder for a sample project.
