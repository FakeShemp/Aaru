using System.IO;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Zip
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_FILE_SIZE) return false;

        Stream stream = filter.GetDataForkStream();

        if(stream.Length < 4) return false;

        stream.Position = 0;

        var sig  = new byte[8];
        int read = stream.Read(sig, 0, sig.Length < stream.Length ? sig.Length : (int)stream.Length);

        if(read < 4) return false;

        // PK\x03\x04 at offset 0
        if(sig[0] == 0x50 && sig[1] == 0x4B && sig[2] == 0x03 && sig[3] == 0x04) return true;

        // PK\x05\x06 at offset 0 (empty archive)
        if(sig[0] == 0x50 && sig[1] == 0x4B && sig[2] == 0x05 && sig[3] == 0x06) return true;

        // PK\x03\x04 at offset 4 (some SFX variants prepend 4 bytes)
        if(read >= 8 && sig[4] == 0x50 && sig[5] == 0x4B && sig[6] == 0x03 && sig[7] == 0x04) return true;

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < MIN_FILE_SIZE) return;

        ErrorNumber errno = Open(filter, encoding);

        if(errno != ErrorNumber.NoError) return;

        var sb = new StringBuilder();
        sb.AppendLine(Localization.ZIP_archive);
        sb.AppendFormat(Localization.ZIP_entries_0, _entries.Count).AppendLine();

        var directories = 0;
        var files       = 0;
        var encrypted   = 0;

        foreach(Entry entry in _entries)
        {
            if(entry.IsDirectory)
                directories++;
            else
                files++;

            if(entry.IsEncrypted) encrypted++;
        }

        sb.AppendFormat(Localization.ZIP_files_0, files).AppendLine();

        if(directories > 0) sb.AppendFormat(Localization.ZIP_directories_0, directories).AppendLine();

        if(encrypted > 0) sb.AppendFormat(Localization.ZIP_encrypted_entries_0, encrypted).AppendLine();

        if(_archiveComment is not null) sb.AppendFormat(Localization.ZIP_comment_0, _archiveComment).AppendLine();

        information = sb.ToString();

        Close();
    }

#endregion
}