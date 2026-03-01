// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Enumerations for the Microsoft NT File System.
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
public sealed partial class NTFS
{
#region Nested type: NtfsRecordMagic

    /// <summary>Magic identifiers present at the beginning of all multi-sector protected NTFS records</summary>
    enum NtfsRecordMagic : uint
    {
        /// <summary>MFT entry ("FILE")</summary>
        File = 0x454c4946,
        /// <summary>Index buffer ("INDX")</summary>
        Indx = 0x58444e49,
        /// <summary>Hole marker ("HOLE")</summary>
        Hole = 0x454c4f48,
        /// <summary>Restart page ("RSTR")</summary>
        Rstr = 0x52545352,
        /// <summary>Log record page ("RCRD")</summary>
        Rcrd = 0x44524352,
        /// <summary>Chkdsk modified ("CHKD")</summary>
        Chkd = 0x444b4843,
        /// <summary>Bad record ("BAAD")</summary>
        Baad = 0x44414142,
        /// <summary>Empty/uninitialized</summary>
        Empty = 0xffffffff
    }

#endregion

#region Nested type: SystemFileNumber

    /// <summary>System files MFT record numbers</summary>
    enum SystemFileNumber : uint
    {
        /// <summary>Master File Table ($MFT)</summary>
        Mft = 0,
        /// <summary>MFT mirror ($MFTMirr)</summary>
        MftMirr = 1,
        /// <summary>Journaling log ($LogFile)</summary>
        LogFile = 2,
        /// <summary>Volume information ($Volume)</summary>
        Volume = 3,
        /// <summary>Attribute definitions ($AttrDef)</summary>
        AttrDef = 4,
        /// <summary>Root directory</summary>
        Root = 5,
        /// <summary>Cluster allocation bitmap ($Bitmap)</summary>
        Bitmap = 6,
        /// <summary>Boot sector ($Boot)</summary>
        Boot = 7,
        /// <summary>Bad cluster list ($BadClus)</summary>
        BadClus = 8,
        /// <summary>Security descriptors ($Secure)</summary>
        Secure = 9,
        /// <summary>Uppercase table ($UpCase)</summary>
        UpCase = 10,
        /// <summary>Extended system directory ($Extend)</summary>
        Extend = 11,
        /// <summary>First user file record</summary>
        FirstUser = 16
    }

#endregion

#region Nested type: MftRecordFlags

    /// <summary>MFT record flags (16-bit)</summary>
    [Flags]
    enum MftRecordFlags : ushort
    {
        /// <summary>Record is allocated and in use</summary>
        InUse = 0x0001,
        /// <summary>Record represents a directory</summary>
        IsDirectory = 0x0002,
        /// <summary>Special record 4 type</summary>
        Is4 = 0x0004,
        /// <summary>Used as view index</summary>
        IsViewIndex = 0x0008
    }

#endregion

#region Nested type: AttributeType

    /// <summary>System-defined attribute types (32-bit)</summary>
    enum AttributeType : uint
    {
        /// <summary>Unused</summary>
        Unused = 0x00,
        /// <summary>$STANDARD_INFORMATION</summary>
        StandardInformation = 0x10,
        /// <summary>$ATTRIBUTE_LIST</summary>
        AttributeList = 0x20,
        /// <summary>$FILE_NAME</summary>
        FileName = 0x30,
        /// <summary>$OBJECT_ID (NTFS 3.0+) / $VOLUME_VERSION (NTFS 1.2)</summary>
        ObjectId = 0x40,
        /// <summary>$SECURITY_DESCRIPTOR</summary>
        SecurityDescriptor = 0x50,
        /// <summary>$VOLUME_NAME</summary>
        VolumeName = 0x60,
        /// <summary>$VOLUME_INFORMATION</summary>
        VolumeInformation = 0x70,
        /// <summary>$DATA</summary>
        Data = 0x80,
        /// <summary>$INDEX_ROOT</summary>
        IndexRoot = 0x90,
        /// <summary>$INDEX_ALLOCATION</summary>
        IndexAllocation = 0xa0,
        /// <summary>$BITMAP</summary>
        Bitmap = 0xb0,
        /// <summary>$REPARSE_POINT (NTFS 3.0+) / $SYMBOLIC_LINK (NTFS 1.2)</summary>
        ReparsePoint = 0xc0,
        /// <summary>$EA_INFORMATION</summary>
        EaInformation = 0xd0,
        /// <summary>$EA</summary>
        Ea = 0xe0,
        /// <summary>$PROPERTY_SET</summary>
        PropertySet = 0xf0,
        /// <summary>$LOGGED_UTILITY_STREAM (NTFS 3.0+)</summary>
        LoggedUtilityStream = 0x100,
        /// <summary>First user-defined attribute</summary>
        FirstUserDefined = 0x1000,
        /// <summary>End marker</summary>
        End = 0xffffffff
    }

#endregion

#region Nested type: CollationRule

