// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System plugin.
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
// These are not part of the Inside Macintosh specification and are
// implementation-specific to the Aaru HFS plugin.

using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class AppleHFS
{
    /// <summary>DirNode implementation for Apple HFS</summary>
    sealed class HfsDirNode : IDirNode
    {
        /// <summary>Array of sorted filenames in the directory</summary>
        internal string[] Contents;

        /// <summary>CNID of this directory for caching purposes</summary>
        internal uint DirectoryCNID;
        /// <summary>Current position in the directory contents array</summary>
        internal int Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#region Nested type: CatalogEntry

    /// <summary>Represents a catalog entry (directory or file)</summary>
    public abstract class CatalogEntry
    {
        /// <summary>Name of the entry</summary>
        public string Name { get; set; }

        /// <summary>Catalog Node ID (CNID) of this entry</summary>
        public uint CNID { get; set; }

        /// <summary>Parent directory ID</summary>
        public uint ParentID { get; set; }

        /// <summary>Type of entry: 1=directory, 2=file</summary>
        public int Type { get; set; }
    }

    /// <summary>Represents a directory entry in the catalog</summary>
    public sealed class DirectoryEntry : CatalogEntry
    {
        /// <summary>Number of entries in this directory</summary>
        public ushort Valence { get; set; }

        /// <summary>Creation date (Mac format)</summary>
        public uint CreationDate { get; set; }

        /// <summary>Last modification date (Mac format)</summary>
        public uint ModificationDate { get; set; }

        /// <summary>Last backup date (Mac format)</summary>
        public uint BackupDate { get; set; }

        /// <summary>Finder information</summary>
        public AppleCommon.DInfo FinderInfo { get; set; }

        /// <summary>Extended Finder information</summary>
        public AppleCommon.DXInfo ExtendedFinderInfo { get; set; }
    }

    /// <summary>Represents a file entry in the catalog</summary>
    public sealed class FileEntry : CatalogEntry
    {
        /// <summary>Finder information</summary>
        public AppleCommon.FInfo FinderInfo { get; set; }

        /// <summary>Extended Finder information</summary>
        public AppleCommon.FXInfo ExtendedFinderInfo { get; set; }

        /// <summary>Logical end-of-file for data fork</summary>
        public uint DataForkLogicalSize { get; set; }

        /// <summary>Physical end-of-file for data fork</summary>
        public uint DataForkPhysicalSize { get; set; }

        /// <summary>First allocation block of data fork</summary>
        public ushort DataForkStartBlock { get; set; }

        /// <summary>First three extents of data fork</summary>
        public ExtDataRec DataForkExtents { get; set; }

        /// <summary>Logical end-of-file for resource fork</summary>
        public uint ResourceForkLogicalSize { get; set; }

        /// <summary>Physical end-of-file for resource fork</summary>
        public uint ResourceForkPhysicalSize { get; set; }

        /// <summary>First allocation block of resource fork</summary>
        public ushort ResourceForkStartBlock { get; set; }

        /// <summary>First three extents of resource fork</summary>
        public ExtDataRec ResourceForkExtents { get; set; }

        /// <summary>Creation date (Mac format)</summary>
        public uint CreationDate { get; set; }

        /// <summary>Last modification date (Mac format)</summary>
        public uint ModificationDate { get; set; }

        /// <summary>Last backup date (Mac format)</summary>
        public uint BackupDate { get; set; }
    }

#endregion
}