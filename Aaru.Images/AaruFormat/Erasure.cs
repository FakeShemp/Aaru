using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_set_erasure_coding(void *context, uint8_t algorithm, uint16_t K, uint16_t M);
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_erasure_coding", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_erasure_coding(IntPtr context, byte algorithm, ushort k, ushort m);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_erasure_coding_auto(void *context, uint8_t recovery_percent);
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_erasure_coding_auto", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_erasure_coding_auto(IntPtr context, byte recoveryPercent);

    /// <summary>
    ///     Configure erasure coding for a newly created image.
    /// </summary>
    /// <param name="algorithm">ErasureCodingAlgorithm (0=XOR, 1=RS).</param>
    /// <param name="k">Data blocks per stripe (>= 1).</param>
    /// <param name="m">Parity blocks per stripe (>= 1).</param>
    /// <returns>AARUF_STATUS_OK on success, error code otherwise.</returns>
    public ErrorNumber SetErasureCoding(byte algorithm, ushort k, ushort m)
    {
        Status res = aaruf_set_erasure_coding(_context, algorithm, k, m);

        return StatusToErrorNumber(res);
    }

    /// <summary>
    ///     Configure erasure coding from a desired recovery percentage.
    ///     Computes K and M from the requested percentage of data that should be
    ///     recoverable. Higher recovery percentages produce higher M values
    ///     (more parity blocks = better burst-corruption tolerance).
    ///     Strategy:
    ///     M = max(2, min(8, round(20 * percent / 100)))
    ///     K = M * 100 / percent
    ///     This keeps K roughly constant (~16–40) while scaling M monotonically
    ///     with the requested recovery level:
    ///     1%   → RS(200, 2)   M=2, minimum burst tolerance
    ///     5%   → RS(40,  2)   M=2
    ///     10%  → RS(20,  2)   M=2
    ///     15%  → RS(20,  3)   M=3, burst tolerance grows
    ///     25%  → RS(20,  5)   M=5
    ///     50%  → RS(16,  8)   M=8 (capped), strong burst tolerance
    ///     100% → RS(8,   8)   M=8 (capped), maximum protection
    /// </summary>
    /// <param name="recoveryPercent">Desired recoverable percentage (1..100).</param>
    /// <returns>AARUF_STATUS_OK on success, error code otherwise.</returns>
    public ErrorNumber SetErasureCodingAuto(byte recoveryPercent)
    {
        Status res = aaruf_set_erasure_coding_auto(_context, recoveryPercent);

        return StatusToErrorNumber(res);
    }
}