    /// <summary>Collation rules for sorting views and indexes (32-bit)</summary>
    enum CollationRule : uint
    {
        /// <summary>Binary compare (first byte most significant)</summary>
        Binary = 0x00,
        /// <summary>File name collation</summary>
        FileName = 0x01,
        /// <summary>Unicode string collation</summary>
        UnicodeString = 0x02,
        /// <summary>Ascending unsigned 32-bit values</summary>
        NtofsUlong = 0x10,
        /// <summary>Ascending SID values</summary>
        NtofsSid = 0x11,
        /// <summary>First by hash then by security_id</summary>
        NtofsSecurityHash = 0x12,
        /// <summary>Sequence of ascending unsigned 32-bit values</summary>
        NtofsUlongs = 0x13
    }

#endregion

#region Nested type: AttrDefFlags

    /// <summary>Attribute definition flags (32-bit)</summary>
    [Flags]
    enum AttrDefFlags : uint
    {
        /// <summary>Attribute can be indexed</summary>
        Indexable = 0x02,
        /// <summary>Attribute can be present multiple times</summary>
        Multiple = 0x04,
        /// <summary>Attribute value must contain at least one non-zero byte</summary>
        NotZero = 0x08,
        /// <summary>Attribute must be indexed and the value must be unique</summary>
        IndexedUnique = 0x10,
        /// <summary>Attribute must be named and the name must be unique</summary>
        NamedUnique = 0x20,
        /// <summary>Attribute must be resident</summary>
        Resident = 0x40,
        /// <summary>Always log modifications to this attribute</summary>
        AlwaysLog = 0x80
    }

#endregion

#region Nested type: AttributeFlags

    /// <summary>Attribute flags (16-bit)</summary>
    [Flags]
    enum AttributeFlags : ushort
    {
        /// <summary>Attribute is compressed</summary>
        Compressed = 0x0001,
        /// <summary>Compression method mask</summary>
        CompressionMask = 0x00ff,
        /// <summary>Attribute is encrypted</summary>
        Encrypted = 0x4000,
        /// <summary>Attribute is sparse</summary>
        Sparse = 0x8000
    }

#endregion

#region Nested type: ResidentAttributeFlags

    /// <summary>Resident attribute flags (8-bit)</summary>
    [Flags]
    enum ResidentAttributeFlags : byte
    {
        /// <summary>Attribute is referenced in an index</summary>
        Indexed = 0x01
    }

#endregion

#region Nested type: FileAttributeFlags

    /// <summary>File attribute flags (32-bit)</summary>
    [Flags]
    enum FileAttributeFlags : uint
    {
        /// <summary>File is read-only</summary>
        ReadOnly = 0x00000001,
        /// <summary>File is hidden</summary>
        Hidden = 0x00000002,
        /// <summary>System file</summary>
        System = 0x00000004,
        /// <summary>Directory</summary>
        Directory = 0x00000010,
        /// <summary>File needs archiving</summary>
        Archive = 0x00000020,
        /// <summary>Device file</summary>
        Device = 0x00000040,
        /// <summary>Normal file (no special attributes)</summary>
        Normal = 0x00000080,
        /// <summary>Temporary file</summary>
        Temporary = 0x00000100,
        /// <summary>Sparse file</summary>
        SparseFile = 0x00000200,
        /// <summary>Reparse point</summary>
        ReparsePoint = 0x00000400,
        /// <summary>File is compressed</summary>
        Compressed = 0x00000800,
        /// <summary>File data is offline</summary>
        Offline = 0x00001000,
        /// <summary>File is excluded from content indexing</summary>
        NotContentIndexed = 0x00002000,
        /// <summary>File is encrypted</summary>
        Encrypted = 0x00004000,
        /// <summary>Recall data on open (cloud/HSM)</summary>
        RecallOnOpen = 0x00040000,
        /// <summary>Duplicate file name index present</summary>
        DupFileNameIndexPresent = 0x10000000,
        /// <summary>Duplicate view index present</summary>
        DupViewIndexPresent = 0x20000000
    }

#endregion

#region Nested type: FileNameNamespace

