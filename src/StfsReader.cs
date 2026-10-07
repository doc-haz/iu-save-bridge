using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IUSaveBridge
{
    public class StfsSaveInfo
    {
        public string FilePath { get; set; }
        public string Magic { get; set; }
        public uint TitleId { get; set; }
        public uint ContentType { get; set; }
        public uint MediaId { get; set; }
        public string DisplayName { get; set; }
        public string TitleName { get; set; }
        public byte[] ThumbnailPng { get; set; }
        public byte[] RawPayloadBytes { get; set; }
        public byte[] ConvertedPayloadBytes { get; set; }
        public byte[] PayloadBytes { get; set; }
        public SavePayload Payload { get; set; }
        public uint OriginalSlot { get; set; }
        public uint Fol { get; set; }
        public uint CapellLevel { get; set; }
        public bool Crc1Valid { get; set; }
        public bool Crc2Valid { get; set; }
        public string DetectedProfile { get; set; }
    }

    public static class StfsReader
    {
        public const string InnerFileName = "InfiniteUndiscovery.dat";
        public const int BlockSize = 4096;
        public const uint StfsMagicCon = 0x434F4E20;   // "CON "
        public const uint StfsMagicLive = 0x4C495645;  // "LIVE"
        public const uint StfsMagicPirs = 0x50495253;  // "PIRS"
        public const uint InfiniteUndiscoveryTitleId = 0x535107DB;
        public const uint SavedGameContentType = 0x00000001;

        public static bool IsStfsContainer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length < 0xA000) return false;

                byte[] magicBuf = new byte[4];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Read(magicBuf, 0, 4) != 4) return false;
                }

                uint magic = SavePayload.ReadUInt32BE(magicBuf, 0);
                return (magic == StfsMagicCon || magic == StfsMagicLive || magic == StfsMagicPirs);
            }
            catch
            {
                return false;
            }
        }

        public static StfsSaveInfo Read(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Xbox 360 container file not found: " + filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < 0xA000)
                {
                    throw new InvalidDataException("File is too small to be a valid Xbox 360 STFS container.");
                }

                // 1. Magic
                byte[] magicBuf = new byte[4];
                fs.Read(magicBuf, 0, 4);
                uint magic = SavePayload.ReadUInt32BE(magicBuf, 0);
                if (magic != StfsMagicCon && magic != StfsMagicLive && magic != StfsMagicPirs)
                {
                    throw new InvalidDataException(string.Format(
                        "File is not a valid Xbox 360 STFS container. Magic: 0x{0:X8}", magic));
                }

                string magicStr = Encoding.ASCII.GetString(magicBuf);

                // 2. Header metadata
                byte[] metaBuf = new byte[64];
                fs.Seek(0x0340, SeekOrigin.Begin);
                fs.Read(metaBuf, 0, 64);

                uint headerSize = SavePayload.ReadUInt32BE(metaBuf, 0); // 0x0340
                uint contentType = SavePayload.ReadUInt32BE(metaBuf, 4); // 0x0344
                uint mediaId = SavePayload.ReadUInt32BE(metaBuf, 24); // 0x0358
                uint titleId = SavePayload.ReadUInt32BE(metaBuf, 32); // 0x0360

                if (titleId != InfiniteUndiscoveryTitleId)
                {
                    throw new InvalidDataException(string.Format(
                        "Container Title ID mismatch: 0x{0:X8}. Expected 0x{1:X8} (Infinite Undiscovery).",
                        titleId, InfiniteUndiscoveryTitleId));
                }

                // Display names
                string displayName = "";
                string titleName = "";
                try
                {
                    byte[] nameBuf = new byte[128];
                    fs.Seek(0x0411, SeekOrigin.Begin);
                    int read = fs.Read(nameBuf, 0, nameBuf.Length);
                    if (read > 0) displayName = Encoding.BigEndianUnicode.GetString(nameBuf).TrimEnd('\0');

                    fs.Seek(0x0D11, SeekOrigin.Begin);
                    read = fs.Read(nameBuf, 0, nameBuf.Length);
                    if (read > 0) titleName = Encoding.BigEndianUnicode.GetString(nameBuf).TrimEnd('\0');
                }
                catch { }

                // 3. STFS Descriptor & Block Mapping
                uint baseBlock = (headerSize + 4095) & 0xF000;
                fs.Seek(0x0379, SeekOrigin.Begin);
                byte[] desc = new byte[36];
                fs.Read(desc, 0, 36);

                byte structureShift = desc[2];
                int shift = (structureShift & 1) != 0 ? 0 : 1;
                ushort fileTableBlockCount = (ushort)(desc[3] | (desc[4] << 8));
                uint fileTableBlockNumber = (uint)(desc[5] | (desc[6] << 8) | (desc[7] << 16));

                long fileTableOffset = baseBlock + ((long)(fileTableBlockNumber + ((fileTableBlockNumber / 170 + 1) << shift))) * BlockSize;
                fs.Seek(fileTableOffset, SeekOrigin.Begin);

                byte[] fileTable = new byte[BlockSize * Math.Max(1, (int)fileTableBlockCount)];
                fs.Read(fileTable, 0, fileTable.Length);

                int foundEntryOffset = -1;
                uint startingBlock = 0;
                int fileSize = 0;

                for (int entry = 0; entry < (fileTable.Length / 64); entry++)
                {
                    int offset = entry * 64;
                    byte flag = fileTable[offset + 40];
                    int nameLen = flag & 0x3F;
                    if (nameLen > 0 && nameLen <= 40)
                    {
                        string name = Encoding.ASCII.GetString(fileTable, offset, nameLen);
                        if (string.Equals(name, InnerFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            foundEntryOffset = offset;
                            startingBlock = (uint)(fileTable[offset + 47] | (fileTable[offset + 48] << 8) | (fileTable[offset + 49] << 16));
                            fileSize = (int)SavePayload.ReadUInt32BE(fileTable, offset + 52);
                            break;
                        }
                    }
                }

                if (foundEntryOffset == -1)
                {
                    throw new FileNotFoundException(
                        "Internal file '" + InnerFileName + "' was not found inside the STFS container.");
                }

                if (fileSize != SavePayload.ExpectedSize)
                {
                    throw new InvalidDataException(string.Format(
                        "Extracted payload has invalid size: {0} bytes (expected {1} bytes).",
                        fileSize, SavePayload.ExpectedSize));
                }

                // 4. Extract Payload Bytes following the actual STFS block chain
                byte[] payloadBytes = new byte[fileSize];
                int blocksToRead = (fileSize + BlockSize - 1) / BlockSize;

                List<uint> blockChain = ReadBlockChain(fs, startingBlock, blocksToRead, baseBlock, shift);
                if (blockChain.Count < blocksToRead)
                {
                    throw new InvalidDataException(string.Format(
                        "Incomplete STFS block chain: found {0} blocks, expected {1}.",
                        blockChain.Count, blocksToRead));
                }

                for (int b = 0; b < blocksToRead; b++)
                {
                    uint logicalBlock = blockChain[b];
                    long hashBlocksBefore = ((logicalBlock / 170) + 1) << shift;
                    long blockPhysOffset = baseBlock + ((long)(logicalBlock + hashBlocksBefore)) * BlockSize;
                    fs.Seek(blockPhysOffset, SeekOrigin.Begin);
                    int toRead = Math.Min(BlockSize, fileSize - (b * BlockSize));
                    fs.Read(payloadBytes, b * BlockSize, toRead);
                }

                // 5. Extract Thumbnail PNG if present
                byte[] thumbnailPng = null;
                try
                {
                    fs.Seek(0x1712, SeekOrigin.Begin);
                    byte[] thumbLenBuf = new byte[4];
                    if (fs.Read(thumbLenBuf, 0, 4) == 4)
                    {
                        uint thumbLen = SavePayload.ReadUInt32BE(thumbLenBuf, 0);
                        if (thumbLen > 0 && thumbLen < 500000 && (0x171A + thumbLen <= fs.Length))
                        {
                            fs.Seek(0x171A, SeekOrigin.Begin);
                            byte[] thumbBuf = new byte[thumbLen];
                            if (fs.Read(thumbBuf, 0, (int)thumbLen) == (int)thumbLen)
                            {
                                // Validate PNG signature
                                if (thumbBuf.Length >= 8 &&
                                    thumbBuf[0] == 0x89 && thumbBuf[1] == 0x50 &&
                                    thumbBuf[2] == 0x4E && thumbBuf[3] == 0x47 &&
                                    thumbBuf[4] == 0x0D && thumbBuf[5] == 0x0A &&
                                    thumbBuf[6] == 0x1A && thumbBuf[7] == 0x0A)
                                {
                                    thumbnailPng = thumbBuf;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 6. Parse intact payload directly with dynamic SavePayload
                SavePayload payload = new SavePayload(payloadBytes);

                // Main character level
                uint capellLevel = 0;
                try
                {
                    CharacterData capell = payload.GetCharacter(0);
                    capellLevel = capell.Level;
                }
                catch { }

                // Profile detection. Xbox 360 media IDs for Infinite Undiscovery are only reliably
                // known for the retail NTSC-U disc, which maps to the modern USA profile. Other
                // editions are ambiguous from the container alone, so we default to USA and let
                // the user pick the exact target profile in the import dialog.
                string detectedProfile = SaveManager.ProfileUsa;
                if (mediaId == 0x20854892 || mediaId == 0x2B0C46F0)
                {
                    detectedProfile = SaveManager.ProfileUsa;
                }

                return new StfsSaveInfo
                {
                    FilePath = filePath,
                    Magic = magicStr,
                    TitleId = titleId,
                    ContentType = contentType,
                    MediaId = mediaId,
                    DisplayName = displayName,
                    TitleName = titleName,
                    ThumbnailPng = thumbnailPng,
                    RawPayloadBytes = payloadBytes,
                    ConvertedPayloadBytes = payloadBytes,
                    PayloadBytes = payloadBytes,
                    Payload = payload,
                    OriginalSlot = payload.SlotNumber,
                    Fol = payload.Fol,
                    CapellLevel = capellLevel,
                    Crc1Valid = (payload.StoredCrc1 == payload.CalculatedCrc1),
                    Crc2Valid = (payload.StoredCrc2 == payload.CalculatedCrc2),
                    DetectedProfile = detectedProfile
                };
            }
        }

        public static List<uint> ReadBlockChain(FileStream fs, uint startingBlock, int blocksToRead, uint baseBlock, int shift)
        {
            return ReadBlockChain((Stream)fs, startingBlock, blocksToRead, baseBlock, shift);
        }

        public static List<uint> ReadBlockChain(Stream fs, uint startingBlock, int blocksToRead, uint baseBlock, int shift)
        {
            List<uint> blocks = new List<uint>();
            HashSet<uint> visited = new HashSet<uint>();
            uint currentBlock = startingBlock;
            byte[] buf = new byte[4];

            while (blocks.Count < blocksToRead)
            {
                if (visited.Contains(currentBlock))
                {
                    break;
                }

                blocks.Add(currentBlock);
                visited.Add(currentBlock);

                long group = currentBlock / 170L;
                long recordIndex = currentBlock % 170L;
                long l0PhysBlock = group * (170L + (1 << shift));
                long recordOffset = baseBlock + (l0PhysBlock * BlockSize) + (recordIndex * 24L) + 20L;

                if (recordOffset + 4 > fs.Length) break;
                fs.Seek(recordOffset, SeekOrigin.Begin);
                if (fs.Read(buf, 0, 4) != 4) break;

                uint nextVal = SavePayload.ReadUInt32BE(buf, 0);
                uint nextBlock = nextVal & 0x00FFFFFF;

                if (nextBlock >= 0x00FFFFF0 || nextBlock == 0)
                {
                    break;
                }

                currentBlock = nextBlock;
            }

            return blocks;
        }
    }
}
