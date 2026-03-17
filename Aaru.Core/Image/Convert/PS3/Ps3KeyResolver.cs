using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;

namespace Aaru.Core.Image.PS3;

/// <summary>
///     Resolves PS3 disc encryption keys from sidecar files, IRD files, or source image media tags.
///     Follows the same resolution cascade as the C reference implementation.
/// </summary>
static class Ps3KeyResolver
{
    const string MODULE_NAME = "PS3 Key Resolver";

    /// <summary>Resolves PS3 disc key material from all available sources.</summary>
    /// <param name="inputPath">Path to the input image file (for locating sidecar files).</param>
    /// <param name="inputImage">The opened input image (for reading media tags).</param>
    /// <param name="discKey">Output: 16-byte derived disc key, or null if not found.</param>
    /// <param name="data1Key">Output: 16-byte data1 key if available, or null.</param>
    /// <param name="ird">Output: parsed IRD file if found, or null.</param>
    public static void ResolveKeys(string inputPath, IMediaImage inputImage, out byte[] discKey, out byte[] data1Key,
                                   out IrdFile? ird)
    {
        discKey  = null;
        data1Key = null;
        ird      = null;

        // 1. Sidecar .disc_key (16 bytes binary)
        byte[] sidecarKey = TrySidecarFile(inputPath, ".disc_key", 16);

        if(sidecarKey != null)
        {
            discKey = sidecarKey;
            AaruLogging.Debug(MODULE_NAME, "Disc key loaded from sidecar .disc_key file");
        }

        // 2. Sidecar .data1 (16 bytes binary) → derive disc key
        if(discKey == null)
        {
            byte[] sidecarData1 = TrySidecarFile(inputPath, ".data1", 16);

            if(sidecarData1 != null)
            {
                data1Key = sidecarData1;
                discKey  = Ps3Crypto.DeriveDiscKey(data1Key);
                AaruLogging.Debug(MODULE_NAME, "Disc key derived from sidecar .data1 file");
            }
        }

        // 3. Sidecar .ird → parse, derive from d1
        if(discKey == null || ird == null) TrySidecarIrd(inputPath, ref discKey, ref data1Key, ref ird);

        // 4. Input image media tags
        if(discKey == null)
        {
            ErrorNumber errno = inputImage.ReadMediaTag(MediaTagType.PS3_DiscKey, out byte[] tagDiscKey);

            if(errno == ErrorNumber.NoError && tagDiscKey is { Length: 16 })
            {
                discKey = tagDiscKey;
                AaruLogging.Debug(MODULE_NAME, "Disc key loaded from source image media tags");
            }
        }

        if(discKey == null)
        {
            ErrorNumber errno = inputImage.ReadMediaTag(MediaTagType.PS3_Data1, out byte[] tagData1);

            if(errno == ErrorNumber.NoError && tagData1 is { Length: 16 })
            {
                data1Key = tagData1;
                discKey  = Ps3Crypto.DeriveDiscKey(data1Key);
                AaruLogging.Debug(MODULE_NAME, "Disc key derived from source image data1 media tag");
            }
        }
    }

    /// <summary>Tries to read a sidecar file with the given suffix.</summary>
    /// <remarks>Tries both "input.iso.suffix" and "input.suffix" (extension replaced).</remarks>
    static byte[] TrySidecarFile(string inputPath, string suffix, int expectedLength)
    {
        // Try appended: "input.iso.disc_key"
        string appendedPath = inputPath + suffix;

        byte[] result = ReadExactFile(appendedPath, expectedLength);

        if(result != null) return result;

        // Try extension replaced: "input.disc_key"
        string extension = Path.GetExtension(inputPath);

        if(!string.IsNullOrEmpty(extension))
        {
            string basePath     = inputPath[..^extension.Length];
            string replacedPath = basePath + suffix;
            result = ReadExactFile(replacedPath, expectedLength);

            if(result != null) return result;
        }

        return null;
    }

    /// <summary>Reads a file and returns its contents only if it is exactly the expected length.</summary>
    static byte[] ReadExactFile(string path, int expectedLength)
    {
        try
        {
            if(!File.Exists(path)) return null;

            byte[] data = File.ReadAllBytes(path);

            return data.Length == expectedLength ? data : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Tries to find and parse a sidecar IRD file.</summary>
    /// <remarks>Tries "input.iso.ird" and "input.ird" (extension replaced).</remarks>
    static void TrySidecarIrd(string inputPath, ref byte[] discKey, ref byte[] data1Key, ref IrdFile? ird)
    {
        // Try appended: "input.iso.ird"
        if(TryParseIrdAt(inputPath + ".ird", ref discKey, ref data1Key, ref ird)) return;

        // Try extension replaced: "input.ird"
        string extension = Path.GetExtension(inputPath);

        if(string.IsNullOrEmpty(extension)) return;

        string basePath = inputPath[..^extension.Length];
        TryParseIrdAt(basePath + ".ird", ref discKey, ref data1Key, ref ird);
    }

    /// <summary>Attempts to parse an IRD file at the given path and extract keys.</summary>
    static bool TryParseIrdAt(string irdPath, ref byte[] discKey, ref byte[] data1Key, ref IrdFile? ird)
    {
        if(ird != null) return false; // Already have an IRD

        if(!IrdParser.Parse(irdPath, out IrdFile parsed) || !parsed.Valid) return false;

        ird = parsed;

        AaruLogging.Debug(MODULE_NAME, "IRD loaded: {0} — {1} (v{2})", parsed.GameId, parsed.GameName, parsed.Version);

        if(discKey != null || parsed.D1 is not { Length: 16 }) return true;

        data1Key = parsed.D1;
        discKey  = Ps3Crypto.DeriveDiscKey(data1Key);
        AaruLogging.Debug(MODULE_NAME, "Disc key derived from IRD data1");

        return true;
    }
}