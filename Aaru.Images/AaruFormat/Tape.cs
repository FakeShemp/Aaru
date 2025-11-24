using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_set_tape_file(void *context, const uint8_t partition, const uint32_t file,
    // const uint64_t starting_block, const uint64_t ending_block)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_tape_file", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_tape_file(IntPtr context, byte partition, uint file, ulong startingBlock,
                                                      ulong  endingBlock);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_tape_partition(void *context, const uint8_t partition,
    // const uint64_t starting_block, const uint64_t ending_block)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_tape_partition", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_tape_partition(IntPtr context, byte partition, ulong startingBlock,
                                                           ulong  endingBlock);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_all_tape_files(const void *context, uint8_t *buffer, size_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_all_tape_files", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_all_tape_files(IntPtr context, IntPtr buffer, ref nuint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_all_tape_partitions(const void *context, uint8_t *buffer, size_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_all_tape_partitions", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_all_tape_partitions(IntPtr context, IntPtr buffer, ref nuint length);

#region IWritableTapeImage Members

    /// <inheritdoc />
    public bool AddFile(TapeFile file)
    {
        if(!IsTape)
        {
            ErrorMessage = "Image is not a tape";

            return false;
        }

        Status res = aaruf_set_tape_file(_context, file.Partition, file.File, file.FirstBlock, file.LastBlock);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }

    /// <inheritdoc />
    public bool AddPartition(TapePartition partition)
    {
        if(!IsTape)
        {
            ErrorMessage = "Image is not a tape";

            return false;
        }

        Status res = aaruf_set_tape_partition(_context, partition.Number, partition.FirstBlock, partition.LastBlock);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }

    /// <inheritdoc />
    public bool IsTape { get; private set; }

    /// <inheritdoc />
    public bool SetTape()
    {
        IsTape = true;

        return true;
    }

    /// <inheritdoc />
    public List<TapeFile> Files
    {
        get
        {
            if(!IsTape)
            {
                ErrorMessage = "Image is not a tape";

                return null;
            }

            nuint  length = 0;
            Status res    = aaruf_get_all_tape_files(_context, IntPtr.Zero, ref length);

            if(res != Status.Ok && res != Status.BufferTooSmall)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)length);

            try
            {
                res = aaruf_get_all_tape_files(_context, buffer, ref length);

                if(res != Status.Ok)
                {
                    ErrorMessage = StatusToErrorMessage(res);

                    return null;
                }

                int structSize = Marshal.SizeOf<TapeFileEntry>();
                int count      = (int)length / structSize;

                List<TapeFile> files = new(count);

                for(var i = 0; i < count; i++)
                {
                    var           structPtr = IntPtr.Add(buffer, i * structSize);
                    TapeFileEntry entry     = Marshal.PtrToStructure<TapeFileEntry>(structPtr);

                    files.Add(new TapeFile
                    {
                        File       = entry.File,
                        Partition  = entry.Partition,
                        FirstBlock = entry.FirstBlock,
                        LastBlock  = entry.LastBlock
                    });
                }

                return files;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    /// <inheritdoc />
    public List<TapePartition> TapePartitions
    {
        get
        {
            if(!IsTape)
            {
                ErrorMessage = "Image is not a tape";

                return null;
            }

            nuint  length = 0;
            Status res    = aaruf_get_all_tape_partitions(_context, IntPtr.Zero, ref length);

            if(res != Status.Ok && res != Status.BufferTooSmall)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)length);

            try
            {
                res = aaruf_get_all_tape_partitions(_context, buffer, ref length);

                if(res != Status.Ok)
                {
                    ErrorMessage = StatusToErrorMessage(res);

                    return null;
                }

                int structSize = Marshal.SizeOf<TapePartitionEntry>();
                int count      = (int)length / structSize;

                List<TapePartition> partitions = new(count);

                for(var i = 0; i < count; i++)
                {
                    var                structPtr = IntPtr.Add(buffer, i * structSize);
                    TapePartitionEntry entry     = Marshal.PtrToStructure<TapePartitionEntry>(structPtr);

                    partitions.Add(new TapePartition
                    {
                        Number     = entry.Number,
                        FirstBlock = entry.FirstBlock,
                        LastBlock  = entry.LastBlock
                    });
                }

                return partitions;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

#endregion
}