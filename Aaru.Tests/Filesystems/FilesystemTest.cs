using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using File = System.IO.File;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Tests.Filesystems;

public abstract class FilesystemTest(string fileSystemType)
{
    protected FilesystemTest() : this(null) {}

    public abstract string      DataFolder { get; }
    public abstract IFilesystem Plugin     { get; }
    public abstract bool        Partitions { get; }

    public abstract FileSystemTest[] Tests { get; }

    [OneTimeSetUp]
    public void InitTest() => PluginBase.Init();


    [Test]
    public void Detect()
    {
        Environment.CurrentDirectory = DataFolder;

        using(new AssertionScope())
        {
            foreach(FileSystemTest test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter inputFilter = PluginRegister.Singleton.GetFilter(testFile);

                inputFilter.Should().NotBeNull(Localization.Filter_0, testFile);

                var image = ImageFormat.Detect(inputFilter) as IMediaImage;

                image.Should().NotBeNull(Localization.Image_format_0, testFile);

                image.Open(inputFilter)
                     .Should()
                     .Be(ErrorNumber.NoError, string.Format(Localization.Cannot_open_image_for_0, testFile));

                List<string> idPlugins;

                if(Partitions)
                {
                    List<Partition> partitionsList = Core.Partitions.GetAll(image);

                    partitionsList.Should().NotBeEmpty(Localization.No_partitions_found_for_0, testFile);

                    var found = false;

                    foreach(Partition p in partitionsList)
                    {
                        Core.Filesystems.Identify(image, out idPlugins, p, true);

                        if(idPlugins.Count == 0) continue;

                        if(!idPlugins.Contains(Plugin.Id.ToString())) continue;

                        found = true;

                        break;
                    }

                    found.Should().BeTrue(Localization.Filesystem_not_identified_for_0, testFile);
                }
                else
                {
                    var wholePart = new Partition
                    {
                        Name   = "Whole device",
                        Length = image.Info.Sectors,
                        Size   = image.Info.Sectors * image.Info.SectorSize
                    };

                    Core.Filesystems.Identify(image, out idPlugins, wholePart, true);

                    idPlugins.Should().NotBeEmpty(Localization.No_filesystems_found_for_0, testFile);

                    idPlugins.Should()
                             .Contain(Plugin.Id.ToString(), string.Format(Localization.Not_identified_for_0, testFile));
                }
            }
        }
    }

    [Test]
    public void ImageInfo()
    {
        Environment.CurrentDirectory = DataFolder;

        using(new AssertionScope())
        {
            foreach(FileSystemTest test in Tests)
            {
                string testFile = test.TestFile;

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter inputFilter = PluginRegister.Singleton.GetFilter(testFile);

                inputFilter.Should().NotBeNull(Localization.Filter_0, testFile);

                var image = ImageFormat.Detect(inputFilter) as IMediaImage;

                image.Should().NotBeNull(Localization.Image_format_0, testFile);

                image.Open(inputFilter)
                     .Should()
                     .Be(ErrorNumber.NoError, string.Format(Localization.Cannot_open_image_for_0, testFile));

                image.Info.MediaType.Should().Be(test.MediaType, testFile);
                image.Info.Sectors.Should().Be(test.Sectors, testFile);
                image.Info.SectorSize.Should().Be(test.SectorSize, testFile);
            }
        }
    }

    [Test]
    public void Info()
    {
        Environment.CurrentDirectory = DataFolder;

        using(new AssertionScope())
        {
            foreach(FileSystemTest test in Tests)
            {
                string testFile  = test.TestFile;
                var    found     = false;
                var    partition = new Partition();

                bool exists = File.Exists(testFile);
                exists.Should().BeTrue(Localization._0_not_found, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It arrives here...
                if(!exists) continue;

                IFilter inputFilter = PluginRegister.Singleton.GetFilter(testFile);

                inputFilter.Should().NotBeNull(Localization.Filter_0, testFile);

                var image = ImageFormat.Detect(inputFilter) as IMediaImage;

                image.Should().NotBeNull(Localization.Image_format_0, testFile);

                image.Open(inputFilter)
                     .Should()
                     .Be(ErrorNumber.NoError, string.Format(Localization.Cannot_open_image_for_0, testFile));

                List<string> idPlugins;

                if(Partitions)
                {
                    List<Partition> partitionsList = Core.Partitions.GetAll(image);

                    partitionsList.Should().NotBeEmpty(Localization.No_partitions_found_for_0, testFile);

                    // In reverse to skip boot partitions we're not interested in
                    for(int index = partitionsList.Count - 1; index >= 0; index--)
                    {
                        Core.Filesystems.Identify(image, out idPlugins, partitionsList[index], true);

                        if(idPlugins.Count == 0) continue;

                        if(!idPlugins.Contains(Plugin.Id.ToString())) continue;

                        found     = true;
                        partition = partitionsList[index];

                        break;
                    }
                }
                else
                {
                    partition = new Partition
                    {
                        Name   = "Whole device",
                        Length = image.Info.Sectors,
                        Size   = image.Info.Sectors * image.Info.SectorSize
                    };

                    Core.Filesystems.Identify(image, out idPlugins, partition, true);

                    idPlugins.Should().NotBeEmpty(Localization.No_filesystems_found_for_0, testFile);

                    found = idPlugins.Contains(Plugin.Id.ToString());
                }

                found.Should().BeTrue(Localization.Filesystem_not_identified_for_0, testFile);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // It is not the case, it changes
                if(!found) continue;

                var fs = Activator.CreateInstance(Plugin.GetType()) as IFilesystem;

                fs.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_for_0, testFile);

                fs.GetInformation(image, partition, null, out _, out FileSystem fsMetadata);

                if(test.ApplicationId != null)
                {
                    fsMetadata.ApplicationIdentifier.Should()
                              .Be(test.ApplicationId, string.Format(Localization.Application_ID_0, testFile));
                }

                fsMetadata.Bootable.Should().Be(test.Bootable, string.Format(Localization.Bootable_0, testFile));

                fsMetadata.Clusters.Should().Be((ulong)test.Clusters, string.Format(Localization.Clusters_0, testFile));

                fsMetadata.ClusterSize.Should()
                          .Be(test.ClusterSize, string.Format(Localization.Cluster_size_0, testFile));

                if(test.SystemId != null)
                {
                    fsMetadata.SystemIdentifier.Should()
                              .Be(test.SystemId, string.Format(Localization.System_ID_0, testFile));
                }

                fsMetadata.Type.Should()
                          .Be(fileSystemType ?? test.Type, string.Format(Localization.Filesystem_type_0, testFile));

                fsMetadata.VolumeName.Should().Be(test.VolumeName, string.Format(Localization.Volume_name_0, testFile));

                fsMetadata.VolumeSerial.Should()
                          .Be(test.VolumeSerial, string.Format(Localization.Volume_serial_0, testFile));
            }
        }
    }
}