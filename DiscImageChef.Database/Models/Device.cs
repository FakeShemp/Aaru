using System;
using DiscImageChef.CommonTypes.Metadata;

namespace DiscImageChef.Database.Models
{
    public class Device : DeviceReportV2
    {
        public Device(DeviceReportV2 report)
        {
            ATA              = report.ATA;
            ATAPI            = report.ATA;
            CompactFlash     = report.CompactFlash;
            FireWire         = report.FireWire;
            LastSynchronized = DateTime.UtcNow;
            MultiMediaCard   = report.MultiMediaCard;
            PCMCIA           = report.PCMCIA;
            SCSI             = report.SCSI;
            SecureDigital    = report.SecureDigital;
            USB              = report.USB;
        }

        public DateTime LastSynchronized { get; set; }
    }
}