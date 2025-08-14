#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Aaru.Generators;

[Generator]
public sealed class PluginRegisterGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, string> PluginInterfaces = new()
    {
        ["IArchive"]              = "RegisterArchivePlugins",
        ["IChecksum"]             = "RegisterChecksumPlugins",
        ["IFilesystem"]           = "RegisterFilesystemPlugins",
        ["IFilter"]               = "RegisterFilterPlugins",
        ["IFloppyImage"]          = "RegisterFloppyImagePlugins",
        ["IMediaImage"]           = "RegisterMediaImagePlugins",
        ["IPartition"]            = "RegisterPartitionPlugins",
        ["IReadOnlyFilesystem"]   = "RegisterReadOnlyFilesystemPlugins",
        ["IWritableFloppyImage"]  = "RegisterWritableFloppyImagePlugins",
        ["IWritableImage"]        = "RegisterWritableImagePlugins",
        ["IByteAddressableImage"] = "RegisterByteAddressablePlugins",
        ["IFluxImage"]            = "RegisterFluxImagePlugins",
        ["IWritableFluxImage"]    = "RegisterWritableFluxImagePlugins"
    };

#region IIncrementalGenerator Members

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<PluginInfo>> pluginClasses = context.SyntaxProvider
           .CreateSyntaxProvider(static (node, _) => node is ClassDeclarationSyntax,
                                 static (ctx,  _) => GetPluginInfo(ctx))
           .Where(static info => info is not null)
           .Collect();

        context.RegisterSourceOutput(pluginClasses, (ctx, pluginInfos) => GeneratePluginRegister(ctx, pluginInfos!));
    }

#endregion

    private static PluginInfo? GetPluginInfo(GeneratorSyntaxContext context)
    {
        if(context.Node is not ClassDeclarationSyntax classDecl) return null;

        var info = new PluginInfo
        {
            ClassName  = classDecl.Identifier.Text,
            Namespace  = GetNamespace(classDecl),
            IsRegister = ImplementsInterface(classDecl, "IPluginRegister")
        };

        foreach(string? iface in PluginInterfaces.Keys)
        {
            if(ImplementsInterface(classDecl, iface)) info.Interfaces.Add(iface);
        }

        if(info is { IsRegister: false, Interfaces.Count: 0 }) return null;

        return info;
    }

    private static bool ImplementsInterface(ClassDeclarationSyntax classDecl, string interfaceName)
    {
        return classDecl.BaseList?.Types.Any(t => (t.Type as IdentifierNameSyntax)?.Identifier.ValueText ==
                                                  interfaceName) ==
               true;
    }

    private static string? GetNamespace(SyntaxNode node) =>
        node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();

    private static void GeneratePluginRegister(SourceProductionContext context, IReadOnlyList<PluginInfo> pluginInfos)
    {
        PluginInfo? registerClass = pluginInfos.FirstOrDefault(p => p.IsRegister);

        if(registerClass is null) return;

        var sb = new StringBuilder();

        sb.AppendLine("""
                      // /***************************************************************************
                      // Aaru Data Preservation Suite
                      // ----------------------------------------------------------------------------
                      //
                      // Filename       : Register.g.cs
                      // Author(s)      : Natalia Portillo <claunia@claunia.com>
                      //
                      // --[ Description ] ----------------------------------------------------------
                      //
                      //     Registers all plugins in this assembly.
                      //
                      // --[ License ] --------------------------------------------------------------
                      //
                      //     Permission is hereby granted, free of charge, to any person obtaining a
                      //     copy of this software and associated documentation files (the
                      //     "Software"), to deal in the Software without restriction, including
                      //     without limitation the rights to use, copy, modify, merge, publish,
                      //     distribute, sublicense, and/or sell copies of the Software, and to
                      //     permit persons to whom the Software is furnished to do so, subject to
                      //     the following conditions:
                      //
                      //     The above copyright notice and this permission notice shall be included
                      //     in all copies or substantial portions of the Software.
                      //
                      //     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
                      //     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
                      //     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
                      //     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
                      //     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
                      //     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
                      //     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
                      //
                      // ----------------------------------------------------------------------------
                      // Copyright © 2011-2025 Natalia Portillo
                      // ****************************************************************************/
                      """);

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Aaru.CommonTypes.Interfaces;");
        sb.AppendLine();
        sb.AppendLine($"namespace {registerClass.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public sealed partial class {registerClass.ClassName} : IPluginRegister");
        sb.AppendLine("{");

        foreach(KeyValuePair<string, string> kvp in PluginInterfaces)
        {
            string? interfaceName = kvp.Key;
            string? methodName    = kvp.Value;

            var plugins = pluginInfos.Where(p => p.Interfaces.Contains(interfaceName))
                                     .Select(p => p.ClassName)
                                     .Distinct()
                                     .ToList();

            sb.AppendLine($"    public void {methodName}(IServiceCollection services)");
            sb.AppendLine("    {");

            foreach(string? plugin in plugins)
                sb.AppendLine($"        services.AddTransient<{interfaceName}, {plugin}>();");

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        context.AddSource("Register.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

#region Nested type: PluginInfo

    private sealed class PluginInfo
    {
        public string?      Namespace  { get; set; }
        public string       ClassName  { get; set; } = "";
        public bool         IsRegister { get; set; }
        public List<string> Interfaces { get; } = [];
    }

#endregion
}