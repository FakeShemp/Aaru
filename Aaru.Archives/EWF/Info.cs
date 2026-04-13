// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies Expert Witness Format logical evidence files.
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
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class EwfArchive
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < SIGNATURE_LENGTH) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var signatureBytes = new byte[SIGNATURE_LENGTH];
        stream.ReadExactly(signatureBytes, 0, SIGNATURE_LENGTH);

        if(signatureBytes.AsSpan().SequenceEqual(LVF_SIGNATURE)) return true;

        if(signatureBytes.AsSpan().SequenceEqual(LEF2_SIGNATURE)) return true;

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Opened) return;

        var sb = new StringBuilder();
        sb.AppendLine("[bold][blue]EWF Logical Evidence File:[/][/]");
        sb.AppendFormat("[slateblue1]Format:[/] [green]{0}[/]", _isV2 ? "EWF2 (Lx01)" : "EWF (L01)").AppendLine();
        sb.AppendFormat("[slateblue1]Files:[/] [teal]{0}[/]",   _entries?.Count ?? 0).AppendLine();

        information = sb.ToString();
    }

#endregion
}