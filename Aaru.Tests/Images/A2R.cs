// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : A2R.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// Component      : Aaru unit testing.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes.Interfaces;
using NUnit.Framework;

namespace Aaru.Tests.Images;

[TestFixture]
public class A2R : FluxMediaImageTest
{
    public override string DataFolder => Path.Combine(Consts.TestFilesRoot, "Media image formats", "A2R");
    public override IMediaImage Plugin => new Aaru.Images.A2R();

    public override FluxImageTestExpected[] Tests =>
    [
        new()
        {
            TestFile = "The Quest.a2r",
            FluxCaptureCount = 162,
            FluxCaptures = [
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 1,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 1,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 1,
                    SubTrack = 0,
                    CaptureIndex = 1,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 1,
                    SubTrack = 0,
                    CaptureIndex = 2,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 1,
                    SubTrack = 0,
                    CaptureIndex = 3,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
            ]
        },
        new()
        {
            TestFile = "Lotus 1-2-3 v2 - Utility Disk.a2r",
            FluxCaptureCount = 320,
            FluxCaptures = [
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 1,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 2,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 1,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 1,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 1,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 1,
                    Track = 0,
                    SubTrack = 0,
                    CaptureIndex = 2,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 1,
                    Track = 1,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 2,
                    SubTrack = 0,
                    CaptureIndex = 0,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 2,
                    SubTrack = 0,
                    CaptureIndex = 1,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
                new FluxCaptureTestExpected
                {
                    Head = 0,
                    Track = 2,
                    SubTrack = 0,
                    CaptureIndex = 2,
                    IndexResolution = 62500,
                    DataResolution = 62500
                },
            ]
        },
    ];
}
