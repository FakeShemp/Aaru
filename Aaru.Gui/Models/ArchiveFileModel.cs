// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ArchiveFileModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about a file entry inside an archive.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.CommonTypes.Structs;
using Avalonia.Media;

namespace Aaru.Gui.Models;

public sealed class ArchiveFileModel
{
    public int    EntryNumber { get; set; }
    public string Name        { get; set; }

    public string Size => $"{Stat?.Length ?? 0}";

    public string CreationTime =>
        Stat is null || Stat.CreationTime == default(DateTime) ? "" : $"{Stat.CreationTime:G}";

    public string LastAccessTime => Stat is null || Stat.AccessTime == default(DateTime) ? "" : $"{Stat.AccessTime:G}";

    public string ChangedTime => Stat is null || Stat.StatusChangeTime == default(DateTime)
                                     ? ""
                                     : $"{Stat.StatusChangeTime:G}";

    public string LastBackupTime => Stat is null || Stat.BackupTime == default(DateTime) ? "" : $"{Stat.BackupTime:G}";

    public string LastWriteTime => Stat is null || Stat.LastWriteTime == default(DateTime)
                                       ? ""
                                       : $"{Stat.LastWriteTime:G}";

    public string Attributes => Stat is null ? "" : $"{Stat.Attributes}";

    public string Gid => Stat is null ? "" : $"{Stat.GID}";

    public string Uid => Stat is null ? "" : $"{Stat.UID}";

    public string Inode => Stat is null ? "" : $"{Stat.Inode}";

    public string Links => Stat is null ? "" : $"{Stat.Links}";

    public string Mode => Stat is null ? "" : $"{Stat.Mode}";

    public FileEntryInfo Stat  { get; set; }
    public IBrush        Color { get; set; }
}