// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles extended attributes
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetEntry(path, out DirectoryEntryInfo entry);

        if(err != ErrorNumber.NoError) return err;

        xattrs = [];

        // RISC OS 12-bit filetype is stored in load_address[19:8] when bits[31:20] == 0xFFF
        if(HasFiletype(entry.LoadAddr)) xattrs.Add(Xattrs.XATTR_ACORN_RISCOS_FILETYPE);

        // Always expose full 32-bit attributes for ADFS-G/big directories, or 8-bit for standard
        xattrs.Add(Xattrs.XATTR_ACORN_RISCOS_ATTRIBUTES);

        // Expose load/exec addresses for unstamped files (pre-RISC OS or raw binary)
        // These contain the actual load and execution addresses rather than filetype/timestamp
        if(!HasFiletype(entry.LoadAddr))
        {
            xattrs.Add(Xattrs.XATTR_ACORN_RISCOS_LOAD_ADDR);
            xattrs.Add(Xattrs.XATTR_ACORN_RISCOS_EXEC_ADDR);
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        buf = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetEntry(path, out DirectoryEntryInfo entry);

        if(err != ErrorNumber.NoError) return err;

        switch(xattr)
        {
            case Xattrs.XATTR_ACORN_RISCOS_FILETYPE:
                if(!HasFiletype(entry.LoadAddr)) return ErrorNumber.NoSuchExtendedAttribute;

                ushort filetype = GetFiletype(entry.LoadAddr);
                buf = BitConverter.GetBytes(filetype);

                return ErrorNumber.NoError;

            case Xattrs.XATTR_ACORN_RISCOS_ATTRIBUTES:
                // Return full 32-bit attributes for ADFS-G, or 8-bit zero-extended for standard
                buf = BitConverter.GetBytes(entry.Attributes);

                return ErrorNumber.NoError;

            case Xattrs.XATTR_ACORN_RISCOS_LOAD_ADDR:
                if(HasFiletype(entry.LoadAddr)) return ErrorNumber.NoSuchExtendedAttribute;

                buf = BitConverter.GetBytes(entry.LoadAddr);

                return ErrorNumber.NoError;

            case Xattrs.XATTR_ACORN_RISCOS_EXEC_ADDR:
                if(HasFiletype(entry.LoadAddr)) return ErrorNumber.NoSuchExtendedAttribute;

                buf = BitConverter.GetBytes(entry.ExecAddr);

                return ErrorNumber.NoError;

            default:
                return ErrorNumber.NoSuchExtendedAttribute;
        }
    }

    /// <summary>Checks if the load address contains a valid RISC OS filetype</summary>
    /// <param name="loadAddr">Load address</param>
    /// <returns>True if upper 12 bits are 0xFFF indicating a valid filetype</returns>
    static bool HasFiletype(uint loadAddr) => (loadAddr & 0xFFF00000) == 0xFFF00000;

    /// <summary>Extracts the 12-bit RISC OS filetype from a load address</summary>
    /// <param name="loadAddr">Load address</param>
    /// <returns>12-bit filetype value</returns>
    static ushort GetFiletype(uint loadAddr) => (ushort)(loadAddr >> 8 & 0xFFF);
}