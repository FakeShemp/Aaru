// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

// ReSharper disable UnusedType.Local

// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Local

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

// Information from Inside Macintosh
// https://developer.apple.com/legacy/library/documentation/mac/pdf/Files/File_Manager.pdf
public sealed partial class AppleHFS
{
#region Nested type: BTHdrRed

    /// <summary>B*-tree header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BTHdrRed
    {
        /// <summary>Current depth of tree.</summary>
        public ushort bthDepth;
        /// <summary>Number of root node.</summary>
        public uint bthRoot;
        /// <summary>Number of leaf records in tree.</summary>
        public uint bthNRecs;
        /// <summary>Number of first leaf node.</summary>
        public uint bthFNode;
        /// <summary>Number of last leaf node.</summary>
        public uint bthLNode;
        /// <summary>Size of a node.</summary>
        public ushort bthNodeSize;
        /// <summary>Maximum length of a key.</summary>
        public ushort bthKeyLen;
        /// <summary>Total number of nodes in tree.</summary>
        public uint bthNNodes;
        /// <summary>Number of free nodes.</summary>
        public uint bthFree;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 76)]
        public sbyte[] bthResv;
    }

#endregion

#region Nested type: CatDataRec

    /// <summary>Catalog data record header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct CatDataRec
    {
        public CatDataType cdrType;
        public sbyte       cdrResvr2;
    }

#endregion

#region Nested type: CatKeyRec

    /// <summary>Catalog key record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct CatKeyRec
    {
        /// <summary>Key length.</summary>
        public sbyte ckrKeyLen;
        /// <summary>Reserved.</summary>
        public sbyte ckrResrv1;
        /// <summary>Parent directory ID.</summary>
        public uint ckrParID;
        /// <summary>Catalog node name. Full 32 bytes in index nodes but only the needed bytes, padded to word, in leaf nodes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ckrCName;
    }

#endregion

#region Nested type: CdrDirRec

    /// <summary>Directory record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct CdrDirRec
    {
        public CatDataRec dirHdr;
        /// <summary>Directory flags.</summary>
        public ushort dirFlags;
        /// <summary>Directory valence.</summary>
        public ushort dirVal;
        /// <summary>Directory ID.</summary>
        public uint dirDirID;
        /// <summary>Date and time of creation.</summary>
        public uint dirCrDat;
        /// <summary>Date and time of last modification.</summary>
        public uint dirMdDat;
        /// <summary>Date and time of last backup.</summary>
        public uint dirBkDat;
        /// <summary>Finder information.</summary>
        public AppleCommon.DInfo dirUsrInfo;
        /// <summary>Additional Finder information.</summary>
        public AppleCommon.DXInfo dirFndrInfo;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] dirResrv;
    }

#endregion

#region Nested type: CdrFilRec

    /// <summary>File record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct CdrFilRec
    {
        public CatDataRec filHdr;
        /// <summary>File flags.</summary>
        public sbyte filFlags;
        /// <summary>File type.</summary>
        public sbyte filType;
        /// <summary>Finder information.</summary>
        public AppleCommon.FInfo filUsrWds;
        /// <summary>File ID.</summary>
        public uint filFlNum;
        /// <summary>First allocation block of data fork.</summary>
        public ushort filStBlk;
        /// <summary>Logical EOF of data fork.</summary>
        public uint filLgLen;
        /// <summary>Physical EOF of data fork.</summary>
        public uint filPyLen;
        /// <summary>First allocation block of resource fork.</summary>
        public ushort filRStBlk;
        /// <summary>Logical EOF of resource fork.</summary>
        public uint filRLgLen;
        /// <summary>Physical EOF of resource fork.</summary>
        public uint filRPyLen;
        /// <summary>Date and time of creation.</summary>
        public uint filCrDat;
        /// <summary>Date and time of last modification.</summary>
        public uint filMdDat;
        /// <summary>Date and time of last backup.</summary>
        public uint filBkDat;
        /// <summary>Additional Finder information.</summary>
        public AppleCommon.FXInfo filFndrInfo;
        /// <summary>File clump size.</summary>
        public ushort filClpSize;
        /// <summary>First data fork extent record.</summary>
        public ExtDataRec filExtRec;
        /// <summary>First resource fork extent record.</summary>
        public ExtDataRec filRExtRec;
        /// <summary>Reserved</summary>
        public uint filResrv;
    }

#endregion

#region Nested type: CdrFThdRec

    /// <summary>File thread record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CdrFThdRec
    {
        public readonly CatDataRec fthdHdr;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] fthdResrv;
        /// <summary>Parent ID for this file.</summary>
        public readonly uint fthdParID;
        /// <summary>Name of this file.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] fthdCName;
    }

#endregion

#region Nested type: CdrThdRec

    /// <summary>Directory thread record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CdrThdRec
    {
        public readonly CatDataRec thdHdr;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] thdResrv;
        /// <summary>Parent ID for this directory.</summary>
        public readonly uint thdParID;
        /// <summary>Name of this directory.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] thdCName;
    }

#endregion

#region Nested type: ExtDataRec

    /// <summary>Extent data record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    public partial struct ExtDataRec
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ExtDescriptor[] xdr;
    }

#endregion

#region Nested type: ExtDescriptor

    /// <summary>Extent descriptor</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    public partial struct ExtDescriptor
    {
        /// <summary>First allocation block</summary>
        public ushort xdrStABN;
        /// <summary>Number of allocation blocks</summary>
        public ushort xdrNumABlks;
    }

#endregion

#region Nested type: ExtKeyRec

    /// <summary>Extent key record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtKeyRec
    {
        /// <summary>Key length.</summary>
        public readonly sbyte xkrKeyLen;
        /// <summary>Fork type.</summary>
        public readonly ForkType xkrFkType;
        /// <summary>File number.</summary>
        public readonly uint xkrFNum;
        /// <summary>Starting file allocation block.</summary>
        public readonly ushort xkrFABN;
    }

