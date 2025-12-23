// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SwapEndianGenerator.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru.Generators.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Aaru.Generators;

[Generator]
public class SwapEndianGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with the [SwapEndian] attribute and collect semantic info
        // The attribute is defined in Aaru.CommonTypes.Attributes namespace
        IncrementalValuesProvider<(StructDeclarationSyntax structDecl,
            List<(string fieldName, ITypeSymbol typeSymbol, string typeName)> fieldTypes)?> structDeclarations =
            context.SyntaxProvider
                   .CreateSyntaxProvider(static (s,   _) => IsSyntaxTargetForGeneration(s),
                                         static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                   .Where(static m => m.HasValue);

        context.RegisterSourceOutput(structDeclarations, static (spc, source) => Execute(source.Value, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is StructDeclarationSyntax { AttributeLists.Count: > 0 };

    private static (StructDeclarationSyntax structDecl,
        List<(string fieldName, ITypeSymbol typeSymbol, string typeName)> fieldTypes)? GetSemanticTargetForGeneration(
            GeneratorSyntaxContext context)
    {
        var structDeclaration = (StructDeclarationSyntax)context.Node;

        foreach(AttributeListSyntax attributeList in structDeclaration.AttributeLists)
        {
            foreach(AttributeSyntax attribute in attributeList.Attributes)
            {
                ISymbol symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;

                if(symbol is not IMethodSymbol attributeSymbol) continue;

                INamedTypeSymbol attributeContainingType = attributeSymbol.ContainingType;
                string           fullName                = attributeContainingType.ToDisplayString();

                if(fullName != "Aaru.CommonTypes.Attributes.SwapEndianAttribute") continue;

                // Collect field type information here where we have access to semantic model

                IEnumerable<FieldDeclarationSyntax> fields = structDeclaration.Members.OfType<FieldDeclarationSyntax>();

                var fieldTypes = (from field in fields
                                  let typeInfo = context.SemanticModel.GetTypeInfo(field.Declaration.Type)
                                  let typeSymbol = typeInfo.Type
                                  let typeName = field.Declaration.Type.ToString()
                                  from variable in field.Declaration.Variables
                                  select (variable.Identifier.Text, typeSymbol, typeName)).ToList();

                return (structDeclaration, fieldTypes);
            }
        }

        return null;
    }

    private static void Execute(
        (StructDeclarationSyntax structDeclaration, List<(string fieldName, ITypeSymbol typeSymbol, string typeName)>
            fieldTypes) source, SourceProductionContext context)
    {
        if(source.structDeclaration == null) return;

        string                              structName      = source.structDeclaration.Identifier.Text;
        string                              namespaceName   = GetNamespace(source.structDeclaration);
        List<(string Name, string Keyword)> containingTypes = GetContainingTypes(source.structDeclaration);

        string generatedSource =
            GenerateSwapEndianMethod(structName, namespaceName, containingTypes, source.fieldTypes);

        // Create unique file name by including containing types
        string fileName = containingTypes.Count > 0
                              ? $"{string.Join("_", containingTypes.Select(static t => t.Name))}_{structName}_SwapEndian.g.cs"
                              : $"{structName}_SwapEndian.g.cs";

        context.AddSource(fileName, SourceText.From(generatedSource, Encoding.UTF8));
    }

    private static string GetNamespace(SyntaxNode syntax)
    {
        // Try file-scoped namespace first
        FileScopedNamespaceDeclarationSyntax fileScopedNamespace = syntax.Ancestors()
                                                                         .OfType<FileScopedNamespaceDeclarationSyntax>()
                                                                         .FirstOrDefault();

        if(fileScopedNamespace != null) return fileScopedNamespace.Name.ToString();

        // Try regular namespace
        NamespaceDeclarationSyntax namespaceDeclaration = syntax.Ancestors()
                                                                .OfType<NamespaceDeclarationSyntax>()
                                                                .FirstOrDefault();

        return namespaceDeclaration?.Name.ToString() ?? string.Empty;
    }

    private static List<(string Name, string Keyword)> GetContainingTypes(SyntaxNode syntax)
    {
        var containingTypes = new List<(string Name, string Keyword)>();

        foreach(SyntaxNode ancestor in syntax.Ancestors())
        {
            switch(ancestor)
            {
                case ClassDeclarationSyntax classDecl:
                    string classModifiers = GetValidPartialModifiers(classDecl.Modifiers);
                    containingTypes.Insert(0, (classDecl.Identifier.Text, $"{classModifiers}partial class".Trim()));

                    break;
                case StructDeclarationSyntax structDecl:
                    string structModifiers = GetValidPartialModifiers(structDecl.Modifiers);
                    containingTypes.Insert(0, (structDecl.Identifier.Text, $"{structModifiers}partial struct".Trim()));

                    break;
                case RecordDeclarationSyntax recordDecl:
                    string recordModifiers = GetValidPartialModifiers(recordDecl.Modifiers);
                    containingTypes.Insert(0, (recordDecl.Identifier.Text, $"{recordModifiers}partial record".Trim()));

                    break;
            }
        }

        return containingTypes;
    }

    private static string GetValidPartialModifiers(SyntaxTokenList modifiers)
    {
        var validModifiers = (from modifier in modifiers
                              select modifier.Text
                              into modifierText
                              where modifierText != "partial"
                              where modifierText is "public" or "internal" or "private" or "protected" or "unsafe"
                              select modifierText).ToList();

        return validModifiers.Count > 0 ? string.Join(" ", validModifiers) + " " : string.Empty;
    }

    private static string GenerateSwapEndianMethod(string                              structName, string namespaceName,
                                                   List<(string Name, string Keyword)> containingTypes,
                                                   List<(string fieldName, ITypeSymbol typeSymbol, string typeName)>
                                                       fieldTypes)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using Aaru.Helpers;");
        sb.AppendLine();

        if(!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // Generate containing type declarations
        foreach((string name, string keyword) in containingTypes)
        {
            sb.AppendLine($"{keyword} {name}");
            sb.AppendLine("{");
        }

        var hasEnums = false;
        sb.AppendLine($"    partial struct {structName} : ISwapEndian<{structName}>");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>Swaps the endianness of all numeric fields in this structure</summary>");
        sb.AppendLine("        /// <returns>A new structure with swapped endianness</returns>");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"        public {structName} SwapEndian()");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = this;");
        sb.AppendLine();

        // Process each field
        foreach((string fieldName, ITypeSymbol typeSymbol, string typeName) in fieldTypes)
        {
            (string swapCode, bool usesEnumHelper) = GenerateSwapCode(typeName, fieldName, typeSymbol);

            if(string.IsNullOrEmpty(swapCode)) continue;

            sb.AppendLine(swapCode);

            if(usesEnumHelper) hasEnums = true;
        }

        sb.AppendLine();
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");

        // Add the SwapEnumValue helper method only if we have enums
        if(hasEnums) sb.Append(GenerateSwapEnumValueHelper());

        sb.AppendLine("    }");

        // Close containing type declarations
        for(var i = 0; i < containingTypes.Count; i++) sb.AppendLine("}");

        return sb.ToString();
    }

    private static (string code, bool usesEnumHelper) GenerateSwapCode(string      typeName, string fieldName,
                                                                       ITypeSymbol typeSymbol)
    {
        // Check for arrays BEFORE cleaning the type name
        if(typeName.Contains("["))
        {
            // Handle array types
            string elementType = typeName.Substring(0, typeName.IndexOf('[')).Trim();

            return elementType switch
                   {
                       "short" or "Int16" => ($"""
                                                       for (int i = 0; i < result.{fieldName}.Length; i++)
                                                           result.{fieldName}[i] = (short)((result.{fieldName}[i] << 8) | ((result.{fieldName}[i] >> 8) & 0xFF));
                                               """, false),

                       "ushort" or "UInt16" => ($"""
                                                         for (int i = 0; i < result.{fieldName}.Length; i++)
                                                             result.{fieldName}[i] = (ushort)((result.{fieldName}[i] << 8) | (result.{fieldName}[i] >> 8));
                                                 """, false),

                       "int" or "Int32" => ($$"""
                                                      for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                      {
                                                          var temp = result.{{fieldName}}[i];
                                                          temp = (int)((temp << 8) & 0xFF00FF00 | ((uint)temp >> 8) & 0xFF00FF);
                                                          result.{{fieldName}}[i] = (int)(((uint)temp << 16) | ((uint)temp >> 16) & 0xFFFF);
                                                      }
                                              """, false),

                       "uint" or "UInt32" => ($$"""
                                                        for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                        {
                                                            var temp = result.{{fieldName}}[i];
                                                            temp = (temp << 8) & 0xFF00FF00 | (temp >> 8) & 0xFF00FF;
                                                            result.{{fieldName}}[i] = (temp << 16) | (temp >> 16);
                                                        }
                                                """, false),

                       "long" or "Int64" => ($$"""
                                                       for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                       {
                                                           var temp = result.{{fieldName}}[i];
                                                           temp = (temp & 0x00000000FFFFFFFF) << 32 | (long)(((ulong)temp & 0xFFFFFFFF00000000) >> 32);
                                                           temp = (temp & 0x0000FFFF0000FFFF) << 16 | (long)(((ulong)temp & 0xFFFF0000FFFF0000) >> 16);
                                                           result.{{fieldName}}[i] = (temp & 0x00FF00FF00FF00FF) << 8 | (long)(((ulong)temp & 0xFF00FF00FF00FF00) >> 8);
                                                       }
                                               """, false),

                       "ulong" or "UInt64" => ($$"""
                                                         for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                         {
                                                             var temp = result.{{fieldName}}[i];
                                                             temp = (temp & 0x00000000FFFFFFFF) << 32 | (temp & 0xFFFFFFFF00000000) >> 32;
                                                             temp = (temp & 0x0000FFFF0000FFFF) << 16 | (temp & 0xFFFF0000FFFF0000) >> 16;
                                                             result.{{fieldName}}[i] = (temp & 0x00FF00FF00FF00FF) << 8 | (temp & 0xFF00FF00FF00FF00) >> 8;
                                                         }
                                                 """, false),

                       "byte" or "Byte" or "sbyte" or "SByte" =>
                           ($"        // {fieldName} - no swap needed for byte array", false),

                       // Check if it's an array of structs using semantic info
                       _ => HandleStructOrEnumArray(fieldName, elementType, typeSymbol)
                   };
        }

        // Remove array brackets and modifiers for type checking
        string cleanTypeName = typeName.TrimEnd('[', ']', '?').Trim();

        string code = cleanTypeName switch
                      {
                          "short" or "Int16" =>
                              $"        result.{fieldName} = (short)((result.{fieldName} << 8) | ((result.{fieldName} >> 8) & 0xFF));",

                          "ushort" or "UInt16" =>
                              $"        result.{fieldName} = (ushort)((result.{fieldName} << 8) | (result.{fieldName} >> 8));",

                          "int" or "Int32" => $"""
                                                       var {fieldName}_temp = result.{fieldName};
                                                       {fieldName}_temp = (int)(({fieldName}_temp << 8) & 0xFF00FF00 | ((uint){fieldName}_temp >> 8) & 0xFF00FF);
                                                       result.{fieldName} = (int)(((uint){fieldName}_temp << 16) | ((uint){fieldName}_temp >> 16) & 0xFFFF);
                                               """,

                          "uint" or "UInt32" => $"""
                                                         var {fieldName}_temp = result.{fieldName};
                                                         {fieldName}_temp = ({fieldName}_temp << 8) & 0xFF00FF00 | ({fieldName}_temp >> 8) & 0xFF00FF;
                                                         result.{fieldName} = ({fieldName}_temp << 16) | ({fieldName}_temp >> 16);
                                                 """,

                          "long" or "Int64" => $"""
                                                        var {fieldName}_temp = result.{fieldName};
                                                        {fieldName}_temp = ({fieldName}_temp & 0x00000000FFFFFFFF) << 32 | (long)(((ulong){fieldName}_temp & 0xFFFFFFFF00000000) >> 32);
                                                        {fieldName}_temp = ({fieldName}_temp & 0x0000FFFF0000FFFF) << 16 | (long)(((ulong){fieldName}_temp & 0xFFFF0000FFFF0000) >> 16);
                                                        result.{fieldName} = ({fieldName}_temp & 0x00FF00FF00FF00FF) << 8 | (long)(((ulong){fieldName}_temp & 0xFF00FF00FF00FF00) >> 8);
                                                """,

                          "ulong" or "UInt64" => $"""
                                                          var {fieldName}_temp = result.{fieldName};
                                                          {fieldName}_temp = ({fieldName}_temp & 0x00000000FFFFFFFF) << 32 | ({fieldName}_temp & 0xFFFFFFFF00000000) >> 32;
                                                          {fieldName}_temp = ({fieldName}_temp & 0x0000FFFF0000FFFF) << 16 | ({fieldName}_temp & 0xFFFF0000FFFF0000) >> 16;
                                                          result.{fieldName} = ({fieldName}_temp & 0x00FF00FF00FF00FF) << 8 | ({fieldName}_temp & 0xFF00FF00FF00FF00) >> 8;
                                                  """,

                          "float" or "Single" => $$"""
                                                           {
                                                               var bytes = BitConverter.GetBytes(result.{{fieldName}});
                                                               Array.Reverse(bytes);
                                                               result.{{fieldName}} = BitConverter.ToSingle(bytes, 0);
                                                           }
                                                   """,

                          "double" or "Double" => $$"""
                                                            {
                                                                var bytes = BitConverter.GetBytes(result.{{fieldName}});
                                                                Array.Reverse(bytes);
                                                                result.{{fieldName}} = BitConverter.ToDouble(bytes, 0);
                                                            }
                                                    """,

                          "byte" or "Byte" or "sbyte" or "SByte" =>
                              $"        // {fieldName} - no swap needed for byte types",

                          "string" or "String" => $"        // {fieldName} - no swap needed for string types",

                          "Guid" => $"        // TODO: Implement GUID swap for {fieldName}",


                          _ => null
                      };

        if(code != null) return (code, false);

        // Use semantic information to determine if it's an enum or struct
        if(typeSymbol == null) return ($"        result.{fieldName} = result.{fieldName}.SwapEndian();", false);

        if(typeSymbol.TypeKind == TypeKind.Enum)
        {
            // It's an enum - swap using runtime helper that will determine underlying type
            return ($"        result.{fieldName} = SwapEnumValue(result.{fieldName});", true);
        }

        // Check if it's a struct - but it might be a primitive type through a type alias
        if(typeSymbol.TypeKind != TypeKind.Struct)
            return ($"        result.{fieldName} = result.{fieldName}.SwapEndian();", false);

        // Get the fully qualified type name to check if it's a primitive (resolves type aliases)
        string fullTypeName = typeSymbol.ToDisplayString();

        // Check if the resolved type is a primitive numeric type
        string primitiveCode = fullTypeName switch
                               {
                                   "short" or "System.Int16" =>
                                       $"        result.{fieldName} = (short)((result.{fieldName} << 8) | ((result.{fieldName} >> 8) & 0xFF));",

                                   "ushort" or "System.UInt16" =>
                                       $"        result.{fieldName} = (ushort)((result.{fieldName} << 8) | (result.{fieldName} >> 8));",

                                   "int" or "System.Int32" => $"""
                                                                       var {fieldName}_temp = result.{fieldName};
                                                                       {fieldName}_temp = (int)(({fieldName}_temp << 8) & 0xFF00FF00 | ((uint){fieldName}_temp >> 8) & 0xFF00FF);
                                                                       result.{fieldName} = (int)(((uint){fieldName}_temp << 16) | ((uint){fieldName}_temp >> 16) & 0xFFFF);
                                                               """,

                                   "uint" or "System.UInt32" => $"""
                                                                         var {fieldName}_temp = result.{fieldName};
                                                                         {fieldName}_temp = ({fieldName}_temp << 8) & 0xFF00FF00 | ({fieldName}_temp >> 8) & 0xFF00FF;
                                                                         result.{fieldName} = ({fieldName}_temp << 16) | ({fieldName}_temp >> 16);
                                                                 """,

                                   "long" or "System.Int64" => $"""
                                                                        var {fieldName}_temp = result.{fieldName};
                                                                        {fieldName}_temp = ({fieldName}_temp & 0x00000000FFFFFFFF) << 32 | (long)(((ulong){fieldName}_temp & 0xFFFFFFFF00000000) >> 32);
                                                                        {fieldName}_temp = ({fieldName}_temp & 0x0000FFFF0000FFFF) << 16 | (long)(((ulong){fieldName}_temp & 0xFFFF0000FFFF0000) >> 16);
                                                                        result.{fieldName} = ({fieldName}_temp & 0x00FF00FF00FF00FF) << 8 | (long)(((ulong){fieldName}_temp & 0xFF00FF00FF00FF00) >> 8);
                                                                """,

                                   "ulong" or "System.UInt64" => $"""
                                                                          var {fieldName}_temp = result.{fieldName};
                                                                          {fieldName}_temp = ({fieldName}_temp & 0x00000000FFFFFFFF) << 32 | ({fieldName}_temp & 0xFFFFFFFF00000000) >> 32;
                                                                          {fieldName}_temp = ({fieldName}_temp & 0x0000FFFF0000FFFF) << 16 | ({fieldName}_temp & 0xFFFF0000FFFF0000) >> 16;
                                                                          result.{fieldName} = ({fieldName}_temp & 0x00FF00FF00FF00FF) << 8 | ({fieldName}_temp & 0xFF00FF00FF00FF00) >> 8;
                                                                  """,

                                   "byte" or "System.Byte" or "sbyte" or "System.SByte" =>
                                       $"        // {fieldName} - no swap needed for byte types",

                                   "string" or "System.String" =>
                                       $"        // {fieldName} - no swap needed for string types",

                                   _ => null
                               };

        return primitiveCode != null
                   ? (primitiveCode, false)
                   :

                   // Not a primitive - it's a custom struct, call SwapEndian on it
                   ($"        result.{fieldName} = result.{fieldName}.SwapEndian();", false);

        // Fallback to assuming it's a nested struct
    }

    private static (string code, bool usesEnumHelper) HandleStructOrEnumArray(string      fieldName, string elementType,
                                                                              ITypeSymbol typeSymbol)
    {
        // Check if we have semantic information about the array type
        if(typeSymbol is not IArrayTypeSymbol arrayType)
        {
            return ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapEndian();
                     """, false);
        }

        ITypeSymbol elementTypeSymbol = arrayType.ElementType;

        // Check if it's an enum array
        if(elementTypeSymbol.TypeKind == TypeKind.Enum)
        {
            return ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = SwapEnumValue(result.{fieldName}[i]);
                     """, true);
        }

        // Check if it's a struct - but make sure it's not a primitive type
        if(elementTypeSymbol.TypeKind != TypeKind.Struct)
        {
            return ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapEndian();
                     """, false);
        }

        // Get the fully qualified type name to check if it's a primitive
        string fullTypeName = elementTypeSymbol.ToDisplayString();

        // Check if it's a primitive numeric type (these are resolved from type aliases)
#pragma warning disable PH2093, PH2093
        (string, bool) primitiveResult = fullTypeName switch
#pragma warning restore PH2093, PH2093
                                         {
                                             "short" or "System.Int16" => ($"""
                                                                                    for (int i = 0; i < result.{fieldName}.Length; i++)
                                                                                        result.{fieldName}[i] = (short)((result.{fieldName}[i] << 8) | ((result.{fieldName}[i] >> 8) & 0xFF));
                                                                            """, false),

                                             "ushort" or "System.UInt16" => ($"""
                                                                                      for (int i = 0; i < result.{fieldName}.Length; i++)
                                                                                          result.{fieldName}[i] = (ushort)((result.{fieldName}[i] << 8) | (result.{fieldName}[i] >> 8));
                                                                              """, false),

                                             "int" or "System.Int32" => ($$"""
                                                                                   for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                                                   {
                                                                                       var temp = result.{{fieldName}}[i];
                                                                                       temp = (int)((temp << 8) & 0xFF00FF00 | ((uint)temp >> 8) & 0xFF00FF);
                                                                                       result.{{fieldName}}[i] = (int)(((uint)temp << 16) | ((uint)temp >> 16) & 0xFFFF);
                                                                                   }
                                                                           """, false),

                                             "uint" or "System.UInt32" => ($$"""
                                                                                     for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                                                     {
                                                                                         var temp = result.{{fieldName}}[i];
                                                                                         temp = (temp << 8) & 0xFF00FF00 | (temp >> 8) & 0xFF00FF;
                                                                                         result.{{fieldName}}[i] = (temp << 16) | (temp >> 16);
                                                                                     }
                                                                             """, false),

                                             "long" or "System.Int64" => ($$"""
                                                                                    for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                                                    {
                                                                                        var temp = result.{{fieldName}}[i];
                                                                                        temp = (temp & 0x00000000FFFFFFFF) << 32 | (long)(((ulong)temp & 0xFFFFFFFF00000000) >> 32);
                                                                                        temp = (temp & 0x0000FFFF0000FFFF) << 16 | (long)(((ulong)temp & 0xFFFF0000FFFF0000) >> 16);
                                                                                        result.{{fieldName}}[i] = (temp & 0x00FF00FF00FF00FF) << 8 | (long)(((ulong)temp & 0xFF00FF00FF00FF00) >> 8);
                                                                                    }
                                                                            """, false),

                                             "ulong" or "System.UInt64" => ($$"""
                                                                                      for (int i = 0; i < result.{{fieldName}}.Length; i++)
                                                                                      {
                                                                                          var temp = result.{{fieldName}}[i];
                                                                                          temp = (temp & 0x00000000FFFFFFFF) << 32 | (temp & 0xFFFFFFFF00000000) >> 32;
                                                                                          temp = (temp & 0x0000FFFF0000FFFF) << 16 | (temp & 0xFFFF0000FFFF0000) >> 16;
                                                                                          result.{{fieldName}}[i] = (temp & 0x00FF00FF00FF00FF) << 8 | (temp & 0xFF00FF00FF00FF00) >> 8;
                                                                                      }
                                                                              """, false),

                                             "byte" or "System.Byte" or "sbyte" or "System.SByte" =>
                                                 ($"        // {fieldName} - no swap needed for byte array", false),

                                             _ => (null, false)
                                         };

        // If it matched a primitive type, return that
        return primitiveResult.Item1 != null
                   ? primitiveResult
                   :

                   // Otherwise it's a custom struct - call SwapEndian on each element
                   ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapEndian();
                     """, false);

        // Fallback: assume it's a struct that implements SwapEndian
    }

    // Helper method to generate at the struct level for enum swapping only
    private static string GenerateSwapEnumValueHelper() => """

                                                                   private static T SwapEnumValue<T>(T value) where T : struct
                                                                   {
                                                                       var type = typeof(T);

                                                                       // Handle enums by converting to underlying type
                                                                       if (type.IsEnum)
                                                                       {
                                                                           var underlyingType = Enum.GetUnderlyingType(type);

                                                                           if (underlyingType == typeof(short))
                                                                           {
                                                                               var v = (short)(object)value;
                                                                               v = (short)((v << 8) | ((v >> 8) & 0xFF));
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(ushort))
                                                                           {
                                                                               var v = (ushort)(object)value;
                                                                               v = (ushort)((v << 8) | (v >> 8));
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(int))
                                                                           {
                                                                               var v = (int)(object)value;
                                                                               v = (int)((v << 8) & 0xFF00FF00 | ((uint)v >> 8) & 0xFF00FF);
                                                                               v = (int)(((uint)v << 16) | ((uint)v >> 16) & 0xFFFF);
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(uint))
                                                                           {
                                                                               var v = (uint)(object)value;
                                                                               v = (v << 8) & 0xFF00FF00 | (v >> 8) & 0xFF00FF;
                                                                               v = (v << 16) | (v >> 16);
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(long))
                                                                           {
                                                                               var v = (long)(object)value;
                                                                               v = (v & 0x00000000FFFFFFFF) << 32 | (long)(((ulong)v & 0xFFFFFFFF00000000) >> 32);
                                                                               v = (v & 0x0000FFFF0000FFFF) << 16 | (long)(((ulong)v & 0xFFFF0000FFFF0000) >> 16);
                                                                               v = (v & 0x00FF00FF00FF00FF) << 8 | (long)(((ulong)v & 0xFF00FF00FF00FF00) >> 8);
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(ulong))
                                                                           {
                                                                               var v = (ulong)(object)value;
                                                                               v = (v & 0x00000000FFFFFFFF) << 32 | (v & 0xFFFFFFFF00000000) >> 32;
                                                                               v = (v & 0x0000FFFF0000FFFF) << 16 | (v & 0xFFFF0000FFFF0000) >> 16;
                                                                               v = (v & 0x00FF00FF00FF00FF) << 8 | (v & 0xFF00FF00FF00FF00) >> 8;
                                                                               return (T)(object)v;
                                                                           }
                                                                           if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte))
                                                                           {
                                                                               return value; // No swap needed
                                                                           }
                                                                       }

                                                                       // If not an enum or unknown underlying type, return unchanged
                                                                       return value;
                                                                   }
                                                           """;
}