// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Identify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies Ray Arachelian's disk images.
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

using System.IO;
using System.Text.RegularExpressions;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class RayDim
{
#region IWritableImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        if(stream.Length < Marshal.SizeOf<Header>()) return false;

        var buffer = new byte[Marshal.SizeOf<Header>()];
        stream.Seek(0, SeekOrigin.Begin);
        stream.EnsureRead(buffer, 0, buffer.Length);

        Header header = Marshal.ByteArrayToStructureLittleEndian<Header>(buffer);

        string signature = StringHandlers.CToString(header.signature);

        AaruLogging.Debug(MODULE_NAME, "header.signature = {0}", signature);
        AaruLogging.Debug(MODULE_NAME, "header.diskType = {0}",  header.diskType);
        AaruLogging.Debug(MODULE_NAME, "header.heads = {0}",     header.heads);

        AaruLogging.Debug(MODULE_NAME, "header.cylinders = {0}", header.cylinders);

        AaruLogging.Debug(MODULE_NAME, "header.sectorsPerTrack = {0}", header.sectorsPerTrack);

        Regex sx = SignatureRegex();
        Match sm = sx.Match(signature);

        AaruLogging.Debug(MODULE_NAME, "header.signature matches? = {0}", sm.Success);

        return sm.Success;
    }

#endregion
}