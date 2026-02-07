// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Professional File System</summary>
public sealed partial class PFS
{
#region Nested type: ModeFlags

    /// <summary>PFS rootblock options/mode flags</summary>
    [Flags]
    enum ModeFlags : uint
    {
        /// <summary>No special options</summary>
        None = 0,
        /// <summary>Hard disk mode (as opposed to floppy)</summary>
        HardDisk = 1 << 0,
        /// <summary>Splitted anodes mode</summary>
        SplittedAnodes = 1 << 1,
        /// <summary>Directory extension enabled</summary>
        DirExtension = 1 << 2,
        /// <summary>Deleted directory enabled</summary>
        DelDir = 1 << 3,
        /// <summary>Disk size field present in rootblock</summary>
        SizeField = 1 << 4,
        /// <summary>Rootblock extension present</summary>
        Extension = 1 << 5,
        /// <summary>Datestamp was on at format time</summary>
        Datestamp = 1 << 6,
        /// <summary>Super index blocks enabled (for large disks)</summary>
        SuperIndex = 1 << 7,
        /// <summary>Super deldir enabled</summary>
        SuperDelDir = 1 << 8,
        /// <summary>Extended roving pointer</summary>
        ExtRoving = 1 << 9,
        /// <summary>Long filename support (> 30 chars)</summary>
        LongFn = 1 << 10,
        /// <summary>Large file support (> 4GB)</summary>
        LargeFile = 1 << 11
    }

#endregion

#region Nested type: EntryType

    /// <summary>Directory entry types (same as AmigaDOS)</summary>
    enum EntryType : sbyte
    {
        /// <summary>File entry</summary>
        File = -3,
        /// <summary>Directory entry</summary>
        Directory = 2,
        /// <summary>Soft link entry</summary>
        SoftLink = 3,
        /// <summary>Hard link to file</summary>
        HardLinkFile = -4,
        /// <summary>Hard link to directory</summary>
        HardLinkDir = 4,
        /// <summary>Rollover file</summary>
        RolloverFile = -16
    }

#endregion

#region Nested type: ProtectionBits

    /// <summary>AmigaDOS protection bits (active low for RWED)</summary>
    [Flags]
    enum ProtectionBits : byte
    {
        /// <summary>No protection bits set</summary>
        None = 0,
        /// <summary>Delete protection (active low)</summary>
        Delete = 1 << 0,
        /// <summary>Execute protection (active low)</summary>
        Execute = 1 << 1,
        /// <summary>Write protection (active low)</summary>
        Write = 1 << 2,
        /// <summary>Read protection (active low)</summary>
        Read = 1 << 3,
        /// <summary>Archive bit (active high)</summary>
        Archive = 1 << 4,
        /// <summary>Pure bit - reentrant/reexecutable (active high)</summary>
        Pure = 1 << 5,
        /// <summary>Script bit (active high)</summary>
        Script = 1 << 6,
        /// <summary>Hold bit - keep in memory (active high)</summary>
        Hold = 1 << 7
    }

#endregion

#region Nested type: ExtendedProtectionBits

    /// <summary>Extended protection bits stored in ExtraFields.prot (bytes 1-3)</summary>
    [Flags]
    enum ExtendedProtectionBits : uint
    {
        /// <summary>No extended protection bits</summary>
        None = 0,
        /// <summary>Group delete (active low)</summary>
        GroupDelete = 1 << 8,
        /// <summary>Group execute (active low)</summary>
        GroupExecute = 1 << 9,
        /// <summary>Group write (active low)</summary>
        GroupWrite = 1 << 10,
        /// <summary>Group read (active low)</summary>
        GroupRead = 1 << 11,
        /// <summary>Other delete (active low)</summary>
        OtherDelete = 1 << 16,
        /// <summary>Other execute (active low)</summary>
        OtherExecute = 1 << 17,
        /// <summary>Other write (active low)</summary>
        OtherWrite = 1 << 18,
        /// <summary>Other read (active low)</summary>
        OtherRead = 1 << 19
    }

#endregion
}