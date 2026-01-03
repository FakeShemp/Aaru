# AGENTS.md - AI Coding Agent Guidelines for Aaru

This document provides guidelines for AI coding assistants working with the Aaru Data Preservation Suite codebase.

## Project Overview

**Aaru** is an open-source Data Preservation Suite for creating, managing, and preserving digital media images. It supports dumping media from various drives (magnetic disks, optical discs, tapes, flash devices), converting between image formats, and analyzing filesystems.

- **Language:** C# 8+ (latest features enabled)
- **Framework:** .NET 10
- **Solution File:** `Aaru.slnx`
- **Build System:** MSBuild with `dotnet` CLI

## Repository Structure

The repository is organized into multiple modules:

| Module | Purpose |
|--------|---------|
| `Aaru/` | Main CLI application and entry point |
| `Aaru.Archives/` | Archive format support (AMG, Arc, Ha, Stfs, Symbian, Zoo) |
| `Aaru.Checksums/` | Checksum, hashing, and error correction algorithms |
| `Aaru.CommonTypes/` | Shared interfaces, structures, and enumerations |
| `Aaru.Compression/` | Compression algorithm implementations |
| `Aaru.Console/` | Console abstraction for CLI/GUI output |
| `Aaru.Core/` | Core functionality: dumping, conversion, filesystem operations |
| `Aaru.Database/` | Database models and access |
| `Aaru.Decoders/` | Protocol structures and decoder implementations |
| `Aaru.Decryption/` | Media decryption (CSS, CPRM, etc.) |
| `Aaru.Devices/` | Hardware device communication across platforms |
| `Aaru.Dto/` | Data Transfer Objects for client-server communication |
| `Aaru.EntityFramework/` | Entity Framework integration |
| `Aaru.Filesystems/` | Filesystem identification and extraction |
| `Aaru.Filters/` | Data filters (compression, encoding transformations) |
| `Aaru.Generators/` | Source generators |
| `Aaru.Gui/` | Avalonia-based graphical user interface |
| `Aaru.Helpers/` | Utility functions (marshalling, byte manipulation, etc.) |
| `Aaru.Images/` | Disk/disc image format readers and writers |
| `Aaru.Localization/` | Localized strings (shared resources) |
| `Aaru.Logging/` | Logging infrastructure |
| `Aaru.Partitions/` | Partition scheme readers |
| `Aaru.Settings/` | Application settings management |
| `Aaru.Tests/` | Unit tests (xUnit) |
| `Aaru.Tests.Devices/` | Device command testing (menu-driven) |
| `Aaru.Tui/` | Terminal UI implementation |

## Coding Style Guidelines

The project follows strict coding conventions. An `.editorconfig` file is included to enforce these.

### General Rules

- **Braces:** BSD style (braces on new line, unindented)
- **Empty braces:** Open and close on same line: `{ }`
- **Single-statement blocks:** No braces required
- **Indentation:** 4 spaces (soft tabs)
- **Line length:** 120 characters maximum
- **Line endings:** UNIX (`\n`)
- **Final newline:** Do NOT end files with a newline
- **Blank lines:** Do not use more than one consecutive blank line

### Naming Conventions

- **Constants:** `ALL_UPPER_CASE`
- **Instance/static fields (private):** `lowerCamelCase` (often prefixed with `_`)
- **Public fields:** `UpperCamelCase`
- **Properties/Methods:** `UpperCamelCase`

### C# Specific

- **Do NOT use `var`** - always use explicit types
- **Use built-in type keywords:** `uint` instead of `UInt32`, `string` instead of `String`
- **Expression bodies:** Only for properties, indexers, and events; use block bodies for methods
- **Use inline variable declaration:** `if(Foo(out var result))`
- **Keywords order:** `public, private, protected, internal, file, new, static, abstract, virtual, sealed, readonly, override, extern, unsafe, volatile, async, required`
- **`else`, `while`, `catch`, `finally`:** Always on new lines
- **No spaces around parentheses:** `if(condition)` not `if (condition)`
- **Prefer structs over classes** when only storing data; use nullable structs when needed
- **Avoid unnecessary OOP abstractions** - this is low-level code

### LINQ

LINQ is acceptable and commonly used throughout the codebase.

