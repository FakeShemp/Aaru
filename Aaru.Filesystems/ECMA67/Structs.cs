// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the ECMA-67 file system and shows information.
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

using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

public sealed partial class ECMA67
{
#region Nested type: VolumeLabel

    /// <summary>Volume Label (VOL1) as defined in ECMA-67 section 7.3. Recorded in cylinder 00, side 0, sector 07.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeLabel
    {
        /// <summary>CP 1-3: Label Identifier. Shall be "VOL".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] labelIdentifier;
        /// <summary>CP 4: Label Number. Shall be digit ONE.</summary>
        public readonly byte labelNumber;
        /// <summary>CP 5-10: Volume Identifier. Identifies the volume, assigned by the owner.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] volumeIdentifier;
        /// <summary>CP 11: Volume Accessibility Indicator. SPACE means no access restriction.</summary>
        public readonly byte volumeAccessibility;
        /// <summary>CP 12-37: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public readonly byte[] reserved1;
        /// <summary>CP 38-51: Owner Identifier. Identifies the owner of the volume.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public readonly byte[] owner;
        /// <summary>CP 52-71: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly byte[] reserved2;
        /// <summary>CP 72: Surface Indicator. SPACE or '1' = side 0 only (ECMA-66); 'M' = both sides (ECMA-70).</summary>
        public readonly byte surface;
        /// <summary>CP 73-75: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved3;
        /// <summary>CP 76: Physical Record Length Identifier. '1' means 256 character positions.</summary>
        public readonly byte recordLength;
        /// <summary>CP 77-78: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved4;
        /// <summary>CP 79: File Label Allocation. SPACE = no labels on side 1; '1' = side 1 sectors reserved for HDR1 labels.</summary>
        public readonly byte fileLabelAllocation;
        /// <summary>CP 80: Label Standard Version. '1' indicates ECMA-67, January 1981.</summary>
        public readonly byte labelStandardVersion;
        /// <summary>CP 81-128: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public readonly byte[] reserved5;
    }

#endregion

#region Nested type: FileLabel

    /// <summary>
    ///     File Header Label (HDR1) as defined in ECMA-67 section 7.4. Recorded in cylinder 00, side 0 sectors 08-16 and
    ///     side 1 sectors 01-16.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileLabel
    {
        /// <summary>CP 1-3: Label Identifier. Shall be "HDR".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] labelIdentifier;
        /// <summary>CP 4: Label Number. Shall be digit ONE.</summary>
        public readonly byte labelNumber;
        /// <summary>CP 5: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved1;
        /// <summary>CP 6-22: File Identifier. Uniquely identifies the file on the volume.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public readonly byte[] fileIdentifier;
        /// <summary>CP 23-27: Block Length. Number of characters per block, as ASCII digits.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] blockLength;
        /// <summary>CP 28: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved2;
        /// <summary>CP 29-33: Begin of Extent. Address of the first Physical Record of the extent (CCSSN: cylinder, side, sector).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] beginOfExtent;
        /// <summary>CP 34: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved3;
        /// <summary>CP 35-39: End of Extent. Address of the last Physical Record of the extent (CCSSN: cylinder, side, sector).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] endOfExtent;
        /// <summary>CP 40: Record Format. SPACE or 'F' = fixed-length; 'V' = variable-length.</summary>
        public readonly byte recordFormat;
        /// <summary>CP 41: Bypass Indicator. SPACE = file intended for interchange; 'B' = file not intended for interchange.</summary>
        public readonly byte bypassIndicator;
        /// <summary>CP 42: File Accessibility Indicator. SPACE = no access restriction.</summary>
        public readonly byte fileAccessibility;
        /// <summary>CP 43: Write Protect. SPACE = no protection; 'P' = file is protected against alteration.</summary>
        public readonly byte writeProtect;
        /// <summary>CP 44: Interchange Type. SPACE = Basic Interchange; '1' = E1; '2' = E2; capital letter = non-conforming.</summary>
        public readonly byte interchangeType;
        /// <summary>
        ///     CP 45: Multivolume Indicator. SPACE = file entirely in volume; 'C' = continues on another volume; 'L' = ends
        ///     but does not begin in volume.
        /// </summary>
        public readonly byte multivolumeIndicator;
        /// <summary>
        ///     CP 46-47: File Section Number. SPACEs = not numbered; digits 01-99 = ordinal section number in a multivolume
        ///     file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] fileSectionNumber;
        /// <summary>CP 48-53: Creation Date. SPACEs = not significant; otherwise YYMMDD.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] creationDate;
        /// <summary>CP 54-57: Record Length. SPACEs = equal to block length; otherwise maximum characters per record.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] recordLength;
        /// <summary>CP 58-62: Offset to Next Record Space. Unused character positions in the last block. SPACEs or ZEROs = none.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] offsetToNextRecordSpace;
        /// <summary>CP 63: Record Attribute. SPACE = unblocked records; 'B' = blocked records.</summary>
        public readonly byte recordAttribute;
        /// <summary>CP 64: File Organization. SPACE or 'S' = sequential.</summary>
        public readonly byte fileOrganization;
        /// <summary>CP 65-66: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved4;
        /// <summary>
        ///     CP 67-72: Expiration Date. SPACEs = may be deleted; "999999" = never; otherwise earliest deletion date as
        ///     YYMMDD.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] expirationDate;
        /// <summary>CP 73: Verify/Copy Indicator. SPACE = not verified or copied, or not relevant.</summary>
        public readonly byte verifyCopyIndicator;
        /// <summary>CP 74: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved5;
        /// <summary>
        ///     CP 75-79: End of Data. Address of the Physical Record containing the beginning of the next available unused
        ///     block (CCSSN).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] endOfData;
        /// <summary>CP 80-128: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 49)]
        public readonly byte[] reserved6;
    }

#endregion

#region Nested type: ErrorMapLabel

    /// <summary>Error Map Label (ERMAP) as defined in ECMA-67 section 7.5. Recorded in cylinder 00, side 0, sector 05.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ErrorMapLabel
    {
        /// <summary>CP 1-5: Label Identifier. Shall be "ERMAP".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] labelIdentifier;
        /// <summary>CP 6: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved1;
        /// <summary>
        ///     CP 7-9: Defective Cylinder Identification 1. SPACEs = none; otherwise two-digit cylinder number (01-32)
        ///     followed by ZERO.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] defectiveCylinder1;
        /// <summary>CP 10: Reserved for future standardization. Shall be SPACE.</summary>
        public readonly byte reserved2;
        /// <summary>
        ///     CP 11-13: Defective Cylinder Identification 2. SPACEs = none or only one defective; otherwise two-digit
        ///     cylinder number (02-33) followed by ZERO.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] defectiveCylinder2;
        /// <summary>CP 14-128: Reserved for future standardization. Shall be SPACEs.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 115)]
        public readonly byte[] reserved3;
    }

#endregion
}