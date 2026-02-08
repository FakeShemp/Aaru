// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of Acorn's Advanced Data Filing System (ADFS)</summary>
public sealed partial class AcornADFS
{
#region Disc record constants

    /// <summary>Size of disc record structure</summary>
    const int DISC_RECORD_SIZE = 60;

#endregion

#region Filesystem type identifier

    const string FS_TYPE = "adfs";

#endregion

#region Maximum values

    /// <summary>Maximum overall name length including ,xyz filetype suffix</summary>
    const int MAX_NAME_LEN = 256 + 4;

#endregion

#region Boot block constants

    /// <summary>Location for boot block, in bytes</summary>
    const ulong BOOT_BLOCK_LOCATION = 0xC00;

    /// <summary>Size of boot block, in bytes</summary>
    const uint BOOT_BLOCK_SIZE = 0x200;

    /// <summary>Offset of disc record within boot block</summary>
    const int BOOT_BLOCK_DISC_RECORD_OFFSET = 0x1C0;

    /// <summary>Location of disc record in bytes (ADFS_DISCRECORD in Linux)</summary>
    const ulong DISC_RECORD_LOCATION = 0xC00 + 0x1C0;

    /// <summary>Offset of disc record from zone check/free link header</summary>
    const int DISC_RECORD_OFFSET = 4;

#endregion

#region Directory location constants

    /// <summary>Location of new directory, in bytes</summary>
    const ulong NEW_DIRECTORY_LOCATION = 0x400;

    /// <summary>Location of old directory, in bytes</summary>
    const ulong OLD_DIRECTORY_LOCATION = 0x200;

#endregion

#region Directory size constants

    /// <summary>Size of old directory (1280 bytes)</summary>
    const uint OLD_DIRECTORY_SIZE = 1280;

    /// <summary>Size of new directory (F format, 2048 bytes)</summary>
    const uint NEW_DIRECTORY_SIZE = 2048;

    /// <summary>Maximum number of entries in old directory</summary>
    const int OLD_DIR_MAX_ENTRIES = 47;

    /// <summary>Maximum number of entries in new directory (F format)</summary>
    const int NEW_DIR_MAX_ENTRIES = 77;

    /// <summary>Size of a directory entry in F format</summary>
    const int DIR_ENTRY_SIZE = 26;

    /// <summary>Maximum filename length in F format directories</summary>
    const int F_NAME_LEN = 10;

    /// <summary>Maximum filename length in F+ format directories</summary>
    const int FPLUS_NAME_LEN = 255;

#endregion

#region Directory magic numbers

    /// <summary>New directory format magic number, "Nick"</summary>
    const uint NEW_DIR_MAGIC = 0x6B63694E;

    /// <summary>Old directory format magic number, "Hugo"</summary>
    const uint OLD_DIR_MAGIC = 0x6F677548;

    /// <summary>Big directory (F+ format) start name magic, "SBPr"</summary>
    const uint BIG_DIR_START_NAME = 0x72504253;

    /// <summary>Big directory (F+ format) end name magic, "oven"</summary>
    const uint BIG_DIR_END_NAME = 0x6E65766F;

#endregion

#region Filetype constants

    /// <summary>No filetype assigned (pre-RISC OS or data file)</summary>
    const ushort FILETYPE_NONE = 0xFFFF;

    /// <summary>Filetype for LinkFS symbolic links</summary>
    const ushort FILETYPE_LINKFS = 0xFC0;

    /// <summary>Filetype for Unix executable files</summary>
    const ushort FILETYPE_UNIXEXEC = 0xFE6;

    /// <summary>Filetype for text files</summary>
    const ushort FILETYPE_TEXT = 0xFFF;

    /// <summary>Filetype for data files</summary>
    const ushort FILETYPE_DATA = 0xFFD;

    /// <summary>Filetype for BASIC programs</summary>
    const ushort FILETYPE_BASIC = 0xFFB;

    /// <summary>Filetype for modules</summary>
    const ushort FILETYPE_MODULE = 0xFFA;

#endregion
}