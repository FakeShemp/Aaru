// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ReleaseType.cs
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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Aaru.Localization;

namespace Aaru.CommonTypes.AaruMetadata;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ReleaseType
{
    [LocalizedDescription(nameof(UI.ReleaseType_Retail))]
    Retail,
    [LocalizedDescription(nameof(UI.ReleaseType_Bundle))]
    Bundle,
    [LocalizedDescription(nameof(UI.ReleaseType_Coverdisc))]
    Coverdisc,
    [LocalizedDescription(nameof(UI.ReleaseType_Subscription))]
    Subscription,
    [LocalizedDescription(nameof(UI.ReleaseType_Demo))]
    Demo,
    [LocalizedDescription(nameof(UI.ReleaseType_OEM))]
    OEM,
    [LocalizedDescription(nameof(UI.ReleaseType_Shareware))]
    Shareware,
    [LocalizedDescription(nameof(UI.ReleaseType_FOSS))]
    FOSS,
    [LocalizedDescription(nameof(UI.ReleaseType_Adware))]
    Adware,
    [LocalizedDescription(nameof(UI.ReleaseType_Donationware))]
    Donationware,
    [LocalizedDescription(nameof(UI.ReleaseType_DigitalDownload))]
    DigitalDownload,
    [LocalizedDescription(nameof(UI.ReleaseType_SaaS))]
    SaaS
}