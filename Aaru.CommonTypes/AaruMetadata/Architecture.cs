// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Architecture.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Metadata.
//
// --[ Description ] ----------------------------------------------------------
//
//     Defines format for metadata.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Text.Json.Serialization;
using Aaru.Localization;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Aaru.CommonTypes.AaruMetadata;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum Architecture
{
    [JsonPropertyName("4004")]
    [LocalizedDescription(nameof(UI.Architecture_4004))]
    _4004,
    [JsonPropertyName("4040")]
    [LocalizedDescription(nameof(UI.Architecture_4040))]
    _4040,
    [JsonPropertyName("6502")]
    [LocalizedDescription(nameof(UI.Architecture_6502))]
    _6502,
    [JsonPropertyName("65816")]
    [LocalizedDescription(nameof(UI.Architecture_65816))]
    _65816,
    [JsonPropertyName("8008")]
    [LocalizedDescription(nameof(UI.Architecture_8008))]
    _8008,
    [JsonPropertyName("8051")]
    [LocalizedDescription(nameof(UI.Architecture_8051))]
    _8051,
    [JsonPropertyName("8080")]
    [LocalizedDescription(nameof(UI.Architecture_8080))]
    _8080,
    [JsonPropertyName("8085")]
    [LocalizedDescription(nameof(UI.Architecture_8085))]
    _8085,
    [LocalizedDescription(nameof(UI.Architecture_Aarch64))]
    Aarch64,
    [LocalizedDescription(nameof(UI.Architecture_Am29000))]
    Am29000,
    [LocalizedDescription(nameof(UI.Architecture_Amd64))]
    Amd64,
    [LocalizedDescription(nameof(UI.Architecture_Apx432))]
    Apx432,
    [LocalizedDescription(nameof(UI.Architecture_Arm))]
    Arm,
    [LocalizedDescription(nameof(UI.Architecture_Avr))]
    Avr,
    [LocalizedDescription(nameof(UI.Architecture_Avr32))]
    Avr32,
    [LocalizedDescription(nameof(UI.Architecture_Axp))]
    Axp,
    [LocalizedDescription(nameof(UI.Architecture_Clipper))]
    Clipper,
    [LocalizedDescription(nameof(UI.Architecture_Cray))]
    Cray,
    [LocalizedDescription(nameof(UI.Architecture_Esa390))]
    Esa390,
    [LocalizedDescription(nameof(UI.Architecture_Hobbit))]
    Hobbit,
    [LocalizedDescription(nameof(UI.Architecture_I86))]
    I86,
    [LocalizedDescription(nameof(UI.Architecture_I860))]
    I860,
    [LocalizedDescription(nameof(UI.Architecture_I960))]
    I960,
    [LocalizedDescription(nameof(UI.Architecture_Ia32))]
    Ia32,
    [LocalizedDescription(nameof(UI.Architecture_Ia64))]
    Ia64,
    [LocalizedDescription(nameof(UI.Architecture_M56K))]
    M56K,
    [LocalizedDescription(nameof(UI.Architecture_M6800))]
    M6800,
    [LocalizedDescription(nameof(UI.Architecture_M6801))]
    M6801,
    [LocalizedDescription(nameof(UI.Architecture_M6805))]
    M6805,
    [LocalizedDescription(nameof(UI.Architecture_M6809))]
    M6809,
    [LocalizedDescription(nameof(UI.Architecture_M68K))]
    M68K,
    [LocalizedDescription(nameof(UI.Architecture_M88K))]
    M88K,
    [LocalizedDescription(nameof(UI.Architecture_Mcs41))]
    Mcs41,
    [LocalizedDescription(nameof(UI.Architecture_Mcs48))]
    Mcs48,
    [LocalizedDescription(nameof(UI.Architecture_Mips32))]
    Mips32,
    [LocalizedDescription(nameof(UI.Architecture_Mips64))]
    Mips64,
    [LocalizedDescription(nameof(UI.Architecture_Msp430))]
    Msp430,
    [LocalizedDescription(nameof(UI.Architecture_Nios2))]
    Nios2,
    [LocalizedDescription(nameof(UI.Architecture_Openrisc))]
    Openrisc,
    [LocalizedDescription(nameof(UI.Architecture_Parisc))]
    Parisc,
    [LocalizedDescription(nameof(UI.Architecture_PDP1))]
    PDP1,
    [LocalizedDescription(nameof(UI.Architecture_PDP10))]
    PDP10,
    [LocalizedDescription(nameof(UI.Architecture_PDP11))]
    PDP11,
    [LocalizedDescription(nameof(UI.Architecture_PDP7))]
    PDP7,
    [LocalizedDescription(nameof(UI.Architecture_PDP8))]
    PDP8,
    [LocalizedDescription(nameof(UI.Architecture_Pic))]
    Pic,
    [LocalizedDescription(nameof(UI.Architecture_Power))]
    Power,
    [LocalizedDescription(nameof(UI.Architecture_Ppc))]
    Ppc,
    [LocalizedDescription(nameof(UI.Architecture_Ppc64))]
    Ppc64,
    [LocalizedDescription(nameof(UI.Architecture_Prism))]
    Prism,
    [LocalizedDescription(nameof(UI.Architecture_Renesasrx))]
    Renesasrx,
    [LocalizedDescription(nameof(UI.Architecture_Riscv))]
    Riscv,
    [LocalizedDescription(nameof(UI.Architecture_S360))]
    S360,
    [LocalizedDescription(nameof(UI.Architecture_S370))]
    S370,
    [LocalizedDescription(nameof(UI.Architecture_Sh))]
    Sh,
    [LocalizedDescription(nameof(UI.Architecture_Sh1))]
    Sh1,
    [LocalizedDescription(nameof(UI.Architecture_Sh2))]
    Sh2,
    [LocalizedDescription(nameof(UI.Architecture_Sh3))]
    Sh3,
    [LocalizedDescription(nameof(UI.Architecture_Sh4))]
    Sh4,
    [LocalizedDescription(nameof(UI.Architecture_Sh5))]
    Sh5,
    [LocalizedDescription(nameof(UI.Architecture_Sh64))]
    Sh64,
    [LocalizedDescription(nameof(UI.Architecture_Sparc))]
    Sparc,
    [LocalizedDescription(nameof(UI.Architecture_Sparc64))]
    Sparc64,
    [LocalizedDescription(nameof(UI.Architecture_Transputer))]
    Transputer,
    [LocalizedDescription(nameof(UI.Architecture_Vax))]
    Vax,
    [LocalizedDescription(nameof(UI.Architecture_We32000))]
    We32000,
    [LocalizedDescription(nameof(UI.Architecture_X32))]
    X32,
    [LocalizedDescription(nameof(UI.Architecture_Z80))]
    Z80,
    [LocalizedDescription(nameof(UI.Architecture_Z800))]
    Z800,
    [LocalizedDescription(nameof(UI.Architecture_Z8000))]
    Z8000,
    [LocalizedDescription(nameof(UI.Architecture_Z80000))]
    Z80000,
    [LocalizedDescription(nameof(UI.Architecture_Zarch))]
    Zarch
}