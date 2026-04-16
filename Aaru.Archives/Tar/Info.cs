// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Tar plugin.
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

public sealed partial class Tar
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < BLOCK_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[BLOCK_SIZE];
        stream.ReadExactly(header, 0, BLOCK_SIZE);

        TarFormat format = DetectFormat(header);

        // Only identify by magic, not by filename or checksum-only heuristics
        return format is TarFormat.Ustar or TarFormat.Gnu or TarFormat.Star;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = string.Empty;

        if(filter.DataForkLength < BLOCK_SIZE) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[BLOCK_SIZE];
        stream.ReadExactly(header, 0, BLOCK_SIZE);

        TarFormat format = DetectFormat(header);

        if(format == TarFormat.V7) return;

        string formatName = format switch
                            {
                                TarFormat.Gnu          => "GNU tar",
                                TarFormat.Ustar        => "POSIX USTAR",
                                TarFormat.Star         => "STAR",
                                TarFormat.V7Recognized => "Unix V7 tar",
                                _                      => "tar"
                            };

        var sb = new StringBuilder();
        sb.AppendLine("[bold][blue]TAR archive:[/][/]");
        sb.AppendFormat("[slateblue1]Format:[/] [teal]{0}[/]", formatName).AppendLine();

        if(Opened)
        {
            sb.AppendFormat("[slateblue1]Number of entries:[/] [teal]{0}[/]", _entries.Count).AppendLine();

            long totalSize = _entries.Where(e => e.Type is TypeFlag.File or TypeFlag.AltFile or TypeFlag.Contiguous)
                                     .Sum(e => e.Size);

            sb.AppendFormat("[slateblue1]Total data size:[/] [teal]{0} bytes[/]", totalSize).AppendLine();
        }

        information = sb.ToString();
    }

#endregion
}