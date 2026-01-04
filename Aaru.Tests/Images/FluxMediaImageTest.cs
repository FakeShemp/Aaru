// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FluxMediaImageTest.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Images;

public abstract class FluxMediaImageTest : BaseMediaImageTest
{
    public abstract FluxImageTestExpected[] Tests { get; }

    [OneTimeSetUp]
    public void InitTest() => PluginBase.Init();

    [Test]
    public void Info()
    {
        Environment.CurrentDirectory = DataFolder;

        Assert.Multiple(() =>
        {
            foreach(FluxImageTestExpected test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                Assert.That(exists, string.Format(Localization._0_not_found, testFile));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter filter = PluginRegister.Singleton.GetFilter(testFile);
                filter.Open(testFile);

                var image = Activator.CreateInstance(Plugin.GetType()) as IMediaImage;

                Assert.That(image,
                            Is.Not.Null,
                            string.Format(Localization.Could_not_instantiate_filesystem_for_0, testFile));

                ErrorNumber opened = image.Open(filter);
                Assert.That(opened, Is.EqualTo(ErrorNumber.NoError), string.Format(Localization.Open_0, testFile));

                if(opened != ErrorNumber.NoError) continue;

                if(image is not IFluxImage fluxImage)
                {
                    Assert.Fail($"Image {testFile} does not implement IFluxImage");
                    continue;
                }

                ErrorNumber error = fluxImage.GetAllFluxCaptures(out List<FluxCapture> captures);

                Assert.That(error, Is.EqualTo(ErrorNumber.NoError), $"GetAllFluxCaptures failed with {error}");
                Assert.That(captures, Is.Not.Null, "GetAllFluxCaptures returned null");
                Assert.That(captures, Is.Not.Empty, "GetAllFluxCaptures returned empty list");

                Assert.That(captures.Count,
                            Is.EqualTo(test.FluxCaptureCount),
                            $"Expected {test.FluxCaptureCount} flux captures, got {captures.Count}");

                captures.Should().NotBeEmpty("Flux captures list should not be empty");

                foreach(FluxCapture capture in captures)
                {
                    // Verify each capture has valid properties
                    capture.IndexResolution.Should().BeGreaterThan(0, "IndexResolution should be greater than 0");
                    capture.DataResolution.Should().BeGreaterThan(0, "DataResolution should be greater than 0");
                }
            }
        });
    }

    [Test]
    public void Contents()
    {
        Environment.CurrentDirectory = DataFolder;

        Assert.Multiple(() =>
        {
            foreach(FluxImageTestExpected test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                Assert.That(exists, string.Format(Localization._0_not_found, testFile));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter filter = PluginRegister.Singleton.GetFilter(testFile);
                filter.Open(testFile);

                var image = Activator.CreateInstance(Plugin.GetType()) as IMediaImage;

                Assert.That(image,
                            Is.Not.Null,
                            string.Format(Localization.Could_not_instantiate_filesystem_for_0, testFile));

                ErrorNumber opened = image.Open(filter);
                Assert.That(opened, Is.EqualTo(ErrorNumber.NoError), string.Format(Localization.Open_0, testFile));

                if(opened != ErrorNumber.NoError) continue;

                if(image is not IFluxImage fluxImage)
                {
                    Assert.Fail($"Image {testFile} does not implement IFluxImage");
                    continue;
                }

                ErrorNumber error = fluxImage.GetAllFluxCaptures(out List<FluxCapture> captures);

                Assert.That(error, Is.EqualTo(ErrorNumber.NoError), "GetAllFluxCaptures should succeed");
                Assert.That(captures, Is.Not.Null.And.Not.Empty, "Should have at least one flux capture");
                Assert.That(captures.Count,
                            Is.EqualTo(test.FluxCaptureCount),
                            $"Expected {test.FluxCaptureCount} flux captures, got {captures.Count}");

                // If FluxCaptures array is provided, validate those captures
                if(test.FluxCaptures != null && test.FluxCaptures.Length > 0)
                {
                    Assert.That(captures.Count,
                                Is.GreaterThanOrEqualTo(test.FluxCaptures.Length),
                                $"Image has {captures.Count} captures, but {test.FluxCaptures.Length} expected captures specified");

                    foreach(FluxCaptureTestExpected expectedCapture in test.FluxCaptures)
                    {
                        FluxCapture actualCapture = captures.Find(c => c.Head == expectedCapture.Head &&
                                                                       c.Track == expectedCapture.Track &&
                                                                       c.SubTrack == expectedCapture.SubTrack &&
                                                                       c.CaptureIndex == expectedCapture.CaptureIndex);

                        Assert.That(actualCapture,
                                    Is.Not.Null,
                                    $"Flux capture not found: head={expectedCapture.Head}, track={expectedCapture.Track}, subTrack={expectedCapture.SubTrack}, captureIndex={expectedCapture.CaptureIndex}");

                        if(actualCapture != null)
                        {
                            Assert.That(actualCapture.IndexResolution,
                                        Is.EqualTo(expectedCapture.IndexResolution),
                                        $"IndexResolution mismatch for head={expectedCapture.Head}, track={expectedCapture.Track}, subTrack={expectedCapture.SubTrack}, captureIndex={expectedCapture.CaptureIndex}");
                            Assert.That(actualCapture.DataResolution,
                                        Is.EqualTo(expectedCapture.DataResolution),
                                        $"DataResolution mismatch for head={expectedCapture.Head}, track={expectedCapture.Track}, subTrack={expectedCapture.SubTrack}, captureIndex={expectedCapture.CaptureIndex}");
                        }
                    }
                }
            }
        });
    }
}
