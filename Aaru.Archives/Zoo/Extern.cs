using System;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Zoo : IArchive
{
    [LibraryImport("Aaru.Compression.Native")]
    private static partial IntPtr CreateLZDContext();

    [LibraryImport("libAaru.Compression.Native")]
    private static partial void DestroyLZDContext(IntPtr ctx);

    [LibraryImport("libAaru.Compression.Native")]
    private static partial int LZD_FeedNative(IntPtr ctx, [In] byte[] inputBuffer, nuint inputSize);

    [LibraryImport("libAaru.Compression.Native")]
    private static partial int LZD_DrainNative(IntPtr    ctx, [Out] byte[] outputBuffer, nuint outputCapacity,
                                               out nuint produced);
}