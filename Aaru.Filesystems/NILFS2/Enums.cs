// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

// ReSharper disable UnusedMember.Local

using System;

namespace Aaru.Filesystems;

public sealed partial class NILFS2
{
#region Nested type: State

    /// <summary>File system states</summary>
    [Flags]
    enum State : ushort
    {
        /// <summary>Unmounted cleanly</summary>
        Valid = 0x0001,
        /// <summary>Errors detected</summary>
        Error = 0x0002,
        /// <summary>Resize required</summary>
        Resize = 0x0004
    }

#endregion

#region Nested type: FileType

    /// <summary>NILFS2 directory file types</summary>
    enum FileType : byte
    {
        /// <summary>Unknown file type</summary>
        Unknown = 0,
        /// <summary>Regular file</summary>
        RegFile = 1,
        /// <summary>Directory</summary>
        Dir = 2,
        /// <summary>Character device</summary>
        ChrDev = 3,
        /// <summary>Block device</summary>
        BlkDev = 4,
        /// <summary>FIFO (named pipe)</summary>
        Fifo = 5,
        /// <summary>Socket</summary>
        Sock = 6,
        /// <summary>Symbolic link</summary>
        Symlink = 7
    }

#endregion

#region Nested type: CheckpointFlags

    /// <summary>Checkpoint flags</summary>
    [Flags]
    enum CheckpointFlags : uint
    {
        /// <summary>Checkpoint is a snapshot</summary>
        Snapshot = 1 << 0,
        /// <summary>Checkpoint is invalid</summary>
        Invalid = 1 << 1,
        /// <summary>Checkpoint is a sketch</summary>
        Sketch = 1 << 2,
        /// <summary>Checkpoint is minor</summary>
        Minor = 1 << 3
    }

#endregion

#region Nested type: SegmentUsageFlags

    /// <summary>Segment usage flags</summary>
    [Flags]
    enum SegmentUsageFlags : uint
    {
        /// <summary>Segment is active</summary>
        Active = 1 << 0,
        /// <summary>Segment is dirty</summary>
        Dirty = 1 << 1,
        /// <summary>Segment has errors</summary>
        Error = 1 << 2
    }

#endregion

#region Nested type: SegmentSummaryFlags

    /// <summary>Segment summary flags</summary>
    [Flags]
    enum SegmentSummaryFlags : ushort
    {
        /// <summary>Begins a logical segment</summary>
        LogBegin = 0x0001,
        /// <summary>Ends a logical segment</summary>
        LogEnd = 0x0002,
        /// <summary>Has super root</summary>
        SR = 0x0004,
        /// <summary>Includes data only updates</summary>
        SynDt = 0x0008,
        /// <summary>Segment written for cleaner operation</summary>
        GC = 0x0010
    }

#endregion
}