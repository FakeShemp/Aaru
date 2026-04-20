// ReSharper disable InconsistentNaming

using Aaru.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Aaru.Tests.Devices.SecureDigital;

[TestFixture]
public class CSD
{
    readonly string[] cards =
    [
        "microsdhc_goodram_16gb", "microsdhc_kingston_4gb", "microsdhc_kingston_8gb", "microsdhc_kodak_2gb",
        "microsdhc_nobrand_2gb", "microsdhc_sandisk_16gb", "microsdhc_sandisk_32gb", "microsdhc_trascend_2gb",
        "sd_adata_4gb", "sdhc_fujifilm_4gb", "sdhc_kodak_4gb", "sdhc_pny_4gb", "sdhc_puntitos_4gb", "sd_pqi_64mb"
    ];

    readonly string[] csds =
    [
        "400e00325b590000740f7f800a400001", "400e00325b5900001d877f800a400001", "400e00325b5900003b677f800a400001",
        "002601325b5a83c7f6dbff9f16804001", "002e00325b5a83a9ffffff8016800001", "400e00325b59000076b27f800a404001",
        "400e00325b590000edc87f800a404001", "007fff325b5a83baf6dbdfff0e800001", "005e0032575b83d56db7ffff96c00001",
        "400e00325b5900001da77f800a400001", "400e00325b5900001deb7f800a400001", "400e00325b5900001d8a7f800a404001",
        "400e00325b5900001dbf7f800a400001", "002d0032135983c9f6d9cf8016400001"
    ];

    readonly byte[] structure_versions = [1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0];

    readonly byte[] taacs = [14, 14, 14, 38, 46, 14, 14, 127, 94, 14, 14, 14, 14, 45];

    readonly byte[] nsacs = [0, 0, 0, 1, 0, 0, 0, 255, 0, 0, 0, 0, 0, 0];

    readonly byte[] speeds =
    [
        // ReSharper disable once UseUtf8StringLiteral
        50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50
    ];

    readonly ushort[] classes = [1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1397, 1461, 1461, 1461, 1461, 309];

    readonly byte[] read_block_lengths = [9, 9, 9, 10, 10, 9, 9, 10, 11, 9, 9, 9, 9, 9];

    readonly bool[] read_partial_blocks =
    [
        false, false, false, true, true, false, false, true, true, false, false, false, false, true
    ];

    readonly bool[] write_misaligned_block = new bool[14];

    readonly bool[] read_misaligned_block = new bool[14];

    readonly bool[] dsr_implemented = new bool[14];

    readonly uint[] card_sizes =
    [
        29711, 7559, 15207, 3871, 3751, 30386, 60872, 3819, 3925, 7591, 7659, 7562, 7615, 3879
    ];

    readonly byte[] min_read_current = [0, 0, 0, 6, 7, 0, 0, 6, 5, 0, 0, 0, 0, 6];

    readonly byte[] max_read_current = [0, 0, 0, 6, 7, 0, 0, 6, 5, 0, 0, 0, 0, 6];

    readonly byte[] min_write_current = [0, 0, 0, 6, 7, 0, 0, 6, 5, 0, 0, 0, 0, 6];

    readonly byte[] max_write_current = [0, 0, 0, 6, 7, 0, 0, 6, 5, 0, 0, 0, 0, 6];

    readonly byte[] size_multiplier = [0, 0, 0, 7, 7, 0, 0, 7, 7, 0, 0, 0, 0, 3];

    readonly bool[] erase_block_enable =
    [
        true, true, true, true, true, true, true, true, true, true, true, true, true, true
    ];

    readonly byte[] erase_sector_sizes = [127, 127, 127, 127, 127, 127, 127, 63, 127, 127, 127, 127, 127, 31];

    readonly byte[] write_protect_group_size = [0, 0, 0, 31, 0, 0, 0, 127, 127, 0, 0, 0, 0, 0];

    readonly bool[] write_protect_group_enable =
    [
        false, false, false, false, false, false, false, false, true, false, false, false, false, false
    ];

    readonly byte[] r2w_factors = [2, 2, 2, 5, 5, 2, 2, 3, 5, 2, 2, 2, 2, 5];

    readonly bool[] file_format_group =
    [
        false, false, false, false, false, false, false, false, false, false, false, false, false, false
    ];

    readonly bool[] copy =
    [
        false, false, false, true, false, true, true, false, false, false, false, true, false, false, false
    ];

    readonly bool[] permanent_write_protect = new bool[14];

    readonly bool[] temporary_write_protect = new bool[14];

    readonly byte[] file_format = new byte[14];

    [Test]
    public void Test()
    {
        for(var i = 0; i < cards.Length; i++)
        {
            using(new AssertionScope())
            {
                int count = Marshal.ConvertFromHexAscii(csds[i], out byte[] response);
                count.Should().Be(16, string.Format(Localization.Size_0, cards[i]));
                Decoders.SecureDigital.CSD csd = Decoders.SecureDigital.Decoders.DecodeCSD(response);
                csd.Should().NotBeNull(Localization.Decoded_0, cards[i]);

                csd.Structure.Should().Be(structure_versions[i], string.Format(Localization.Version_0, cards[i]));

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

                csd.Size.Should().Be(card_sizes[i], string.Format(Localization.Card_size_0, cards[i]));

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

                csd.EraseBlockEnable.Should()
                   .Be(erase_block_enable[i], string.Format(Localization.Erase_block_enable_0, cards[i]));

                csd.EraseSectorSize.Should()
                   .Be(erase_sector_sizes[i], string.Format(Localization.Erase_sector_size_0, cards[i]));

                csd.WriteProtectGroupSize.Should()
                   .Be(write_protect_group_size[i], string.Format(Localization.Write_protect_group_size_0, cards[i]));

                csd.WriteProtectGroupEnable.Should()
                   .Be(write_protect_group_enable[i],
                       string.Format(Localization.Write_protect_group_enable_0, cards[i]));

                csd.WriteSpeedFactor.Should()
                   .Be(r2w_factors[i], string.Format(Localization.Read_to_write_factor_0, cards[i]));

                csd.FileFormatGroup.Should()
                   .Be(file_format_group[i], string.Format(Localization.File_format_group_0, cards[i]));

                csd.Copy.Should().Be(copy[i], string.Format(Localization.Copy_0, cards[i]));

                csd.PermanentWriteProtect.Should()
                   .Be(permanent_write_protect[i], string.Format(Localization.Permanent_write_protect_0, cards[i]));

                csd.TemporaryWriteProtect.Should()
                   .Be(temporary_write_protect[i], string.Format(Localization.Temporary_write_protect_0, cards[i]));

                csd.FileFormat.Should().Be(file_format[i], string.Format(Localization.File_format_0, cards[i]));
            }
        }
    }
}