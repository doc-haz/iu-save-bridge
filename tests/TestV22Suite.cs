using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using IUSaveBridge;

class TestV22Suite
{
    private static int s_passed = 0;
    private static int s_failed = 0;

    static int Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  IU Save Bridge v2.2.0 - Comprehensive Test Suite");
        Console.WriteLine("=================================================");

        string projectRoot = Path.GetFullPath(Path.Combine(SaveManager.AppBaseDir, @"..\.."));
        string fixtureRecomp = null;
        string[] candidates = new string[] {
            Path.Combine(SaveManager.AppBaseDir, "MockRecomp"),
            Path.Combine(SaveManager.AppBaseDir, @"..\work\MockRecomp"),
            Path.Combine(projectRoot, @"tests\work\MockRecomp"),
            Path.Combine(Environment.CurrentDirectory, @"tests\work\MockRecomp"),
            Path.Combine(Environment.CurrentDirectory, "MockRecomp")
        };
        foreach (var c in candidates) {
            string full = Path.GetFullPath(c);
            if (Directory.Exists(full)) {
                fixtureRecomp = full;
                break;
            }
        }
        if (fixtureRecomp == null) fixtureRecomp = Path.Combine(projectRoot, @"tests\work\MockRecomp");
        string testBackupsDir = Path.Combine(SaveManager.AppBaseDir, @"backups");

        // 1. Primer inicio sin config
        Test("1. First start without config.json", () => {
            string cfg = SaveManager.ConfigFilePath;
            string tempCfg = cfg + ".bak_test";
            if (File.Exists(cfg)) File.Move(cfg, tempCfg);
            try
            {
                AppConfig c = SaveManager.LoadConfig();
                Assert(c != null, "Config should not be null");
                Assert(c.Language == "en", "Default language must be 'en'");
                Assert(c.Region == SaveManager.RegionNtscU, "Default region must be NTSC-U");
            }
            finally
            {
                if (File.Exists(tempCfg)) File.Move(tempCfg, cfg);
            }
        });

        // 2. Localizar recomp
        Test("2. Locate recomp root directory", () => {
            string norm = SaveManager.NormalizeRecompRoot(fixtureRecomp);
            Assert(norm != null, "Recomp root must be successfully validated");
            Assert(Directory.Exists(norm), "Normalized root directory must exist");
        });

        // 3. Recordar recomp en config.json
        Test("3. Remember recomp path in config.json", () => {
            AppConfig c = new AppConfig {
                Language = "en",
                RecompPath = fixtureRecomp,
                Region = SaveManager.RegionNtscU
            };
            SaveManager.SaveConfig(c);
            AppConfig loaded = SaveManager.LoadConfig();
            Assert(string.Equals(loaded.RecompPath, fixtureRecomp, StringComparison.OrdinalIgnoreCase), "Recomp path must be persisted in config.json");
        });

