using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    /// <summary>
    ///     Converts an AaruFormat.Status to Aaru.CommonTypes.Enums.ErrorNumber.
    /// </summary>
    /// <param name="status">The AaruFormat status to convert</param>
    /// <returns>The corresponding ErrorNumber</returns>
    static ErrorNumber StatusToErrorNumber(Status status)
    {
        return status switch
               {
                   Status.Ok                     => ErrorNumber.NoError,
                   Status.NotAaruFormat          => ErrorNumber.InvalidArgument,
                   Status.FileTooSmall           => ErrorNumber.InvalidArgument,
                   Status.IncompatibleVersion    => ErrorNumber.NotSupported,
                   Status.CannotReadIndex        => ErrorNumber.InOutError,
                   Status.SectorOutOfBounds      => ErrorNumber.OutOfRange,
                   Status.CannotReadHeader       => ErrorNumber.InOutError,
                   Status.CannotReadBlock        => ErrorNumber.InOutError,
                   Status.UnsupportedCompression => ErrorNumber.NotSupported,
                   Status.NotEnoughMemory        => ErrorNumber.OutOfMemory,
                   Status.BufferTooSmall         => ErrorNumber.InvalidArgument,
                   Status.MediaTagNotPresent     => ErrorNumber.NoData,
                   Status.IncorrectMediaType     => ErrorNumber.InvalidArgument,
                   Status.TrackNotFound          => ErrorNumber.NoData,
                   Status.ReachedUnreachableCode => ErrorNumber.InvalidArgument,
                   Status.InvalidTrackFormat     => ErrorNumber.InvalidArgument,
                   Status.SectorTagNotPresent    => ErrorNumber.NoData,
                   Status.CannotDecompressBlock  => ErrorNumber.InOutError,
                   Status.InvalidBlockCrc        => ErrorNumber.InOutError,
                   Status.CannotCreateFile       => ErrorNumber.InvalidArgument,
                   Status.InvalidAppNameLength   => ErrorNumber.InvalidArgument,
                   Status.CannotWriteHeader      => ErrorNumber.InOutError,
                   Status.ReadOnly               => ErrorNumber.ReadOnly,
                   Status.CannotWriteBlockHeader => ErrorNumber.InOutError,
                   Status.CannotWriteBlockData   => ErrorNumber.InOutError,
                   Status.CannotSetDdtEntry      => ErrorNumber.InOutError,
                   Status.IncorrectDataSize      => ErrorNumber.InvalidArgument,
                   Status.InvalidTag             => ErrorNumber.InvalidArgument,
                   Status.TapeFileNotFound       => ErrorNumber.NoData,
                   Status.TapePartitionNotFound  => ErrorNumber.NoData,
                   Status.MetadataNotPresent     => ErrorNumber.NoData,
                   _                             => ErrorNumber.InvalidArgument
               };
    }
}