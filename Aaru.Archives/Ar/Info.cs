// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Ar plugin.
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
using System.IO;
using System.Linq;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Ar
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MAGIC_LENGTH) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var magic = new byte[MAGIC_LENGTH];
        stream.ReadExactly(magic, 0, MAGIC_LENGTH);

        return magic.AsSpan().SequenceEqual(MAGIC);
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = string.Empty;

        if(!Identify(filter)) return;

        var sb = new StringBuilder();
        sb.AppendLine("[bold][blue]AR archive:[/][/]");

        if(Opened)
        {
            sb.AppendFormat("[slateblue1]Number of entries:[/] [teal]{0}[/]", _entries.Count).AppendLine();

            long totalSize = _entries.Sum(e => e.Size);

            sb.AppendFormat("[slateblue1]Total data size:[/] [teal]{0} bytes[/]", totalSize).AppendLine();
        }

        information = sb.ToString();
    }

#endregion
}