    /// <summary>Possible namespaces for file names in NTFS (8-bit)</summary>
    enum FileNameNamespace : byte
    {
        /// <summary>POSIX namespace (case sensitive, most permissive)</summary>
        Posix = 0x00,
        /// <summary>Win32 namespace (case insensitive)</summary>
        Win32 = 0x01,
        /// <summary>DOS 8.3 namespace</summary>
        Dos = 0x02,
        /// <summary>Win32 and DOS names are identical</summary>
        Win32AndDos = 0x03
    }

#endregion

#region Nested type: VolumeFlags

    /// <summary>NTFS volume flags (16-bit)</summary>
    [Flags]
    enum VolumeFlags : ushort
    {
        /// <summary>Volume is dirty (needs chkdsk)</summary>
        IsDirty = 0x0001,
        /// <summary>Resize LogFile on next mount</summary>
        ResizeLogFile = 0x0002,
        /// <summary>Upgrade volume on mount</summary>
        UpgradeOnMount = 0x0004,
        /// <summary>Mounted on NT4</summary>
        MountedOnNt4 = 0x0008,
        /// <summary>USN journal deletion in progress</summary>
        DeleteUsnUnderway = 0x0010,
        /// <summary>Repair $ObjId on next mount</summary>
        RepairObjectId = 0x0020,
        /// <summary>Chkdsk is running</summary>
        ChkdskUnderway = 0x4000,
        /// <summary>Volume modified by chkdsk</summary>
        ModifiedByChkdsk = 0x8000
    }

#endregion

#region Nested type: IndexHeaderFlags

    /// <summary>Index header flags (8-bit)</summary>
    [Flags]
    enum IndexHeaderFlags : byte
    {
        /// <summary>Small index / leaf node</summary>
        SmallIndex = 0,
        /// <summary>Large index / internal node with sub-nodes</summary>
        LargeIndex = 1
    }

#endregion

#region Nested type: IndexEntryFlags

    /// <summary>Index entry flags (16-bit)</summary>
    [Flags]
    enum IndexEntryFlags : ushort
    {
        /// <summary>Entry points to a sub-node (index block VCN)</summary>
        Node = 0x0001,
        /// <summary>Last entry in index block/root</summary>
        End = 0x0002
    }

#endregion

#region Nested type: SecurityDescriptorControl

    /// <summary>Security descriptor control flags (16-bit)</summary>
    [Flags]
    enum SecurityDescriptorControl : ushort
    {
        /// <summary>Owner was defaulted</summary>
        OwnerDefaulted = 0x0001,
        /// <summary>Group was defaulted</summary>
        GroupDefaulted = 0x0002,
        /// <summary>DACL present</summary>
        DaclPresent = 0x0004,
        /// <summary>DACL was defaulted</summary>
        DaclDefaulted = 0x0008,
        /// <summary>SACL present</summary>
        SaclPresent = 0x0010,
        /// <summary>SACL was defaulted</summary>
        SaclDefaulted = 0x0020,
        /// <summary>DACL auto-inherit requested</summary>
        DaclAutoInheritReq = 0x0100,
        /// <summary>SACL auto-inherit requested</summary>
        SaclAutoInheritReq = 0x0200,
        /// <summary>DACL was auto-inherited</summary>
        DaclAutoInherited = 0x0400,
        /// <summary>SACL was auto-inherited</summary>
        SaclAutoInherited = 0x0800,
        /// <summary>DACL is protected from inheritance</summary>
        DaclProtected = 0x1000,
        /// <summary>SACL is protected from inheritance</summary>
        SaclProtected = 0x2000,
        /// <summary>RM control valid</summary>
        RmControlValid = 0x4000,
        /// <summary>Self-relative format</summary>
        SelfRelative = 0x8000
    }

#endregion

#region Nested type: AceType

