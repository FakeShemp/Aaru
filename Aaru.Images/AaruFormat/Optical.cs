using System;
using System.Collections.Generic;
using System.IO;
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

            nuint length = 0;

            Status res = aaruf_get_tracks(_context, null, ref length);

            if(res != Status.BufferTooSmall)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            var buffer = new byte[length];
            res = aaruf_get_tracks(_context, buffer, ref length);

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
    public List<Session> Sessions
    {
        get
        {
            if(Tracks is null) return null;

            List<Session> sessions = [];

            for(var i = 1; i <= Tracks.Max(t => t.Session); i++)
            {
                sessions.Add(new Session
                {
                    Sequence    = (ushort)i,
                    StartTrack  = Tracks.Where(t => t.Session == i).Min(t => t.Sequence),
                    EndTrack    = Tracks.Where(t => t.Session == i).Max(t => t.Sequence),
                    StartSector = Tracks.Where(t => t.Session == i).Min(t => t.StartSector),
                    EndSector   = Tracks.Where(t => t.Session == i).Max(t => t.EndSector)
                });
            }

            return sessions;
        }
    }

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session) => Tracks?.Where(t => t.Session == session.Sequence).ToList();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session) => Tracks?.Where(t => t.Session == session).ToList();

    /// <inheritdoc />
    public bool SetTracks(List<Track> tracks)
    {
        _tracks = tracks;
        List<TrackEntry> trackEntries = [];

        _trackFlags ??= [];
        _trackIsrcs ??= [];

        foreach(Track track in Tracks)
        {
            _trackFlags.TryGetValue((byte)track.Sequence, out byte flags);
            _trackIsrcs.TryGetValue((byte)track.Sequence, out byte[] isrc);

            if((flags & (int)CdFlags.DataTrack) == 0 && track.Type != TrackType.Audio) flags += (byte)CdFlags.DataTrack;

            trackEntries.Add(new TrackEntry
            {
                Sequence = (byte)track.Sequence,
                Type     = (byte)track.Type,
                Start    = (long)track.StartSector,
                End      = (long)track.EndSector,
                Pregap   = (long)track.Pregap,
                Session  = (byte)track.Session,
                Isrc     = isrc,
                Flags    = flags
            });

            switch(track.Indexes.ContainsKey(0))
            {
                case false when track.Pregap > 0:
                    track.Indexes[0] = (int)track.StartSector;
                    track.Indexes[1] = (int)(track.StartSector + track.Pregap);

                    break;
                case false when !track.Indexes.ContainsKey(1):
                    track.Indexes[0] = (int)track.StartSector;

                    break;
            }
        }

        // If there are tracks build the tracks block
        var blockStream = new MemoryStream();

        foreach(TrackEntry entry in trackEntries)
        {
            IntPtr structurePointer = Marshal.AllocHGlobal(Marshal.SizeOf<TrackEntry>());

            var structureBytes = new byte[Marshal.SizeOf<TrackEntry>()];
            Marshal.StructureToPtr(entry, structurePointer, true);

            Marshal.Copy(structurePointer, structureBytes, 0, structureBytes.Length);

            Marshal.FreeHGlobal(structurePointer);
            blockStream.Write(structureBytes, 0, structureBytes.Length);
        }

        byte[] blockBytes = blockStream.ToArray();
        Status res        = aaruf_set_tracks(_context, blockBytes, trackEntries.Count);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_tracks(const void *context, uint8_t *buffer, size_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_tracks", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_tracks(IntPtr context, byte[] buffer, ref nuint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_tracks(void *context, TrackEntry *tracks, const int count)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_tracks", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_tracks(IntPtr context, [In] byte[] tracks, int count);
}