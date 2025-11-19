// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about a device.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

namespace Aaru.Gui.Models;

public class DeviceModel
{
    /// <summary>Device path</summary>
    public string Path { get; set; }
    /// <summary>Device vendor or manufacturer</summary>
    public string Vendor { get; set; }
    /// <summary>Device model or product name</summary>
    public string Model { get; set; }
    /// <summary>Device serial number</summary>
    public string Serial { get; set; }
    /// <summary>Bus the device is attached to</summary>
    public string Bus { get; set; }
    /// <summary>
    ///     Set to <c>true</c> if Aaru can send commands to the device in the current machine or remote, <c>false</c>
    ///     otherwise
    /// </summary>
    public bool Supported { get; set; }
}