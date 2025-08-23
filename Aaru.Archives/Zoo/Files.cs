// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Files.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Zoo plugin.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class Zoo
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName)
    {
        fileName = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _files.Count) return ErrorNumber.OutOfRange;

        Direntry entry = _files[entryNumber];

        fileName = StringHandlers.CToString(entry.lfname ?? entry.fname, _encoding);

        if(entry.dirname is null) return ErrorNumber.NoError;

        string directoryName = StringHandlers.CToString(entry.dirname, _encoding);

        // Path separators are UNIX in archive, change them
        if(entry.system_id != 1 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            directoryName = directoryName.Replace('/', '\\');
        else
            directoryName = directoryName.Replace('\\', '/');

        fileName = Path.Combine(directoryName, fileName);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetCompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _files.Count) return ErrorNumber.OutOfRange;

        Direntry entry = _files[entryNumber];

        length = entry.size_now;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _files.Count) return ErrorNumber.OutOfRange;

        Direntry entry = _files[entryNumber];

        length = entry.org_size;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetAttributes(int entryNumber, out FileAttributes attributes)
    {
        // TODO: Decode them
        attributes = FileAttributes.None;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _files.Count) return ErrorNumber.OutOfRange;

        return ErrorNumber.NoError;
    }

#endregion
}