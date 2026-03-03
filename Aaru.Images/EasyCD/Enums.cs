// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains enumerations for Easy CD Creator disc images.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class EasyCD
{
#region Nested type: CifImageType

    /// <summary>CIF disc image type</summary>
    enum CifImageType : ushort
    {
        /// <summary>Data-only disc</summary>
        Data = 0x01,
        /// <summary>Mixed mode disc (data and audio)</summary>
        Mixed = 0x02,
        /// <summary>Audio-only disc</summary>
        Music = 0x03,
        /// <summary>Enhanced CD (CD Extra / CD Plus)</summary>
        Enhanced = 0x04,
        /// <summary>Video CD</summary>
        Video = 0x05,
        /// <summary>Bootable disc</summary>
        Bootable = 0x06,
        /// <summary>MP3 disc</summary>
        Mp3 = 0x07
    }

#endregion

#region Nested type: CifSessionType

    /// <summary>CIF session type</summary>
    enum CifSessionType : ushort
    {
        /// <summary>CD-DA (Digital Audio) session</summary>
        CdDa = 0x00,
        /// <summary>CD-ROM session</summary>
        CdRom = 0x01,
        /// <summary>CD-ROM XA session</summary>
        CdRomXa = 0x03
    }

#endregion

#region Nested type: CifTrackType

    /// <summary>CIF track type</summary>
    enum CifTrackType : ushort
    {
        /// <summary>Audio track (2352 bytes/sector)</summary>
        Audio = 0x00,
        /// <summary>Mode 1 data track (2048 bytes/sector)</summary>
        Mode1 = 0x01,
        /// <summary>Mode 2 Form 1 data track (2056 bytes/sector)</summary>
        Mode2Form1 = 0x02,
        /// <summary>Mode 2 Mixed data track (2332 bytes/sector)</summary>
        Mode2Mixed = 0x04
    }

#endregion

#region Nested type: CifDaoMode

    /// <summary>CIF recording mode</summary>
    enum CifDaoMode : ushort
    {
        /// <summary>Track-at-once recording</summary>
        Tao = 0x00,
        /// <summary>Disc-at-once recording</summary>
        Dao = 0x04
    }

#endregion
}