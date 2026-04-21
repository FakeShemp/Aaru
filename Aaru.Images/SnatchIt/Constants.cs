// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for Software Pirates SNATCH-IT disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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

namespace Aaru.Images;

public sealed partial class SnatchIt
{
    const string MODULE_NAME = "SNATCH-IT plugin";

    /// <summary>Image signature string.</summary>
    const string SOFTWARE_PIRATES = "SOFTWARE PIRATES";
    /// <summary>Version string prefix.</summary>
    const string RELEASE_PREFIX = "Release";

    /// <summary>Upper bound on the number of cylinders represented per side.</summary>
    const int NCYL_MAX = 84;
    /// <summary>Maximum sectors per track supported by the CP2 format.</summary>
    const int CP2_MAX_SPT = 24;
    /// <summary>Magic data offset of the first sector in each segment.</summary>
    const ushort CP2_SEC_OFS_MAGIC = 0x16AD;
    /// <summary>Trailer byte value marking the last segment.</summary>
    const byte CP2_TRAILER_LAST = 0xFF;
    /// <summary>uPD765 ST2 control-mark bit (deleted Data Address Mark).</summary>
    const byte CP2_ST2_DELETED_DAM = 0x40;

    /// <summary>Placeholder text returned for an entirely missing track.</summary>
    const string CP2_TRACK_MISSING_TEXT = "CP2 track missing!";
    /// <summary>Placeholder text returned for an individual missing sector.</summary>
    const string CP2_SECTOR_MISSING_TEXT = "CP2 sector missing!";
}