#endregion

#region Nested type: MasterDirectoryBlock

    /// <summary>Master Directory Block, should be sector 2 in volume</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct MasterDirectoryBlock // Should be sector 2 in volume
    {
        /// <summary>0x000, Signature, 0x4244</summary>
        public ushort drSigWord;
        /// <summary>0x002, Volume creation date</summary>
        public uint drCrDate;
        /// <summary>0x006, Volume last modification date</summary>
        public uint drLsMod;
        /// <summary>0x00A, Volume attributes</summary>
        public AppleCommon.VolumeAttributes drAtrb;
        /// <summary>0x00C, Files in root directory</summary>
        public ushort drNmFls;
        /// <summary>0x00E, Start 512-byte sector of volume bitmap</summary>
        public ushort drVBMSt;
        /// <summary>0x010, Allocation block to begin next allocation</summary>
        public ushort drAllocPtr;
        /// <summary>0x012, Allocation blocks</summary>
        public ushort drNmAlBlks;
        /// <summary>0x014, Bytes per allocation block</summary>
        public uint drAlBlkSiz;
        /// <summary>0x018, Bytes to allocate when extending a file</summary>
        public uint drClpSiz;
        /// <summary>0x01C, Start 512-byte sector of first allocation block</summary>
        public ushort drAlBlSt;
        /// <summary>0x01E, CNID for next file</summary>
        public uint drNxtCNID;
        /// <summary>0x022, Free allocation blocks</summary>
        public ushort drFreeBks;
        /// <summary>0x024, Volume name (28 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] drVN;
        /// <summary>0x040, Volume last backup time</summary>
        public uint drVolBkUp;
        /// <summary>0x044, Volume backup sequence number</summary>
        public ushort drVSeqNum;
        /// <summary>0x046, Filesystem write count</summary>
        public uint drWrCnt;
        /// <summary>0x04A, Bytes to allocate when extending the extents B-Tree</summary>
        public uint drXTClpSiz;
        /// <summary>0x04E, Bytes to allocate when extending the catalog B-Tree</summary>
        public uint drCTClpSiz;
        /// <summary>0x052, Number of directories in root directory</summary>
        public ushort drNmRtDirs;
        /// <summary>0x054, Number of files in the volume</summary>
        public uint drFilCnt;
        /// <summary>0x058, Number of directories in the volume</summary>
        public uint drDirCnt;
        /// <summary>0x05C, finderInfo[0], CNID for bootable system's directory</summary>
        public uint drFndrInfo0;
        /// <summary>0x060, finderInfo[1], CNID of the directory containing the boot application</summary>
        public uint drFndrInfo1;
        /// <summary>0x064, finderInfo[2], CNID of the directory that should be opened on boot</summary>
        public uint drFndrInfo2;
        /// <summary>0x068, finderInfo[3], CNID for Mac OS 8 or 9 directory</summary>
        public uint drFndrInfo3;
        /// <summary>0x06C, finderInfo[4], Reserved</summary>
        public uint drFndrInfo4;
        /// <summary>0x070, finderInfo[5], CNID for Mac OS X directory</summary>
        public uint drFndrInfo5;
        /// <summary>0x074, finderInfo[6], first part of Mac OS X volume ID</summary>
        public uint drFndrInfo6;
        /// <summary>0x078, finderInfo[7], second part of Mac OS X volume ID</summary>
        public uint drFndrInfo7;

        // If wrapping HFS+
        /// <summary>0x07C, Embedded volume signature, "H+" if HFS+ is embedded ignore following two fields if not</summary>
        public ushort drEmbedSigWord;
        /// <summary>0x07E, Starting block number of embedded HFS+ volume</summary>
        public ushort xdrStABNt;
        /// <summary>0x080, Allocation blocks used by embedded volume</summary>
        public ushort xdrNumABlks;

        // If not (use the previous fields, just the names here)
        /// <summary>0x07C, Size in blocks of volume cache</summary>
        //public ushort drVCSize;
        /// <summary>0x07E, Size in blocks of volume bitmap cache</summary>
        //public ushort drVBMCSize;
        /// <summary>0x080, Size in blocks of volume common cache</summary>
        //public ushort drCtlCSize;

        // End of variable variables :D
        /// <summary>0x082, Bytes in the extents B-Tree 3 HFS extents following, 32 bits each</summary>
        public uint drXTFlSize;
        /// <summary>0x086, Extents B-Tree extent records (3 ExtDescriptors)</summary>
        public ExtDataRec drXTExtRec;
        /// <summary>0x092, Bytes in the catalog B-Tree 3 HFS extents following, 32 bits each</summary>
        public uint drCTFlSize;
        /// <summary>0x096, Catalog B-Tree extent records (3 ExtDescriptors)</summary>
        public ExtDataRec drCTExtRec;
    }

#endregion

#region Nested type: NodeDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct NodeDescriptor
    {
        /// <summary>A link to the next node of this type, or <c>null</c> if this is the last one.</summary>
        public uint ndFLink;
        /// <summary>A link to the previous node of this type, or <c>null</c> if this is the first one.</summary>
        public uint ndBLink;
        /// <summary>The type of this node.</summary>
        public NodeType ndType;
        /// <summary>The depth of this node in the B*-tree hierarchy. Maximum depth is apparently 8.</summary>
        public sbyte ndNHeight;
        /// <summary>The number of records contained in this node.</summary>
        public ushort ndNRecs;
        /// <summary>Reserved, should be 0.</summary>
        public ushort ndResv2;
    }

#endregion
}