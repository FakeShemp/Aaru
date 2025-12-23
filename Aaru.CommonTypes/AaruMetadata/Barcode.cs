// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Barcode.cs
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Aaru.Localization;

namespace Aaru.CommonTypes.AaruMetadata;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum BarcodeType
{
    [LocalizedDescription(nameof(UI.BarcodeType_Aztec))]
    Aztec,
    [LocalizedDescription(nameof(UI.BarcodeType_Codabar))]
    Codabar,
    [LocalizedDescription(nameof(UI.BarcodeType_Code11))]
    Code11,
    [LocalizedDescription(nameof(UI.BarcodeType_Code128))]
    Code128,
    [LocalizedDescription(nameof(UI.BarcodeType_Code39))]
    Code39,
    [LocalizedDescription(nameof(UI.BarcodeType_Code93))]
    Code93,
    [LocalizedDescription(nameof(UI.BarcodeType_CPC_Binary))]
    CPC_Binary,
    [LocalizedDescription(nameof(UI.BarcodeType_EZcode))]
    EZcode,
    [LocalizedDescription(nameof(UI.BarcodeType_FIM))]
    FIM,
    [LocalizedDescription(nameof(UI.BarcodeType_ITF))]
    ITF,
    [LocalizedDescription(nameof(UI.BarcodeType_ITF14))]
    ITF14,
    [LocalizedDescription(nameof(UI.BarcodeType_EAN13))]
    EAN13,
    [LocalizedDescription(nameof(UI.BarcodeType_EAN8))]
    EAN8,
    [LocalizedDescription(nameof(UI.BarcodeType_MaxiCode))]
    MaxiCode,
    [LocalizedDescription(nameof(UI.BarcodeType_ISBN))]
    ISBN,
    [LocalizedDescription(nameof(UI.BarcodeType_ISRC))]
    ISRC,
    [LocalizedDescription(nameof(UI.BarcodeType_MSI))]
    MSI,
    [LocalizedDescription(nameof(UI.BarcodeType_ShotCode))]
    ShotCode,
    [LocalizedDescription(nameof(UI.BarcodeType_RM4SCC))]
    RM4SCC,
    [LocalizedDescription(nameof(UI.BarcodeType_QR))]
    QR,
    [LocalizedDescription(nameof(UI.BarcodeType_EAN5))]
    EAN5,
    [LocalizedDescription(nameof(UI.BarcodeType_EAN2))]
    EAN2,
    [LocalizedDescription(nameof(UI.BarcodeType_POSTNET))]
    POSTNET,
    [LocalizedDescription(nameof(UI.BarcodeType_PostBar))]
    PostBar,
    [LocalizedDescription(nameof(UI.BarcodeType_Plessey))]
    Plessey,
    [LocalizedDescription(nameof(UI.BarcodeType_Pharmacode))]
    Pharmacode,
    [LocalizedDescription(nameof(UI.BarcodeType_PDF417))]
    PDF417,
    [LocalizedDescription(nameof(UI.BarcodeType_PatchCode))]
    PatchCode
}

public class Barcode
{
    public BarcodeType Type  { get; set; }
    public string      Value { get; set; }

    [Obsolete("Will be removed in Aaru 7")]
    public static implicit operator Barcode(Schemas.BarcodeType cicm) => cicm is null
                                                                             ? null
                                                                             : new Barcode
                                                                             {
                                                                                 Type  = (BarcodeType)cicm.type,
                                                                                 Value = cicm.Value
                                                                             };
}
