// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SwapPdpEndianGenerator.cs
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
public class SwapPdpEndianGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with the [SwapPdpEndian] attribute and collect semantic info
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

                if(fullName != "Aaru.CommonTypes.Attributes.SwapPdpEndianAttribute") continue;

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
            GenerateSwapPdpEndianMethod(structName, namespaceName, containingTypes, source.fieldTypes);

        // Create unique file name by including containing types
        string fileName = containingTypes.Count > 0
                              ? $"{string.Join("_", containingTypes.Select(static t => t.Name))}_{structName}_SwapPdpEndian.g.cs"
                              : $"{structName}_SwapPdpEndian.g.cs";

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

    private static string GenerateSwapPdpEndianMethod(string structName, string namespaceName,
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
        sb.AppendLine($"    partial struct {structName} : ISwapPdpEndian<{structName}>");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>Swaps the endianness of int/uint fields in this structure following PDP-11 conventions</summary>");
        sb.AppendLine("        /// <returns>A new structure with swapped endianness</returns>");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"        public {structName} SwapPdpEndian()");
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

        // Add the SwapPdpEnumValue helper method only if we have enums
        if(hasEnums) sb.Append(GenerateSwapPdpEnumValueHelper());

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
                       // For PDP endian, only int and uint are swapped
                       "int" or "Int32" => ($"""
                                                     for (int i = 0; i < result.{fieldName}.Length; i++)
                                                         result.{fieldName}[i] = (int)((result.{fieldName}[i] & 0xFFFF) << 16 | (int)(((uint)result.{fieldName}[i] & 0xFFFF0000) >> 16));
                                             """, false),

                       "uint" or "UInt32" => ($"""
                                                       for (int i = 0; i < result.{fieldName}.Length; i++)
                                                           result.{fieldName}[i] = (result.{fieldName}[i] & 0xFFFF) << 16 | (result.{fieldName}[i] & 0xFFFF0000) >> 16;
                                               """, false),

                       // All other types are left unchanged in PDP endian
                       "short"
                        or "Int16"
                        or "ushort"
                        or "UInt16"
                        or "long"
                        or "Int64"
                        or "ulong"
                        or "UInt64"
                        or "byte"
                        or "Byte"
                        or "sbyte"
                        or "SByte"
                        or "float"
                        or "Single"
                        or "double"
                        or "Double" => ($"        // {fieldName} - no swap needed for {elementType} in PDP endian",
                                        false),

                       // Check if it's an array of enums or structs using semantic info
                       _ => HandleStructOrEnumArray(fieldName, elementType, typeSymbol)
                   };
        }

        // Remove array brackets and modifiers for type checking
        string cleanTypeName = typeName.TrimEnd('[', ']', '?').Trim();

        string code = cleanTypeName switch
                      {
                          // For PDP endian, only int and uint are swapped
                          "int" or "Int32" =>
                              $"        result.{fieldName} = (int)((result.{fieldName} & 0xFFFF) << 16 | (int)(((uint)result.{fieldName} & 0xFFFF0000) >> 16));",

                          "uint" or "UInt32" =>
                              $"        result.{fieldName} = (result.{fieldName} & 0xFFFF) << 16 | (result.{fieldName} & 0xFFFF0000) >> 16;",

                          // All other types are left unchanged in PDP endian
                          "short"
                           or "Int16"
                           or "ushort"
                           or "UInt16"
                           or "long"
                           or "Int64"
                           or "ulong"
                           or "UInt64"
                           or "byte"
                           or "Byte"
                           or "sbyte"
                           or "SByte"
                           or "float"
                           or "Single"
                           or "double"
                           or "Double" => $"        // {fieldName} - no swap needed for {cleanTypeName} in PDP endian",

                          "string" or "String" => $"        // {fieldName} - no swap needed for string types",

                          "Guid" => $"        // {fieldName} - no swap needed for Guid in PDP endian",

                          _ => null
                      };

        if(code != null) return (code, false);

        // Use semantic information to determine if it's an enum or struct
        if(typeSymbol == null) return ($"        result.{fieldName} = result.{fieldName}.SwapPdpEndian();", false);

        if(typeSymbol.TypeKind == TypeKind.Enum)
        {
            // It's an enum - check if underlying type is int or uint
            var         enumSymbol        = (INamedTypeSymbol)typeSymbol;
            ITypeSymbol underlyingType    = enumSymbol.EnumUnderlyingType;
            string      underlyingTypeStr = underlyingType.ToDisplayString();

            if(underlyingTypeStr is "int" or "System.Int32" or "uint" or "System.UInt32")
            {
                // Swap enum values based on int/uint
                return ($"        result.{fieldName} = SwapPdpEnumValue(result.{fieldName});", true);
            }

            // Other enum types don't need swapping
            return ($"        // {fieldName} - no swap needed for enum with underlying type {underlyingTypeStr}",
                    false);
        }

        // Check if it's a struct - but it might be a primitive type through a type alias
        if(typeSymbol.TypeKind != TypeKind.Struct)
            return ($"        result.{fieldName} = result.{fieldName}.SwapPdpEndian();", false);

        // Get the fully qualified type name to check if it's a primitive (resolves type aliases)
        string fullTypeName = typeSymbol.ToDisplayString();

        // Check if the resolved type is a primitive numeric type
        string primitiveCode = fullTypeName switch
                               {
                                   // For PDP endian, only int and uint are swapped
                                   "int" or "System.Int32" =>
                                       $"        result.{fieldName} = (int)((result.{fieldName} & 0xFFFF) << 16 | (int)(((uint)result.{fieldName} & 0xFFFF0000) >> 16));",

                                   "uint" or "System.UInt32" =>
                                       $"        result.{fieldName} = (result.{fieldName} & 0xFFFF) << 16 | (result.{fieldName} & 0xFFFF0000) >> 16;",

                                   // All other types are left unchanged in PDP endian
                                   "short"
                                    or "System.Int16"
                                    or "ushort"
                                    or "System.UInt16"
                                    or "long"
                                    or "System.Int64"
                                    or "ulong"
                                    or "System.UInt64"
                                    or "byte"
                                    or "System.Byte"
                                    or "sbyte"
                                    or "System.SByte" => $"        // {fieldName} - no swap needed in PDP endian",

                                   "string" or "System.String" =>
                                       $"        // {fieldName} - no swap needed for string types",

                                   _ => null
                               };

        return primitiveCode != null
                   ? (primitiveCode, false)
                   :

                   // Not a primitive - it's a custom struct, call SwapPdpEndian on it
                   ($"        result.{fieldName} = result.{fieldName}.SwapPdpEndian();", false);
    }

    private static (string code, bool usesEnumHelper) HandleStructOrEnumArray(string      fieldName, string elementType,
                                                                              ITypeSymbol typeSymbol)
    {
        // Check if we have semantic information about the array type
        if(typeSymbol is not IArrayTypeSymbol arrayType)
        {
            return ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapPdpEndian();
                     """, false);
        }

        ITypeSymbol elementTypeSymbol = arrayType.ElementType;

        // Check if it's an enum array
        if(elementTypeSymbol.TypeKind == TypeKind.Enum)
        {
            // Check if underlying type is int or uint
            var         enumSymbol        = (INamedTypeSymbol)elementTypeSymbol;
            ITypeSymbol underlyingType    = enumSymbol.EnumUnderlyingType;
            string      underlyingTypeStr = underlyingType.ToDisplayString();

            if(underlyingTypeStr is "int" or "System.Int32" or "uint" or "System.UInt32")
            {
                return ($"""
                                 for (int i = 0; i < result.{fieldName}.Length; i++)
                                     result.{fieldName}[i] = SwapPdpEnumValue(result.{fieldName}[i]);
                         """, true);
            }

            // Other enum types don't need swapping
            return ($"        // {fieldName} - no swap needed for enum array with underlying type {underlyingTypeStr}",
                    false);
        }

        // Check if it's a struct - but make sure it's not a primitive type
        if(elementTypeSymbol.TypeKind != TypeKind.Struct)
        {
            return ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapPdpEndian();
                     """, false);
        }

        // Get the fully qualified type name to check if it's a primitive
        string fullTypeName = elementTypeSymbol.ToDisplayString();

        // Check if it's a primitive numeric type (these are resolved from type aliases)
