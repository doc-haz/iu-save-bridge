using System;
using System.Collections.Generic;
using System.IO;

namespace IUSaveBridge
{
    public class SaveSlotInfo
    {
        public int SlotNumber { get; set; }
        public string SlotName { get; set; }
        public string Region { get; set; }
        public string DirectoryPath { get; set; }
        public string PayloadPath { get; set; }
        public string ThumbnailPath { get; set; }
        public bool HasThumbnail { get; set; }
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public uint Fol { get; set; }
        public bool Crc1Valid { get; set; }
        public bool Crc2Valid { get; set; }
        public string Sha256 { get; set; }
    }

    public class BackupItemInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Region { get; set; }
        public int SlotNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public long FileSize { get; set; }
        public uint Fol { get; set; }
        public string Sha256 { get; set; }
        public string MetaPath { get; set; }
        public string ThumbnailPath { get; set; }
        public bool HasThumbnail { get; set; }
    }

    public class AppConfig
    {
        public string Language { get; set; }
        public string RecompPath { get; set; }
        public string Region { get; set; }

        public AppConfig()
        {
            Language = "en";
            RecompPath = "";
            Region = "NTSC-U";
        }
    }

    public class SaveManager
    {
        public const string RegionNtscU = "NTSC-U";
        public const string RegionPal = "PAL";
        public const string TitleIdHex = "535107DB";

        public static string AppBaseDir
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string BackupsBaseDir
        {
            get
            {
                string dir = Path.Combine(AppBaseDir, "backups");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine(AppBaseDir, "config.json");
            }
        }

        #region Configuration Management
        public static AppConfig LoadConfig()
        {
            AppConfig cfg = new AppConfig();
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    cfg.Language = ExtractJsonString(json, "language") ?? "en";
                    cfg.RecompPath = ExtractJsonString(json, "recompPath") ?? "";
                    cfg.Region = ExtractJsonString(json, "region") ?? RegionNtscU;
                }
            }
            catch { }

            // Normalize
            if (!string.Equals(cfg.Language, "es", StringComparison.OrdinalIgnoreCase))
            {
                cfg.Language = "en";
            }
            if (string.IsNullOrEmpty(cfg.Region))
            {
                cfg.Region = RegionNtscU;
            }

            return cfg;
        }

        public static void SaveConfig(AppConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                string escapedPath = (cfg.RecompPath ?? "").Replace("\\", "\\\\");
                string json = string.Format(
                    "{{\r\n  \"language\": \"{0}\",\r\n  \"recompPath\": \"{1}\",\r\n  \"region\": \"{2}\"\r\n}}\r\n",
                    cfg.Language ?? "en",
                    escapedPath,
                    cfg.Region ?? RegionNtscU);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { }
        }

        public static string LoadLanguagePreference()
        {
            return LoadConfig().Language;
        }

        public static void SaveLanguagePreference(string langCode)
        {
            AppConfig cfg = LoadConfig();
            cfg.Language = string.Equals(langCode, "es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
            SaveConfig(cfg);
        }

        private static string ExtractJsonString(string json, string key)
        {
            int idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            int colon = json.IndexOf(':', idx);
            if (colon < 0) return null;

            int q1 = json.IndexOf('\"', colon);
            if (q1 < 0) return null;

            int q2 = json.IndexOf('\"', q1 + 1);
            while (q2 > 0 && json[q2 - 1] == '\\')
            {
                q2 = json.IndexOf('\"', q2 + 1);
            }

            if (q2 > q1)
            {
                string raw = json.Substring(q1 + 1, q2 - q1 - 1);
                return raw.Replace("\\\\", "\\");
            }
            return null;
        }
        #endregion

        #region Recomp Discovery & Validation
        public static bool ValidateRecompDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;

            string exe1 = Path.Combine(dir, "InfiniteUndiscoveryRecomp.exe");
            bool hasExe = File.Exists(exe1);

            string ntscSaves = Path.Combine(dir, RegionNtscU, "saves");
            string palSaves = Path.Combine(dir, RegionPal, "saves");
            bool hasSaves = Directory.Exists(ntscSaves) || Directory.Exists(palSaves);

            return hasExe || hasSaves;
        }

        public static string NormalizeRecompRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (File.Exists(path))
            {
                // User picked an EXE file
                path = Path.GetDirectoryName(path);
            }

            if (!Directory.Exists(path)) return null;

            // If user selected NTSC-U or PAL subfolder, go up
            string dirName = Path.GetFileName(path.TrimEnd('\\', '/'));
            if (string.Equals(dirName, RegionNtscU, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, RegionPal, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "saves", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(path);
                if (string.Equals(dirName, "saves", StringComparison.OrdinalIgnoreCase))
                {
                    parent = Path.GetDirectoryName(parent);
                }
                if (ValidateRecompDirectory(parent)) return parent;
            }

            if (ValidateRecompDirectory(path)) return path;

            return null;
        }

        public static string FindRecompRoot()
        {
            // 1. Check existing config
            AppConfig cfg = LoadConfig();
            if (!string.IsNullOrEmpty(cfg.RecompPath))
            {
                string norm = NormalizeRecompRoot(cfg.RecompPath);
                if (norm != null) return norm;
            }

            // 2. Search local candidate locations near IU Save Bridge
            string baseDir = AppBaseDir;
            string parentDir = Path.GetDirectoryName(baseDir.TrimEnd('\\', '/'));

            List<string> candidates = new List<string>();
            candidates.Add(baseDir);
            candidates.Add(Path.Combine(baseDir, "InfiniteUndiscoveryRecomp"));
            candidates.Add(Path.Combine(baseDir, "Infinite Undiscovery Recomp"));

            if (!string.IsNullOrEmpty(parentDir))
            {
                candidates.Add(parentDir);
                candidates.Add(Path.Combine(parentDir, "InfiniteUndiscoveryRecomp"));
                candidates.Add(Path.Combine(parentDir, "Infinite Undiscovery Recomp"));
                candidates.Add(Path.Combine(parentDir, @"Infinite Undiscovery Recomp\InfiniteUndiscoveryRecomp-v1.0.0"));
                                candidates.Add(Path.Combine(parentDir, "InfiniteUndiscoveryRecomp-v1.0.0"));
            }

            foreach (string c in candidates)
            {
                string norm = NormalizeRecompRoot(c);
                if (norm != null)
                {
                    cfg.RecompPath = norm;
                    SaveConfig(cfg);
                    return norm;
                }
            }

            return null;
        }

        public static List<string> GetAvailableRegions(string recompRoot)
        {
            List<string> regions = new List<string>();
            if (string.IsNullOrEmpty(recompRoot) || !Directory.Exists(recompRoot)) return regions;

            string ntscDir = Path.Combine(recompRoot, RegionNtscU, "saves");
            string palDir = Path.Combine(recompRoot, RegionPal, "saves");

            if (Directory.Exists(ntscDir)) regions.Add(RegionNtscU);
            if (Directory.Exists(palDir)) regions.Add(RegionPal);

            return regions;
        }

        public static string GetRegionSavesDir(string recompRoot, string region)
        {
            if (string.IsNullOrEmpty(recompRoot)) return null;
            return Path.Combine(recompRoot, region, "saves");
        }

        public static string GetBackupsDirForRegion(string region)
        {
            string dir = Path.Combine(BackupsBaseDir, region ?? RegionNtscU);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
        #endregion

        #region Save Scanning (Ignoring DLC, Headers, and Runtime cache)
        public static List<SaveSlotInfo> ScanSlots(string recompRoot, string region)
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
            if (string.IsNullOrEmpty(recompRoot) || string.IsNullOrEmpty(region)) return slots;

            string savesRoot = GetRegionSavesDir(recompRoot, region);
            if (!Directory.Exists(savesRoot)) return slots;

            // Collect all candidate save folders matching "InfiniteUndiscovery_*.bin"
            List<string> saveDirCandidates = new List<string>();

            // Strategy A: Standard ReXGlue structure:
            // <savesRoot>\<XUID>\535107DB\00000001\InfiniteUndiscovery_*.bin\InfiniteUndiscovery.dat
            // EXPLICITLY ignore XUID "0000000000000000" (shared / DLC) and ignore "00000002" (DLC)
            try
            {
                string[] subDirs = Directory.GetDirectories(savesRoot);
                foreach (string sub in subDirs)
                {
                    string folderName = Path.GetFileName(sub);

                    // Strictly ignore DLC shared folder
                    if (folderName == "0000000000000000" ||
                        string.Equals(folderName, "cache", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(folderName, "Headers", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Check for 535107DB\00000001
                    string savesPath = Path.Combine(sub, TitleIdHex, "00000001");
                    if (Directory.Exists(savesPath))
                    {
                        string[] slotDirs = Directory.GetDirectories(savesPath, "InfiniteUndiscovery_*.bin");
                        saveDirCandidates.AddRange(slotDirs);
                    }

                    // Also check if slot dirs were placed directly in sub
                    if (folderName.StartsWith("InfiniteUndiscovery_", StringComparison.OrdinalIgnoreCase) && folderName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                    {
                        saveDirCandidates.Add(sub);
                    }
                }

                // Strategy B: Saves directly in <savesRoot>\InfiniteUndiscovery_*.bin
                string[] rootSlotDirs = Directory.GetDirectories(savesRoot, "InfiniteUndiscovery_*.bin");
                saveDirCandidates.AddRange(rootSlotDirs);
            }
            catch { }

            // Deduplicate candidates
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string slotDir in saveDirCandidates)
            {
                string fullSlotDir = Path.GetFullPath(slotDir);
                if (seen.Contains(fullSlotDir)) continue;
                seen.Add(fullSlotDir);

                string datPath = Path.Combine(fullSlotDir, "InfiniteUndiscovery.dat");
                if (!File.Exists(datPath)) continue;

                FileInfo fi = new FileInfo(datPath);
                // Must be valid payload size
                if (fi.Length != SavePayload.ExpectedSize) continue;

                // Validate magic
                try
                {
                    byte[] header = new byte[4];
                    using (FileStream fs = File.OpenRead(datPath))
                    {
                        fs.Read(header, 0, 4);
                    }
                    uint magic = SavePayload.ReadUInt32BE(header, 0);
                    if (magic != SavePayload.ExpectedMagic) continue;
                }
                catch { continue; }

                string dirName = Path.GetFileName(fullSlotDir);
                string thumbPath = Path.Combine(fullSlotDir, "__thumbnail.png");

                SaveSlotInfo info = new SaveSlotInfo
                {
                    Region = region,
                    DirectoryPath = fullSlotDir,
                    PayloadPath = datPath,
                    SlotName = dirName,
                    ThumbnailPath = thumbPath,
                    HasThumbnail = File.Exists(thumbPath),
                    LastModified = fi.LastWriteTime,
                    FileSize = fi.Length
                };

                // Extract slot number from name (e.g. InfiniteUndiscovery_0002.bin -> 2)
                string numStr = dirName.Replace("InfiniteUndiscovery_", "").Replace(".bin", "");
                int num;
                if (int.TryParse(numStr, out num)) info.SlotNumber = num;

                // Inspect payload
                try
                {
                    SavePayload payload = SavePayload.FromFile(datPath);
                    info.Fol = payload.Fol;
                    info.Crc1Valid = (payload.StoredCrc1 == payload.CalculatedCrc1);
                    info.Crc2Valid = (payload.StoredCrc2 == payload.CalculatedCrc2);
                    info.Sha256 = payload.Sha256Hash;
                }
                catch
                {
                    info.Fol = 0;
                    info.Crc1Valid = false;
                    info.Crc2Valid = false;
                    info.Sha256 = "INVALID";
                }

                slots.Add(info);
            }

            slots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            return slots;
        }
        #endregion

        #region Backups Management
        public static string CreateBackup(string sourceDatPath, string region, string slotName, int slotNumber = 0)
        {
            if (!File.Exists(sourceDatPath))
                throw new FileNotFoundException("Save payload file to backup not found: " + sourceDatPath);

            string regionDir = GetBackupsDirForRegion(region);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string slotTag = (slotNumber > 0)
                ? string.Format("{0:D4}", slotNumber)
                : slotName.Replace("InfiniteUndiscovery_", "").Replace(".bin", "");

            string backupFileName = string.Format("InfiniteUndiscovery_{0}_{1}.bin", slotTag, ts);
            string backupPath = Path.Combine(regionDir, backupFileName);
            if (File.Exists(backupPath))
            {
                int seq = 1;
                while (File.Exists(backupPath))
                {
                    backupFileName = string.Format("InfiniteUndiscovery_{0}_{1}_{2:D2}.bin", slotTag, ts, seq);
                    backupPath = Path.Combine(regionDir, backupFileName);
                    seq++;
                }
            }

            // Copy save file
            File.Copy(sourceDatPath, backupPath, false);

            // Copy thumbnail if available
            string sourceDir = Path.GetDirectoryName(sourceDatPath);
            string sourceThumb = Path.Combine(sourceDir, "__thumbnail.png");
            if (File.Exists(sourceThumb))
            {
                string thumbBackup = Path.Combine(regionDir, string.Format("InfiniteUndiscovery_{0}_{1}.png", slotTag, ts));
                try { File.Copy(sourceThumb, thumbBackup, true); } catch { }
            }

            // Create sidecar metadata
            try
            {
                SavePayload p = SavePayload.FromFile(backupPath);
                string meta = string.Format(
                    "Date: {0}\r\nRegion: {1}\r\nSlot: {2}\r\nSource: {3}\r\nSHA256: {4}\r\nFol: {5}\r\n",
                    DateTime.Now.ToString("s"),
                    region ?? RegionNtscU,
                    slotTag,
                    sourceDatPath,
                    p.Sha256Hash,
                    p.Fol);
                File.WriteAllText(backupPath + ".meta", meta);
            }
            catch { }

            return backupPath;
        }

        public static List<BackupItemInfo> ScanBackups(string region = null)
        {
            List<BackupItemInfo> list = new List<BackupItemInfo>();
            List<string> dirsToScan = new List<string>();

            if (!string.IsNullOrEmpty(region))
            {
                dirsToScan.Add(GetBackupsDirForRegion(region));
            }
            else
            {
                if (Directory.Exists(BackupsBaseDir))
                {
                    dirsToScan.Add(BackupsBaseDir);
                    dirsToScan.Add(Path.Combine(BackupsBaseDir, RegionNtscU));
                    dirsToScan.Add(Path.Combine(BackupsBaseDir, RegionPal));
                }
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string dir in dirsToScan)
            {
                if (!Directory.Exists(dir)) continue;

                string[] files = Directory.GetFiles(dir, "InfiniteUndiscovery_*.bin");
                foreach (string f in files)
                {
                    if (seen.Contains(f)) continue;
                    seen.Add(f);

                    FileInfo fi = new FileInfo(f);
                    string name = fi.Name;

                    string reg = Path.GetFileName(dir);
                    if (reg != RegionNtscU && reg != RegionPal) reg = region ?? RegionNtscU;

                    int slotNum = 0;
                    string[] parts = name.Split('_');
                    if (parts.Length >= 2)
                    {
                        int.TryParse(parts[1], out slotNum);
                    }

                    string metaPath = f + ".meta";
                    string thumbPath = Path.ChangeExtension(f, ".png");

                    uint fol = 0;
                    string sha = "---";

                    if (File.Exists(metaPath))
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(metaPath);
                            foreach (string line in lines)
                            {
                                if (line.StartsWith("Fol:", StringComparison.OrdinalIgnoreCase))
                                {
                                    uint.TryParse(line.Substring(4).Trim(), out fol);
                                }
                                else if (line.StartsWith("SHA256:", StringComparison.OrdinalIgnoreCase))
                                {
                                    sha = line.Substring(7).Trim();
                                }
                            }
                        }
                        catch { }
                    }

                    BackupItemInfo bi = new BackupItemInfo
                    {
                        FileName = name,
                        FullPath = f,
                        Region = reg,
                        SlotNumber = slotNum,
                        Timestamp = fi.LastWriteTime,
                        FileSize = fi.Length,
                        Fol = fol,
                        Sha256 = sha,
                        MetaPath = metaPath,
                        ThumbnailPath = thumbPath,
                        HasThumbnail = File.Exists(thumbPath)
                    };
                    list.Add(bi);
                }
            }

            list.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return list;
        }

        public static string RestoreBackup(string backupFilePath, string targetDatPath, string region, string slotName, int slotNumber)
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found: " + backupFilePath);

            // Step 1: Validate backup file payload
            SavePayload backupPayload = SavePayload.FromFile(backupFilePath);

            // Step 2: Safety backup of the CURRENT save before replacing it!
            if (File.Exists(targetDatPath))
            {
                CreateBackup(targetDatPath, region, slotName, slotNumber);
            }

            // Step 3: Write payload safely
            SafePath.AtomicSave(backupPayload.Data, targetDatPath);

            // Step 4: Restore thumbnail if backup has one
            string thumbBackup = Path.ChangeExtension(backupFilePath, ".png");
            if (File.Exists(thumbBackup))
            {
                string targetDir = Path.GetDirectoryName(targetDatPath);
                string targetThumb = Path.Combine(targetDir, "__thumbnail.png");
                try { File.Copy(thumbBackup, targetThumb, true); } catch { }
            }

            return targetDatPath;
        }

        public static string SavePayloadDirect(SavePayload payload, string targetDatPath, string region, string slotName, int slotNumber)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            if (string.IsNullOrEmpty(targetDatPath)) throw new ArgumentNullException("targetDatPath");

            // Step 1: Mandatory auto-backup of original save before ANY modification
            if (File.Exists(targetDatPath))
            {
                CreateBackup(targetDatPath, region, slotName, slotNumber);
            }

            // Step 2: Recalculate checksums
            payload.RecalculateChecksums();

            // Step 3: Atomic write
            SafePath.AtomicSave(payload.Data, targetDatPath);

            // Step 4: Verify written file
            SavePayload verified = SavePayload.FromFile(targetDatPath);
            if (verified.Sha256Hash != payload.Sha256Hash)
            {
                throw new InvalidOperationException("Verification mismatch after writing save payload!");
            }

            return targetDatPath;
        }
        #endregion
    }
}
