// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : TitleKeys.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
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
// Copyright © 2020-2026 Rebecca Wallander
// ****************************************************************************/

using System.Collections.Generic;
using System.Linq;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    void InitializeMissingTitleKeysCache()
    {
        if(_resume?.MissingTitleKeys is null)
        {
            _missingTitleKeysLookup = null;
            _missingTitleKeysDirty  = false;

            return;
        }

        _missingTitleKeysLookup = [.._resume.MissingTitleKeys];
        _missingTitleKeysDirty  = false;
    }

    bool ContainsMissingTitleKey(ulong sector)
    {
        if(_resume?.MissingTitleKeys is null) return false;

        _missingTitleKeysLookup ??= [.._resume.MissingTitleKeys];

        return _missingTitleKeysLookup.Contains(sector);
    }

    bool MarkTitleKeyDumped(ulong sector)
    {
        if(_resume?.MissingTitleKeys is null) return false;

        _missingTitleKeysLookup ??= [.._resume.MissingTitleKeys];

        bool removed = _missingTitleKeysLookup.Remove(sector);

        if(removed) _missingTitleKeysDirty = true;

        return removed;
    }

    int MissingTitleKeyCount()
    {
        if(_resume?.MissingTitleKeys is null) return 0;

        _missingTitleKeysLookup ??= [.._resume.MissingTitleKeys];

        return _missingTitleKeysLookup.Count;
    }

    ulong[] MissingTitleKeysSnapshot(bool forward)
    {
        if(_resume?.MissingTitleKeys is null) return [];

        _missingTitleKeysLookup ??= [.._resume.MissingTitleKeys];

        return forward
                   ? [.._missingTitleKeysLookup.OrderBy(static k => k)]
                   : [.._missingTitleKeysLookup.OrderByDescending(static k => k)];
    }

    void SyncMissingTitleKeysToResume()
    {
        if(_resume?.MissingTitleKeys is null ||
           _missingTitleKeysLookup is null   ||
           !_missingTitleKeysDirty)
            return;

        _resume.MissingTitleKeys = [.._missingTitleKeysLookup.OrderBy(static k => k)];
        _missingTitleKeysDirty   = false;
    }
}
