using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Arj
{
    /// <summary>
    ///     Parse OS/2 FEA records from a decompressed EA block and return the EA names. Block format: count(2 LE) +
    ///     records of fEA(1) + nameLen(1) + valueLen(2 LE) + name(nameLen) + value(valueLen).
    /// </summary>
    static List<string> ParseEaNames(byte[] eaBlock)
    {
        var names = new List<string>();

        if(eaBlock is null || eaBlock.Length < 2) return names;

        var offset = 0;
        var count  = BitConverter.ToUInt16(eaBlock, offset);
        offset += 2;

        for(var i = 0; i < count && offset + 4 <= eaBlock.Length; i++)
        {
            byte fEa     = eaBlock[offset++];
            byte nameLen = eaBlock[offset++];
            var  valLen  = BitConverter.ToUInt16(eaBlock, offset);
            offset += 2;

            if(offset + nameLen + valLen > eaBlock.Length) break;

            string name = Encoding.ASCII.GetString(eaBlock, offset, nameLen);
            offset += nameLen;
            offset += valLen;

            if(!string.IsNullOrEmpty(name)) names.Add(name);
        }

        return names;
    }

    /// <summary>Find an OS/2 FEA record by name and return its raw value bytes.</summary>
    static byte[] FindEaValue(byte[] eaBlock, string eaName)
    {
        if(eaBlock is null || eaBlock.Length < 2) return null;

        var offset = 0;
        var count  = BitConverter.ToUInt16(eaBlock, offset);
        offset += 2;

        for(var i = 0; i < count && offset + 4 <= eaBlock.Length; i++)
        {
            byte fEa     = eaBlock[offset++];
            byte nameLen = eaBlock[offset++];
            var  valLen  = BitConverter.ToUInt16(eaBlock, offset);
            offset += 2;

            if(offset + nameLen + valLen > eaBlock.Length) return null;

            string name = Encoding.ASCII.GetString(eaBlock, offset, nameLen);
            offset += nameLen;

            if(name == eaName)
            {
                var value = new byte[valLen];
                Array.Copy(eaBlock, offset, value, 0, valLen);

                return value;
            }

            offset += valLen;
        }

        return null;
    }

#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        xattrs = [];

        Entry entry = _entries[entryNumber];

        if(entry.Comment is not null) xattrs.Add("comment");

        // Parse OS/2 extended attribute names from the decompressed EA block
        if(entry.ExtendedAttributes is { Length: >= 2 })
        {
            List<string> eaNames = ParseEaNames(entry.ExtendedAttributes);
            xattrs.AddRange(eaNames);
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer)
    {
        buffer = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        Entry entry = _entries[entryNumber];

        if(xattr == "comment")
        {
            if(entry.Comment is null) return ErrorNumber.NoSuchExtendedAttribute;

            buffer = Encoding.UTF8.GetBytes(entry.Comment);

            return ErrorNumber.NoError;
        }

        // Look up OS/2 extended attribute by name
        if(entry.ExtendedAttributes is { Length: >= 2 })
        {
            byte[] value = FindEaValue(entry.ExtendedAttributes, xattr);

            if(value is not null)
            {
                buffer = value;

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

#endregion
}