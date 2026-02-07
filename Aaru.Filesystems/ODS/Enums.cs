// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Enumerations for the Files-11 On-Disk Structure.
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

public sealed partial class ODS
{
#region Nested type: VolumeCharacteristics

    /// <summary>Volume characteristics flags (volchar field in home block)</summary>
    [Flags]
    enum VolumeCharacteristics : ushort
    {
        /// <summary>Verify all read operations</summary>
        ReadCheck = 0x0001,
        /// <summary>Verify all write operations</summary>
        WriteCheck = 0x0002,
        /// <summary>Erase file contents before deallocation</summary>
        Erase = 0x0004,
        /// <summary>Disable highwater marking</summary>
        NoHighwater = 0x0008,
        /// <summary>Volume uses class protection</summary>
        ClassProt = 0x0010,
        /// <summary>Maintain file access times</summary>
        AccessTimes = 0x0020,
        /// <summary>Volume supports hardlinks</summary>
        HardLinks = 0x0040
    }

#endregion

#region Nested type: RecordTypeValue

    /// <summary>FAT record type values</summary>
    enum RecordTypeValue : byte
    {
        /// <summary>Undefined record type</summary>
        Undefined = 0,
        /// <summary>Fixed length records</summary>
        Fixed = 1,
        /// <summary>Variable length records</summary>
        Variable = 2,
        /// <summary>Variable with fixed control</summary>
        Vfc = 3,
        /// <summary>RMS-11 stream format</summary>
        Stream = 4,
        /// <summary>LF-terminated stream format</summary>
        StreamLf = 5,
        /// <summary>CR-terminated stream format</summary>
        StreamCr = 6
    }

#endregion

#region Nested type: FileOrganization

    /// <summary>FAT file organization values</summary>
    enum FileOrganization : byte
    {
        /// <summary>Sequential organization</summary>
        Sequential = 0,
        /// <summary>Relative organization</summary>
        Relative = 1,
        /// <summary>Indexed organization</summary>
        Indexed = 2,
        /// <summary>Direct organization</summary>
        Direct = 3,
        /// <summary>Special file organization</summary>
        Special = 4
    }

#endregion

#region Nested type: SpecialFileType

    /// <summary>Special file type values (when FileOrganization is Special)</summary>
    enum SpecialFileType : byte
    {
        /// <summary>FIFO special file</summary>
        Fifo = 1,
        /// <summary>Character special file</summary>
        CharSpecial = 2,
        /// <summary>Block special file</summary>
        BlockSpecial = 3,
        /// <summary>Symbolic link (pre-V8.2)</summary>
        SymLink = 4,
        /// <summary>Symbolic link (V8.2 and beyond)</summary>
        SymbolicLink = 5
    }

#endregion

#region Nested type: RecordAttributeFlags

    /// <summary>FAT record attribute flags</summary>
    [Flags]
    enum RecordAttributeFlags : byte
    {
        /// <summary>Fortran carriage control</summary>
        FortranCC = 0x01,
        /// <summary>Implied carriage control</summary>
        ImpliedCC = 0x02,
        /// <summary>Print file carriage control</summary>
        PrintCC = 0x04,
        /// <summary>No spanned records</summary>
        NoSpan = 0x08,
        /// <summary>Format of RCW (0=LSB, 1=MSB)</summary>
        MsbRcw = 0x10
    }

#endregion

#region Nested type: GlobalBufferCountFlags

    /// <summary>Global buffer count flags</summary>
    [Flags]
    enum GlobalBufferCountFlags : byte
    {
        /// <summary>GBC is a percentage</summary>
        Percent = 0x01,
        /// <summary>Use default GBC</summary>
        Default = 0x02
    }

#endregion

#region Nested type: FileCharacteristicFlags

    /// <summary>File characteristics flags</summary>
    [Flags]
    enum FileCharacteristicFlags : uint
    {
        /// <summary>File was (or is) contiguous</summary>
        WasContig = 0x00000001,
        /// <summary>File is not to be backed up</summary>
        NoBackup = 0x00000002,
        /// <summary>Write back caching enabled</summary>
        WriteBack = 0x00000004,
        /// <summary>Verify all read operations</summary>
        ReadCheck = 0x00000008,
        /// <summary>Verify all write operations</summary>
        WriteCheck = 0x00000010,
        /// <summary>Keep file contiguous when extending</summary>
        ContigB = 0x00000020,
        /// <summary>File is deaccess-locked</summary>
        Locked = 0x00000040,
        /// <summary>File is contiguous</summary>
        Contig = 0x00000080,
        /// <summary>ACL is invalid</summary>
        BadAcl = 0x00000800,
        /// <summary>File is a spool file</summary>
        Spool = 0x00001000,
        /// <summary>File is a directory</summary>
        Directory = 0x00002000,
        /// <summary>File contains bad blocks</summary>
        BadBlock = 0x00004000,
        /// <summary>File is marked for delete</summary>
        MarkDel = 0x00008000,
        /// <summary>Do not charge quota for file</summary>
        NoCharge = 0x00010000,
        /// <summary>Erase file contents before deallocation</summary>
        Erase = 0x00020000,
        /// <summary>ALM access in progress</summary>
        AlmAip = 0x00040000,
        /// <summary>File is shelved</summary>
        Shelved = 0x00080000,
        /// <summary>File is a scratch file</summary>
        Scratch = 0x00100000,
        /// <summary>File cannot be moved</summary>
        NoMove = 0x00200000,
        /// <summary>File cannot be shelved</summary>
        NoShelvable = 0x00400000,
        /// <summary>File is preshelved</summary>
        PreShelved = 0x00800000
    }

#endregion

#region Nested type: DirectoryRecordType

    /// <summary>Directory record type values</summary>
    enum DirectoryRecordType : byte
    {
        /// <summary>File ID entry</summary>
        Fid = 0,
        /// <summary>Link name entry</summary>
        LinkName = 1
    }

#endregion

#region Nested type: DirectoryNameType

    /// <summary>Directory name type values</summary>
    enum DirectoryNameType : byte
    {
        /// <summary>ODS-2 compatible name</summary>
        Ods2 = 0,
        /// <summary>ISO Latin-1 Stratum-1 name</summary>
        Isl1 = 1,
        /// <summary>UCS-2 (Unicode) name</summary>
        Ucs2 = 3
    }

#endregion

#region Nested type: DirectoryRecordFlags

    /// <summary>Directory record flags</summary>
    [Flags]
    enum DirectoryRecordFlags : byte
    {
        /// <summary>Record type mask (3 bits)</summary>
        TypeMask = 0x07,
        /// <summary>Name type mask (3 bits, shifted by 3)</summary>
        NameTypeMask = 0x38,
        /// <summary>Next record flag</summary>
        NextRec = 0x40,
        /// <summary>Previous record flag</summary>
        PrevRec = 0x80
    }

#endregion

#region Nested type: FileIdentControl

    /// <summary>File ident control byte values (ODS-5)</summary>
    enum FileIdentControl : byte
    {
        /// <summary>ODS-2 compatible name</summary>
        Ods2 = 0,
        /// <summary>ISO Latin-1 Stratum-1 name</summary>
        Isl1 = 1,
        /// <summary>UCS-2 (Unicode) name</summary>
        Ucs2 = 3
    }

#endregion
}