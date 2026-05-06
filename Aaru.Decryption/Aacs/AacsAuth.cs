// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsAuth.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     SCSI/MMC commands implementing the AACS Drive Authentication protocol
//     and host-bound reads of Volume Identifier and Media Key Block.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Security.Cryptography;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Decryption.Aacs;

/// <summary>
///     Whether the target media is HD DVD or Blu-ray. Maps directly to MMC
///     READ DISC STRUCTURE byte 1 (Media Type).
/// </summary>
public enum AacsMediaKind : byte
{
    /// <summary>HD DVD (and DVD).</summary>
    HdDvd = 0,

    /// <summary>Blu-ray Disc.</summary>
    BluRay = 1
}

/// <summary>Result of <see cref="AacsAuth.Authenticate" />.</summary>
public enum AacsAuthResult
{
    /// <summary>Mutual authentication succeeded and a bus key has been derived.</summary>
    Success,

    /// <summary>The drive does not support AACS or the AACS feature is inactive.</summary>
    AacsNotSupported,

    /// <summary>No AGID could be obtained from the drive.</summary>
    NoAgid,

    /// <summary>The host certificate or private key supplied was rejected by the drive.</summary>
    HostCertRejected,

    /// <summary>The drive's certificate failed AACS Licensing Authority signature verification.</summary>
    DriveCertInvalid,

    /// <summary>The drive's signature over its key point is not valid.</summary>
    DriveSignatureInvalid,

    /// <summary>The drive returned an error during authentication.</summary>
    DriveError,

    /// <summary>An internal cryptographic error occurred.</summary>
    CryptoError
}

/// <summary>
///     Drive-side AACS Drive Authentication: gets an AGID, performs the host/drive certificate
///     and key exchange (sections 4.2-4.3 of the Common Cryptographic Elements book), and reads
///     the Volume Identifier (and optionally the Media Key Block) bound to a per-session bus key.
/// </summary>
public sealed class AacsAuth(Device dev)
{
    const string MODULE_NAME = "AACS auth";

    /// <summary>The current Authentication Grant ID, or <c>0xFF</c> if none has been allocated.</summary>
    public byte Agid { get; private set; } = 0xFF;

    /// <summary>The 16-byte bus key derived from the most recent successful authentication.</summary>
    public byte[]? BusKey { get; private set; }

    /// <summary>The 92-byte drive certificate received during authentication.</summary>
    public byte[]? DriveCertificate { get; private set; }

    /// <summary>The 16-byte Volume Identifier read after authentication, or <c>null</c>.</summary>
    public byte[]? VolumeIdentifier { get; private set; }

    /// <summary>The 16-byte pre-recorded media serial number read after authentication, or <c>null</c>.</summary>
    public byte[]? PrerecordedMediaSerialNumber { get; private set; }

    /// <summary>The 16-byte media identifier read after authentication, or <c>null</c>.</summary>
    public byte[]? MediaIdentifier { get; private set; }

    /// <summary>The raw Media Key Block read from the drive (concatenation of all packs), or <c>null</c>.</summary>
    public byte[]? MediaKeyBlock { get; private set; }

    /// <summary>
    ///     True after <see cref="CheckAacsFeature" /> has detected the AACS feature descriptor with
    ///     the active bit set.
    /// </summary>
    public bool AacsActive { get; private set; }

    /// <summary>The AACS version reported by the drive (1 = AACS 1.0, 2 = AACS 2.0).</summary>
    public byte AacsVersion { get; private set; }

    /// <summary>
    ///     Issue a GET CONFIGURATION (0x46) for the AACS feature 0x010D. Populates
    ///     <see cref="AacsActive" /> and <see cref="AacsVersion" />.
    /// </summary>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns><c>true</c> if the AACS feature is present and active.</returns>
    public bool CheckAacsFeature(uint timeout)
    {
        Span<byte> cdb = dev.CdbBuffer[..12];
        cdb.Clear();
        byte[] buf = new byte[16];

        cdb[0] = (byte)ScsiCommands.GetConfiguration;
        cdb[1] = 0x01;
        cdb[2] = 0x01;
        cdb[3] = 0x0D;
        cdb[7] = (byte)((buf.Length & 0xFF00) >> 8);
        cdb[8] = (byte)(buf.Length & 0xFF);

        dev.SendScsiCommand(cdb, ref buf, timeout, ScsiDirection.In, out double duration, out bool sense);

        AaruLogging.Debug(MODULE_NAME, "GET CONFIGURATION (AACS) Sense={0} took {1} ms.", sense, duration);

        if(sense || buf.Length < 16) return false;

        ushort feature = (ushort)((buf[8] << 8) | buf[9]);

        if(feature != 0x010D) return false;

        AacsActive  = (buf[10] & 0x01) != 0;
        AacsVersion = buf[15];

        AaruLogging.Debug(MODULE_NAME,
                          "AACS feature: active={0} version={1} agids={2}",
                          AacsActive,
                          AacsVersion,
                          buf[14] & 0x0F);

        return AacsActive;
    }

