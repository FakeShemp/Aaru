// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains enumerations for Expert Witness Format logical evidence files.
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
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Archives;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class EwfArchive
{
    /// <summary>EWF v2 compression method</summary>
    enum EwfCompressionMethod : ushort
    {
        None    = 0,
        Deflate = 1,
        Bzip2   = 2
    }

    /// <summary>EWF v2 section type codes</summary>
    enum EwfSectionTypeV2 : uint
    {
        DeviceInformation   = 0x00000001,
        CaseData            = 0x00000002,
        SectorData          = 0x00000003,
        SectorTable         = 0x00000004,
        ErrorTable          = 0x00000005,
        SessionTable        = 0x00000006,
        IncrementData       = 0x00000007,
        Md5Hash             = 0x00000008,
        Sha1Hash            = 0x00000009,
        RestartData         = 0x0000000A,
        EncryptionKeys      = 0x0000000B,
        MemoryExtentsTable  = 0x0000000C,
        Next                = 0x0000000D,
        FinalInformation    = 0x0000000E,
        Done                = 0x0000000F,
        AnalyticalData      = 0x00000010,
        SingleFilesData     = 0x00000020,
        SingleFilesTree     = 0x00000021,
        SingleFilesMetadata = 0x00000022,
        SingleFilesSource   = 0x00000023
    }

    /// <summary>LEF file entry flags from opr field</summary>
    [Flags]
    enum EwfLefEntryFlags : uint
    {
        ReadOnly  = 0x00000001,
        Hidden    = 0x00000002,
        System    = 0x00000004,
        Archive   = 0x00000008,
        Symlink   = 0x00000010,
        Deleted   = 0x00000080,
        HardLink  = 0x00001000,
        AltStream = 0x00002000,
        Folder    = 0x02000000,
        Sparse    = 0x04000000
    }
}