using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Humanizer;
using Spectre.Console;

namespace Aaru.Archives;

public sealed partial class Stfs
{
    static void ReverseShorts(byte[] shorts, int start, int count)
    {
        for(int i = start; i < start + count; i += 2) (shorts[i], shorts[i + 1]) = (shorts[i + 1], shorts[i]);
    }

#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<RemotePackage>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[Marshal.SizeOf<RemotePackage>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        RemotePackage header = Marshal.ByteArrayToStructureBigEndian<RemotePackage>(hdr);

        if(header.Magic is not (PackageMagic.Console or PackageMagic.Live or PackageMagic.Microsoft)) return false;

        // SVOD is managed as a media image
        return header.Metadata.DescriptorType == 0;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < Marshal.SizeOf<RemotePackage>()) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[Marshal.SizeOf<RemotePackage>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        // Reverse positions that hold UTF16-BE strings
        ReverseShorts(hdr, 0x0411, 0x1300);

        RemotePackage header = Marshal.ByteArrayToStructureBigEndian<RemotePackage>(hdr);

        if(header.Magic is not (PackageMagic.Console or PackageMagic.Live or PackageMagic.Microsoft)) return;

        // SVOD is managed as a media image
        if(header.Metadata.DescriptorType != 0) return;

        var sb = new StringBuilder();

        switch(header.Magic)
        {
            case PackageMagic.Console:
                sb.AppendLine(Localization.Console_package);

                break;
            case PackageMagic.Live:
                sb.AppendLine(Localization.Live_package);

                break;
            case PackageMagic.Microsoft:
                sb.AppendLine(Localization.Microsoft_package);

                break;
        }

        if(header.Magic == PackageMagic.Console)
        {
            ConsolePackage consolePackage = Marshal.ByteArrayToStructureBigEndian<ConsolePackage>(hdr);

            sb.AppendFormat(Localization.Certificate_owner_console_ID_0_1_2_3_4,
                            consolePackage.CertificateOwnerConsoleId[0],
                            consolePackage.CertificateOwnerConsoleId[1],
                            consolePackage.CertificateOwnerConsoleId[2],
                            consolePackage.CertificateOwnerConsoleId[3],
                            consolePackage.CertificateOwnerConsoleId[4])
              .AppendLine();

            sb.AppendFormat(Localization.Certificate_owner_console_part_number_0,
                            Markup.Escape(consolePackage.CertificateOwnerConsolePartNumber))
              .AppendLine();

            sb.AppendFormat(Localization.Certificate_owner_console_type_0, consolePackage.CertificateOwnerConsoleType)
              .AppendLine();

            sb.AppendFormat(Localization.Certificate_date_of_generation_0,
                            Markup.Escape(consolePackage.CertificateDateOfGeneration))
              .AppendLine();
        }
        else
        {
            // Calculate signature's SHA1
            using var sha1          = SHA1.Create();
            byte[]    signatureSha1 = sha1.ComputeHash(header.Signature);

            sb.AppendFormat(Localization.Signatures_SHA1_0, BitConverter.ToString(signatureSha1).Replace("-", ""))
              .AppendLine();
        }

        sb.AppendFormat(Localization.Header_size_0,  header.Metadata.HeaderSize).AppendLine();
        sb.AppendFormat(Localization.Content_type_0, header.Metadata.ContentType.Humanize()).AppendLine();

        if(header.Metadata.ContentSize > 0)
            sb.AppendFormat(Localization.Content_size_0, header.Metadata.ContentSize).AppendLine();

        if(header.Metadata.TitleId > 0) sb.AppendFormat(Localization.Title_ID_0, header.Metadata.TitleId).AppendLine();
        if(header.Metadata.MediaId > 0) sb.AppendFormat(Localization.Media_ID_0, header.Metadata.MediaId).AppendLine();

        if(header.Metadata.PublisherName != "")
            sb.AppendFormat(Localization.Publisher_name_0, Markup.Escape(header.Metadata.PublisherName)).AppendLine();

        if(header.Metadata.TitleName != "")
            sb.AppendFormat(Localization.Title_name_0, Markup.Escape(header.Metadata.TitleName)).AppendLine();

        sb.AppendFormat(Localization.Metadata_version_0, header.Metadata.MetadataVersion).AppendLine();
        if(header.Metadata.Version > 0) sb.AppendFormat(Localization.Version_0, header.Metadata.Version).AppendLine();

        if(header.Metadata.BaseVersion > 0)
            sb.AppendFormat(Localization.Base_version_0, header.Metadata.BaseVersion).AppendLine();

        sb.AppendFormat(Localization.Descriptor_type_0, header.Metadata.DescriptorType).AppendLine();

        foreach(LocalizedString displayName in
                header.Metadata.DisplayName.Where(static displayName => displayName.Name is not ""))
            sb.AppendFormat(Localization.Display_name_0, Markup.Escape(displayName.Name)).AppendLine();

        foreach(LocalizedString description in
                header.Metadata.DisplayDescription.Where(static description => description.Name is not ""))
            sb.AppendFormat(Localization.Display_description_0, Markup.Escape(description.Name)).AppendLine();

        if(header.Metadata.DeviceId.Any(static c => c != 0x00))
        {
            sb.AppendFormat(Localization.Device_ID_0,
                            StringHandlers.CToString(header.Metadata.DeviceId, Encoding.ASCII).Trim())
              .AppendLine();
        }

        if(header.Metadata.ConsoleId.Any(static c => c != 0x00))
        {
            sb.AppendFormat(Localization.Console_ID_0,
                            BitConverter.ToString(header.Metadata.ConsoleId).Replace("-", ""))
              .AppendLine();
        }

        if(header.Metadata.ProfileId.Any(static c => c != 0x00))
        {
            sb.AppendFormat(Localization.Profile_ID_0,
                            BitConverter.ToString(header.Metadata.ProfileId).Replace("-", ""))
              .AppendLine();
        }

        information = sb.ToString();
    }

#endregion
}