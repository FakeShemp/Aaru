// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SwapPdpEndianAttribute.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Common Types.
//
// --[ Description ] ----------------------------------------------------------
//
//     Attribute to mark structs for compile-time PDP endianness swapping.
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

namespace Aaru.CommonTypes.Attributes;

/// <summary>
///     Marks a struct for automatic generation of PDP endianness swapping code.
///     PDP endianness swaps the two 16-bit words in 32-bit integers, but leaves other types unchanged.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class SwapPdpEndianAttribute : Attribute {}