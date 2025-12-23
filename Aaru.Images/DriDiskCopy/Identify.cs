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
//     Identifies Digital Research's DISKCOPY disk images.
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

using System.IO;
using System.Text.RegularExpressions;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class DriDiskCopy
{
#region IWritableImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        if((stream.Length - Marshal.SizeOf<Footer>()) % 512 != 0) return false;

        var buffer = new byte[Marshal.SizeOf<Footer>()];
        stream.Seek(-buffer.Length, SeekOrigin.End);
        stream.EnsureRead(buffer, 0, buffer.Length);

        Footer tmpFooter = Marshal.ByteArrayToStructureLittleEndian<Footer>(buffer);

        string sig = StringHandlers.CToString(tmpFooter.signature);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.signature = \"{0}\"", sig);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.five = {0}",      tmpFooter.bpb.five);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb._driveCode = {0}", tmpFooter.bpb._driveCode);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.unknown = {0}", tmpFooter.bpb.unknown);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.cylinders = {0}", tmpFooter.bpb.cylinders);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.unknown2 = {0}", tmpFooter.bpb.unknown2);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.bps = {0}",      tmpFooter.bpb.bps);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.spc = {0}",      tmpFooter.bpb.spc);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.rsectors = {0}", tmpFooter.bpb.rsectors);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.fats_no = {0}",  tmpFooter.bpb.fats_no);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.sectors = {0}",  tmpFooter.bpb.sectors);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.media_descriptor = {0}", tmpFooter.bpb.media_descriptor);

        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.spfat = {0}",    tmpFooter.bpb.spfat);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.sptrack = {0}",  tmpFooter.bpb.sptrack);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.heads = {0}",    tmpFooter.bpb.heads);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.hsectors = {0}", tmpFooter.bpb.hsectors);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.drive_no = {0}", tmpFooter.bpb.drive_no);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.unknown3 = {0}", tmpFooter.bpb.unknown3);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.unknown4 = {0}", tmpFooter.bpb.unknown4);
        AaruLogging.Debug(MODULE_NAME, "tmp_footer.bpb.sptrack2 = {0}", tmpFooter.bpb.sptrack2);

        AaruLogging.Debug(MODULE_NAME,
                          "ArrayHelpers.ArrayIsNullOrEmpty(tmp_footer.bpb.unknown5) = {0}",
                          ArrayHelpers.ArrayIsNullOrEmpty(tmpFooter.bpb.unknown5));

        Regex regexSignature = DriRegex();
        Match matchSignature = regexSignature.Match(sig);

        AaruLogging.Debug(MODULE_NAME, "MatchSignature.Success? = {0}", matchSignature.Success);

        if(!matchSignature.Success) return false;

        if(tmpFooter.bpb.sptrack * tmpFooter.bpb.cylinders * tmpFooter.bpb.heads != tmpFooter.bpb.sectors) return false;

        return tmpFooter.bpb.sectors * tmpFooter.bpb.bps + Marshal.SizeOf<Footer>() == stream.Length;
    }

#endregion
}