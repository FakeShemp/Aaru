// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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

// Internal structures for caching and managing catalog entries.
// These are not part of the Apple TechNote 1150 specification and are
// implementation-specific to the Aaru HFS+ plugin.

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>DirNode implementation for Apple HFS+</summary>
    public sealed class HfsPlusDirNode : IDirNode
    {
        /// <summary>Array of sorted filenames in the directory (in Mac OS display format with colons)</summary>
        internal string[] Contents;

        /// <summary>CNID of this directory for caching purposes</summary>
        internal uint DirectoryCNID;

        /// <summary>Current position in the directory contents array</summary>
        internal int Position;

        /// <summary>Path to this directory</summary>
        public string Path { get; init; }
    }

    /// <summary>FileNode implementation for Apple HFS+</summary>
    public sealed class HfsPlusFileNode : IFileNode
    {
        /// <summary>File entry with metadata</summary>
        internal FileEntry FileEntry { get; set; }

        /// <summary>All extents for this fork (lazy-loaded)</summary>
        internal List<HFSPlusExtentDescriptor> AllExtents { get; set; }

        /// <summary>Current offset in file for reads</summary>
        public long Offset { get; set; }

        /// <summary>Path to this file</summary>
        public string Path { get; init; }

        /// <summary>Total length of data fork in bytes</summary>
        public long Length { get; init; }
    }

#region Nested type: CatalogEntry

    /// <summary>Represents a catalog entry (directory or file)</summary>
    internal abstract class CatalogEntry
    {
        /// <summary>Name of the entry</summary>
        public string Name { get; set; }

        /// <summary>Catalog Node ID (CNID) of this entry</summary>
        public uint CNID { get; set; }

        /// <summary>Parent directory ID</summary>
        public uint ParentID { get; set; }

        /// <summary>Type of entry: 1=directory, 2=file, 3=directory thread, 4=file thread</summary>
        public int Type { get; set; }
    }

    /// <summary>Represents a directory entry in the catalog</summary>
    sealed class DirectoryEntry : CatalogEntry
    {
        /// <summary>Number of entries in this directory</summary>
        public uint Valence { get; set; }

        /// <summary>Creation date (Mac format - seconds since 1904)</summary>
        public uint CreationDate { get; set; }

        /// <summary>Last content modification date (Mac format)</summary>
        public uint ContentModDate { get; set; }

        /// <summary>Last attribute modification date (Mac format)</summary>
        public uint AttributeModDate { get; set; }

        /// <summary>Last access date (Mac format)</summary>
        public uint AccessDate { get; set; }

        /// <summary>Last backup date (Mac format)</summary>
        public uint BackupDate { get; set; }

        /// <summary>Finder information</summary>
        public AppleCommon.DInfo FinderInfo { get; set; }

        /// <summary>Extended Finder information</summary>
        public AppleCommon.DXInfo ExtendedFinderInfo { get; set; }

        /// <summary>Text encoding hint for the folder name</summary>
        public uint TextEncoding { get; set; }

        /// <summary>BSD permissions structure</summary>
        public HFSPlusBSDInfo permissions { get; set; }
    }

    /// <summary>Represents a file entry in the catalog</summary>
    internal sealed class FileEntry : CatalogEntry
    {
        /// <summary>Finder information</summary>
        public AppleCommon.FInfo FinderInfo { get; set; }

        /// <summary>Extended Finder information</summary>
        public AppleCommon.FXInfo ExtendedFinderInfo { get; set; }

        /// <summary>Logical size of data fork in bytes</summary>
        public ulong DataForkLogicalSize { get; set; }

        /// <summary>Physical size of data fork in bytes</summary>
        public ulong DataForkPhysicalSize { get; set; }

        /// <summary>Number of allocation blocks used by data fork</summary>
        public uint DataForkTotalBlocks { get; set; }

        /// <summary>First 8 extents of data fork</summary>
        public HFSPlusExtentRecord DataForkExtents { get; set; }

        /// <summary>Logical size of resource fork in bytes</summary>
        public ulong ResourceForkLogicalSize { get; set; }

        /// <summary>Physical size of resource fork in bytes</summary>
        public ulong ResourceForkPhysicalSize { get; set; }

        /// <summary>Number of allocation blocks used by resource fork</summary>
        public uint ResourceForkTotalBlocks { get; set; }

        /// <summary>First 8 extents of resource fork</summary>
        public HFSPlusExtentRecord ResourceForkExtents { get; set; }

        /// <summary>Creation date (Mac format - seconds since 1904)</summary>
        public uint CreationDate { get; set; }

        /// <summary>Last content modification date (Mac format)</summary>
        public uint ContentModDate { get; set; }

        /// <summary>Last attribute modification date (Mac format)</summary>
        public uint AttributeModDate { get; set; }

        /// <summary>Last access date (Mac format)</summary>
        public uint AccessDate { get; set; }

        /// <summary>Last backup date (Mac format)</summary>
        public uint BackupDate { get; set; }

        /// <summary>Text encoding hint for the file name</summary>
        public uint TextEncoding { get; set; }

        /// <summary>BSD permissions structure</summary>
        public HFSPlusBSDInfo permissions { get; set; }
    }

    /// <summary>Represents a thread record entry in the catalog</summary>
    sealed class ThreadEntry : CatalogEntry
    {
        /// <summary>Name of the file or folder this thread references</summary>
        public string NodeName { get; set; }
    }

#endregion
}