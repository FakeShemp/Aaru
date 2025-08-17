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
    // name → (registration method, directOnly)
    static readonly (string Name, string Method, bool DirectOnly)[] PluginMap = new[]
    {
        ("IArchive", "RegisterArchivePlugins", true), ("IChecksum", "RegisterChecksumPlugins", true),
        ("IFilesystem", "RegisterFilesystemPlugins", true), ("IFilter", "RegisterFilterPlugins", true),
        ("IFloppyImage", "RegisterFloppyImagePlugins", true),
        ("IMediaImage", "RegisterMediaImagePlugins", true), // direct only
        ("IPartition", "RegisterPartitionPlugins", true),
        ("IReadOnlyFilesystem", "RegisterReadOnlyFilesystemPlugins", true),
        ("IWritableFloppyImage", "RegisterWritableFloppyImagePlugins", true),
        ("IWritableImage", "RegisterWritableImagePlugins", false), // inherited OK
        ("IByteAddressableImage", "RegisterByteAddressablePlugins", false),
        ("IFluxImage", "RegisterFluxImagePlugins", true),
        ("IWritableFluxImage", "RegisterWritableFluxImagePlugins", false)

        // …add more as needed…
    };

#region IIncrementalGenerator Members

    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // 1) grab every ClassDeclarationSyntax that has a base list
        IncrementalValueProvider<ImmutableArray<ClassDeclarationSyntax>> classSyntaxes = ctx.SyntaxProvider
           .CreateSyntaxProvider((node, ct) => node is ClassDeclarationSyntax cds && cds.BaseList != null,
                                 (ctx,  ct) => (ClassDeclarationSyntax)ctx.Node)
           .Collect();

        // 2) combine with the compilation for symbol lookups
        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)>
            compilationAndClasses = ctx.CompilationProvider.Combine(classSyntaxes);

        // 3) register our source output
        ctx.RegisterSourceOutput(compilationAndClasses,
                                 (spc, source) =>
                                 {
                                     (Compilation? compilation, ImmutableArray<ClassDeclarationSyntax> classDecls) =
                                         source;

                                     if(compilation is null) return;

                                     // load all plugin‐interface symbols
                                     (string Name, string Method, bool DirectOnly, INamedTypeSymbol? Symbol)[]
                                         ifaceDefs = PluginMap.Select(x =>
                                                               {
                                                                   INamedTypeSymbol? sym =
                                                                       compilation
                                                                          .GetTypeByMetadataName($"Aaru.CommonTypes.Interfaces.{x.Name}");

                                                                   return (x.Name, x.Method, x.DirectOnly, Symbol: sym);
                                                               })
                                                              .Where(x => x.Symbol is not null)
                                                              .ToArray();

                                     // load IPluginRegister
                                     INamedTypeSymbol? registerSym =
                                         compilation
                                            .GetTypeByMetadataName("Aaru.CommonTypes.Interfaces.IPluginRegister");

                                     var plugins = new List<PluginInfo>();

                                     foreach(ClassDeclarationSyntax? decl in classDecls.Distinct())
                                     {
                                         SemanticModel model = compilation.GetSemanticModel(decl.SyntaxTree);

                                         var cls = model.GetDeclaredSymbol(decl, spc.CancellationToken)
                                                       as INamedTypeSymbol;

                                         if(cls is null) continue;

                                         // direct vs. all (transitive) interfaces
                                         ImmutableArray<INamedTypeSymbol> directIfaces = cls.Interfaces;
                                         ImmutableArray<INamedTypeSymbol> allIfaces    = cls.AllInterfaces;

                                         var info = new PluginInfo
                                         {
                                             Namespace = cls.ContainingNamespace.ToDisplayString(),
                                             ClassName = cls.Name,
                                             IsRegister =
                                                 registerSym != null &&
                                                 allIfaces.Contains(registerSym, SymbolEqualityComparer.Default)
                                         };

                                         // for each plugin interface, choose direct or inherited match
                                         foreach((string Name, string Method, bool DirectOnly,
                                                  INamedTypeSymbol? Symbol) in ifaceDefs)
                                         {
                                             bool matches = DirectOnly
                                                                ? directIfaces.Contains(Symbol!,
                                                                    SymbolEqualityComparer.Default)
                                                                : allIfaces.Contains(Symbol!,
                                                                    SymbolEqualityComparer.Default);

                                             if(matches) info.Interfaces.Add((Name, Method));
                                         }

                                         if(info.IsRegister || info.Interfaces.Count > 0) plugins.Add(info);
                                     }

                                     if(plugins.Count == 0) return;

                                     // find the one class that implements IPluginRegister
                                     PluginInfo? registrar = plugins.FirstOrDefault(p => p.IsRegister);

                                     if(registrar is null) return;

                                     // build the generated file
                                     var sb = new StringBuilder();
                                     sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
                                     sb.AppendLine("using Aaru.CommonTypes.Interfaces;");
                                     sb.AppendLine($"namespace {registrar.Namespace};");
                                     sb.AppendLine($"public sealed partial class {registrar.ClassName} : IPluginRegister");
                                     sb.AppendLine("{");

                                     // emit one registration method per plugin‐interface
                                     foreach((string Name, string Method, bool _) in PluginMap)
                                     {
                                         sb.AppendLine($"    public void {Method}(IServiceCollection services)");
                                         sb.AppendLine("    {");

                                         foreach(string? impl in plugins
                                                                .Where(pi => pi.Interfaces.Any(i => i.Name == Name))
                                                                .Select(pi => pi.ClassName)
                                                                .Distinct())
                                             sb.AppendLine($"        services.AddTransient<{Name}, {impl}>();");

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
        public          string                             ClassName  = "";
        public readonly List<(string Name, string Method)> Interfaces = new();
        public          bool                               IsRegister;
        public          string                             Namespace = "";
    }

#endregion
}