#pragma warning disable PH2093
        (string, bool) primitiveResult = fullTypeName switch
#pragma warning restore PH2093
                                         {
                                             // For PDP endian, only int and uint arrays need swapping
                                             "int" or "System.Int32" => ($"""
                                                                                  for (int i = 0; i < result.{fieldName}.Length; i++)
                                                                                      result.{fieldName}[i] = (int)((result.{fieldName}[i] & 0xFFFF) << 16 | (int)(((uint)result.{fieldName}[i] & 0xFFFF0000) >> 16));
                                                                          """, false),

                                             "uint" or "System.UInt32" => ($"""
                                                                                    for (int i = 0; i < result.{fieldName}.Length; i++)
                                                                                        result.{fieldName}[i] = (result.{fieldName}[i] & 0xFFFF) << 16 | (result.{fieldName}[i] & 0xFFFF0000) >> 16;
                                                                            """, false),

                                             // All other types are left unchanged in PDP endian
                                             "short"
                                              or "System.Int16"
                                              or "ushort"
                                              or "System.UInt16"
                                              or "long"
                                              or "System.Int64"
                                              or "ulong"
                                              or "System.UInt64"
                                              or "byte"
                                              or "System.Byte"
                                              or "sbyte"
                                              or "System.SByte" =>
                                                 ($"        // {fieldName} - no swap needed for {fullTypeName} array in PDP endian",
                                                  false),

                                             _ => (null, false)
                                         };

        // If it matched a primitive type, return that
        return primitiveResult.Item1 != null
                   ? primitiveResult
                   :

                   // Otherwise it's a custom struct - call SwapPdpEndian on each element
                   ($"""
                             for (int i = 0; i < result.{fieldName}.Length; i++)
                                 result.{fieldName}[i] = result.{fieldName}[i].SwapPdpEndian();
                     """, false);
    }

    // Helper method to generate at the struct level for enum swapping only
    private static string GenerateSwapPdpEnumValueHelper() => """

                                                                      private static T SwapPdpEnumValue<T>(T value) where T : struct
                                                                      {
                                                                          var type = typeof(T);

                                                                          // Handle enums by converting to underlying type
                                                                          if (type.IsEnum)
                                                                          {
                                                                              var underlyingType = Enum.GetUnderlyingType(type);

                                                                              // PDP endian only swaps int and uint
                                                                              if (underlyingType == typeof(int))
                                                                              {
                                                                                  var v = (int)(object)value;
                                                                                  v = (int)((v & 0xFFFF) << 16 | (int)(((uint)v & 0xFFFF0000) >> 16));
                                                                                  return (T)(object)v;
                                                                              }
                                                                              if (underlyingType == typeof(uint))
                                                                              {
                                                                                  var v = (uint)(object)value;
                                                                                  v = (v & 0xFFFF) << 16 | (v & 0xFFFF0000) >> 16;
                                                                                  return (T)(object)v;
                                                                              }

                                                                              // All other enum types are left unchanged
                                                                              return value;
                                                                          }

                                                                          return value;
                                                                      }
                                                              """;
}