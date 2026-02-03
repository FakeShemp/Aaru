// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class BOFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = [];

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.NoError;

        string trimmedPath = path.TrimStart('/');
        int    lastSlash   = trimmedPath.LastIndexOf('/');
        string fileName;

        if(lastSlash < 0)
        {
            // Root level file
            fileName = trimmedPath;

            lock(_rootDirectoryCache)
            {
                if(!_rootDirectoryCache.TryGetValue(fileName, out FileEntry entry)) return ErrorNumber.NoSuchFile;

                if(entry.FileType != 0 && entry.FileType != -1)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ListXAttr: Adding xattr for {0}, FileType=0x{1:X8}",
                                      path,
                                      entry.FileType);

                    xattrs.Add("com.be.filetype");
                }
            }
        }
        else
        {
            // Subdirectory file - use LookupEntry
            try
            {
                ErrorNumber lookupErr = LookupEntry(path, out FileEntry entry);

                AaruLogging.Debug(MODULE_NAME,
                                  "ListXAttr subdir: path={0}, lookupErr={1}, FileType=0x{2:X8}",
                                  path,
                                  lookupErr,
                                  entry.FileType);

                if(lookupErr != ErrorNumber.NoError) return ErrorNumber.NoSuchFile;

                if(entry.FileType != 0 && entry.FileType != -1)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ListXAttr: Adding xattr for {0}, FileType=0x{1:X8}",
                                      path,
                                      entry.FileType);

                    xattrs.Add("com.be.filetype");
                }
            }
            catch(Exception ex)
            {
                AaruLogging.Debug(MODULE_NAME, "ListXAttr subdir exception for {0}: {1}", path, ex);

                return ErrorNumber.NoSuchFile;
            }
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(string.IsNullOrEmpty(path) || path == "/" || xattr != "com.be.filetype")
            return ErrorNumber.NoSuchExtendedAttribute;

        string trimmedPath = path.TrimStart('/');
        int    lastSlash   = trimmedPath.LastIndexOf('/');
        string fileName;

        if(lastSlash < 0)
        {
            // Root level file
            fileName = trimmedPath;

            lock(_rootDirectoryCache)
            {
                if(!_rootDirectoryCache.TryGetValue(fileName, out FileEntry entry)) return ErrorNumber.NoSuchFile;

                if(entry.FileType == 0 || entry.FileType == -1) return ErrorNumber.NoSuchExtendedAttribute;

                byte[] fileTypeBytes = BitConverter.GetBytes(entry.FileType);

                if(buf == null || buf.Length < fileTypeBytes.Length) buf = new byte[fileTypeBytes.Length];

                Array.Copy(fileTypeBytes, buf, fileTypeBytes.Length);

                return ErrorNumber.NoError;
            }
        }

        // Subdirectory file - use LookupEntry
        try
        {
            ErrorNumber lookupErr = LookupEntry(path, out FileEntry entry);

            AaruLogging.Debug(MODULE_NAME,
                              "GetXattr subdir: path={0}, lookupErr={1}, FileType=0x{2:X8}",
                              path,
                              lookupErr,
                              entry.FileType);

            if(lookupErr != ErrorNumber.NoError) return ErrorNumber.NoSuchFile;

            if(entry.FileType == 0 || entry.FileType == -1) return ErrorNumber.NoSuchExtendedAttribute;

            byte[] fileTypeBytes = BitConverter.GetBytes(entry.FileType);

            if(buf == null || buf.Length < fileTypeBytes.Length) buf = new byte[fileTypeBytes.Length];

            Array.Copy(fileTypeBytes, buf, fileTypeBytes.Length);

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "GetXattr subdir exception for {0}: {1}", path, ex);

            return ErrorNumber.NoSuchFile;
        }
    }
}