    /// <summary>ACE types (8-bit)</summary>
    enum AceType : byte
    {
        /// <summary>Allow access</summary>
        AccessAllowed = 0,
        /// <summary>Deny access</summary>
        AccessDenied = 1,
        /// <summary>Audit access</summary>
        SystemAudit = 2,
        /// <summary>Alarm on access</summary>
        SystemAlarm = 3,
        /// <summary>Compound ACE (legacy)</summary>
        AccessAllowedCompound = 4,
        /// <summary>Allow with object-specific rights</summary>
        AccessAllowedObject = 5,
        /// <summary>Deny with object-specific rights</summary>
        AccessDeniedObject = 6,
        /// <summary>Audit with object-specific rights</summary>
        SystemAuditObject = 7,
        /// <summary>Alarm with object-specific rights</summary>
        SystemAlarmObject = 8
    }

#endregion

#region Nested type: AceFlags

    /// <summary>ACE inheritance and audit flags (8-bit)</summary>
    [Flags]
    enum AceFlags : byte
    {
        /// <summary>Files inherit this ACE</summary>
        ObjectInherit = 0x01,
        /// <summary>Subdirectories inherit this ACE</summary>
        ContainerInherit = 0x02,
        /// <summary>Stop inheritance after this level</summary>
        NoPropagateInherit = 0x04,
        /// <summary>Inherit only (not applied to current object)</summary>
        InheritOnly = 0x08,
        /// <summary>ACE was inherited</summary>
        Inherited = 0x10,
        /// <summary>Audit successful access</summary>
        SuccessfulAccess = 0x40,
        /// <summary>Audit failed access</summary>
        FailedAccess = 0x80
    }

#endregion

#region Nested type: AccessRights

    /// <summary>NTFS access rights masks (32-bit)</summary>
    [Flags]
    enum AccessRights : uint
    {
        /// <summary>Read file data / list directory contents</summary>
        ReadData = 0x00000001,
        /// <summary>Write file data / create file in directory</summary>
        WriteData = 0x00000002,
        /// <summary>Append data / create subdirectory</summary>
        AppendData = 0x00000004,
        /// <summary>Read extended attributes</summary>
        ReadEa = 0x00000008,
        /// <summary>Write extended attributes</summary>
        WriteEa = 0x00000010,
        /// <summary>Execute file / traverse directory</summary>
        Execute = 0x00000020,
        /// <summary>Delete children in directory</summary>
        DeleteChild = 0x00000040,
        /// <summary>Read attributes</summary>
        ReadAttributes = 0x00000080,
        /// <summary>Write attributes</summary>
        WriteAttributes = 0x00000100,
        /// <summary>Delete object</summary>
        Delete = 0x00010000,
        /// <summary>Read security descriptor / owner</summary>
        ReadControl = 0x00020000,
        /// <summary>Modify DACL</summary>
        WriteDac = 0x00040000,
        /// <summary>Change owner</summary>
        WriteOwner = 0x00080000,
        /// <summary>Synchronize</summary>
        Synchronize = 0x00100000,
        /// <summary>Access system ACL</summary>
        AccessSystemSecurity = 0x01000000,
        /// <summary>Maximum allowed access</summary>
        MaximumAllowed = 0x02000000,
        /// <summary>Full access</summary>
        GenericAll = 0x10000000,
        /// <summary>Generic execute</summary>
        GenericExecute = 0x20000000,
        /// <summary>Generic write</summary>
        GenericWrite = 0x40000000,
        /// <summary>Generic read</summary>
        GenericRead = 0x80000000
    }

#endregion

#region Nested type: ObjectAceFlags

    /// <summary>Object ACE flags (32-bit)</summary>
    [Flags]
    enum ObjectAceFlags : uint
    {
        /// <summary>Object type GUID present</summary>
        ObjectTypePresent = 1,
        /// <summary>Inherited object type GUID present</summary>
        InheritedObjectTypePresent = 2
    }

#endregion

#region Nested type: ReparseTag

