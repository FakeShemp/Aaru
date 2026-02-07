// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Enumerations for the OS/2 High Performance File System.
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
public sealed partial class HPFS
{
#region Nested type: SpareBlockFlags

    /// <summary>Spare block flags (byte at offset 0x08)</summary>
    [Flags]
    enum SpareBlockFlags : byte
    {
        /// <summary>0 = clean, 1 = "improperly stopped"</summary>
        Dirty = 0x01,
        /// <summary>Spare dirblks used</summary>
        SpareDirUsed = 0x02,
        /// <summary>Hotfixes used</summary>
        HotfixesUsed = 0x04,
        /// <summary>Bad sector, corrupted disk</summary>
        BadSector = 0x08,
        /// <summary>Bad bitmap</summary>
        BadBitmap = 0x10,
        /// <summary>Partition was fast formatted</summary>
        FastFormat = 0x20,
        /// <summary>Old version wrote to partition</summary>
        OldWrote = 0x40,
        /// <summary>Old version wrote to partition (alternate bit)</summary>
        OldWrote1 = 0x80
    }

#endregion

#region Nested type: SpareBlockFlags386

    /// <summary>Spare block HPFS386 flags (byte at offset 0x09)</summary>
    [Flags]
    enum SpareBlockFlags386 : byte
    {
        /// <summary>Install DASD limits</summary>
        InstallDasdLimits = 0x01,
        /// <summary>Resynch DASD limits</summary>
        ResynchDasdLimits = 0x02,
        /// <summary>DASD limits operational</summary>
        DasdLimitsOperational = 0x04,
        /// <summary>Multimedia active</summary>
        MultimediaActive = 0x08,
        /// <summary>DCE ACLs active</summary>
        DceAclsActive = 0x10,
        /// <summary>DASD limits dirty</summary>
        DasdLimitsDirty = 0x20
    }

#endregion

#region Nested type: DirectoryEntryFlags

    /// <summary>Directory entry flags</summary>
    [Flags]
    enum DirectoryEntryFlags : byte
    {
        /// <summary>Set on phony ^A^A (".") entry</summary>
        First = 0x01,
        /// <summary>Entry has ACL</summary>
        HasAcl = 0x02,
        /// <summary>Down pointer present (after name)</summary>
        Down = 0x04,
        /// <summary>Set on phony \377 entry (last entry)</summary>
        Last = 0x08,
        /// <summary>Entry has EA</summary>
        HasEa = 0x10,
        /// <summary>Has extended permission list</summary>
        HasXtdPerm = 0x20,
        /// <summary>Has explicit ACL</summary>
        HasExplicitAcl = 0x40,
        /// <summary>Some EA has NEEDEA set</summary>
        HasNeedEa = 0x80
    }

#endregion

#region Nested type: DosAttributes

    /// <summary>DOS file attributes</summary>
    [Flags]
    enum DosAttributes : byte
    {
        /// <summary>Read-only file</summary>
        ReadOnly = 0x01,
        /// <summary>Hidden file</summary>
        Hidden = 0x02,
        /// <summary>System file</summary>
        System = 0x04,
        /// <summary>Would be volume label (unused in HPFS)</summary>
        VolumeLabel = 0x08,
        /// <summary>Directory</summary>
        Directory = 0x10,
        /// <summary>Archive (needs backup)</summary>
        Archive = 0x20,
        /// <summary>Name is not 8.3 format</summary>
        Not8x3 = 0x40,
        /// <summary>Reserved flag</summary>
        Flag15 = 0x80
    }

#endregion

#region Nested type: FNodeFlags

    /// <summary>FNode flags</summary>
    [Flags]
    enum FNodeFlags : ushort
    {
        /// <summary>EA sector number is an anode</summary>
        EaAnode = 0x0002,
        /// <summary>This is a directory (first extent points to dnode)</summary>
        Directory = 0x0100
    }

#endregion

#region Nested type: BPlusFlags

    /// <summary>B+ tree header flags</summary>
    [Flags]
    enum BPlusFlags : byte
    {
        /// <summary>High bit of first free entry offset</summary>
        Hbff = 0x01,
        /// <summary>Pointed to by fnode, data btree, or EA</summary>
        FnodeParent = 0x20,
        /// <summary>Suggest binary search (unused)</summary>
        BinarySearch = 0x40,
        /// <summary>Internal node (tree of anodes), otherwise leaf (list of extents)</summary>
        Internal = 0x80
    }

#endregion

#region Nested type: ExtendedAttributeFlags

    /// <summary>Extended attribute flags</summary>
    [Flags]
    enum ExtendedAttributeFlags : byte
    {
        /// <summary>Value gives sector number where real value starts</summary>
        Indirect = 0x01,
        /// <summary>Sector is an anode that points to fragmented value</summary>
        Anode = 0x02,
        /// <summary>Required EA (NEEDEA)</summary>
        NeedEa = 0x80
    }

#endregion
}