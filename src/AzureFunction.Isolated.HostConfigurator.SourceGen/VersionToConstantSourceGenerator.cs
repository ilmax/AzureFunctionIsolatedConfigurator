﻿using System.Linq;
using Microsoft.CodeAnalysis;

namespace AzureFunction.Isolated.HostConfigurator.SourceGen
{
    [Generator]
    public sealed class VersionToConstantSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var version = GetVersionFromProjectFile(context);

            string namespaceName = context.Compilation?.AssemblyName ?? "AzureFunction.Isolated.HostConfigurator";

            string source = $@"// <auto-generated/>
namespace {namespaceName};

internal static class ProjectConstants
{{
    public const string Version = ""{version}"";
}}
";

            context.AddSource($"Constant.g.cs", source);
        }

        private string GetVersionFromProjectFile(GeneratorExecutionContext context)
        {
            var assemblySymbol = context.Compilation.GetTypeByMetadataName("System.Reflection.AssemblyInformationalVersionAttribute");
            var attributes = context.Compilation.Assembly.GetAttributes();
            var att = attributes.First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, assemblySymbol));
            var arg = att.ConstructorArguments.First();
            return arg.Value.ToString();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}