## Localization

All user-facing strings must be localized:

- **Shared strings:** `Aaru.Localization/UI.resx` and `Aaru.Localization/Core.resx`
- **Project-specific strings:** In each project's `Localization/` folder
- **Translations:** Currently English (`*.resx`) and Spanish (`*.es.resx`)
- **Resource ID conventions:**
  - `Title_*` - Table/section headers
  - `ButtonLabel_*` - Action buttons (use appropriate verb tense)
  - `*_Q` - Questions
  - `*_WithMarkup` - Contains Spectre.Console markup
  - Numbers in IDs (e.g., `_0`, `_1`) indicate string format arguments `{0}`, `{1}`

For enum values, use `[LocalizedDescription(nameof(UI.EnumName_Value))]` attribute.

## Testing

- **Framework:** xUnit
- **Location:** `Aaru.Tests/` project
- **Note:** Full test suite requires ~900+ GiB of test images; not all tests can be run locally
- **Device tests:** `Aaru.Tests.Devices/` provides interactive device command testing

## Building

```bash
# Build all configurations
dotnet build

# Build specific configuration
dotnet build -c Release

# Publish for specific platform
dotnet publish -f net10.0 -r osx-arm64 -c Release

# Run tests
dotnet test
```

Supported runtime identifiers: `linux-arm64`, `linux-arm`, `linux-x64`, `osx-x64`, `osx-arm64`, `win-arm64`, `win-x64`, `win-x86`

## Key Interfaces

When working with the codebase, be aware of these core interfaces in `Aaru.CommonTypes`:

- `IMediaImage` / `IWritableImage` - Disk/disc image reading/writing
- `IOpticalMediaImage` / `IWritableOpticalImage` - Optical media specific
- `ITapeImage` / `IWritableTapeImage` - Tape media specific
- `IFluxImage` / `IWritableFluxImage` - Flux-level data
- `IFilesystem` / `IReadOnlyFilesystem` - Filesystem operations
- `IPartition` - Partition scheme reading
- `IFilter` - Data transformation filters
- `IArchive` - Archive format support

## Error Handling

- Use `ErrorNumber` enum for return values indicating success/failure
- Check for `ErrorNumber.NoError` for success
- Propagate errors up the call stack with appropriate context

## Events and Progress Reporting

Many operations use event-based progress reporting:

```csharp
public event InitProgressHandler InitProgress;
public event UpdateProgressHandler UpdateProgress;
public event EndProgressHandler EndProgress;
public event UpdateStatusHandler UpdateStatus;
public event ErrorMessageHandler ErrorMessage;
public event ErrorMessageHandler StoppingErrorMessage;
```

## Platform Considerations

- Avoid platform-dependent code unless absolutely necessary
- For system calls, only use `System.*` namespaces when possible
- Device communication uses OS-specific interop in `Aaru.Devices/`:
  - Windows: `KERNEL32.DLL`, `WinUsb`
  - Unix/Linux/macOS: `libc`, `libusb`

## Pull Request Guidelines

1. Do not modify interfaces without discussion
2. Follow the code style guide strictly
3. Include XML documentation for new public APIs
4. Add tests for new functionality when possible
5. Do not include copyrighted content in test files
6. Keep commits focused and well-described

## External Dependencies

- Dependencies are managed via central package management (`Directory.Packages.props`)
- Minimize external library usage - prefer .NET built-in functionality
- Compression: Use `SharpCompress` library
- Mac Resource Forks: Use `Claunia.RsrcFork`
- Encodings: Use `Claunia.Encoding` for legacy codepages
- Property Lists: Use `plist-cil`

## Common Patterns

### Plugin Registration

Plugins (images, filesystems, partitions, etc.) register through `PluginRegister.Singleton`.

### Reading/Writing Images

```csharp
// Reading
(ErrorNumber errno, IMediaImage image) = GetInputImage(imagePath);
if(image is null) return errno;

// Writing
IWritableImage outputFormat = FindOutputFormat(PluginRegister.Singleton, format, outputPath);
```

### Metadata and Dump Hardware

- Use `Metadata` class for image metadata
- Use `DumpHardware` for recording dump device information
- Use `Resume` for dump session resumption data

