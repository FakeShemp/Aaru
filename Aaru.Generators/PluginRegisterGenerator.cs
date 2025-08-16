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
    // your map of simple names → registration methods
    static readonly Dictionary<string, string> PluginInterfaces = new()
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

        // …snip…
    };

#region IIncrementalGenerator Members

    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // 1) pick up every class syntax with a base‐list (so we only inspect ones that *could* have interfaces)
        IncrementalValueProvider<ImmutableArray<ClassDeclarationSyntax>> syntaxProvider = ctx.SyntaxProvider
           .CreateSyntaxProvider((node, ct) => node is ClassDeclarationSyntax cds && cds.BaseList != null,
                                 (ctx,  ct) => (ClassDeclarationSyntax)ctx.Node)
           .Collect(); // gather them all

        // 2) combine with the full Compilation so we can do symbol lookups
        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)>
            compilationAndClasses = ctx.CompilationProvider.Combine(syntaxProvider);

        // 3) finally generate source
        ctx.RegisterSourceOutput(compilationAndClasses,
                                 (spc, pair) =>
                                 {
                                     (Compilation? compilation, ImmutableArray<ClassDeclarationSyntax> classDecls) =
                                         pair;

                                     // locate the interface symbols by metadata name once
                                     (string Name, string Method, INamedTypeSymbol? Symbol)[] interfaceSymbols =
                                         PluginInterfaces
                                            .Select(kvp => (Name: kvp.Key, Method: kvp.Value,
                                                            Symbol: compilation
                                                               .GetTypeByMetadataName($"Aaru.CommonTypes.Interfaces.{kvp.Key}")))
                                            .Where(x => x.Symbol != null)
                                            .ToArray();

                                     // find the one IPluginRegister type as well
                                     INamedTypeSymbol? registerIf =
                                         compilation
                                            .GetTypeByMetadataName("Aaru.CommonTypes.Interfaces.IPluginRegister");

                                     // collect info
                                     var plugins = new List<PluginInfo>();

                                     foreach(ClassDeclarationSyntax? classDecl in classDecls.Distinct())
                                     {
                                         SemanticModel model = compilation.GetSemanticModel(classDecl.SyntaxTree);

                                         var symbol =
                                             model.GetDeclaredSymbol(classDecl, spc.CancellationToken) as
                                                 INamedTypeSymbol;

                                         if(symbol is null) continue;

                                         // which interfaces does it *actually* implement (direct + indirect)?
                                         ImmutableArray<INamedTypeSymbol> allIfaces = symbol.AllInterfaces;

                                         // diagnostics to verify we’re seeing the right interfaces
                                         foreach(INamedTypeSymbol? iface in allIfaces)
                                         {
                                             spc.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("PLGN001",
                                                                          "Found interface",
                                                                          $"Class {symbol.Name} implements {iface.ToDisplayString()}",
                                                                          "PluginGen",
                                                                          DiagnosticSeverity.Info,
                                                                          true),
                                                                      classDecl.GetLocation()));
                                         }

                                         var info = new PluginInfo
                                         {
                                             Namespace = symbol.ContainingNamespace.ToDisplayString(),
                                             ClassName = symbol.Name,
                                             IsRegister =
                                                 registerIf != null &&
                                                 allIfaces.Contains(registerIf, SymbolEqualityComparer.Default)
                                         };

                                         // pick up every plugin‐interface your map knows about
                                         foreach((string Name, string Method, INamedTypeSymbol? Symbol) in
                                                 interfaceSymbols)
                                         {
                                             if(SymbolEqualityComparer.Default.Equals(Symbol, null)) continue;

                                             if(allIfaces.Contains(Symbol, SymbolEqualityComparer.Default))
                                                 info.Interfaces.Add(Name);
                                         }

                                         if(info.IsRegister || info.Interfaces.Count > 0) plugins.Add(info);
                                     }

                                     // nothing to do
                                     if(plugins.Count == 0) return;

                                     // find the one class that implements IPluginRegister
                                     PluginInfo? regCls = plugins.FirstOrDefault(p => p.IsRegister);

                                     if(regCls == null) return;

                                     // build the generated file
                                     var sb = new StringBuilder();
                                     sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
                                     sb.AppendLine("using Aaru.CommonTypes.Interfaces;");
                                     sb.AppendLine($"namespace {regCls.Namespace};");
                                     sb.AppendLine($"public sealed partial class {regCls.ClassName} : IPluginRegister");
                                     sb.AppendLine("{");

                                     foreach(KeyValuePair<string, string> kvp in PluginInterfaces)
                                     {
                                         // grab all classes that implement this interface
                                         IEnumerable<string> implementations = plugins
                                                                              .Where(pi =>
                                                                                   pi.Interfaces
                                                                                      .Contains(kvp.Key))
                                                                              .Select(pi => pi.ClassName)
                                                                              .Distinct();

                                         sb.AppendLine($"    public void {kvp.Value}(IServiceCollection services)");
                                         sb.AppendLine("    {");

                                         foreach(string? impl in implementations)
                                             sb.AppendLine($"        services.AddTransient<{kvp.Key}, {impl}>();");

                                         sb.AppendLine("    }");
                                     }

                                     sb.AppendLine("}");

                                     spc.AddSource("Register.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                                 });
    }

#endregion

#region Nested type: PluginInfo

    class PluginInfo
    {
        public readonly List<string> Interfaces = new();
        public          string       ClassName  = "";
        public          bool         IsRegister;
        public          string       Namespace = "";
    }

#endregion
}