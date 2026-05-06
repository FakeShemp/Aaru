// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsFullMkbReader.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Loads the full AACS Media Key Block from an optical image's
//     UDF filesystem using the on-disc paths defined by the AACS HD DVD/BD specifications.
//     Used when converting an image with device keys.
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
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decryption.Aacs;
using Aaru.Filesystems;

namespace Aaru.Core.Devices.Dumping;

/// <summary>
///     Reads the full AACS Media Key Block from an optical image's UDF filesystem
///     using the on-disc paths defined by the AACS HD DVD/BD specifications.
/// </summary>
internal static class AacsFullMkbReader
{
    /// <summary>HD DVD-ROM full MKB primary path (per HD DVD AACS Book sections 2.4 and 3.2).</summary>
    internal const string HDDVD_PRIMARY = "/AACS/MKBROM.AACS";

    /// <summary>HD DVD-ROM full MKB backup path (per HD DVD AACS Book section 3.2.5).</summary>
    internal const string HDDVD_BACKUP = "/AACS_BAK/MKBROM.AACS";

    /// <summary>Blu-ray full MKB primary path (matches libaacs aacs.c).</summary>
    internal const string BLURAY_PRIMARY = "/AACS/MKB_RO.inf";

    /// <summary>Blu-ray full MKB duplicate/backup path (matches libaacs aacs.c).</summary>
    internal const string BLURAY_BACKUP = "/AACS/DUPLICATE/MKB_RO.inf";

    /// <summary>Maximum acceptable MKB file size (16 MiB hard cap to bound memory).</summary>
    internal const int MAX_MKB_SIZE = 16 * 1024 * 1024;

    /// <summary>
    ///     Returns the ordered list of on-disc paths from which the full MKB should be read
    ///     for the given media kind. Primary first, backup second.
    /// </summary>
    internal static string[] GetCandidatePaths(AacsMediaKind kind) =>
        kind switch
        {
            AacsMediaKind.HdDvd  => [HDDVD_PRIMARY, HDDVD_BACKUP],
            AacsMediaKind.BluRay => [BLURAY_PRIMARY, BLURAY_BACKUP],
            _                    => []
        };

    static bool TryReadFullMkbFromFilesystem(IOpticalMediaImage image, PluginRegister plugins, Encoding encoding,
                                             AacsMediaKind      kind,
                                             out byte[]?        fileBytes)
    {
        fileBytes = null;

        foreach(Partition partition in Partitions.GetAll(image))
        {
            foreach(UDF udfPlugin in plugins.Filesystems.Values.OfType<UDF>())
            {
                if(!udfPlugin.Identify(image, partition))
                    continue;

                if(udfPlugin.Mount(image, partition, encoding, null, null) != ErrorNumber.NoError)
                    continue;

                try
                {
                    string[] candidates = GetCandidatePaths(kind);

                    if(TryReadFirstAvailable(udfPlugin, candidates, out fileBytes))
                        return true;
                }
                finally
                {
                    udfPlugin.Unmount();
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Mounts UDF over the given image and reads the full AACS Media Key Block file.
    /// </summary>
    /// <param name="image">Optical image to mount UDF on.</param>
    /// <param name="plugins">Plugin register that provides the UDF filesystem implementation.</param>
    /// <param name="kind">Whether this is HD DVD or Blu-ray; selects the on-disc path set.</param>
    /// <param name="fullMkb">On success, the bytes of the on-disc MKB file.</param>
    /// <param name="error">On failure, a human-readable diagnostic; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a full MKB was read.</returns>
    internal static bool TryLoad(IMediaImage    image,
                                 PluginRegister plugins,
                                 AacsMediaKind  kind,
                                 out byte[]?    fullMkb,
                                 out string?    error)
    {
        fullMkb = null;
        error   = null;

        if(image is null)
        {
            error = "No image was supplied to AacsFullMkbReader.";

            return false;
        }

        if(GetCandidatePaths(kind).Length == 0)
        {
            error = string.Format(Aaru.Localization.Core.AACS_full_mkb_not_found_for_kind_0, kind);

            return false;
        }

        if(image is not IOpticalMediaImage opticalImage)
        {
            error = Aaru.Localization.Core.AACS_full_mkb_udf_mount_failed;

            return false;
        }

        if(TryReadFullMkbFromFilesystem(opticalImage, plugins, Encoding.UTF8, kind, out byte[]? data) &&
           data is { Length: > 0 })
        {
            fullMkb = data;

            return true;
        }

        error = string.Format(Aaru.Localization.Core.AACS_full_mkb_not_found_for_kind_0, kind);

        return false;
    }

    /// <summary>
    ///     Tries each path in <paramref name="candidates" /> in order and returns the bytes of
    ///     the first one that opens and reads non-empty within <see cref="MAX_MKB_SIZE" />.
    /// </summary>
    internal static bool TryReadFirstAvailable(IReadOnlyFilesystem rofs, IReadOnlyList<string> candidates,
                                               out byte[]?         fileBytes)
    {
        fileBytes = null;

        for(int i = 0; i < candidates.Count; i++)
        {
            if(TryReadFile(rofs, candidates[i], out byte[]? data) && data is { Length: > 0 })
            {
                fileBytes = data;

                return true;
            }
        }

        return false;
    }

    /// <summary>Reads a single file from an already-mounted read-only filesystem with a 16 MiB cap.</summary>
    internal static bool TryReadFile(IReadOnlyFilesystem rofs, string path, out byte[]? fileBytes)
    {
        fileBytes = null;

        if(rofs.OpenFile(path, out IFileNode node) != ErrorNumber.NoError || node is null) return false;

        try
        {
            if(node.Length is <= 0 or > MAX_MKB_SIZE) return false;

            byte[] buf = new byte[node.Length];

            if(rofs.ReadFile(node, node.Length, buf, out long read) != ErrorNumber.NoError ||
               read != node.Length)
                return false;

            fileBytes = buf;

            return true;
        }
        finally
        {
            rofs.CloseFile(node);
        }
    }
}