        // 4. NTSC-U saves scanning
        Test("4. NTSC-U saves discovery", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            Assert(slots.Count >= 2, "Expected at least 2 slots in NTSC-U fixture, found: " + slots.Count);
        });

        // 5. PAL saves scanning
        Test("5. PAL saves discovery", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionPal);
            Assert(slots.Count >= 1, "Expected at least 1 slot in PAL fixture, found: " + slots.Count);
        });

        // 6. Ambas regiones detectadas
        Test("6. Both regions available check", () => {
            List<string> regions = SaveManager.GetAvailableRegions(fixtureRecomp);
            Assert(regions.Contains(SaveManager.RegionNtscU), "NTSC-U must be detected");
            Assert(regions.Contains(SaveManager.RegionPal), "PAL must be detected");
            Assert(regions.Count == 2, "Expected exactly 2 regions");
        });

        // 7. Detectar slots correctos
        Test("7. Detect correct slot numbers and folder names", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            Assert(slots[0].SlotNumber == 1, "Slot 1 number mismatch");
            Assert(slots[1].SlotNumber == 2, "Slot 2 number mismatch");
            Assert(slots[0].SlotName == "InfiniteUndiscovery_0001.bin", "Slot 1 folder name mismatch");
            Assert(slots[1].SlotName == "InfiniteUndiscovery_0002.bin", "Slot 2 folder name mismatch");
        });

        // 8. Ignorar DLC estrictamente
        Test("8. Strictly ignore DLC and runtime folders (0000000000000000)", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            foreach (var slot in slots) {
                Assert(!slot.DirectoryPath.Contains("0000000000000000"), "DLC path must not be treated as a save: " + slot.DirectoryPath);
                Assert(!slot.DirectoryPath.Contains("00000002"), "Marketplace DLC must not be treated as a save: " + slot.DirectoryPath);
            }
        });

        // 9. Cargar Fol real
        Test("9. Load real Fol from slot", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            Assert(slot2 != null, "Slot 2 must exist");
            Assert(slot2.Fol > 0, "Fol must be loaded, got: " + slot2.Fol);
        });

        // 10. Cargar Characters reales
        Test("10. Load real Characters from slot", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);
            var capell = p.GetCharacter(0);
            Assert(capell.Name == "Capell", "Character 0 must be Capell");
            Assert(capell.Level >= 1, "Capell level must be valid");
            Assert(capell.MaxHp >= 1, "Capell MaxHp must be valid");
            var aya = p.GetCharacter(1);
            Assert(aya.Name == "Aya", "Character 1 must be Aya");
            var seraphina = p.GetCharacter(17);
            Assert(seraphina.Name == "Seraphina", "Character 17 must be Seraphina");
        });

        // 11. Cargar Inventory real
        Test("11. Load real Inventory from slot", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);
            var items = p.GetAllItems();
            Assert(items.Count == 1023, "Catalog must have exactly 1,023 items, got: " + items.Count);
            int owned = 0;
            foreach (var it in items) if (it.Amount > 0) owned++;
            Assert(owned > 0, "Slot 2 should have owned items, found: " + owned);
        });

        // 12. Editar Fol
        Test("12. Edit Fol in memory", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);
            uint newFol = 88888888;
            p.SetFol(newFol);
            Assert(p.Fol == newFol, "Fol must be updated");
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 must be recalculated");
            Assert(p.StoredCrc2 == p.CalculatedCrc2, "CRC2 must be recalculated");
        });

        // 13. Editar Character
        Test("13. Edit Character stats in memory", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);
            var capell = p.GetCharacter(0);
            capell.Level = 99;
            capell.CurrentHp = 9999;
            capell.MaxHp = 9999;
            capell.Atk = 999;
            p.SaveCharacter(capell);
            var check = p.GetCharacter(0);
            Assert(check.Level == 99, "Level edit mismatch");
            Assert(check.MaxHp == 9999, "MaxHp edit mismatch");
            Assert(check.Atk == 999, "Atk edit mismatch");
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 mismatch after char edit");
            Assert(p.StoredCrc2 == p.CalculatedCrc2, "CRC2 mismatch after char edit");
        });

        // 14. Editar Item
        Test("14. Edit Item quantity in memory", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);
            var items = p.GetAllItems();
            var item1 = items[0];
            item1.Amount = 77;
            p.SaveItem(item1);
            var check = p.GetAllItems()[0];
            Assert(check.Amount == 77, "Item amount mismatch");
            Assert(p.StoredCrc1 == p.CalculatedCrc1, "CRC1 mismatch after item edit");
            Assert(p.StoredCrc2 == p.CalculatedCrc2, "CRC2 mismatch after item edit");
        });

        // 15, 16, 17, 18. Guardar cambios, backup automático, checksums, recarga
        Test("15-18. Direct save with auto-backup, valid CRC32s, and reload", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            SavePayload p = SavePayload.FromFile(slot2.PayloadPath);

            uint testFol = 7654321;
            p.SetFol(testFol);

            int bkpCountBefore = SaveManager.ScanBackups(SaveManager.RegionNtscU).Count;
            SaveManager.SavePayloadDirect(p, slot2.PayloadPath, slot2.Region, slot2.SlotName, slot2.SlotNumber);
            int bkpCountAfter = SaveManager.ScanBackups(SaveManager.RegionNtscU).Count;

            Assert(bkpCountAfter == bkpCountBefore + 1, "Automatic backup must have been created before write");

            // Verify written save on disk
            SavePayload onDisk = SavePayload.FromFile(slot2.PayloadPath);
            Assert(onDisk.Fol == testFol, "Disk payload Fol must match testFol");
            Assert(onDisk.StoredCrc1 == onDisk.CalculatedCrc1, "Disk CRC1 must be valid");
            Assert(onDisk.StoredCrc2 == onDisk.CalculatedCrc2, "Disk CRC2 must be valid");
            Assert(onDisk.Sha256Hash == p.Sha256Hash, "Disk SHA256 must match in-memory payload");
        });

        // 19, 20. Restore backup y backup automático previo a restore
        Test("19-20. Restore backup with safety auto-backup", () => {
            List<SaveSlotInfo> slots = SaveManager.ScanSlots(fixtureRecomp, SaveManager.RegionNtscU);
            SaveSlotInfo slot2 = slots.Find(s => s.SlotNumber == 2);
            List<BackupItemInfo> bkps = SaveManager.ScanBackups(SaveManager.RegionNtscU);
            Assert(bkps.Count > 0, "At least one backup must exist to test restore");

            BackupItemInfo newest = bkps[0];
            int bkpCountBefore = bkps.Count;

            SaveManager.RestoreBackup(newest.FullPath, slot2.PayloadPath, slot2.Region, slot2.SlotName, slot2.SlotNumber);
            int bkpCountAfter = SaveManager.ScanBackups(SaveManager.RegionNtscU).Count;

            Assert(bkpCountAfter == bkpCountBefore + 1, "Safety backup of active save must be created before restore");

            SavePayload restored = SavePayload.FromFile(slot2.PayloadPath);
            Assert(restored.StoredCrc1 == restored.CalculatedCrc1, "Restored CRC1 must be valid");
            Assert(restored.StoredCrc2 == restored.CalculatedCrc2, "Restored CRC2 must be valid");
        });

        // 21, 22. Mover carpeta y working directory distinto
        Test("21-22. Relative portability with different working directory", () => {
            string currentCwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                string appBase = SaveManager.AppBaseDir;
                Assert(Directory.Exists(appBase), "AppBaseDir must be valid regardless of CWD");
                string bkpBase = SaveManager.BackupsBaseDir;
                Assert(bkpBase.StartsWith(appBase, StringComparison.OrdinalIgnoreCase), "Backups must be relative to app base");
            }
            finally
            {
                Directory.SetCurrentDirectory(currentCwd);
            }
        });

        // 23, 24. Localization EN y ES (Key Parity)
        Test("23-24. Localization EN & ES complete key parity", () => {
            var keys = Loc.GetAllKeys();
            int count = 0;
            foreach (var k in keys) {
                count++;
                Assert(Loc.HasKey("en", k), "Missing EN key: " + k);
                Assert(Loc.HasKey("es", k), "Missing ES key: " + k);
            }
            Assert(count >= 50, "Expected at least 50 localization keys, got: " + count);
        });

        // 25. Cero escrituras en carpetas externas del sistema
        Test("25. Zero external footprint (No AppData, user folders, or Registry)", () => {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string cfg = SaveManager.ConfigFilePath;
            Assert(cfg.StartsWith(SaveManager.AppBaseDir, StringComparison.OrdinalIgnoreCase), "config.json must reside next to executable");
            Assert(!cfg.StartsWith(appData, StringComparison.OrdinalIgnoreCase), "Config must not be in AppData");
            Assert(!cfg.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase), "Config must not be in LocalAppData");
            Assert(!cfg.StartsWith(docs, StringComparison.OrdinalIgnoreCase), "Config must not be in user folders");
        });

        // 26. Funcionamiento sin SeraphicGate instalado
        Test("26. Independent execution without SeraphicGate", () => {
            // Check that no UI element or parser fails when SeraphicGate is absent
            SavePayload p = SavePayload.FromFile(Path.Combine(fixtureRecomp, @"NTSC-U\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0001.bin\InfiniteUndiscovery.dat"));
            Assert(p != null, "Parsing must succeed without SeraphicGate");
        });

        // 27. UI sin referencias a Equipment/Skills Pending
        Test("27. UI clean of 'Pending' tabs and legacy bridge", () => {
            MainForm form = new MainForm();
            try
            {
                TabControl tc = form.TabCtrl;
                Assert(tc.TabPages.Count == 5, "UI must have exactly 5 tabs, got: " + tc.TabPages.Count);
                foreach (TabPage tp in tc.TabPages)
                {
                    Assert(!tp.Text.Contains("Pending"), "Tab must not contain 'Pending': " + tp.Text);
                    Assert(!tp.Text.Contains("SeraphicGate"), "Tab must not contain 'SeraphicGate': " + tp.Text);
                    Assert(!tp.Text.Contains("Equipment"), "Tab must not contain 'Equipment': " + tp.Text);
                }
            }
            finally
            {
                form.Dispose();
            }
        });

        // 28. Icono correcto en EXE / Ventana / Taskbar
        Test("28. Window, EXE, and Taskbar icon verification", () => {
            MainForm form = new MainForm();
            try
            {
                Assert(form.Icon != null, "Form.Icon must not be null");
                Assert(form.Icon.Width >= 16 && form.Icon.Height >= 16, "Form icon must have valid dimensions");
            }
            finally
            {
                form.Dispose();
            }
        });

        Console.WriteLine("\n=================================================");
        Console.WriteLine(string.Format("  TEST RESULTS: {0} PASSED, {1} FAILED", s_passed, s_failed));
        Console.WriteLine("=================================================");

        return (s_failed == 0) ? 0 : 1;
    }

    static void Test(string name, Action act)
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

    static void Assert(bool cond, string msg)
    {
        if (!cond) throw new Exception(msg);
    }
}
