// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class BOFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(string.IsNullOrEmpty(path) || path == "/")
        {
            var dirNode = new BOFSDirNode
            {
                Path                      = "/",
                FirstDirectoryBlockSector = _track0.FirstDirectorySector,
                DirectoryBlocks           = new List<DirectoryBlock>(),
                CurrentEntryIndex         = 0,
                CurrentBlockIndex         = 0,
                Disposed                  = false
            };

            node = dirNode;

            return ErrorNumber.NoError;
        }

        lock(_rootDirectoryCache)
        {
            if(!_rootDirectoryCache.TryGetValue(path.TrimStart('/'), out FileEntry dirEntry))
                return ErrorNumber.NoSuchFile;

            // Check if this entry is actually a directory (FileType = -1 for SDIR)
            if(dirEntry.FileType != DIR_TYPE) return ErrorNumber.NotDirectory;

            var dirNode = new BOFSDirNode
            {
                Path                      = path,
                FirstDirectoryBlockSector = dirEntry.FirstAllocList,
                DirectoryBlocks           = new List<DirectoryBlock>(),
                CurrentEntryIndex         = 0,
                CurrentBlockIndex         = 0,
                Disposed                  = false
            };

            node = dirNode;

            return ErrorNumber.NoError;
        }
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not BOFSDirNode dirNode) return ErrorNumber.InvalidArgument;

        dirNode.Disposed = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(node is not BOFSDirNode dirNode) return ErrorNumber.InvalidArgument;

        if(dirNode.Disposed) return ErrorNumber.InvalidArgument;

        if(dirNode.Path != "/") return ErrorNumber.NotSupported;

        lock(_rootDirectoryCache)
        {
            var entries = new List<string>(_rootDirectoryCache.Keys);

            if(dirNode.CurrentEntryIndex >= entries.Count) return ErrorNumber.NoError;

            filename = entries[dirNode.CurrentEntryIndex];
            dirNode.CurrentEntryIndex++;

            return ErrorNumber.NoError;
        }
    }

    /// <inheritdoc />
    private sealed class BOFSDirNode : IDirNode
    {
        /// <summary>First directory block sector</summary>
        public int FirstDirectoryBlockSector { get; set; }

        /// <summary>List of directory blocks for this directory</summary>
        public List<DirectoryBlock> DirectoryBlocks { get; set; }

        /// <summary>Index of current entry in directory blocks</summary>
        public int CurrentEntryIndex { get; set; }

        /// <summary>Index of current block</summary>
        public int CurrentBlockIndex { get; set; }

        /// <summary>Enumeration state</summary>
        public bool Disposed { get; set; }
        /// <inheritdoc />
        public string Path { get; set; }
    }
}