    /// <summary>
    ///     Run the full AACS authentication sequence and derive the bus key. On success,
    ///     <see cref="Agid" />, <see cref="BusKey" />, and <see cref="DriveCertificate" /> are
    ///     populated. Always invalidate the AGID via <see cref="InvalidateAgid" /> when done.
    /// </summary>
    /// <param name="credentials">Host private key and certificate.</param>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns>Outcome of the authentication.</returns>
    public AacsAuthResult Authenticate(AacsHostCredentials credentials, uint timeout)
    {
        BusKey           = null;
        DriveCertificate = null;

        InvalidateAllAgids(timeout);

        if(!ReportAgid(timeout, out byte agid))
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to obtain AGID from drive.");

            return AacsAuthResult.NoAgid;
        }

        Agid = agid;
        AaruLogging.Debug(MODULE_NAME, "Got AGID {0} from drive.", agid);

        byte[] hostNonce = RandomNumberGenerator.GetBytes(20);

        AacsCrypto.CreateHostKeyPair(out byte[] hostScalar, out byte[] hostPoint);

        if(!SendHostCertChallenge(hostNonce, credentials.Certificate, timeout))
        {
            AaruLogging.Debug(MODULE_NAME, "Drive rejected host certificate (revoked or unsupported).");
            InvalidateAgid(timeout);

            return AacsAuthResult.HostCertRejected;
        }

        if(!ReportDriveCertChallenge(out byte[]? driveNonce, out byte[]? driveCert, timeout))
        {
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveError;
        }

