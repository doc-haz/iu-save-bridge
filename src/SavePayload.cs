using System;
using System.IO;
using System.Security.Cryptography;

namespace IUSaveBridge
{
    public class SavePayload
    {
        public const int ExpectedSize = 409600; // 0x64000
        public const uint ExpectedMagic = 0x55445356; // "UDSV"
        public const uint ExpectedVersion = 0x00000033;
        public const uint ExpectedTitleId = 0x535107DB;
        public const int DefaultBaseOffset = 0x2800;
        public const int DefaultFolOffset = 0x2898;
        public const int FolOffset = DefaultFolOffset; // Kept for backwards compatibility
        public const int SlotNumberOffset = 0x10;

        public byte[] Data { get; private set; }
        public int BaseOffset { get; private set; }
        public int CurrentFolOffset { get { return BaseOffset + 0x98; } }
        public int InventoryOffset { get { return BaseOffset + 0x3A48; } }
        public int CharacterBaseOffset { get { return BaseOffset + 0x1F0F8; } }

        public uint Magic { get; private set; }
        public uint Version { get; private set; }
        public uint TitleId { get; private set; }
        public uint SlotNumber { get; private set; }
        public uint Fol { get; private set; }
        public uint StoredCrc1 { get; private set; }
        public uint StoredCrc2 { get; private set; }
        public uint CalculatedCrc1 { get; private set; }
        public uint CalculatedCrc2 { get; private set; }
        public string Sha256Hash { get; private set; }

        public SavePayload(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (bytes.Length != ExpectedSize)
            {
                throw new InvalidDataException(string.Format(
                    "Invalid payload size: {0} bytes. Expected exactly {1} bytes (0x64000).",
                    bytes.Length, ExpectedSize));
            }

            Data = (byte[])bytes.Clone();
            ParseAndValidate();
        }

        public static SavePayload FromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Payload file not found: " + path);
            }
            byte[] bytes = File.ReadAllBytes(path);
            return new SavePayload(bytes);
        }

        private void ParseAndValidate()
        {
            Magic = ReadUInt32BE(Data, 0x00);
            if (Magic != ExpectedMagic)
            {
                throw new InvalidDataException(string.Format(
                    "Invalid save magic: 0x{0:X8}. Expected 'UDSV' (0x{1:X8}).",
                    Magic, ExpectedMagic));
            }

            Version = ReadUInt32BE(Data, 0x04);
            if (Version != ExpectedVersion)
            {
                throw new InvalidDataException(string.Format(
                    "Invalid save version: 0x{0:X8}. Expected 0x{1:X8}.",
                    Version, ExpectedVersion));
            }

            uint baseOff = ReadUInt32BE(Data, 0x08);
            if (baseOff >= 0x0100 && baseOff <= 0x20000 && (baseOff % 0x100 == 0))
            {
                BaseOffset = (int)baseOff;
            }
            else
            {
                BaseOffset = DefaultBaseOffset;
            }

            TitleId = ReadUInt32BE(Data, 0x0C);
            SlotNumber = ReadUInt32BE(Data, SlotNumberOffset);
            StoredCrc1 = ReadUInt32BE(Data, 0x14);
            StoredCrc2 = ReadUInt32BE(Data, 0x18);
            Fol = ReadUInt32BE(Data, CurrentFolOffset);

            // Compute CRC32s
            byte[] tempHeader = new byte[232];
            Array.Copy(Data, 0, tempHeader, 0, 232);
            for (int i = 20; i < 28; i++) tempHeader[i] = 0;

            CalculatedCrc1 = ComputeCRC(tempHeader, 0, 232);
            CalculatedCrc2 = ComputeCRC(Data, 232, Data.Length - 232);

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Data);
                Sha256Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        public void SetFol(uint newFol)
        {
            WriteUInt32BE(Data, CurrentFolOffset, newFol);
            WriteUInt32BE(Data, 0x24, newFol);
            Fol = newFol;
            RecalculateChecksums();
        }

        public void SetSlotNumber(uint newSlot)
        {
            WriteUInt32BE(Data, SlotNumberOffset, newSlot);
            SlotNumber = newSlot;
            RecalculateChecksums();
        }

        public void RecalculateChecksums()
        {
            // Clear CRC fields
            for (int i = 20; i < 28; i++) Data[i] = 0;

            byte[] tempHeader = new byte[232];
            Array.Copy(Data, 0, tempHeader, 0, 232);
            for (int i = 20; i < 28; i++) tempHeader[i] = 0;

            CalculatedCrc1 = ComputeCRC(tempHeader, 0, 232);
            CalculatedCrc2 = ComputeCRC(Data, 232, Data.Length - 232);

            WriteUInt32BE(Data, 0x14, CalculatedCrc1);
            WriteUInt32BE(Data, 0x18, CalculatedCrc2);
            StoredCrc1 = CalculatedCrc1;
            StoredCrc2 = CalculatedCrc2;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Data);
                Sha256Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        public void SaveToFile(string path, string inputPathForSafety = null)
        {
            SafePath.EnsureSafeOutputPath(path, inputPathForSafety);
            SafePath.AtomicSave(Data, path);
        }

        public CharacterData GetCharacter(int index)
        {
            return CharacterData.ReadFrom(Data, index, CharacterBaseOffset);
        }

        public CharacterData[] GetAllCharacters()
        {
            return CharacterData.ReadAllFrom(Data, CharacterBaseOffset);
        }

        public void SaveCharacter(CharacterData character)
        {
            if (character == null) throw new ArgumentNullException("character");
            character.WriteTo(Data, CharacterBaseOffset);
            RecalculateChecksums();
        }

        public System.Collections.Generic.List<ItemData> GetAllItems()
        {
            return ItemData.ReadAllFrom(Data, InventoryOffset);
        }

        public void SaveItem(ItemData item)
        {
            if (item == null) throw new ArgumentNullException("item");
            ItemData.WriteItem(Data, item, InventoryOffset);
            RecalculateChecksums();
        }

        public void SaveItems(System.Collections.Generic.IEnumerable<ItemData> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            foreach (var item in items)
            {
                ItemData.WriteItem(Data, item, InventoryOffset);
            }
            RecalculateChecksums();
        }

        // Tri-Ace CRC32 (polynomial: 0x04C11DB7)
        public static uint ComputeCRC(byte[] buffer, int offset, int length)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                uint val = ((uint)b << 24) | ((uint)b >> 8);
                crc ^= val;
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x80000000) != 0)
                    {
                        crc = (crc * 2) ^ 0x04C11DB7;
                    }
                    else
                    {
                        crc = crc * 2;
                    }
                }
            }
            return ~crc;
        }

        public static uint ReadUInt32BE(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) |
                   ((uint)buffer[offset + 1] << 16) |
                   ((uint)buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }

        public static void WriteUInt32BE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
