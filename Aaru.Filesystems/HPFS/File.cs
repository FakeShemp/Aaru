// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class HPFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Root directory case
        if(normalizedPath == "/")
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Blocks     = 4, // Root dnode is 4 sectors
                BlockSize  = _bytesPerSector,
                Length     = 4 * _bytesPerSector,
                Inode      = _rootFnode,
                Links      = 1
            };

            return ErrorNumber.NoError;
        }

        // Find the directory entry for this path
        ErrorNumber errno = GetDirectoryEntry(normalizedPath, out DirectoryEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        stat = new FileEntryInfo
        {
            Attributes = new FileAttributes(),
            BlockSize  = _bytesPerSector,
            Length     = entry.file_size,
            Inode      = entry.fnode,
            Links      = 1
        };

        // Convert timestamps (seconds since 1970)
        if(entry.creation_date > 0) stat.CreationTime = DateHandlers.UnixToDateTime(entry.creation_date);

        if(entry.write_date > 0) stat.LastWriteTime = DateHandlers.UnixToDateTime(entry.write_date);

        if(entry.read_date > 0) stat.AccessTime = DateHandlers.UnixToDateTime(entry.read_date);

        // Calculate blocks - need to read fnode for accurate count
        ErrorNumber fnodeErr = ReadFNode(entry.fnode, out FNode fnode);

        if(fnodeErr == ErrorNumber.NoError)
        {
            // Get extent information to calculate actual blocks used
            BPlusLeafNode[] leafNodes = GetBPlusLeafNodes(fnode.btree, fnode.btree_data);
            uint            blocks    = 0;

            foreach(BPlusLeafNode leaf in leafNodes) blocks += leaf.length;

            stat.Blocks = blocks;
        }
        else
        {
            // Fall back to estimated blocks
            stat.Blocks = (entry.file_size + _bytesPerSector - 1) / _bytesPerSector;
        }

        // Map DOS attributes to FileAttributes
        if(entry.attributes.HasFlag(DosAttributes.Directory))
            stat.Attributes |= FileAttributes.Directory;
        else
            stat.Attributes |= FileAttributes.File;

        if(entry.attributes.HasFlag(DosAttributes.ReadOnly)) stat.Attributes |= FileAttributes.ReadOnly;

        if(entry.attributes.HasFlag(DosAttributes.Hidden)) stat.Attributes |= FileAttributes.Hidden;

        if(entry.attributes.HasFlag(DosAttributes.System)) stat.Attributes |= FileAttributes.System;

        if(entry.attributes.HasFlag(DosAttributes.Archive)) stat.Attributes |= FileAttributes.Archive;

        return ErrorNumber.NoError;
    }
}