// ReSharper disable InconsistentNaming

using Aaru.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

// ReSharper disable UseUtf8StringLiteral

namespace Aaru.Tests.Devices.MultiMediaCard;

[TestFixture]
public class CSD
{
    readonly string[] cards = ["mmc_6600_32mb", "mmc_pretec_32mb", "mmc_takems_256mb"];

    readonly string[] csds =
    [
        "8c26012a0f5901e9f6d983e392404001", "8c0e012a0ff981e9f6d981e18a400001", "905e002a1f5983d3edb683ff96400001"
    ];

    readonly byte[] structure_versions = [2, 2, 2];

    readonly byte[] spec_versions = [3, 3, 4];

    readonly byte[] taacs = [38, 14, 94];

    readonly byte[] nsacs = [1, 1, 0];

    readonly byte[] speeds = [42, 42, 42];

    readonly ushort[] classes = [245, 255, 501];

    readonly byte[] read_block_lengths = [9, 9, 9];

    readonly bool[] read_partial_blocks = [false, true, true];

    readonly bool[] write_misaligned_block = new bool[3];

    readonly bool[] read_misaligned_block = new bool[3];

    readonly bool[] dsr_implemented = new bool[3];

    readonly uint[] card_sizes = [1959, 1959, 3919];

    readonly byte[] min_read_current = [6, 6, 5];

    readonly byte[] max_read_current = [6, 6, 5];

    readonly byte[] min_write_current = [6, 6, 5];

    readonly byte[] max_write_current = [6, 6, 5];

    readonly byte[] size_multiplier = [3, 3, 5];

    readonly byte[] sector_sizes = new byte[3];

    readonly byte[] erase_sector_sizes = [31, 15, 31];

    readonly byte[] write_protect_group_size = [3, 1, 31];

    readonly bool[] write_protect_group_enable = [true, true, true];

    readonly byte[] default_eccs = new byte[3];

    readonly byte[] r2w_factors = [4, 2, 5];

    readonly byte[] write_block_lengths = [9, 9, 9];

    readonly bool[] write_partial_blocks = new bool[3];

    readonly bool[] file_format_group = new bool[3];

    readonly bool[] copy = [true, false, false];

    readonly bool[] permanent_write_protect = new bool[3];

    readonly bool[] temporary_write_protect = new bool[3];

    readonly byte[] file_format = new byte[3];

    readonly byte[] ecc = new byte[3];

    [Test]
    public void Test()
    {
        for(var i = 0; i < cards.Length; i++)
        {
            using(new AssertionScope())
            {
                int count = Marshal.ConvertFromHexAscii(csds[i], out byte[] response);
                count.Should().Be(16, string.Format(Localization.Size_0, cards[i]));
                Decoders.MMC.CSD csd = Decoders.MMC.Decoders.DecodeCSD(response);
                csd.Should().NotBeNull(Localization.Decoded_0, cards[i]);

                csd.Structure.Should()
                   .Be(structure_versions[i], string.Format(Localization.Structure_version_0, cards[i]));

                csd.Version.Should()
                   .Be(spec_versions[i], string.Format(Localization.Specification_version_0, cards[i]));

                csd.TAAC.Should().Be(taacs[i], string.Format(Localization.TAAC_0, cards[i]));
                csd.NSAC.Should().Be(nsacs[i], string.Format(Localization.NSAC_0, cards[i]));

                csd.Speed.Should().Be(speeds[i], string.Format(Localization.Transfer_speed_0, cards[i]));

                csd.Classes.Should().Be(classes[i], string.Format(Localization.Classes_0, cards[i]));

                csd.ReadBlockLength.Should()
                   .Be(read_block_lengths[i], string.Format(Localization.Read_block_length_0, cards[i]));

                csd.ReadsPartialBlocks.Should()
                   .Be(read_partial_blocks[i], string.Format(Localization.Reads_partial_blocks_0, cards[i]));

                csd.WriteMisalignment.Should()
                   .Be(write_misaligned_block[i], string.Format(Localization.Writes_misaligned_blocks_0, cards[i]));

                csd.ReadMisalignment.Should()
                   .Be(read_misaligned_block[i], string.Format(Localization.Reads_misaligned_blocks_0, cards[i]));

                csd.DSRImplemented.Should()
                   .Be(dsr_implemented[i], string.Format(Localization.DSR_implemented_0, cards[i]));

                csd.Size.Should().Be((ushort)card_sizes[i], string.Format(Localization.Card_size_0, cards[i]));

                csd.ReadCurrentAtVddMin.Should()
                   .Be(min_read_current[i], string.Format(Localization.Reading_current_at_minimum_Vdd_0, cards[i]));

                csd.ReadCurrentAtVddMax.Should()
                   .Be(max_read_current[i], string.Format(Localization.Reading_current_at_maximum_Vdd_0, cards[i]));

                csd.WriteCurrentAtVddMin.Should()
                   .Be(min_write_current[i], string.Format(Localization.Writing_current_at_minimum_Vdd_0, cards[i]));

                csd.WriteCurrentAtVddMax.Should()
                   .Be(max_write_current[i], string.Format(Localization.Writing_current_at_maximum_Vdd_0, cards[i]));

                csd.SizeMultiplier.Should()
                   .Be(size_multiplier[i], string.Format(Localization.Card_size_multiplier_0, cards[i]));

                csd.EraseGroupSize.Should()
                   .Be(sector_sizes[i], string.Format(Localization.Erase_sector_size_0, cards[i]));

                csd.EraseGroupSizeMultiplier.Should()
                   .Be(erase_sector_sizes[i], string.Format(Localization.Erase_group_size_0, cards[i]));

                csd.WriteProtectGroupSize.Should()
                   .Be(write_protect_group_size[i], string.Format(Localization.Write_protect_group_size_0, cards[i]));

                csd.WriteProtectGroupEnable.Should()
                   .Be(write_protect_group_enable[i],
                       string.Format(Localization.Write_protect_group_enable_0, cards[i]));

                csd.DefaultECC.Should().Be(default_eccs[i], string.Format(Localization.Default_ECC_0, cards[i]));

                csd.WriteSpeedFactor.Should()
                   .Be(r2w_factors[i], string.Format(Localization.Read_to_write_factor_0, cards[i]));

                csd.WriteBlockLength.Should()
                   .Be(write_block_lengths[i], string.Format(Localization.write_block_length_0, cards[i]));

                csd.WritesPartialBlocks.Should()
                   .Be(write_partial_blocks[i], string.Format(Localization.Writes_partial_blocks_0, cards[i]));

                csd.FileFormatGroup.Should()
                   .Be(file_format_group[i], string.Format(Localization.File_format_group_0, cards[i]));

                csd.Copy.Should().Be(copy[i], string.Format(Localization.Copy_0, cards[i]));

                csd.PermanentWriteProtect.Should()
                   .Be(permanent_write_protect[i], string.Format(Localization.Permanent_write_protect_0, cards[i]));

                csd.TemporaryWriteProtect.Should()
                   .Be(temporary_write_protect[i], string.Format(Localization.Temporary_write_protect_0, cards[i]));

                csd.FileFormat.Should().Be(file_format[i], string.Format(Localization.File_format_0, cards[i]));

                csd.ECC.Should().Be(ecc[i], string.Format(Localization.ECC_0, cards[i]));
            }
        }
    }
}