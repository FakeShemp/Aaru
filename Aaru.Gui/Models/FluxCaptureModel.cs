// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FluxCaptureModel.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about flux captures.
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
// Copyright © 2011-2025 Rebecca Wallander
// ****************************************************************************/

namespace Aaru.Gui.Models;

public sealed class FluxCaptureModel
{
    public uint   Head            { get; set; }
    public ushort Track           { get; set; }
    public byte   SubTrack        { get; set; }
    public uint   CaptureIndex    { get; set; }
    public ulong  IndexResolution { get; set; }
    public ulong  DataResolution  { get; set; }
}