        if(driveCert![0] != 0x01)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Unsupported drive certificate type 0x{0:X2} (expected 0x01).",
                              driveCert[0]);
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveCertInvalid;
        }

        if(!AacsCrypto.VerifyAacsCert(driveCert))
        {
            AaruLogging.Debug(MODULE_NAME, "Drive certificate signature is not valid.");
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveCertInvalid;
        }

        DriveCertificate = driveCert;

        if(!ReportDriveKey(out byte[]? drivePoint, out byte[]? driveSig, timeout))
        {
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveError;
        }

        byte[] driveSignedMessage = new byte[60];
        Buffer.BlockCopy(hostNonce,   0, driveSignedMessage, 0,  20);
        Buffer.BlockCopy(drivePoint!, 0, driveSignedMessage, 20, 40);

        byte[] driveX = new byte[20];
        byte[] driveY = new byte[20];
        Buffer.BlockCopy(driveCert, AacsCurve.CERT_PK_X_OFFSET, driveX, 0, 20);
        Buffer.BlockCopy(driveCert, AacsCurve.CERT_PK_Y_OFFSET, driveY, 0, 20);

        if(!AacsCrypto.VerifyAacsSignature(driveX, driveY, driveSignedMessage, driveSig!))
        {
            AaruLogging.Debug(MODULE_NAME, "Drive signature is not valid.");
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveSignatureInvalid;
        }

        byte[] hostSignedMessage = new byte[60];
        Buffer.BlockCopy(driveNonce!, 0, hostSignedMessage, 0,  20);
        Buffer.BlockCopy(hostPoint,   0, hostSignedMessage, 20, 40);

        byte[] hostSignature;

        try
        {
            hostSignature = AacsCrypto.SignAacs(credentials.PrivateKey, hostSignedMessage);
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "Host signing failed: {0}: {1}", ex.GetType().Name, ex.Message);
            InvalidateAgid(timeout);

            return AacsAuthResult.CryptoError;
        }

        if(!SendHostKey(hostPoint, hostSignature, timeout))
        {
            AaruLogging.Debug(MODULE_NAME, "Drive rejected host key/signature.");
            InvalidateAgid(timeout);

            return AacsAuthResult.DriveError;
        }

        byte[] busKey = new byte[16];

        try
        {
            AacsCrypto.DeriveBusKey(hostScalar, drivePoint!, busKey);
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "Bus key derivation failed: {0}", ex.Message);
            InvalidateAgid(timeout);

            return AacsAuthResult.CryptoError;
        }

        BusKey = busKey;

        AaruLogging.Debug(MODULE_NAME, "AACS authentication succeeded; bus key derived.");

        return AacsAuthResult.Success;
    }

    /// <summary>
    ///     Read the AACS Volume Identifier from the drive via READ DISC STRUCTURE format 0x80,
    ///     verifying the CMAC under the bus key. Requires a successful prior call to
    ///     <see cref="Authenticate" />.
    /// </summary>
    /// <param name="kind">HD DVD or BD.</param>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns><c>true</c> on success; <see cref="VolumeIdentifier" /> is populated on success.</returns>
    public bool ReadVolumeId(AacsMediaKind kind, uint timeout)
    {
        VolumeIdentifier = null;

        if(BusKey is null)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadVolumeId called without authentication.");

            return false;
        }

        byte[] buf = new byte[36];

        if(!ReadDiscStructure(kind, MmcDiscStructureFormat.AacsVolId, 0, 0, buf, timeout)) return false;

        byte[] vid = new byte[16];
        byte[] mac = new byte[16];
        Buffer.BlockCopy(buf, 4,  vid, 0, 16);
        Buffer.BlockCopy(buf, 20, mac, 0, 16);

        if(!AacsCrypto.VerifyAacsMac(BusKey, vid, mac))
        {
            AaruLogging.Debug(MODULE_NAME, "VID CMAC mismatch; not accepting Volume ID.");

            return false;
        }

        VolumeIdentifier = vid;

        return true;
    }

    /// <summary>
    ///     Read all packs of the Media Key Block from the drive via READ DISC STRUCTURE format 0x83.
    ///     Concatenates the packs and stores the result in <see cref="MediaKeyBlock" />.
    /// </summary>
    /// <param name="kind">HD DVD or BD.</param>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns><c>true</c> on success; <c>false</c> if the first pack cannot be read.</returns>
    public bool ReadMediaKeyBlock(AacsMediaKind kind, uint timeout)
    {
        MediaKeyBlock = null;

        byte[] buf = new byte[32772];

        if(!ReadDiscStructure(kind, MmcDiscStructureFormat.Aacsmkb, 0, 0, buf, timeout)) return false;

        int len = (buf[0] << 8) | buf[1];
        len -= 2;

        if(len <= 0 || len > 32768) return false;

        byte numPacks = buf[3];

        if(numPacks == 0) numPacks = 1;

        byte[] mkb = new byte[(long)numPacks * 32768];
        int    pos = 0;

        Buffer.BlockCopy(buf, 4, mkb, pos, len);
        pos += len;

        for(int pack = 1; pack < numPacks; pack++)
        {
            byte[] packBuf = new byte[32772];

            if(!ReadDiscStructure(kind, MmcDiscStructureFormat.Aacsmkb, 0, (uint)pack, packBuf, timeout))
            {
                AaruLogging.Debug(MODULE_NAME, "Failed to read MKB pack {0}/{1}; returning partial.", pack, numPacks);

                break;
            }

            int packLen = ((packBuf[0] << 8) | packBuf[1]) - 2;

            if(packLen <= 0 || packLen > 32768) break;

            Buffer.BlockCopy(packBuf, 4, mkb, pos, packLen);
            pos += packLen;
        }

        if(pos < mkb.Length) Array.Resize(ref mkb, pos);

        MediaKeyBlock = mkb;

        return true;
    }

    /// <summary>
    ///     Read the AACS Pre-recorded Media Serial Number from the drive via READ DISC STRUCTURE format 0x81,
    ///     verifying the CMAC under the bus key. Requires a successful prior call to
    ///     <see cref="Authenticate" />.
    /// </summary>
    /// <param name="kind">HD DVD or BD.</param>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns><c>true</c> on success; <c>false</c> if the pre-recorded media serial number cannot be read.</returns>
    public bool ReadPrerecordedMediaSerialNumber(AacsMediaKind kind, uint timeout)
    {
        PrerecordedMediaSerialNumber = null;

        if(BusKey is null)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadPrerecordedMediaSerialNumber called without authentication.");

            return false;
        }

        byte[] buf = new byte[36];

        if(!ReadDiscStructure(kind, MmcDiscStructureFormat.AacsMediaSerial, 0, 0, buf, timeout)) return false;

        byte[] serial = new byte[16];
        byte[] mac = new byte[16];
        Buffer.BlockCopy(buf, 4,  serial, 0, 16);
        Buffer.BlockCopy(buf, 20, mac, 0, 16);

        if(!AacsCrypto.VerifyAacsMac(BusKey, serial, mac))
        {
            AaruLogging.Debug(MODULE_NAME, "Media serial number CMAC mismatch; not accepting media serial number.");

            return false;
        }

        PrerecordedMediaSerialNumber = serial;

        return true;
    }

    /// <summary>
    ///     Read the AACS Media Identifier from the drive via READ DISC STRUCTURE format 0x82,
    ///     verifying the CMAC under the bus key. Requires a successful prior call to
    ///     <see cref="Authenticate" />.
    /// </summary>
    /// <param name="kind">HD DVD or BD.</param>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    /// <returns><c>true</c> on success; <c>false</c> if the media identifier cannot be read.</returns>
    public bool ReadMediaIdentifier(AacsMediaKind kind, uint timeout)
    {
        MediaIdentifier = null;

        if(BusKey is null)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadMediaIdentifier called without authentication.");

            return false;
        }

        byte[] buf = new byte[36];

        if(!ReadDiscStructure(kind, MmcDiscStructureFormat.AacsMediaId, 0, 0, buf, timeout)) return false;

        byte[] identifier = new byte[16];
        byte[] mac = new byte[16];
        Buffer.BlockCopy(buf, 4,  identifier, 0, 16);
        Buffer.BlockCopy(buf, 20, mac, 0, 16);

        if(!AacsCrypto.VerifyAacsMac(BusKey, identifier, mac))
        {
            AaruLogging.Debug(MODULE_NAME, "Media identifier CMAC mismatch; not accepting media identifier.");

            return false;
        }

        MediaIdentifier = identifier;

        return true;
    }

    /// <summary>Invalidate the current AGID, releasing the AACS session resources on the drive.</summary>
    /// <param name="timeout">SCSI timeout in seconds.</param>
    public void InvalidateAgid(uint timeout)
    {
        if(Agid == 0xFF) return;

        ReportKey(Agid, 0, 0, 0x3F, new byte[2], timeout);
        Agid   = 0xFF;
        BusKey = null;
    }

    void InvalidateAllAgids(uint timeout)
    {
        for(byte i = 0; i < 4; i++) ReportKey(i, 0, 0, 0x3F, new byte[2], timeout);
    }

    bool ReportAgid(uint timeout, out byte agid)
    {
        agid = 0;
        byte[] buf = new byte[8];

        if(!ReportKey(0, 0, 0, 0x00, buf, timeout)) return false;

        agid = (byte)((buf[7] & 0xFF) >> 6);

        return true;
    }

    bool SendHostCertChallenge(byte[] hostNonce, byte[] hostCert, uint timeout)
    {
        byte[] buf = new byte[116];
        buf[1] = 0x72;
        Buffer.BlockCopy(hostNonce, 0, buf, 4,  20);
        Buffer.BlockCopy(hostCert,  0, buf, 24, 92);

        return SendKey(Agid, 0x01, buf, timeout);
    }

    bool ReportDriveCertChallenge(out byte[]? driveNonce, out byte[]? driveCert, uint timeout)
    {
        driveNonce = null;
        driveCert  = null;
        byte[] buf = new byte[116];

        if(!ReportKey(Agid, 0, 0, 0x01, buf, timeout)) return false;

        driveNonce = new byte[20];
        driveCert  = new byte[92];
        Buffer.BlockCopy(buf, 4,  driveNonce, 0, 20);
        Buffer.BlockCopy(buf, 24, driveCert,  0, 92);

        return true;
    }

    bool ReportDriveKey(out byte[]? drivePoint, out byte[]? driveSignature, uint timeout)
    {
        drivePoint     = null;
        driveSignature = null;
        byte[] buf = new byte[84];

        if(!ReportKey(Agid, 0, 0, 0x02, buf, timeout)) return false;

        drivePoint     = new byte[40];
        driveSignature = new byte[40];
        Buffer.BlockCopy(buf, 4,  drivePoint,     0, 40);
        Buffer.BlockCopy(buf, 44, driveSignature, 0, 40);

        return true;
    }

    bool SendHostKey(byte[] hostPoint, byte[] hostSignature, uint timeout)
    {
        byte[] buf = new byte[84];
        buf[1] = 0x52;
        Buffer.BlockCopy(hostPoint,     0, buf, 4,  40);
        Buffer.BlockCopy(hostSignature, 0, buf, 44, 40);

        return SendKey(Agid, 0x02, buf, timeout);
    }

    bool ReadDiscStructure(AacsMediaKind kind, MmcDiscStructureFormat format, byte layer, uint address, byte[] buffer,
                           uint timeout)
    {
        Span<byte> cdb = dev.CdbBuffer[..12];
        cdb.Clear();

        cdb[0]  = (byte)ScsiCommands.ReadDiscStructure;
        cdb[1]  = (byte)kind;
        cdb[2]  = (byte)((address & 0xFF000000) >> 24);
        cdb[3]  = (byte)((address & 0x00FF0000) >> 16);
        cdb[4]  = (byte)((address & 0x0000FF00) >> 8);
        cdb[5]  = (byte)(address & 0xFF);
        cdb[6]  = layer;
        cdb[7]  = (byte)format;
        cdb[8]  = (byte)((buffer.Length & 0xFF00) >> 8);
        cdb[9]  = (byte)(buffer.Length & 0xFF);
        cdb[10] = (byte)((Agid & 0x03) << 6);

        dev.SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out double duration, out bool sense);

        AaruLogging.Debug(MODULE_NAME,
                          "READ DISC STRUCTURE format=0x{0:X2} addr={1} layer={2} sense={3} took {4} ms.",
                          (byte)format,
                          address,
                          layer,
                          sense,
                          duration);

        return !sense;
    }

    bool ReportKey(byte agid, uint address, byte blocks, byte format, byte[] buffer, uint timeout)
    {
        Span<byte> cdb = dev.CdbBuffer[..12];
        cdb.Clear();

        cdb[0]  = (byte)ScsiCommands.ReportKey;
        cdb[2]  = (byte)((address & 0xFF000000) >> 24);
        cdb[3]  = (byte)((address & 0x00FF0000) >> 16);
        cdb[4]  = (byte)((address & 0x0000FF00) >> 8);
        cdb[5]  = (byte)(address & 0xFF);
        cdb[6]  = blocks;
        cdb[7]  = 0x02;
        cdb[8]  = (byte)((buffer.Length & 0xFF00) >> 8);
        cdb[9]  = (byte)(buffer.Length & 0xFF);
        cdb[10] = (byte)(((agid & 0x03) << 6) | (format & 0x3F));

        dev.SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out double duration, out bool sense);

        AaruLogging.Debug(MODULE_NAME,
                          "REPORT KEY format=0x{0:X2} agid={1} sense={2} took {3} ms.",
                          format,
                          agid,
                          sense,
                          duration);

        return !sense;
    }

    bool SendKey(byte agid, byte format, byte[] buffer, uint timeout)
    {
        Span<byte> cdb = dev.CdbBuffer[..12];
        cdb.Clear();

        cdb[0]  = (byte)ScsiCommands.SendKey;
        cdb[7]  = 0x02;
        cdb[8]  = (byte)((buffer.Length & 0xFF00) >> 8);
        cdb[9]  = (byte)(buffer.Length & 0xFF);
        cdb[10] = (byte)(((agid & 0x03) << 6) | (format & 0x3F));

        dev.SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.Out, out double duration, out bool sense);

        AaruLogging.Debug(MODULE_NAME,
                          "SEND KEY format=0x{0:X2} agid={1} sense={2} took {3} ms.",
                          format,
                          agid,
                          sense,
                          duration);

        return !sense;
    }
}