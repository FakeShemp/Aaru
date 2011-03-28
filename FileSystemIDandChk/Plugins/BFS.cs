using System;
using System.IO;
using System.Text;
using FileSystemIDandChk;

// Information from Inside Macintosh

namespace FileSystemIDandChk.Plugins
{
	class BeFS : Plugin
	{
		public BeFS(PluginBase Core)
        {
            base.Name = "Be Filesystem";
			base.PluginUUID = new Guid("dc8572b3-b6ad-46e4-8de9-cbe123ff6672");
        }
		
		public override bool Identify(FileStream stream, long offset)
		{
			UInt32 magic;
			
			BinaryReader br = new BinaryReader(stream);
			
			br.BaseStream.Seek(32 + offset, SeekOrigin.Begin); // Seek to magic
			
			magic = br.ReadUInt32();
			
			if(magic == 0x42465331) // Little-endian BFS
				return true;
			else if(magic == 0x31534642) // Big-endian BFS
				return true;
			else
				return false;
		}
		
		public override void GetInformation (FileStream stream, long offset, out string information)
		{
			information = "";
			byte[] name_bytes = new byte[32];
			
			StringBuilder sb = new StringBuilder();
			
			BeSuperBlock besb = new BeSuperBlock();
			
			BinaryReader br = new BinaryReader(stream);
			br.BaseStream.Seek(offset, SeekOrigin.Begin);
			name_bytes = br.ReadBytes(32);
			
			besb.name = StringHandlers.CToString(name_bytes);
			besb.magic1 = br.ReadUInt32();
			besb.fs_byte_order = br.ReadUInt32();
			besb.block_size = br.ReadUInt32();
			besb.block_shift = br.ReadUInt32();
			besb.num_blocks = br.ReadInt64();
			besb.used_blocks = br.ReadInt64();
			besb.inode_size = br.ReadInt32();
			besb.magic2 = br.ReadUInt32();
			besb.blocks_per_ag = br.ReadInt32();
			besb.ag_shift = br.ReadInt32();
			besb.num_ags = br.ReadInt32();
			besb.flags = br.ReadUInt32();
			besb.log_blocks_ag = br.ReadInt32();
			besb.log_blocks_start = br.ReadUInt16();
			besb.log_blocks_len = br.ReadUInt16();
			besb.log_start = br.ReadInt64();
			besb.log_end = br.ReadInt64();
			besb.magic3 = br.ReadUInt32();
			besb.root_dir_ag = br.ReadInt32();
			besb.root_dir_start = br.ReadUInt16();
			besb.root_dir_len = br.ReadUInt16();
			besb.indices_ag = br.ReadInt32();
			besb.indices_start = br.ReadUInt16();
			besb.indices_len = br.ReadUInt16();
			
			if(besb.magic1 == 0x31534642 && besb.fs_byte_order == 0x45474942) // Big-endian filesystem
			{
				sb.AppendLine("Big-endian BeFS");
				
				// Swap everything in the super block
				byte[] sixteen_bits = new byte[2];
				byte[] thirtytwo_bits = new byte[4];
				byte[] sixtyfour_bits = new byte[8];
				
				thirtytwo_bits = BitConverter.GetBytes(besb.magic1);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.magic1 = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.fs_byte_order);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.fs_byte_order = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.block_size);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.block_size = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.block_shift);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.block_shift = BitConverter.ToUInt32(thirtytwo_bits, 0);
				sixtyfour_bits = BitConverter.GetBytes(besb.num_blocks);
				sixtyfour_bits = Swapping.SwapEightBytes(sixtyfour_bits);
				besb.num_blocks = BitConverter.ToInt64(sixtyfour_bits, 0);
				sixtyfour_bits = BitConverter.GetBytes(besb.used_blocks);
				sixtyfour_bits = Swapping.SwapEightBytes(sixtyfour_bits);
				besb.used_blocks = BitConverter.ToInt64(sixtyfour_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.inode_size);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.inode_size = BitConverter.ToInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.magic2);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.magic2 = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.blocks_per_ag);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.blocks_per_ag = BitConverter.ToInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.ag_shift);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.ag_shift = BitConverter.ToInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.num_ags);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.num_ags = BitConverter.ToInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.flags);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.flags = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.log_blocks_ag);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.log_blocks_ag = BitConverter.ToInt32(thirtytwo_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.log_blocks_start);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.log_blocks_start = BitConverter.ToUInt16(sixteen_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.log_blocks_len);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.log_blocks_len = BitConverter.ToUInt16(sixteen_bits, 0);
				sixtyfour_bits = BitConverter.GetBytes(besb.log_start);
				sixtyfour_bits = Swapping.SwapEightBytes(sixtyfour_bits);
				besb.log_start = BitConverter.ToInt64(sixtyfour_bits, 0);
				sixtyfour_bits = BitConverter.GetBytes(besb.log_end);
				sixtyfour_bits = Swapping.SwapEightBytes(sixtyfour_bits);
				besb.log_end = BitConverter.ToInt64(sixtyfour_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.magic3);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.magic3 = BitConverter.ToUInt32(thirtytwo_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.root_dir_ag);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.root_dir_ag = BitConverter.ToInt32(thirtytwo_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.root_dir_start);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.root_dir_start = BitConverter.ToUInt16(sixteen_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.root_dir_len);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.root_dir_len = BitConverter.ToUInt16(sixteen_bits, 0);
				thirtytwo_bits = BitConverter.GetBytes(besb.indices_ag);
				thirtytwo_bits = Swapping.SwapFourBytes(thirtytwo_bits);
				besb.indices_ag = BitConverter.ToInt32(thirtytwo_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.indices_start);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.indices_start = BitConverter.ToUInt16(sixteen_bits, 0);
				sixteen_bits = BitConverter.GetBytes(besb.indices_len);
				sixteen_bits = Swapping.SwapTwoBytes(sixteen_bits);
				besb.indices_len = BitConverter.ToUInt16(sixteen_bits, 0);
			}
			else
				sb.AppendLine("Little-endian BeFS");
			
			if(besb.magic1 != 0x42465331 || besb.fs_byte_order != 0x42494745 ||
			   besb.magic2 != 0xDD121031 || besb.magic3 != 0x15B6830E ||
			   besb.root_dir_len != 1 || besb.indices_len != 1 ||
			   (1 << (int)besb.block_shift) != besb.block_size)
			{
				sb.AppendLine("Superblock seems corrupt, following information may be incorrect");
				sb.AppendFormat("Magic 1: 0x{0:X8} (Should be 0x42465331)", besb.magic1).AppendLine();
				sb.AppendFormat("Magic 2: 0x{0:X8} (Should be 0xDD121031)", besb.magic2).AppendLine();
				sb.AppendFormat("Magic 3: 0x{0:X8} (Should be 0x15B6830E)", besb.magic3).AppendLine();
				sb.AppendFormat("Filesystem endianness: 0x{0:X8} (Should be 0x42494745)", besb.fs_byte_order).AppendLine();
				sb.AppendFormat("Root folder's i-node size: {0} blocks (Should be 1)", besb.root_dir_len).AppendLine();
				sb.AppendFormat("Indices' i-node size: {0} blocks (Should be 1)", besb.indices_len).AppendLine();
				sb.AppendFormat("1 << block_shift == block_size => 1 << {0} == {1} (Should be {2})", besb.block_shift,
				                1 << (int)besb.block_shift, besb.block_size).AppendLine();
			}
			
			if(besb.flags == 0x434C454E)
			{
				if(besb.log_start == besb.log_end)
					sb.AppendLine("Filesystem is clean");
				else
					sb.AppendLine("Filesystem is dirty");
			}
			else if(besb.flags == 0x44495254)
				sb.AppendLine("Filesystem is dirty");
			else
				sb.AppendFormat("Unknown flags: {0:X8}", besb.flags).AppendLine();
			
			sb.AppendFormat("Volume name: {0}", besb.name).AppendLine();
			sb.AppendFormat("{0} bytes per block", besb.block_size).AppendLine();
			sb.AppendFormat("{0} blocks in volume ({1} bytes)", besb.num_blocks, besb.num_blocks*besb.block_size).AppendLine();
			sb.AppendFormat("{0} used blocks ({1} bytes)", besb.used_blocks, besb.used_blocks*besb.block_size).AppendLine();
			sb.AppendFormat("{0} bytes per i-node", besb.inode_size).AppendLine();
			sb.AppendFormat("{0} blocks per allocation group ({1} bytes)", besb.blocks_per_ag, besb.blocks_per_ag*besb.block_size).AppendLine();
			sb.AppendFormat("{0} allocation groups in volume", besb.num_ags).AppendLine();
			sb.AppendFormat("Journal resides in block {0} of allocation group {1} and runs for {2} blocks ({3} bytes)", besb.log_blocks_start,
			                besb.log_blocks_ag, besb.log_blocks_len, besb.log_blocks_len*besb.block_size).AppendLine();
			sb.AppendFormat("Journal starts in byte {0} and ends in byte {1}", besb.log_start, besb.log_end).AppendLine();
			sb.AppendFormat("Root folder's i-node resides in block {0} of allocation group {1} and runs for {2} blocks ({3} bytes)", besb.root_dir_start,
			                besb.root_dir_ag, besb.root_dir_len, besb.root_dir_len*besb.block_size).AppendLine();
			sb.AppendFormat("Indices' i-node resides in block {0} of allocation group {1} and runs for {2} blocks ({3} bytes)", besb.indices_start,
			                besb.indices_ag, besb.indices_len, besb.indices_len*besb.block_size).AppendLine();
			
			information = sb.ToString();
		}
		
		private struct BeSuperBlock
		{
			public string name;             // Volume name, 32 bytes
			public UInt32 magic1;           // "BFS1", 0x42465331
			public UInt32 fs_byte_order;    // "BIGE", 0x42494745
			public UInt32 block_size;       // Bytes per block
			public UInt32 block_shift;      // 1 << block_shift == block_size
			public Int64  num_blocks;       // Blocks in volume
			public Int64  used_blocks;      // Used blocks in volume
			public Int32  inode_size;       // Bytes per inode
			public UInt32 magic2;           // 0xDD121031
			public Int32  blocks_per_ag;    // Blocks per allocation group
			public Int32  ag_shift;         // 1 << ag_shift == blocks_per_ag
			public Int32  num_ags;          // Allocation groups in volume
			public UInt32 flags;            // 0x434c454e if clean, 0x44495254 if dirty
			public Int32  log_blocks_ag;    // Allocation group of journal
			public UInt16 log_blocks_start; // Start block of journal, inside ag
			public UInt16 log_blocks_len;   // Length in blocks of journal, inside ag
			public Int64  log_start;        // Start of journal
			public Int64  log_end;          // End of journal
			public UInt32 magic3;           // 0x15B6830E
			public Int32  root_dir_ag;      // Allocation group where root folder's i-node resides
			public UInt16 root_dir_start;   // Start in ag of root folder's i-node
			public UInt16 root_dir_len;     // As this is part of inode_addr, this is 1
			public Int32  indices_ag;       // Allocation group where indices' i-node resides
			public UInt16 indices_start;    // Start in ag of indices' i-node
			public UInt16 indices_len;      // As this is part of inode_addr, this is 1
		}
	}
}

