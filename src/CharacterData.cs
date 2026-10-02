using System;

namespace IUSaveBridge
{
    public class CharacterData
    {
        public static readonly string[] Names = new string[]
        {
            "Capell", "Aya", "Eugene", "Michelle", "Kiriya", "Sigmund",
            "Edward", "Komachi", "Rico", "Rucha", "Kristofer", "Balbagan",
            "Touma", "Savio", "Vic", "Gustav", "Dominica", "Seraphina"
        };

        public const int CharacterStride = 4840;
        public const int BaseOffset = 0x218F8;

        public int Index { get; set; }
        public string Name { get; set; }
        public uint CharacterId { get; set; }

        public bool InParty { get; set; }
        public bool InActiveParty { get; set; }

        public uint Level { get; set; }
        public uint Exp { get; set; }
        public uint CurrentHp { get; set; }
        public uint MaxHp { get; set; }
        public uint CurrentMp { get; set; }
        public uint MaxMp { get; set; }
        public int Ap { get; set; }

        public uint Atk { get; set; }
        public uint Def { get; set; }
        public uint Agl { get; set; }
        public uint Hit { get; set; }
        public uint Int { get; set; }

        public int Offset
        {
            get { return BaseOffset + (Index * CharacterStride); }
        }

        public static CharacterData ReadFrom(byte[] data, int index)
        {
            if (index < 0 || index >= Names.Length)
                throw new ArgumentOutOfRangeException("index");

            int off = BaseOffset + (index * CharacterStride);
            CharacterData c = new CharacterData();
            c.Index = index;
            c.Name = Names[index];

            c.CharacterId = ReadU32(data, off + 0x00);
            ushort partyFlags = ReadU16(data, off + 0x08);
            ushort activeFlags = ReadU16(data, off + 0x0A);
            c.InParty = (partyFlags & 0x0800) != 0;
            c.InActiveParty = (activeFlags & 0x0001) != 0;

            c.Level = ReadU32(data, off + 0x18);
            c.Exp = ReadU32(data, off + 0x24);
            c.MaxHp = ReadU32(data, off + 0x28);
            c.CurrentHp = ReadU32(data, off + 0x30);
            c.CurrentMp = ReadU32(data, off + 0x34) / 1000;
            c.MaxMp = ReadU32(data, off + 0x38) / 1000;
            c.Ap = ReadI32(data, off + 0x40);

            c.Atk = ReadU32(data, off + 0x4C);
            c.Def = ReadU32(data, off + 0x58);
            c.Agl = ReadU32(data, off + 0x64);
            c.Hit = ReadU32(data, off + 0x94);
            c.Int = ReadU32(data, off + 0xA0);

            return c;
        }

        public void WriteTo(byte[] data)
        {
            int off = Offset;

            // In Party flag (0x0800)
            ushort curParty = ReadU16(data, off + 0x08);
            if (InParty) curParty |= 0x0800;
            else curParty = (ushort)(curParty & ~0x0800);
            WriteU16(data, off + 0x08, curParty);

            // In Active Party flag (0x0001)
            ushort curActive = ReadU16(data, off + 0x0A);
            if (InActiveParty) curActive |= 0x0001;
            else curActive = (ushort)(curActive & ~0x0001);
            WriteU16(data, off + 0x0A, curActive);

            WriteU32(data, off + 0x18, Level);
            WriteU32(data, off + 0x24, Exp);

            WriteU32(data, off + 0x28, MaxHp);
            WriteU32(data, off + 0x2C, MaxHp);
            WriteU32(data, off + 0x30, CurrentHp);

            WriteU32(data, off + 0x34, CurrentMp * 1000);
            WriteU32(data, off + 0x38, MaxMp * 1000);
            WriteU32(data, off + 0x3C, MaxMp * 1000);

            WriteI32(data, off + 0x40, Ap);
            WriteI32(data, off + 0x44, Ap);
            WriteI32(data, off + 0x48, Ap);

            WriteU32(data, off + 0x4C, Atk);
            WriteU32(data, off + 0x58, Def);
            WriteU32(data, off + 0x64, Agl);
            WriteU32(data, off + 0x94, Hit);
            WriteU32(data, off + 0xA0, Int);
        }

        private static uint ReadU32(byte[] b, int o)
        {
            return (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);
        }

        private static int ReadI32(byte[] b, int o)
        {
            return (int)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);
        }

        private static ushort ReadU16(byte[] b, int o)
        {
            return (ushort)((b[o] << 8) | b[o + 1]);
        }

        private static void WriteU32(byte[] b, int o, uint val)
        {
            b[o] = (byte)(val >> 24);
            b[o + 1] = (byte)(val >> 16);
            b[o + 2] = (byte)(val >> 8);
            b[o + 3] = (byte)val;
        }

        private static void WriteI32(byte[] b, int o, int val)
        {
            WriteU32(b, o, (uint)val);
        }

        private static void WriteU16(byte[] b, int o, ushort val)
        {
            b[o] = (byte)(val >> 8);
            b[o + 1] = (byte)val;
        }
    }
}
