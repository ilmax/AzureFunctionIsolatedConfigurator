# Problem
The new dotnet-isolated hosting model of Azure Functions doesn't allow you to configure the connection for triggers or binding the same way it does in the In-Process model. This is a problem when you want to use a connection string from Key Vault, for example.
The problem is that the host process needs the connection before it calls into your code (i.e. before your `Program.cs` is called) so you only have a couple of ways to configure it:
- Use an environment variable
- Use a `local.settings.json` file

Both of these options may not be sufficient to cover your scenario so this package helps you work around this limitation that will hopefully be resolved soon on the Azure Functions side. See [this issue](https://github.com/Azure/azure-functions-dotnet-worker/issues/1790) and [this issue](https://github.com/MicrosoftDocs/azure-docs/issues/95950) for context.

## How it works
Azure Functions using the dotnet-isolated hosting model will load the extensions that your project references and will initialize the extensions before trying to resolve the connection strings.

This step happens inside the `AddScriptHost` method of the `ScriptHostBuilderExtensions` when the host calls the `HasExternalConfigurationStartups()` method. (the code I'm talking about here is in the [azure-function-host](https://github.com/Azure/azure-functions-host) project on GitHub [here](https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/ScriptHostBuilderExtensions.cs).

So this package essentially hooks into that functionality to allow you to customize the host configuration before it's used.

This package implements a WebJob extension that will delegate the configuration to a type that **you have to implement and specify in your project using the assembly attribute** `[HostConfiguratorAttribute(typeof(TheTypeToBeInvoked))]`.

You need to specify a type that implements the interface `IWebJobsConfigurationStartup` from the package `Microsoft.Azure.WebJobs` in the attribute, this type will then be invoked by this extension allowing you to add additional configuration sources to the host configuration.

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
4. Set the `ConfiguratorAssemblyName` configuration with the assembly name of the dll that contains the `[assembly: HostConfiguratorAttribute]` attribute. Such configuration can be added to either the `host.json` the `appsettings.json` or as an environment variable

`host.json` example
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  },
  "ConfiguratorAssemblyName": "FunctionApp1.dll"
}
```
`appsettings.json` example
```json
{
  "ConfiguratorAssemblyName": "FunctionApp1.dll"
}

```

## Caveats
This package provide a workaround for an unsupported functionality loading an assembly that you specify into the function host process, this comes with a set of limitations and constraints as you can see below.

- If you need to call `AddJsonFile('appsettings.json')`, make sure to call `Path.Combine(context.ApplicationRootPath, "appsettings.json")` because the host will not be running from the same directory your code is running from.

- If you use a single assembly approach, you're forced to target `net6.0` and your assembly will be loaded twice, once in the host process and once by the Azure Functions host. If you have any static initialization code, it may potentially be called twice. To prevent this issue you should define a standalone assembly that contains the configuration code instead of adding the type in your functions code directly.

- If you need an assembly reference in your configuration class, you have to make sure it can be loaded in the host process (the function host) that is on .NET 6.0 and you may run into file load exceptions, e.g. trying to use `Console.WriteLine` in a **net7.0** project throws the following:

    ```
    Error building configuration in an external startup class. FunctionApp1: Could not load file or assembly 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.
    ```
    while running the same code in a **net6.0** project works just fine. I suggest keeping the code in the host configurator class that you will implement as small as possible, and if you need to target `net7.0`, place it in a separate assembly and target `net6.0`, see an example [here](https://github.com/ilmax/AzureFunctionIsolatedConfigurator/tree/main/samples/AppConfigDifferentAssemblySample).

- Since `local.settings.json` is not published during deployment, I decided not to include that file in the dll name lookup [here](https://github.com/ilmax/AzureFunctionIsolatedConfigurator/blob/main/src/AzureFunction.Isolated.HostConfigurator/DelegatingWebJobsConfigurationStartup.cs#L27-L31). If you add the `appsettings.json` file, make sure to set the `Copy to Output Directory` property to `Copy always` or `Copy if newer` so that the file is copied to the output directory during the build.
You're also constrained to the version of the packages you're using, for KeyVault and App Configuration, you can copy that from the relative samples.

- You may get a warning telling you that: `The Functions scale controller may not scale the following functions correctly because some configuration values were modified in an external startup class.`
This is caused by a validation that checks if the configuration values have been modified by an external startup class, which is the case here since the original configuration has no means to read from the additional configuration sources we want to add. This warning can be safely ignored since it's the problem this library tries to solve.
This behaviour is implemented in the [ExternalConfigurationStartupValidator](https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/DependencyInjection/ExternalConfigurationStartupValidator.cs#L63-L64) in the azure-functions-host project.

> Note that if you use managed identities with e.g. Azure Service Bus you won't get the warning because the configuration value is null but not the nested configuration called `fullyQualifiedNamespace` and the service only checks for top level ones.


- You can't set a breakpoint in the code loaded by the host configurator and if you do so, you will realize that the breakpoint won't be hit. If you need to debug the code, you can use the `Debugger.Launch()` method to attach a debugger to the process.

## Samples
Check the [/samples](https://github.com/ilmax/AzureFunctionIsolatedConfigurator/tree/main/samples) folder for some sample projects.

> This has been tested on Visual Studio 17.6.5 and via the func cli 4.0.5085 both of which can successfully run a project.
