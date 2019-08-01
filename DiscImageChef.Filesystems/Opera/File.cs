using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DiscImageChef.CommonTypes.Structs;

namespace DiscImageChef.Filesystems
{
    public partial class OperaFS
    {
        public Errno MapBlock(string path, long fileBlock, out long deviceBlock) => throw new NotImplementedException();

        public Errno GetAttributes(string path, out FileAttributes attributes) => throw new NotImplementedException();

        public Errno Read(string path, long offset, long size, ref byte[] buf) => throw new NotImplementedException();

        public Errno Stat(string path, out FileEntryInfo stat)
        {
            stat = null;
            if(!mounted) return Errno.AccessDenied;

            Errno err = GetFileEntry(path, out DirectoryEntryWithPointers entryWithPointers);
            if(err != Errno.NoError) return err;

            DirectoryEntry entry = entryWithPointers.entry;

            stat = new FileEntryInfo
            {
                Attributes = new FileAttributes(),
                Blocks     = entry.block_count,
                BlockSize  = entry.block_size,
                Length     = entry.byte_count,
                Inode      = entry.id,
                Links      = (ulong)entryWithPointers.pointers.Length
            };

            FileFlags flags = (FileFlags)(entry.flags & FLAGS_MASK);

            if(flags == FileFlags.Directory) stat.Attributes |= FileAttributes.Directory;
            if(flags == FileFlags.Special) stat.Attributes   |= FileAttributes.Device;

            return Errno.NoError;
        }

        Errno GetFileEntry(string path, out DirectoryEntryWithPointers entry)
        {
            entry = null;

            string cutPath = path.StartsWith("/", StringComparison.Ordinal)
                                 ? path.Substring(1).ToLower(CultureInfo.CurrentUICulture)
                                 : path.ToLower(CultureInfo.CurrentUICulture);
            string[] pieces = cutPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            if(pieces.Length == 0) return Errno.InvalidArgument;

            string parentPath = string.Join("/", pieces, 0, pieces.Length - 1);

            if(!directoryCache.TryGetValue(parentPath, out _))
            {
                Errno err = ReadDir(parentPath, out _);

                if(err != Errno.NoError) return err;
            }

            Dictionary<string, DirectoryEntryWithPointers> parent;

            if(pieces.Length == 1) parent = rootDirectoryCache;
            else if(!directoryCache.TryGetValue(parentPath, out parent)) return Errno.InvalidArgument;

            KeyValuePair<string, DirectoryEntryWithPointers> dirent =
                parent.FirstOrDefault(t => t.Key.ToLower(CultureInfo.CurrentUICulture) == pieces[pieces.Length - 1]);

            if(string.IsNullOrEmpty(dirent.Key)) return Errno.NoSuchFile;

            entry = dirent.Value;
            return Errno.NoError;
        }
    }
}