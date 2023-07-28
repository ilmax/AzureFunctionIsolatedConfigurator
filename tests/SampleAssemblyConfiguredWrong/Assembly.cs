using AzureFunction.Isolated.HostConfigurator;

[assembly: HostConfigurator(typeof(int))]

public static class AssemblyNameProviderWrong
{
    public static string AssemblyName => typeof(AssemblyNameProviderWrong).Assembly.GetName().Name!;
}