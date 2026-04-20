using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using FluentAssertions;
using NUnit.Framework;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Tests.Issues;

/// <summary>This will extract (and discard data) all files in all filesystems detected in an image.</summary>
public abstract class FsExtractIssueTest
{
    public abstract string                     DataFolder       { get; }
    public abstract string                     TestFile         { get; }
    public abstract Dictionary<string, string> ParsedOptions    { get; }
    public abstract bool                       Debug            { get; }
    public abstract bool                       Xattrs           { get; }
    public abstract string                     Encoding         { get; }
    public abstract bool                       ExpectPartitions { get; }
    public abstract string                     Namespace        { get; }

    [OneTimeSetUp]
    public void InitTest() => PluginBase.Init();

    [Test]
    public void Test()
    {
        Environment.CurrentDirectory = DataFolder;

        IFilter inputFilter = PluginRegister.Singleton.GetFilter(TestFile);

        Dictionary<string, string> options = ParsedOptions;
        options["debug"] = Debug.ToString();

        inputFilter.Should().NotBeNull(Localization.Cannot_open_specified_file);

        Encoding encodingClass = null;

        if(Encoding != null) encodingClass = Claunia.Encoding.Encoding.GetEncoding(Encoding);

        PluginRegister plugins = PluginRegister.Singleton;

        var imageFormat = ImageFormat.Detect(inputFilter) as IMediaImage;

        imageFormat.Should().NotBeNull(Localization.Image_format_not_identified_not_proceeding_with_analysis);

        imageFormat.Open(inputFilter).Should().Be(ErrorNumber.NoError, Localization.Unable_to_open_image_format);

        List<Partition> partitions = Core.Partitions.GetAll(imageFormat);

        if(partitions.Count == 0)
        {
            ExpectPartitions.Should().BeFalse(Localization.No_partitions_found);

            partitions.Add(new Partition
            {
                Description = "Whole device",
                Length      = imageFormat.Info.Sectors,
                Offset      = 0,
                Size        = imageFormat.Info.SectorSize * imageFormat.Info.Sectors,
                Sequence    = 1,
                Start       = 0
            });
        }

        var filesystemFound = false;

        for(var i = 0; i < partitions.Count; i++)
        {
            Core.Filesystems.Identify(imageFormat, out List<string> idPlugins, partitions[i]);

            if(idPlugins.Count == 0) continue;

            ErrorNumber error;

            if(idPlugins.Count > 1)
            {
                foreach(string pluginName in idPlugins)
                {
                    if(!plugins.ReadOnlyFilesystems.TryGetValue(pluginName, out IReadOnlyFilesystem fs)) continue;

                    fs.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_0, pluginName);

                    filesystemFound = true;

                    error = fs.Mount(imageFormat, partitions[i], encodingClass, options, Namespace);

                    error.Should()
                         .Be(ErrorNumber.NoError,
                             string.Format(Localization.Could_not_mount_0_in_partition_1, pluginName, i));

                    ExtractFilesInDir("/", fs, Xattrs);
                }
            }
            else
            {
                plugins.ReadOnlyFilesystems.TryGetValue(idPlugins[0], out IReadOnlyFilesystem fs);

                fs.Should().NotBeNull(Localization.Could_not_instantiate_filesystem_0, fs?.Name);

                filesystemFound = true;

                error = fs.Mount(imageFormat, partitions[i], encodingClass, options, Namespace);

                error.Should()
                     .Be(ErrorNumber.NoError, string.Format(Localization.Could_not_mount_0_in_partition_1, fs.Name, i));

                ExtractFilesInDir("/", fs, Xattrs);
            }
        }

        filesystemFound.Should().BeTrue(Localization.No_filesystems_found);
    }

    static void ExtractFilesInDir(string path, IReadOnlyFilesystem fs, bool doXattrs)
    {
        if(path.StartsWith('/')) path = path[1..];

        ErrorNumber error = fs.OpenDir(path, out IDirNode node);

        error.Should()
             .Be(ErrorNumber.NoError, string.Format(Localization.Error_0_reading_root_directory, error.ToString()));

        while(fs.ReadDir(node, out string entry) == ErrorNumber.NoError && entry is not null)
        {
            error = fs.Stat(path + "/" + entry, out FileEntryInfo stat);

            error.Should().Be(ErrorNumber.NoError, string.Format(Localization.Error_getting_stat_for_entry_0, entry));

            if(stat.Attributes.HasFlag(FileAttributes.Directory))
            {
                ExtractFilesInDir(path + "/" + entry, fs, doXattrs);

                continue;
            }

            if(doXattrs)
            {
                error = fs.ListXAttr(path + "/" + entry, out List<string> xattrs);

                error.Should()
                     .Be(ErrorNumber.NoError,
                         string.Format(Localization.Error_0_getting_extended_attributes_for_entry_1,
                                       error,
                                       path + "/" + entry));

                if(error == ErrorNumber.NoError)
                {
                    foreach(string xattr in xattrs)
                    {
                        byte[] xattrBuf = [];
                        error = fs.GetXattr(path + "/" + entry, xattr, ref xattrBuf);

                        error.Should()
                             .Be(ErrorNumber.NoError,
                                 string.Format(Localization.Error_0_reading_extended_attributes_for_entry_1,
                                               error,
                                               path + "/" + entry));
                    }
                }
            }

            var         buffer = new byte[stat.Length];
            ErrorNumber ret    = fs.OpenFile(path + "/" + entry, out IFileNode fileNode);

            ret.Should()
               .Be(ErrorNumber.NoError, string.Format(Localization.Error_0_reading_file_1, ret, path + "/" + entry));

            ret = fs.ReadFile(fileNode, stat.Length, buffer, out long readBytes);

            ret.Should()
               .Be(ErrorNumber.NoError, string.Format(Localization.Error_0_reading_file_1, ret, path + "/" + entry));

            readBytes.Should()
                     .Be(stat.Length,
                         string.Format(Localization.Error_0_reading_file_1,
                                       readBytes,
                                       stat.Length,
                                       path + "/" + entry));
        }

        fs.CloseDir(node);
    }
}