    /// <summary>Reparse point tag values (32-bit)</summary>
    [Flags]
    enum ReparseTag : uint
    {
        /// <summary>Directory bit</summary>
        Directory = 0x10000000,
        /// <summary>Name surrogate bit (alias)</summary>
        IsAlias = 0x20000000,
        /// <summary>High-latency bit</summary>
        IsHighLatency = 0x40000000,
        /// <summary>Microsoft-owned tag</summary>
        IsMicrosoft = 0x80000000,
        /// <summary>CSV</summary>
        Csv = 0x80000009,
        /// <summary>Dedup</summary>
        Dedup = 0x80000013,
        /// <summary>DFS</summary>
        Dfs = 0x8000000A,
        /// <summary>DFSR</summary>
        Dfsr = 0x80000012,
        /// <summary>HSM</summary>
        Hsm = 0xC0000004,
        /// <summary>HSM2</summary>
        Hsm2 = 0x80000006,
        /// <summary>Junction / mount point</summary>
        MountPoint = 0xA0000003,
        /// <summary>NFS</summary>
        Nfs = 0x80000014,
        /// <summary>SIS</summary>
        Sis = 0x80000007,
        /// <summary>Symbolic link</summary>
        Symlink = 0xA000000C,
        /// <summary>WIM</summary>
        Wim = 0x80000008,
        /// <summary>DFM</summary>
        Dfm = 0x80000016,
        /// <summary>Windows Overlay Filter</summary>
        Wof = 0x80000017,
        /// <summary>WCI</summary>
        Wci = 0x80000018,
        /// <summary>Cloud</summary>
        Cloud = 0x9000001A,
        /// <summary>App execution link</summary>
        AppExecLink = 0x8000001B,
        /// <summary>GVFS</summary>
        Gvfs = 0x9000001C,
        /// <summary>WSL symlink</summary>
        LxSymlink = 0xA000001D,
        /// <summary>WSL AF_UNIX socket</summary>
        AfUnix = 0x80000023,
        /// <summary>WSL FIFO</summary>
        LxFifo = 0x80000024,
        /// <summary>WSL character device</summary>
        LxChr = 0x80000025,
        /// <summary>WSL block device</summary>
        LxBlk = 0x80000026
    }

#endregion

#region Nested type: QuotaFlags

    /// <summary>Quota entry flags (32-bit)</summary>
    [Flags]
    enum QuotaFlags : uint
    {
        /// <summary>Use default limits</summary>
        DefaultLimits = 0x00000001,
        /// <summary>Quota limit reached</summary>
        LimitReached = 0x00000002,
        /// <summary>Quota ID deleted</summary>
        IdDeleted = 0x00000004,
        /// <summary>Quota tracking enabled</summary>
        TrackingEnabled = 0x00000010,
        /// <summary>Quota enforcement enabled</summary>
        EnforcementEnabled = 0x00000020,
        /// <summary>Tracking requested</summary>
        TrackingRequested = 0x00000040,
        /// <summary>Log when threshold reached</summary>
        LogThreshold = 0x00000080,
        /// <summary>Log when limit reached</summary>
        LogLimit = 0x00000100,
        /// <summary>Quota data out of date</summary>
        OutOfDate = 0x00000200,
        /// <summary>Quota entry corrupt</summary>
        Corrupt = 0x00000400,
        /// <summary>Pending quota deletes</summary>
        PendingDeletes = 0x00000800
    }

#endregion

#region Nested type: EaFlags

    /// <summary>Extended attribute flags (8-bit)</summary>
    [Flags]
    enum EaFlags : byte
    {
        /// <summary>Critical EA — file cannot be properly interpreted without understanding this EA</summary>
        NeedEa = 0x80
    }

#endregion

#region Nested type: RestartFlags

    /// <summary>LogFile restart area flags (16-bit)</summary>
    [Flags]
    enum RestartFlags : ushort
    {
        /// <summary>Volume is clean</summary>
        VolumeIsClean = 0x0002
    }

#endregion
}