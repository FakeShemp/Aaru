namespace Aaru.Archives;

public sealed partial class Arc
{
#region ArchiveInformationType enum

    public enum ArchiveInformationType : byte
    {
        Description = 0,
        Creator     = 1,
        Modifier    = 2
    }

#endregion

#region FileInformationType enum

    public enum FileInformationType : byte
    {
        Description   = 0,
        LongName      = 1,
        ExtendedDates = 2,
        Icon          = 3,
        Attributes    = 4
    }

#endregion

#region Method enum

    public enum Method : byte
    {
        EndOfArchive       = 0,
        UnpackedOld        = 1,
        Unpacked           = 2,
        Pack               = 3,
        Squeeze            = 4,
        CrunchOld          = 5,
        Crunch             = 6,
        CrunchFastHash     = 7,
        CrunchDynamic      = 8,
        Squash             = 9,
        Crush              = 10,
        Distill            = 11,
        ArchiveInformation = 20,
        FileInformation    = 21,
        Subdirectory       = 30,
        SubdirectoryEnd    = 31
    }

#endregion
}