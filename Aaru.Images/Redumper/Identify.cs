// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Identify.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies Redumper raw DVD dump images.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Images;

public sealed partial class Redumper
{
#region IOpticalMediaImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        string filename = imageFilter.Filename;

        if(string.IsNullOrEmpty(filename)) return false;

        string extension = Path.GetExtension(filename)?.ToLower();

        if(extension != ".state") return false;

        string basePath = filename[..^".state".Length];
        string sdramPath = basePath + ".sdram";

        if(!File.Exists(sdramPath)) return false;

        long stateLength = imageFilter.DataForkLength;
        long sdramLength = new FileInfo(sdramPath).Length;

        if(sdramLength == 0 || stateLength == 0) return false;

        if(sdramLength % RECORDING_FRAME_SIZE != 0) return false;

        long frameCount = sdramLength / RECORDING_FRAME_SIZE;

        return stateLength == frameCount;
    }

#endregion
}