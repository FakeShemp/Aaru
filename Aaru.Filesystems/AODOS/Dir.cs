// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory operations for the AO-DOS file system plugin.
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

using System;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class AODOS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        string[] entries;

        if(normalizedPath == "/")
        {
            // Root directory: list files belonging to root (directory == 0) and subdirectory markers
            entries = _directoryCache.Where(static e => e.directory == 0)
                                     .Select(e => StringHandlers.CToString(e.filename, _encoding).Trim())
                                     .Where(static f => !string.IsNullOrWhiteSpace(f))
                                     .ToArray();
        }
        else
        {
            // Strip leading slash(es) to get the subdirectory name
            string dirName = normalizedPath.TrimStart('/');

            // Find the directory marker entry: directoryNumber > 0, directory == 0, name matches
            DirectoryEntry? dirMarker = _directoryCache.Where(static e => e.directoryNumber > 0 && e.directory == 0)
                                                       .Cast<DirectoryEntry?>()
                                                       .FirstOrDefault(e => string.Equals(StringHandlers
                                                                              .CToString(e.Value.filename,
                                                                                   _encoding)
                                                                              .Trim(),
                                                                           dirName,
                                                                           StringComparison.OrdinalIgnoreCase));

            if(dirMarker is null)
            {
                AaruLogging.Debug(MODULE_NAME, "Directory not found: {0}", dirName);

                return ErrorNumber.NoSuchFile;
            }

            byte dirNumber = dirMarker.Value.directoryNumber;

            // List all entries that belong to this subdirectory
            entries = _directoryCache.Where(e => e.directory == dirNumber)
                                     .Select(e => StringHandlers.CToString(e.filename, _encoding).Trim())
                                     .Where(static f => !string.IsNullOrWhiteSpace(f))
                                     .ToArray();
        }

        node = new AoDosDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = entries
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node) =>
        node is not AoDosDirNode ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AoDosDirNode aoDosDirNode) return ErrorNumber.InvalidArgument;

        if(aoDosDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(aoDosDirNode.Position >= aoDosDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = aoDosDirNode.Entries[aoDosDirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Loads all directory entries and caches them</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading directory...");

        _directoryCache.Clear();

        // Read sector 0 which contains the boot block and the start of the directory
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sector0Data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading sector 0: {0}", errno);

            return errno;
        }

        // Directory entries start at offset 320 in sector 0
        // Each entry is 24 bytes
        // Maximum entries in sector 0: (512 - 320) / 24 = 8

        int totalEntries = _bootBlock.files;
        var entriesRead  = 0;

        // Read entries from sector 0
        for(var i = 0; i < ENTRIES_IN_BLOCK_0 && entriesRead < totalEntries; i++)
        {
            int offset = DIR_START_OFFSET + i * DIR_ENTRY_SIZE;

            if(offset + DIR_ENTRY_SIZE > sector0Data.Length) break;

            DirectoryEntry entry =
                Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(sector0Data, offset, DIR_ENTRY_SIZE);

            _directoryCache.Add(entry);
            entriesRead++;

            AaruLogging.Debug(MODULE_NAME,
                              "Entry: '{0}' (dirNo={1}, dir={2}, block={3}, blocks={4}, length={5})",
                              StringHandlers.CToString(entry.filename, _encoding).Trim(),
                              entry.directoryNumber,
                              entry.directory,
                              entry.blockNumber,
                              entry.blocks,
                              entry.length);
        }

        // If we need more entries, read subsequent sectors
        uint currentSector = 1;

        while(entriesRead < totalEntries)
        {
            if(_partition.Start + currentSector >= _partition.End)
            {
                AaruLogging.Debug(MODULE_NAME, "Reached end of partition while reading directory");

                break;
            }

            errno = _imagePlugin.ReadSector(_partition.Start + currentSector, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading sector {0}: {1}", currentSector, errno);

                return errno;
            }

            for(var i = 0; i < ENTRIES_PER_SECTOR && entriesRead < totalEntries; i++)
            {
                int offset = i * DIR_ENTRY_SIZE;

                if(offset + DIR_ENTRY_SIZE > sectorData.Length) break;

                DirectoryEntry entry =
                    Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(sectorData, offset, DIR_ENTRY_SIZE);

                _directoryCache.Add(entry);
                entriesRead++;

                AaruLogging.Debug(MODULE_NAME,
                                  "Entry: '{0}' (dirNo={1}, dir={2}, block={3}, blocks={4}, length={5})",
                                  StringHandlers.CToString(entry.filename, _encoding).Trim(),
                                  entry.directoryNumber,
                                  entry.directory,
                                  entry.blockNumber,
                                  entry.blocks,
                                  entry.length);
            }

            currentSector++;
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} directory entries", _directoryCache.Count);

        return ErrorNumber.NoError;
    }
}