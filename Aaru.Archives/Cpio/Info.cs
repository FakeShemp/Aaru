// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CPIO plugin.
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
using System.Linq;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Cpio
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(header, 0, MIN_HEADER_SIZE);

        // Check ASCII magics (odc, newc, newcrc)
        if(header[0] == '0' && header[1] == '7' && header[2] == '0' && header[3] == '7')
        {
            if(header[4] == '0' && header[5] == '7') return true;
            if(header[4] == '0' && header[5] == '1') return true;
            if(header[4] == '0' && header[5] == '2') return true;
        }

        // Check binary magics (BE and LE)
        if(header[0] == 0x71 && header[1] == 0xC7) return true;
        if(header[0] == 0xC7 && header[1] == 0x71) return true;

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = string.Empty;

        if(filter.DataForkLength < MIN_HEADER_SIZE) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(header, 0, MIN_HEADER_SIZE);

        string formatName = null;

        if(header[0] == '0' && header[1] == '7' && header[2] == '0' && header[3] == '7')
        {
            if(header[4] == '0' && header[5] == '7')
                formatName = "Old character (odc)";
            else if(header[4] == '0' && header[5] == '1')
                formatName                                           = "New ASCII (newc)";
            else if(header[4] == '0' && header[5] == '2') formatName = "New CRC (newcrc)";
        }
        else if(header[0] == 0x71 && header[1] == 0xC7)
            formatName                                             = "Old binary (big-endian)";
        else if(header[0] == 0xC7 && header[1] == 0x71) formatName = "Old binary (little-endian)";

        if(formatName is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("[bold][blue]CPIO archive:[/][/]");
        sb.AppendFormat("[slateblue1]Format:[/] [teal]{0}[/]", formatName).AppendLine();

        if(Opened)
        {
            sb.AppendFormat("[slateblue1]Number of entries:[/] [teal]{0}[/]", _entries.Count).AppendLine();

            long totalSize = _entries.Where(e => e.FileType == CpioFileType.Regular).Sum(e => e.Size);

            sb.AppendFormat("[slateblue1]Total data size:[/] [teal]{0} bytes[/]", totalSize).AppendLine();
        }

        information = sb.ToString();
    }

#endregion
}