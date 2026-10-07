using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using IUSaveBridge;

class TestSuite
{
    private static int s_passed = 0;
    private static int s_failed = 0;
    private static string s_recomp = null;
    private static string s_legacy = null;
    private static string s_mixed = null;

    private const string User = "1234567890ABCDEF";

    [STAThread]
    static int Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  IU Save Bridge v2.3.0 - Comprehensive Test Suite");
        Console.WriteLine("  (profile model v1.0.0-rc1 + legacy NTSC-U/PAL)");
        Console.WriteLine("=================================================");

        if (args.Length >= 3)
        {
            s_recomp = Path.GetFullPath(args[0]);
            s_legacy = Path.GetFullPath(args[1]);
            s_mixed = Path.GetFullPath(args[2]);
        }
        else
        {
            string baseDir = SaveManager.AppBaseDir;
            s_recomp = Path.Combine(baseDir, "MockRecomp");
            s_legacy = Path.Combine(baseDir, "MockLegacyRecomp");
            s_mixed = Path.Combine(baseDir, "MockMixedRecomp");
        }

        // ---------------------------------------------------------------
        // 1-5. New profile discovery (USA, USA-UNDUB, EUROPE, JAPAN, ASIA)
        // ---------------------------------------------------------------
        Test("1. USA profile discovery", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_recomp);
            Assert(profiles.Contains(SaveManager.ProfileUsa), "USA profile must be detected");
            Assert(SaveManager.ScanSlots(s_recomp, SaveManager.ProfileUsa).Count >= 2, "USA must expose >= 2 slots");
        });

        Test("2. USA-UNDUB profile discovery", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_recomp);
            Assert(profiles.Contains(SaveManager.ProfileUsaUndub), "USA-UNDUB profile must be detected");
            Assert(SaveManager.ScanSlots(s_recomp, SaveManager.ProfileUsaUndub).Count >= 1, "USA-UNDUB must expose >= 1 slot");
        });

        Test("3. EUROPE profile discovery", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_recomp);
            Assert(profiles.Contains(SaveManager.ProfileEurope), "EUROPE profile must be detected");
            Assert(SaveManager.ScanSlots(s_recomp, SaveManager.ProfileEurope).Count >= 1, "EUROPE must expose >= 1 slot");
        });

        Test("4. JAPAN profile discovery", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_recomp);
            Assert(profiles.Contains(SaveManager.ProfileJapan), "JAPAN profile must be detected");
            Assert(SaveManager.ScanSlots(s_recomp, SaveManager.ProfileJapan).Count >= 1, "JAPAN must expose >= 1 slot");
        });

        Test("5. ASIA profile discovery", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_recomp);
            Assert(profiles.Contains(SaveManager.ProfileAsia), "ASIA profile must be detected");
            Assert(SaveManager.ScanSlots(s_recomp, SaveManager.ProfileAsia).Count >= 1, "ASIA must expose >= 1 slot");
        });

        // ---------------------------------------------------------------
        // 6-7. Legacy mapping NTSC-U -> USA, PAL -> EUROPE
        // ---------------------------------------------------------------
        Test("6. Legacy NTSC-U maps to USA", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_legacy);
            Assert(profiles.Contains(SaveManager.ProfileUsa), "Legacy NTSC-U install must surface as USA");
            Assert(!profiles.Contains("NTSC-U"), "Raw legacy name NTSC-U must not be surfaced");

            List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_legacy, SaveManager.ProfileUsa);
            Assert(slots.Count == 1, "Legacy USA must expose 1 slot, found: " + slots.Count);
            Assert(slots[0].IsLegacyProfile, "Slot from NTSC-U must be flagged as legacy");
        });

        Test("7. Legacy PAL maps to EUROPE", () => {
            List<string> profiles = SaveManager.GetAvailableProfiles(s_legacy);
            Assert(profiles.Contains(SaveManager.ProfileEurope), "Legacy PAL install must surface as EUROPE");
            Assert(!profiles.Contains("PAL"), "Raw legacy name PAL must not be surfaced");

            List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_legacy, SaveManager.ProfileEurope);
            Assert(slots.Count == 1, "Legacy EUROPE must expose 1 slot, found: " + slots.Count);
            Assert(slots[0].IsLegacyProfile, "Slot from PAL must be flagged as legacy");
        });

        // ---------------------------------------------------------------
        // 8-9. Preference of native folder over legacy when both exist
        // ---------------------------------------------------------------
        Test("8. Prefer USA over NTSC-U when both exist", () => {
            string dir = SaveManager.GetProfileSavesDir(s_mixed, SaveManager.ProfileUsa);
            Assert(string.Equals(Path.GetFileName(Path.GetDirectoryName(dir)), "USA", StringComparison.OrdinalIgnoreCase),
                "USA must win over NTSC-U, resolved: " + dir);

            List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_mixed, SaveManager.ProfileUsa);
            Assert(slots.Count == 1, "Mixed USA must expose exactly 1 slot");
            Assert(slots[0].Fol == 111111, "Mixed USA must read the USA save (Fol 111111), got: " + slots[0].Fol);
            Assert(!slots[0].IsLegacyProfile, "Preferred USA slot must not be flagged legacy");
        });

        Test("9. Prefer EUROPE over PAL when both exist", () => {
            string dir = SaveManager.GetProfileSavesDir(s_mixed, SaveManager.ProfileEurope);
            Assert(string.Equals(Path.GetFileName(Path.GetDirectoryName(dir)), "EUROPE", StringComparison.OrdinalIgnoreCase),
                "EUROPE must win over PAL, resolved: " + dir);

            List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_mixed, SaveManager.ProfileEurope);
            Assert(slots.Count == 1, "Mixed EUROPE must expose exactly 1 slot");
            Assert(slots[0].Fol == 333333, "Mixed EUROPE must read the EUROPE save (Fol 333333), got: " + slots[0].Fol);
        });

        // ---------------------------------------------------------------
        // 10-12. config.json: new profile key + legacy region fallback
        // ---------------------------------------------------------------
        Test("10. New config.json uses \"profile\" key", () => {
            AppConfig c = new AppConfig();
            c.Language = "en";
            c.RecompPath = s_recomp;
            c.Profile = SaveManager.ProfileJapan;
            SaveManager.SaveConfig(c);

            string raw = File.ReadAllText(SaveManager.ConfigFilePath);
            Assert(raw.Contains("\"profile\""), "config.json must persist a profile key");
            Assert(!raw.Contains("\"region\""), "config.json must not persist a legacy region key");

            AppConfig loaded = SaveManager.LoadConfig();
            Assert(loaded.Profile == SaveManager.ProfileJapan, "Profile JAPAN must round-trip");
        });

        Test("11. Legacy config region=NTSC-U maps to USA", () => {
            File.WriteAllText(SaveManager.ConfigFilePath,
                "{\r\n  \"language\": \"en\",\r\n  \"recompPath\": \"\",\r\n  \"region\": \"NTSC-U\"\r\n}\r\n");
            AppConfig loaded = SaveManager.LoadConfig();
            Assert(loaded.Profile == SaveManager.ProfileUsa, "region=NTSC-U must map to USA, got: " + loaded.Profile);
        });

        Test("12. Legacy config region=PAL maps to EUROPE", () => {
            File.WriteAllText(SaveManager.ConfigFilePath,
                "{\r\n  \"language\": \"en\",\r\n  \"recompPath\": \"\",\r\n  \"region\": \"PAL\"\r\n}\r\n");
            AppConfig loaded = SaveManager.LoadConfig();
            Assert(loaded.Profile == SaveManager.ProfileEurope, "region=PAL must map to EUROPE, got: " + loaded.Profile);
        });

        // ---------------------------------------------------------------
        // 13-14. Backups per profile + legacy backup visibility
        // ---------------------------------------------------------------
        Test("13. Backups are stored per profile", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileJapan, 1);
            int before = SaveManager.ScanBackups(SaveManager.ProfileJapan).Count;
            string bkp = SaveManager.CreateBackup(src, SaveManager.ProfileJapan, "InfiniteUndiscovery_0001.bin", 1);
            Assert(File.Exists(bkp), "Backup file must exist");
            Assert(bkp.IndexOf(Path.Combine("backups", SaveManager.ProfileJapan), StringComparison.OrdinalIgnoreCase) >= 0,
                "Backup must live under backups\\JAPAN, got: " + bkp);
            int after = SaveManager.ScanBackups(SaveManager.ProfileJapan).Count;
            Assert(after == before + 1, "JAPAN backup list must grow by 1");
        });

        Test("14. Legacy backups (backups\\NTSC-U) visible as USA", () => {
            string legacyDir = Path.Combine(SaveManager.BackupsBaseDir, "NTSC-U");
            Directory.CreateDirectory(legacyDir);
            string legacyName = "InfiniteUndiscovery_0001_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_legacytest.bin";
            string legacyPath = Path.Combine(legacyDir, legacyName);
            File.Copy(SavePath(s_recomp, SaveManager.ProfileUsa, 1), legacyPath, true);

            try
            {
                List<BackupItemInfo> us = SaveManager.ScanBackups(SaveManager.ProfileUsa);
                BackupItemInfo found = null;
                foreach (BackupItemInfo b in us)
                {
                    if (string.Equals(b.FileName, legacyName, StringComparison.OrdinalIgnoreCase)) { found = b; break; }
                }
                Assert(found != null, "Legacy NTSC-U backup must be visible while scanning USA");
                Assert(found.Profile == SaveManager.ProfileUsa, "Legacy backup must be reported as USA");
                Assert(found.IsLegacyBackup, "Legacy backup must be flagged as legacy");
            }
            finally
            {
                if (File.Exists(legacyPath)) File.Delete(legacyPath);
            }
        });

        // ---------------------------------------------------------------
        // 15. DLC / shared directory isolation
        // ---------------------------------------------------------------
        Test("15. No DLC directory false positives", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_recomp, SaveManager.ProfileUsa);
            Assert(slots.Count == 2, "USA must expose exactly the 2 real slots, found: " + slots.Count);
            foreach (SaveSlotInfo slot in slots)
            {
                Assert(!slot.DirectoryPath.Contains("0000000000000000"), "Shared/DLC XUID must be ignored: " + slot.DirectoryPath);
                Assert(!slot.DirectoryPath.Contains("00000002"), "DLC content folder must be ignored: " + slot.DirectoryPath);
            }
        });

        // ---------------------------------------------------------------
        // 16-20. Save edit, backup, restore, CRC and SHA refresh
        // ---------------------------------------------------------------
        Test("16. Save edit creates a mandatory backup", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileUsa, 2);
            SavePayload p = SavePayload.FromFile(src);
            p.SetFol(43210987);

            int before = SaveManager.ScanBackups(SaveManager.ProfileUsa).Count;
            SaveManager.SavePayloadDirect(p, src, SaveManager.ProfileUsa, "InfiniteUndiscovery_0002.bin", 2);
            int after = SaveManager.ScanBackups(SaveManager.ProfileUsa).Count;

            Assert(after == before + 1, "A backup must be created before writing");
            Assert(SavePayload.FromFile(src).Fol == 43210987, "Fol must be persisted");
        });

        Test("17. Restore creates a safety backup", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileUsa, 2);
            List<BackupItemInfo> bkps = SaveManager.ScanBackups(SaveManager.ProfileUsa);
            BackupItemInfo newest = bkps[0];
            int before = bkps.Count;

            SaveManager.RestoreBackup(newest.FullPath, src, SaveManager.ProfileUsa, "InfiniteUndiscovery_0002.bin", 2);
            int after = SaveManager.ScanBackups(SaveManager.ProfileUsa).Count;
            Assert(after == before + 1, "Restore must create a safety backup of the live save first");
        });

        Test("18. CRC1 valid after write", () => {
            SavePayload p = SavePayload.FromFile(SavePath(s_recomp, SaveManager.ProfileUsa, 2));
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 must be valid on disk");
        });

        Test("19. CRC2 valid after write", () => {
            SavePayload p = SavePayload.FromFile(SavePath(s_recomp, SaveManager.ProfileUsa, 2));
            Assert(p.StoredCrc2 == p.CalculatedCrc2, "CRC2 must be valid on disk");
        });

        Test("20. SHA-256 refreshed after edit", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileUsa, 2);
            SavePayload p = SavePayload.FromFile(src);
            p.SetFol(24681357);
            SaveManager.SavePayloadDirect(p, src, SaveManager.ProfileUsa, "InfiniteUndiscovery_0002.bin", 2);

            SavePayload onDisk = SavePayload.FromFile(src);
            byte[] bytes = File.ReadAllBytes(src);
            string expected;
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                expected = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToUpperInvariant();
            }
            Assert(onDisk.Sha256Hash == expected, "SHA-256 must match the file on disk");
        });

        // ---------------------------------------------------------------
        // 21. Portability regardless of the current working directory
        // ---------------------------------------------------------------
        Test("21. Portable with different working directory", () => {
            string current = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                string appBase = SaveManager.AppBaseDir;
                Assert(Directory.Exists(appBase), "AppBaseDir must resolve regardless of CWD");
                Assert(SaveManager.BackupsBaseDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase),
                    "Backups must stay relative to the application base");
                Assert(SaveManager.ConfigFilePath.StartsWith(appBase, StringComparison.OrdinalIgnoreCase),
                    "config.json must stay next to the executable");
            }
            finally
            {
                Directory.SetCurrentDirectory(current);
            }
        });

        // ---------------------------------------------------------------
        // 22. EN/ES localization parity
        // ---------------------------------------------------------------
        Test("22. EN/ES localization parity", () => {
            int count = 0;
            foreach (string key in Loc.GetAllKeys())
            {
                count++;
                Assert(Loc.HasKey("en", key), "Missing EN key: " + key);
                Assert(Loc.HasKey("es", key), "Missing ES key: " + key);
            }
            Assert(count >= 70, "Expected a rich key set, got: " + count);
        });

        // ---------------------------------------------------------------
        // 23. Zero external footprint
        // ---------------------------------------------------------------
        Test("23. No AppData / Documents writes", () => {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string cfg = SaveManager.ConfigFilePath;

            Assert(cfg.StartsWith(SaveManager.AppBaseDir, StringComparison.OrdinalIgnoreCase), "config.json must sit next to the exe");
            Assert(!cfg.StartsWith(appData, StringComparison.OrdinalIgnoreCase), "Config must not live in AppData");
            Assert(!cfg.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase), "Config must not live in LocalAppData");
            Assert(!cfg.StartsWith(docs, StringComparison.OrdinalIgnoreCase), "Config must not live in Documents");
        });

        // ---------------------------------------------------------------
        // 24. UI profile selector
        // ---------------------------------------------------------------
        Test("24. UI exposes a 5-profile selector", () => {
            using (MainForm form = new MainForm())
            {
                Assert(form.ProfileChoiceCount == 5, "UI must offer exactly 5 profiles, got: " + form.ProfileChoiceCount);
                Assert(SaveManager.IsSupportedProfile(form.ActiveProfile), "Active profile must be a supported code");
                Assert(form.TabCtrl.TabPages.Count == 5, "UI must have exactly 5 tabs");
                foreach (TabPage tp in form.TabCtrl.TabPages)
                {
                    Assert(!tp.Text.Contains("Region"), "Tab text must not mention Region: " + tp.Text);
                }
            }
        });

        // ---------------------------------------------------------------
        // 25. Invalid save rejection
        // ---------------------------------------------------------------
        Test("25. Invalid save rejection (magic + size)", () => {
            string badMagic = Path.Combine(SaveManager.AppBaseDir, "bad_magic.dat");
            byte[] bm = new byte[SavePayload.ExpectedSize];
            bm[0] = 0x58; bm[1] = 0x58; bm[2] = 0x58; bm[3] = 0x58;
            File.WriteAllBytes(badMagic, bm);
            bool threw = false;
            try { SavePayload.FromFile(badMagic); } catch { threw = true; }
            Assert(threw, "Invalid magic must be rejected");

            string badSize = Path.Combine(SaveManager.AppBaseDir, "bad_size.dat");
            File.WriteAllBytes(badSize, new byte[1000]);
            threw = false;
            try { SavePayload.FromFile(badSize); } catch { threw = true; }
            Assert(threw, "Invalid size must be rejected");
        });

        // ---------------------------------------------------------------
        // Extra regression coverage (kept from the v2.2.0 suite)
        // ---------------------------------------------------------------
        Test("26. Edit character stats with CRC integrity", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileUsa, 1);
            SavePayload p = SavePayload.FromFile(src);
            CharacterData capell = p.GetCharacter(0);
            capell.Level = 99;
            capell.CurrentHp = 9999;
            capell.MaxHp = 9999;
            capell.Atk = 999;
            p.SaveCharacter(capell);
            Assert(p.GetCharacter(0).Level == 99, "Level edit mismatch");
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 mismatch after char edit");
            Assert(p.StoredCrc2 == p.CalculatedCrc2, "CRC2 mismatch after char edit");
        });

        Test("27. Edit inventory with CRC integrity", () => {
            string src = SavePath(s_recomp, SaveManager.ProfileUsa, 1);
            SavePayload p = SavePayload.FromFile(src);
            List<ItemData> items = p.GetAllItems();
            Assert(items.Count == 1023, "Catalog must have 1,023 items, got: " + items.Count);
            items[0].Amount = 77;
            p.SaveItem(items[0]);
            Assert(p.GetAllItems()[0].Amount == 77, "Item amount mismatch");
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 mismatch after item edit");
        });

        Test("28. STFS fragmented block-chain traversal", () => {
            byte[] originalPayload = new byte[SavePayload.ExpectedSize];
            originalPayload[0] = 0x55; originalPayload[1] = 0x44; originalPayload[2] = 0x53; originalPayload[3] = 0x56;
            SavePayload.WriteUInt32BE(originalPayload, 4, 0x33);
            SavePayload.WriteUInt32BE(originalPayload, 8, 0x5C00);
            SavePayload.WriteUInt32BE(originalPayload, 0x10, 3);
            SavePayload.WriteUInt32BE(originalPayload, 0x5C98, 7777777);

            int capOff = 0x5C00 + 0x1F0F8;
            SavePayload.WriteUInt32BE(originalPayload, capOff + 0x00, 1);
            originalPayload[capOff + 0x08] = 0x08;
            originalPayload[capOff + 0x0A] = 0x01;
            originalPayload[capOff + 0x1B] = 99;

            SavePayload origP = new SavePayload(originalPayload);
            origP.RecalculateChecksums();
            byte[] payloadData = origP.Data;

            int numBlocks = 100;
            List<uint> blockOrder = new List<uint>();
            for (uint i = 1; i <= (uint)numBlocks; i += 2) blockOrder.Add(i);
            for (uint i = 2; i <= (uint)numBlocks; i += 2) blockOrder.Add(i);

            int totalSize = 0xA000 + (2 + 1 + numBlocks) * 4096;
            byte[] container = new byte[totalSize];

            container[0] = (byte)'C'; container[1] = (byte)'O'; container[2] = (byte)'N'; container[3] = (byte)' ';
            SavePayload.WriteUInt32BE(container, 0x0340, 0x9200);
            SavePayload.WriteUInt32BE(container, 0x0344, 1);
            SavePayload.WriteUInt32BE(container, 0x0358, 0x20854892);
            SavePayload.WriteUInt32BE(container, 0x0360, 0x535107DB);

            container[0x0379 + 2] = 0;
            container[0x0379 + 3] = 1;

            SavePayload.WriteUInt32BE(container, 0xA000 + 20, 0x80FFFFFF);
            SavePayload.WriteUInt32BE(container, 0xB000 + 20, 0x80FFFFFF);

            for (int k = 0; k < blockOrder.Count; k++)
            {
                uint currentB = blockOrder[k];
                uint nextB = (k == blockOrder.Count - 1) ? 0x00FFFFFF : blockOrder[k + 1];
                uint nextPtr = 0x80000000 | nextB;
                SavePayload.WriteUInt32BE(container, 0xA000 + (int)(currentB * 24) + 20, nextPtr);
                SavePayload.WriteUInt32BE(container, 0xB000 + (int)(currentB * 24) + 20, nextPtr);
            }

            int ftOff = 0xC000;
            byte[] fn = Encoding.ASCII.GetBytes("InfiniteUndiscovery.dat");
            Array.Copy(fn, 0, container, ftOff, fn.Length);
            container[ftOff + 40] = (byte)(fn.Length | 0x40);
            container[ftOff + 41] = (byte)numBlocks;
            container[ftOff + 44] = (byte)numBlocks;
            uint startB = blockOrder[0];
            container[ftOff + 47] = (byte)(startB & 0xFF);
            container[ftOff + 48] = (byte)((startB >> 8) & 0xFF);
            container[ftOff + 49] = (byte)((startB >> 16) & 0xFF);
            container[ftOff + 50] = 0xFF; container[ftOff + 51] = 0xFF;
            SavePayload.WriteUInt32BE(container, ftOff + 52, (uint)payloadData.Length);

            for (int k = 0; k < blockOrder.Count; k++)
            {
                uint logicalB = blockOrder[k];
                int physOff = 0xA000 + (int)(logicalB + 2) * 4096;
                Array.Copy(payloadData, k * 4096, container, physOff, 4096);
            }

            string tempFile = Path.Combine(Path.GetTempPath(), "test_frag_" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(tempFile, container);

            try
            {
                StfsSaveInfo info = StfsReader.Read(tempFile);
                Assert(info.OriginalSlot == 3, "OriginalSlot must be 3");
                Assert(info.Fol == 7777777, "Fol must be 7,777,777");
                Assert(info.CapellLevel == 99, "Capell Level must be 99");
                Assert(info.Crc1Valid && info.Crc2Valid, "Both CRC1 and CRC2 must be valid");
                Assert(info.DetectedProfile == SaveManager.ProfileUsa, "Detected profile must be USA");
                Assert(info.ConvertedPayloadBytes.Length == payloadData.Length, "Payload length mismatch");
                for (int i = 0; i < payloadData.Length; i++)
                {
                    if (info.ConvertedPayloadBytes[i] != payloadData[i])
                        throw new Exception(string.Format("Byte mismatch at 0x{0:X5}", i));
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        });

        Test("29. Save discovery ignores invalid payload files", () => {
            string dir = Path.Combine(s_recomp, SaveManager.ProfileAsia, "saves", User, SaveManager.TitleIdHex, "00000001", "InfiniteUndiscovery_0099.bin");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "InfiniteUndiscovery.dat"), new byte[1234]);
            try
            {
                List<SaveSlotInfo> slots = SaveManager.ScanSlots(s_recomp, SaveManager.ProfileAsia);
                foreach (SaveSlotInfo s in slots)
                {
                    Assert(s.SlotNumber != 99, "Invalid payload must not be discovered as a slot");
                }
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        });

        Test("30. achievements folder excluded from discovery", () => {
            // The decoy exists on disk and is a structurally valid save-shaped payload; only the
            // explicit achievements exclusion must keep it out of the slot list.
            string decoy = Path.Combine(s_recomp, "USA", "saves", SaveManager.AchievementsFolderName,
                SaveManager.TitleIdHex, "00000001", "InfiniteUndiscovery_0001.bin", "InfiniteUndiscovery.dat");
            Assert(File.Exists(decoy), "Achievements decoy fixture must exist on disk: " + decoy);

            List<SaveSlotInfo> us = SaveManager.ScanSlots(s_recomp, SaveManager.ProfileUsa);
            Assert(us.Count == 2, "USA must expose only the 2 real XUID slots, got: " + us.Count);
            foreach (SaveSlotInfo sl in us)
            {
                Assert(sl.DirectoryPath.IndexOf(@"\achievements\", StringComparison.OrdinalIgnoreCase) < 0,
                    "Achievements path must never be discovered: " + sl.DirectoryPath);
            }

            // A normal save under a real XUID must still be discovered.
            bool hasXuidSlot = false;
            foreach (SaveSlotInfo sl in us)
            {
                if (sl.DirectoryPath.IndexOf(User, StringComparison.OrdinalIgnoreCase) >= 0) { hasXuidSlot = true; break; }
            }
            Assert(hasXuidSlot, "Normal save under XUID must still appear");

            // Legacy NTSC-U install: the same decoy must not add an extra slot.
            List<SaveSlotInfo> lg = SaveManager.ScanSlots(s_legacy, SaveManager.ProfileUsa);
            Assert(lg.Count == 1, "Legacy USA must expose 1 slot despite the achievements decoy, got: " + lg.Count);
            foreach (SaveSlotInfo sl in lg)
            {
                Assert(sl.DirectoryPath.IndexOf(@"\achievements\", StringComparison.OrdinalIgnoreCase) < 0,
                    "Legacy achievements path must never be discovered: " + sl.DirectoryPath);
            }
        });

        Console.WriteLine("\n=================================================");
        Console.WriteLine(string.Format("  TEST RESULTS: {0} PASSED, {1} FAILED", s_passed, s_failed));
        Console.WriteLine("=================================================");
        return (s_failed == 0) ? 0 : 1;
    }

    private static string SavePath(string recomp, string profileFolder, int slot)
    {
        return Path.Combine(recomp, profileFolder, "saves", User, SaveManager.TitleIdHex, "00000001",
            string.Format("InfiniteUndiscovery_{0:D4}.bin", slot), "InfiniteUndiscovery.dat");
    }

    private static void Test(string name, Action act)
    {
        try
        {
            act();
            Console.WriteLine(string.Format("  [PASS] {0}", name));
            s_passed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("  [FAIL] {0}: {1}", name, ex.Message));
            Console.ResetColor();
            s_failed++;
        }
    }

    private static void Assert(bool cond, string msg)
    {
        if (!cond) throw new Exception(msg);
    }
}
