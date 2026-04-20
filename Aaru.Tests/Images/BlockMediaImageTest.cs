using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aaru.Checksums;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Tests.Filesystems;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Aaru.Tests.Images;

public abstract class BlockMediaImageTest : BaseMediaImageTest
{
    // How many sectors to read at once
    const           uint                     SECTORS_TO_READ = 256;
    public abstract BlockImageTestExpected[] Tests { get; }

    [OneTimeSetUp]
    public void InitTest() => PluginBase.Init();

    [Test]
    public void Info()
    {
        Environment.CurrentDirectory = DataFolder;

        using(new AssertionScope())
        {
            foreach(BlockImageTestExpected test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter filter = PluginRegister.Singleton.GetFilter(testFile);
                filter.Open(testFile);

                var image = Activator.CreateInstance(Plugin.GetType()) as IMediaImage;

                image.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_for_0, testFile);

                ErrorNumber opened = image.Open(filter);
                opened.Should().Be(ErrorNumber.NoError, string.Format(Localization.Open_0, testFile));

                if(opened != ErrorNumber.NoError) continue;

                using(new AssertionScope())
                {
                    image.Info.Sectors.Should().Be(test.Sectors, string.Format(Localization.Sectors_0, testFile));

                    image.Info.SectorSize.Should()
                         .Be(test.SectorSize, string.Format(Localization.Sector_size_0, testFile));

                    image.Info.MediaType.Should()
                         .Be(test.MediaType, string.Format(Localization.Media_type_0, testFile));
                }
            }
        }
    }

    [Test]
    public void Hashes()
    {
        Environment.CurrentDirectory = DataFolder;
        ErrorNumber errno;

        using(new AssertionScope())
        {
            foreach(BlockImageTestExpected test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter filter = PluginRegister.Singleton.GetFilter(testFile);
                filter.Open(testFile);

                var image = Activator.CreateInstance(Plugin.GetType()) as IMediaImage;

                image.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_for_0, testFile);

                ErrorNumber opened = image.Open(filter);
                opened.Should().Be(ErrorNumber.NoError, string.Format(Localization.Open_0, testFile));

                if(opened != ErrorNumber.NoError) continue;

                ulong doneSectors = 0;
                var   ctx         = new Md5Context();

                while(doneSectors < image.Info.Sectors)
                {
                    byte[] sector;

                    if(image.Info.Sectors - doneSectors >= SECTORS_TO_READ)
                    {
                        errno       =  image.ReadSectors(doneSectors, false, SECTORS_TO_READ, out sector, out _);
                        doneSectors += SECTORS_TO_READ;
                    }
                    else
                    {
                        errno = image.ReadSectors(doneSectors,
                                                  false,
                                                  (uint)(image.Info.Sectors - doneSectors),
                                                  out sector,
                                                  out _);

                        doneSectors += image.Info.Sectors - doneSectors;
                    }

                    errno.Should().Be(ErrorNumber.NoError);
                    ctx.Update(sector);
                }

                ctx.End().Should().Be(test.Md5, string.Format(Localization.Hash_0, testFile));
            }
        }
    }

    [Test]
    public void Contents()
    {
        Environment.CurrentDirectory = DataFolder;
        PluginRegister plugins = PluginRegister.Singleton;

        using(new AssertionScope())
        {
            foreach(BlockImageTestExpected test in Tests)
            {
                if(test.Partitions is null) continue;

                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter filter = PluginRegister.Singleton.GetFilter(testFile);
                filter.Open(testFile);

                var image = Activator.CreateInstance(Plugin.GetType()) as IMediaImage;

                image.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_for_0, testFile);

                ErrorNumber opened = image.Open(filter);
                opened.Should().Be(ErrorNumber.NoError, string.Format(Localization.Open_0, testFile));

                if(opened != ErrorNumber.NoError) continue;

                List<Partition> partitions = Core.Partitions.GetAll(image);

                if(partitions.Count == 0)
                {
                    partitions.Add(new Partition
                    {
                        Description = "Whole device",
                        Length      = image.Info.Sectors,
                        Offset      = 0,
                        Size        = image.Info.SectorSize * image.Info.Sectors,
                        Sequence    = 1,
                        Start       = 0
                    });
                }

                partitions.Should()
                          .HaveCount(test.Partitions.Length,
                                     string.Format(Localization.Expected_0_partitions_in_1_but_found_2,
                                                   test.Partitions.Length,
                                                   testFile,
                                                   partitions.Count));

                using(new AssertionScope())
                {
                    for(var i = 0; i < test.Partitions.Length; i++)
                    {
                        BlockPartitionVolumes expectedPartition = test.Partitions[i];
                        Partition             foundPartition    = partitions[i];

                        foundPartition.Start.Should()
                                      .Be(expectedPartition.Start,
                                          string.Format(Localization
                                                           .Expected_partition_0_to_start_at_sector_1_but_found_it_starts_at_2_in_3,
                                                        i,
                                                        expectedPartition.Start,
                                                        foundPartition.Start,
                                                        testFile));

                        foundPartition.Length.Should()
                                      .Be(expectedPartition.Length,
                                          string.Format(Localization
                                                           .Expected_partition_0_to_have_1_sectors_but_found_it_has_2_sectors_in_3,
                                                        i,
                                                        expectedPartition.Length,
                                                        foundPartition.Length,
                                                        testFile));

                        var expectedDataFilename = $"{testFile}.contents.partition{i}.json";

                        if(!File.Exists(expectedDataFilename)) continue;

                        var serializerOptions = new JsonSerializerOptions
                        {
                            Converters =
                            {
                                new JsonStringEnumConverter()
                            },
                            MaxDepth                    = 1536, // More than this an we get a StackOverflowException
                            WriteIndented               = true,
                            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
                            PropertyNameCaseInsensitive = true,
                            IncludeFields               = true
                        };

                        var          sr           = new FileStream(expectedDataFilename, FileMode.Open);
                        VolumeData[] expectedData = JsonSerializer.Deserialize<VolumeData[]>(sr, serializerOptions);
                        sr.Close();

                        expectedData.Should().NotBeNull();

                        Core.Filesystems.Identify(image, out List<string> idPlugins, partitions[i]);

                        if(expectedData.Length != idPlugins.Count) continue;

                        // Uncomment to generate JSON file
                        /*
                            expectedData = new VolumeData[idPlugins.Count];

                            for(int j = 0; j < idPlugins.Count; j++)
                            {
                                string pluginName = idPlugins[j];

                                if(!plugins.ReadOnlyFilesystems.TryGetValue(pluginName,
                                                                                out IReadOnlyFilesystem fs))
                                    continue;

                                Assert.IsNotNull(fs, string.Format(Localization.Could_not_instantiate_filesystem_0, pluginName));

                                ErrorNumber error = fs.Mount(image, partitions[i], null, null, null);

                                Assert.AreEqual(ErrorNumber.NoError, error,
                                                string.Format(Localization.Could_not_mount_0_in_partition_1, pluginName, i));

                                if(error != ErrorNumber.NoError)
                                    continue;

                                expectedData[j] = new VolumeData
                                {
                                    Files = ReadOnlyFilesystemTest.BuildDirectory(fs, "/", 0)
                                };
                            }

                            var sw = new FileStream(expectedDataFilename, FileMode.Create);
                            JsonSerializer.Serialize(sw, expectedData, serializerOptions);
                            sw.Close();
                            */

                        if(idPlugins.Count == 0) continue;

                        idPlugins.Should()
                                 .HaveCount(expectedData.Length,
                                            $"Expected {expectedData.Length} filesystems identified in partition {i
                                            } but found {idPlugins.Count} in {testFile}");

                        for(var j = 0; j < idPlugins.Count; j++)
                        {
                            string pluginName = idPlugins[j];

                            if(!plugins.ReadOnlyFilesystems.TryGetValue(pluginName, out IReadOnlyFilesystem fs))
                                continue;

                            fs.Should().NotBeNull($"Could not instantiate filesystem {pluginName} in {testFile}");

                            ErrorNumber error = fs.Mount(image, partitions[i], null, null, null);

                            error.Should()
                                 .Be(ErrorNumber.NoError,
                                     $"Could not mount {pluginName} in partition {i} in {testFile}.");

                            if(error != ErrorNumber.NoError) continue;

                            VolumeData volumeData = expectedData[j];

                            var currentDepth = 0;

                            ReadOnlyFilesystemTest.TestDirectory(fs,
                                                                 "/",
                                                                 volumeData.Files,
                                                                 testFile,
                                                                 true,
                                                                 out List<ReadOnlyFilesystemTest.NextLevel>
                                                                         currentLevel,
                                                                 currentDepth);

                            while(currentLevel.Count > 0)
                            {
                                currentDepth++;
                                List<ReadOnlyFilesystemTest.NextLevel> nextLevels = [];

                                foreach(ReadOnlyFilesystemTest.NextLevel subLevel in currentLevel)
                                {
                                    ReadOnlyFilesystemTest.TestDirectory(fs,
                                                                         subLevel.Path,
                                                                         subLevel.Children,
                                                                         testFile,
                                                                         true,
                                                                         out List<ReadOnlyFilesystemTest.NextLevel>
                                                                                 nextLevel,
                                                                         currentDepth);

                                    nextLevels.AddRange(nextLevel);
                                }

                                currentLevel = nextLevels;
                            }
                        }
                    }
                }
            }
        }
    }
}