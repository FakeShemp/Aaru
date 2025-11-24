// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ls.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using Aaru.CommonTypes.Structs;

namespace Aaru.Core;

public static class FileAttributesExtensions
{
    /// <summary>
    ///     Returns a 19-character string representation of the attributes
    /// </summary>
    public static string ToAttributeChars(this FileAttributes flags)
    {
        var attr                                     = new char[19];
        for(var i = 0; i < attr.Length; i++) attr[i] = '.';

        (FileAttributes Flag, int Slot, char Symbol)[] attrs =
        [
            (FileAttributes.AppendOnly, 1, 'a'), (FileAttributes.Alias, 0, 'l'), (FileAttributes.Archive, 1, 'A'),
            (FileAttributes.BlockDevice, 0, 'b'), (FileAttributes.Bundle, 0, 'B'),
            (FileAttributes.CharDevice, 0, 'c'), (FileAttributes.Compressed, 5, 'z'),
            (FileAttributes.Device, 0, 'v'), (FileAttributes.Directory, 0, 'd'), (FileAttributes.Encrypted, 6, 'e'),
            (FileAttributes.Extents, 7, 'e'), (FileAttributes.FIFO, 0, 'F'), (FileAttributes.File, 0, 'f'),
            (FileAttributes.HasBeenInited, 8, 'i'), (FileAttributes.HasCustomIcon, 9, 'c'),
            (FileAttributes.HasNoINITs, 8, 'n'), (FileAttributes.Hidden, 4, 'H'),
            (FileAttributes.Immutable, 2, 'i'), (FileAttributes.IndexedDirectory, 1, 'i'),
            (FileAttributes.Inline, 11, 'i'), (FileAttributes.IntegrityStream, 12, 'i'),
            (FileAttributes.IsOnDesk, 13, 'd'), (FileAttributes.Journaled, 14, 'j'),
            (FileAttributes.NoAccessTime, 15, 'a'), (FileAttributes.NoCopyOnWrite, 16, 'w'),
            (FileAttributes.NoDump, 1, 'd'), (FileAttributes.Password, 17, 'p'), (FileAttributes.ReadOnly, 2, 'R'),
            (FileAttributes.ReparsePoint, 0, 'r'), (FileAttributes.Sparse, 18, 's'),
            (FileAttributes.Shadow, 0, 'l'), (FileAttributes.Stationery, 0, 't'), (FileAttributes.Symlink, 0, 'l'),
            (FileAttributes.System, 3, 'S'), (FileAttributes.TopDirectory, 0, 'T'),
            (FileAttributes.Undeletable, 2, 'u'), (FileAttributes.Pipe, 0, 'p'), (FileAttributes.Socket, 0, 's'),
            (FileAttributes.Deleted, 2, 'd'), (FileAttributes.Executable, 10, 'x')
        ];

        // 4) Post-process extras: only overwrite slots still ‘.’
        foreach((FileAttributes flag, int slot, char symbol) in attrs)
        {
            if(slot >= 0 && slot < attr.Length && attr[slot] == '.' && flags.HasFlag(flag)) attr[slot] = symbol;
        }

        return new string(attr);
    }
}