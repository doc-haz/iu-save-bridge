using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IUSaveBridge
{
    public class ItemData
    {
        public const int BaseOffset = 0x6248;
        public const int ItemStride = 8;
        public const int ItemCount = 1023;

        private static string[] s_itemNames = null;

        public ushort Id { get; set; }
        public string Name { get; set; }
        public ushort Amount { get; set; }
        public uint Flags { get; set; }
        public int Offset { get; set; }

        public static string[] LoadCatalog()
        {
            if (s_itemNames != null) return s_itemNames;

            // 1. Try embedded manifest resource
            try
            {
                Assembly asm = typeof(ItemData).Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                foreach (string res in resNames)
                {
                    if (res.EndsWith("ItemNames.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream s = asm.GetManifestResourceStream(res))
                        using (StreamReader reader = new StreamReader(s))
                        {
                            List<string> lines = new List<string>();
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                lines.Add(line);
                            }
                            if (lines.Count >= ItemCount)
                            {
                                s_itemNames = lines.ToArray();
                                return s_itemNames;
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Try relative disk paths
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] searchPaths = new string[]
            {
                Path.Combine(baseDir, @"resources\ItemNames.txt"),
                Path.Combine(baseDir, @"ItemNames.txt")
            };

            foreach (string p in searchPaths)
            {
                if (File.Exists(p))
                {
                    try
                    {
                        s_itemNames = File.ReadAllLines(p);
                        return s_itemNames;
                    }
                    catch { }
                }
            }

            // Fallback placeholder names
            s_itemNames = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                s_itemNames[i] = string.Format("Item #{0:D4}", i + 1);
            }
            return s_itemNames;
        }

        public static List<ItemData> ReadAllFrom(byte[] data, int baseOffset = BaseOffset)
        {
            string[] names = LoadCatalog();
            List<ItemData> list = new List<ItemData>(ItemCount);

            for (int i = 0; i < ItemCount; i++)
            {
                int off = baseOffset + (i * ItemStride);
                ushort id = ReadU16(data, off + 0x00);
                ushort amount = ReadU16(data, off + 0x02);
                uint flags = ReadU32(data, off + 0x04);

                string name = (i < names.Length && !string.IsNullOrEmpty(names[i])) ? names[i] : string.Format("Item #{0:D4}", i + 1);

                ItemData item = new ItemData
                {
                    Id = (ushort)(i + 1),
                    Name = name,
                    Amount = amount,
                    Flags = flags,
                    Offset = off
                };
                list.Add(item);
            }

            return list;
        }

        public static void WriteItem(byte[] data, ItemData item, int baseOffset = BaseOffset)
        {
            int off = baseOffset + ((item.Id - 1) * ItemStride);
            if (item.Amount > 0)
            {
                WriteU16(data, off + 0x00, item.Id);
                WriteU16(data, off + 0x02, item.Amount);
                WriteU32(data, off + 0x04, 16777472); // 0x01000100
            }
            else
            {
                WriteU16(data, off + 0x00, 0);
                WriteU16(data, off + 0x02, 0);
                WriteU32(data, off + 0x04, 0);
            }
        }

        private static ushort ReadU16(byte[] b, int o)
        {
            return (ushort)((b[o] << 8) | b[o + 1]);
        }

        private static uint ReadU32(byte[] b, int o)
        {
            return (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);
        }

        private static void WriteU16(byte[] b, int o, ushort val)
        {
            b[o] = (byte)(val >> 8);
            b[o + 1] = (byte)val;
        }

        private static void WriteU32(byte[] b, int o, uint val)
        {
            b[o] = (byte)(val >> 24);
            b[o + 1] = (byte)(val >> 16);
            b[o + 2] = (byte)(val >> 8);
            b[o + 3] = (byte)val;
        }
    }
}
