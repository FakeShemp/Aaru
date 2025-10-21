// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HP Logical Interchange Format plugin
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

// Information from http://www.hp9845.net/9845/projects/hpdir/#lif_filesystem
/// <inheritdoc />
/// <summary>Implements detection of the LIF filesystem</summary>
public sealed partial class LIF
{
#region Nested type: SystemBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SystemBlock
    {
        public ushort magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] volumeLabel;
        public uint   directoryStart;
        public ushort lifId;
        public ushort unused;
        public uint   directorySize;
        public ushort lifVersion;
        public ushort unused2;
        public uint   tracks;
        public uint   heads;
        public uint   sectors;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] creationDate;
    }

#endregion
}