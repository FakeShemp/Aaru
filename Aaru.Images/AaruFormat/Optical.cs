using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    Dictionary<int, byte>   _trackFlags;
    Dictionary<int, byte[]> _trackIsrcs;

    List<Track> _tracks;

#region IWritableOpticalImage Members

    /// <inheritdoc />
    public List<Track> Tracks
    {
        get
        {
            if(_tracks is not null) return _tracks;

            byte[] buffer = null;
            ulong  length = 0;

            Status res = aaruf_get_tracks(_context, buffer, ref length);

            if(res != Status.BufferTooSmall)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            buffer = new byte[length];
            res    = aaruf_get_tracks(_context, buffer, ref length);

            if(res != Status.Ok)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            // Marshal the received buffer, that's an array of TrackEntry structures into a list of Track
            int trackEntrySize = Marshal.SizeOf<TrackEntry>();
            var trackCount     = (int)(length / (ulong)trackEntrySize);

            _tracks     = new List<Track>(trackCount);
            _trackFlags = [];
            _trackIsrcs = [];

            IntPtr ptr = Marshal.AllocHGlobal(trackCount * trackEntrySize);

            try
            {
                Marshal.Copy(buffer, 0, ptr, (int)length);

                for(var i = 0; i < trackCount; i++)
                {
                    var        trackPtr = IntPtr.Add(ptr, i * trackEntrySize);
                    TrackEntry entry    = Marshal.PtrToStructure<TrackEntry>(trackPtr);

                    var track = new Track
                    {
                        Sequence    = entry.Sequence,
                        Type        = (TrackType)entry.Type,
                        StartSector = (ulong)entry.Start,
                        EndSector   = (ulong)entry.End,
                        Pregap      = (ulong)entry.Pregap,
                        Session     = entry.Session,
                        FileType    = "BINARY"
                    };

                    _trackIsrcs[entry.Sequence] = entry.Isrc;
                    _trackFlags[entry.Sequence] = entry.Flags;

                    _tracks.Add(track);
                }
            }
            catch
            {
                _tracks = null;
#pragma warning disable ERP022
            }
#pragma warning restore ERP022
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return _tracks;
        }
    }

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session) => Tracks?.Where(t => t.Session == session.Sequence).ToList();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session) => Tracks?.Where(t => t.Session == session).ToList();

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_tracks(const void *context, uint8_t *buffer, size_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_tracks", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_tracks(IntPtr context, byte[] buffer, ref ulong length);
}