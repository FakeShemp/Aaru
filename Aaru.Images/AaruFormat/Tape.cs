using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableTapeImage Members

    /// <inheritdoc />
    public bool AddFile(TapeFile file)
    {
        Status res = aaruf_set_tape_file(_context, file.Partition, file.File, file.FirstBlock, file.LastBlock);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_tape_file(void *context, const uint8_t partition, const uint32_t file,
    // const uint64_t starting_block, const uint64_t ending_block)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_tape_file", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_tape_file(IntPtr context, byte partition, uint file, ulong startingBlock,
                                                      ulong  endingBlock);
}