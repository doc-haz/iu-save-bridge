using System;
using System.Collections.Generic;
using System.IO;

namespace IUSaveBridge
{
    public class SaveSlotInfo
    {
        public int SlotNumber { get; set; }
        public string SlotName { get; set; }
        public string Profile { get; set; }
        public bool IsLegacyProfile { get; set; }
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
        public string Profile { get; set; }
        public bool IsLegacyBackup { get; set; }
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
        public string Profile { get; set; }

        public AppConfig()
        {
            Language = "en";
            RecompPath = "";
            Profile = SaveManager.ProfileUsa;
        }
    }

    public class SaveManager
    {
        public const string TitleIdHex = "535107DB";

        // --- Final IU Recomp v1.0.0-rc1 profile model (stable codes) ---
        public const string ProfileUsa = "USA";
        public const string ProfileUsaUndub = "USA-UNDUB";
        public const string ProfileEurope = "EUROPE";
        public const string ProfileJapan = "JAPAN";
        public const string ProfileAsia = "ASIA";

        // --- Legacy folder names still found in pre-1.0.0-rc1 installations ---
        public const string LegacyFolderNtscU = "NTSC-U";
        public const string LegacyFolderPal = "PAL";

        /// <summary>
        /// Recomp stores the achievements subsystem at &lt;savesRoot&gt;\achievements\.
        /// That tree can contain a payload shaped like
        /// achievements\535107DB\00000001\InfiniteUndiscovery_XXXX.bin\InfiniteUndiscovery.dat
        /// which structurally resembles a save slot but is not a playable save. It is excluded
        /// explicitly (case-insensitive) from discovery.
        /// </summary>
        public const string AchievementsFolderName = "achievements";

        /// <summary>
        /// Canonical, ordered list of supported IU Recomp profiles.
        /// These codes are used as on-disk folder names under the Recomp root and under backups\.
        /// </summary>
        public static readonly string[] SupportedProfiles = new string[]
        {
            ProfileUsa,
            ProfileUsaUndub,
            ProfileEurope,
            ProfileJapan,
            ProfileAsia
        };

        /// <summary>Default profile used when nothing is configured or a value cannot be resolved.</summary>
        public const string DefaultProfile = ProfileUsa;

        /// <summary>
        /// Maps any incoming value (new profile code or legacy region code) to a canonical profile code.
        /// Legacy mapping: NTSC-U -> USA, PAL -> EUROPE. Returns null for null/empty input.
        /// Unrecognized values are returned unchanged so the UI can still surface them.
        /// </summary>
        public static string NormalizeProfile(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            string v = value.Trim();
            if (v.Length == 0) return null;

            if (string.Equals(v, LegacyFolderNtscU, StringComparison.OrdinalIgnoreCase)) return ProfileUsa;
            if (string.Equals(v, LegacyFolderPal, StringComparison.OrdinalIgnoreCase)) return ProfileEurope;

            foreach (string p in SupportedProfiles)
            {
                if (string.Equals(v, p, StringComparison.OrdinalIgnoreCase)) return p;
            }

            return v;
        }

        /// <summary>Returns the legacy folder name that maps to the given canonical profile, or null.</summary>
        public static string GetLegacyFolderForProfile(string profile)
        {
            string code = NormalizeProfile(profile);
            if (string.Equals(code, ProfileUsa, StringComparison.OrdinalIgnoreCase)) return LegacyFolderNtscU;
            if (string.Equals(code, ProfileEurope, StringComparison.OrdinalIgnoreCase)) return LegacyFolderPal;
            return null;
        }

        public static bool IsSupportedProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile)) return false;
            foreach (string p in SupportedProfiles)
            {
                if (string.Equals(p, profile, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static string AppBaseDir
        {
            get
            {
                try
                {
                    string asmLoc = typeof(SaveManager).Assembly.Location;
                    if (!string.IsNullOrEmpty(asmLoc))
                    {
                        return Path.GetDirectoryName(Path.GetFullPath(asmLoc));
                    }
                }
                catch { }
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

                    // New portable format ("profile") with transparent legacy fallback ("region").
                    string profileRaw = ExtractJsonString(json, "profile");
                    if (string.IsNullOrEmpty(profileRaw))
                    {
                        // Legacy config.json: NTSC-U -> USA, PAL -> EUROPE (mapped in memory).
                        profileRaw = ExtractJsonString(json, "region");
                    }
                    cfg.Profile = NormalizeProfile(profileRaw);
                }
            }
            catch { }

            // Normalize
            if (!string.Equals(cfg.Language, "es", StringComparison.OrdinalIgnoreCase))
            {
                cfg.Language = "en";
            }
            if (string.IsNullOrEmpty(cfg.Profile))
            {
                cfg.Profile = DefaultProfile;
            }

            return cfg;
        }

        public static void SaveConfig(AppConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                string escapedPath = (cfg.RecompPath ?? "").Replace("\\", "\\\\");
                // Always persist the new format: "profile" only (no legacy "region" key).
                string json = string.Format(
                    "{{\r\n  \"language\": \"{0}\",\r\n  \"recompPath\": \"{1}\",\r\n  \"profile\": \"{2}\"\r\n}}\r\n",
                    cfg.Language ?? "en",
                    escapedPath,
                    NormalizeProfile(cfg.Profile) ?? DefaultProfile);
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

            bool hasSaves = false;
            foreach (string p in SupportedProfiles)
            {
                if (Directory.Exists(Path.Combine(dir, p, "saves"))) { hasSaves = true; break; }
                string legacy = GetLegacyFolderForProfile(p);
                if (legacy != null && Directory.Exists(Path.Combine(dir, legacy, "saves"))) { hasSaves = true; break; }
            }

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

            // If the user selected a profile / legacy-region / saves subfolder, go up to the Recomp root.
            string dirName = Path.GetFileName(path.TrimEnd('\\', '/'));
            bool isProfileFolder = IsSupportedProfile(dirName) ||
                                   string.Equals(dirName, LegacyFolderNtscU, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(dirName, LegacyFolderPal, StringComparison.OrdinalIgnoreCase);
            if (isProfileFolder || string.Equals(dirName, "saves", StringComparison.OrdinalIgnoreCase))
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
            string grandparentDir = !string.IsNullOrEmpty(parentDir) ? Path.GetDirectoryName(parentDir.TrimEnd('\\', '/')) : null;

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

            if (!string.IsNullOrEmpty(grandparentDir))
            {
                candidates.Add(grandparentDir);
                candidates.Add(Path.Combine(grandparentDir, "InfiniteUndiscoveryRecomp"));
                candidates.Add(Path.Combine(grandparentDir, "Infinite Undiscovery Recomp"));
                candidates.Add(Path.Combine(grandparentDir, @"Infinite Undiscovery Recomp\InfiniteUndiscoveryRecomp-v1.0.0"));
                candidates.Add(Path.Combine(grandparentDir, "InfiniteUndiscoveryRecomp-v1.0.0"));
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

        /// <summary>
        /// Returns the canonical profile codes that are actually installed under the Recomp root.
        /// New-style folders (USA\, EUROPE\, ...) are preferred; legacy folders (NTSC-U\, PAL\)
        /// are reported as their canonical profile (USA / EUROPE) and never as raw legacy names.
        /// </summary>
        public static List<string> GetAvailableProfiles(string recompRoot)
        {
            List<string> profiles = new List<string>();
            if (string.IsNullOrEmpty(recompRoot) || !Directory.Exists(recompRoot)) return profiles;

            foreach (string p in SupportedProfiles)
            {
                string native = Path.Combine(recompRoot, p, "saves");
                if (Directory.Exists(native))
                {
                    profiles.Add(p);
                    continue;
                }

                // Fall back to the matching legacy folder only when the native one is absent.
                string legacy = GetLegacyFolderForProfile(p);
                if (legacy != null && Directory.Exists(Path.Combine(recompRoot, legacy, "saves")))
                {
                    profiles.Add(p);
                }
            }

            return profiles;
        }

        /// <summary>True when the given profile resolves to a legacy folder for this Recomp root.</summary>
        public static bool IsLegacyProfileInstall(string recompRoot, string profile)
        {
            bool isLegacy;
            ResolveProfileSavesDir(recompRoot, profile, out isLegacy);
            return isLegacy;
        }

        /// <summary>
        /// Resolves the on-disk "saves" directory for a profile, preferring the new profile folder
        /// and transparently falling back to the legacy NTSC-U / PAL folders for old installs.
        /// </summary>
        public static string ResolveProfileSavesDir(string recompRoot, string profile, out bool isLegacy)
        {
            isLegacy = false;
            if (string.IsNullOrEmpty(recompRoot)) return null;

            string code = NormalizeProfile(profile) ?? DefaultProfile;

            string native = Path.Combine(recompRoot, code, "saves");
            if (Directory.Exists(native)) return native;

            string legacyFolder = GetLegacyFolderForProfile(code);
            if (legacyFolder != null)
            {
                string legacy = Path.Combine(recompRoot, legacyFolder, "saves");
                if (Directory.Exists(legacy))
                {
                    isLegacy = true;
                    return legacy;
                }
            }

            // Nothing exists yet: report the canonical destination (used for import/create).
            return native;
        }

        public static List<string> GetAvailableUserIds(string recompRoot, string profile)
        {
            List<string> users = new List<string>();
            if (string.IsNullOrEmpty(recompRoot) || string.IsNullOrEmpty(profile)) return users;

            string savesRoot = GetProfileSavesDir(recompRoot, profile);
            if (Directory.Exists(savesRoot))
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(savesRoot);
                    foreach (string d in dirs)
                    {
                        string name = Path.GetFileName(d);
                        if (name == "0000000000000000" ||
                            string.Equals(name, "cache", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(name, "Headers", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(name, AchievementsFolderName, StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("InfiniteUndiscovery_", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        users.Add(name);
                    }
                }
                catch { }
            }

            if (users.Count == 0)
            {
                users.Add("0000000100000001");
            }

            return users;
        }

        public static string GetSlotDirectory(string recompRoot, string profile, string userId, int slotNumber)
        {
            if (string.IsNullOrEmpty(recompRoot)) return null;
            string savesRoot = GetProfileSavesDir(recompRoot, profile);
            string user = string.IsNullOrEmpty(userId) ? "0000000100000001" : userId;
            return Path.Combine(savesRoot, Path.Combine(user, Path.Combine(TitleIdHex, Path.Combine("00000001", string.Format("InfiniteUndiscovery_{0:D4}.bin", slotNumber)))));
        }

        public static bool SlotExists(string recompRoot, string profile, string userId, int slotNumber)
        {
            string dir = GetSlotDirectory(recompRoot, profile, userId, slotNumber);
            if (string.IsNullOrEmpty(dir)) return false;
            string dat = Path.Combine(dir, "InfiniteUndiscovery.dat");
            return File.Exists(dat);
        }

        public static string ImportXboxSave(StfsSaveInfo info, string recompRoot, string targetProfile, string targetUserId, int targetSlotNumber, bool replaceExisting)
        {
            if (info == null || info.Payload == null)
                throw new ArgumentNullException("info", "Invalid Xbox 360 save info.");
            if (string.IsNullOrEmpty(recompRoot))
                throw new ArgumentNullException("recompRoot", "Recomp root directory is required.");

            string prof = string.IsNullOrEmpty(targetProfile)
                ? (NormalizeProfile(info.DetectedProfile) ?? DefaultProfile)
                : NormalizeProfile(targetProfile);
            string user = string.IsNullOrEmpty(targetUserId) ? "0000000100000001" : targetUserId;
            int slot = (targetSlotNumber > 0) ? targetSlotNumber : (int)info.OriginalSlot;
            if (slot <= 0) slot = 1;

            string targetDir = GetSlotDirectory(recompRoot, prof, user, slot);
            string targetDat = Path.Combine(targetDir, "InfiniteUndiscovery.dat");

            if (File.Exists(targetDat))
            {
                if (!replaceExisting)
                {
                    throw new InvalidOperationException(string.Format("Slot {0} already exists in {1}.", slot, prof));
                }
                // Mandatory automatic safety backup before overwriting!
                CreateBackup(targetDat, prof, Path.GetFileName(targetDir), slot);
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Keep extracted 409,600-byte UDSV payload structurally intact
            byte[] dataToSave = (byte[])info.Payload.Data.Clone();

            // Only modify slot number and recalculate CRCs if target slot differs from source slot!
            if (info.OriginalSlot != (uint)slot)
            {
                SavePayload.WriteUInt32BE(dataToSave, SavePayload.SlotNumberOffset, (uint)slot);

                // Recalculate CRCs
                for (int i = 20; i < 28; i++) dataToSave[i] = 0;
                byte[] tempHeader = new byte[232];
                Array.Copy(dataToSave, 0, tempHeader, 0, 232);
                for (int i = 20; i < 28; i++) tempHeader[i] = 0;

                uint crc1 = SavePayload.ComputeCRC(tempHeader, 0, 232);
                uint crc2 = SavePayload.ComputeCRC(dataToSave, 232, dataToSave.Length - 232);
                SavePayload.WriteUInt32BE(dataToSave, 0x14, crc1);
                SavePayload.WriteUInt32BE(dataToSave, 0x18, crc2);
            }

            // Atomic write of intact payload
            SafePath.AtomicSave(dataToSave, targetDat);

            // Save thumbnail if available
            if (info.ThumbnailPng != null && info.ThumbnailPng.Length > 0)
            {
                try
                {
                    string targetThumb = Path.Combine(targetDir, "__thumbnail.png");
                    File.WriteAllBytes(targetThumb, info.ThumbnailPng);
                }
                catch { }
            }

            return targetDat;
        }

        /// <summary>
        /// Returns the on-disk "saves" directory for a profile, resolving legacy NTSC-U / PAL
        /// folders transparently when the new profile folder is not present.
        /// </summary>
        public static string GetProfileSavesDir(string recompRoot, string profile)
        {
            bool isLegacy;
            return ResolveProfileSavesDir(recompRoot, profile, out isLegacy);
        }

        /// <summary>
        /// Canonical backups directory for a profile: backups\&lt;PROFILE&gt;\.
        /// Legacy backups are never moved automatically; they are read via ScanBackups.
        /// </summary>
        public static string GetBackupsDirForProfile(string profile)
        {
            string code = NormalizeProfile(profile) ?? DefaultProfile;
            string dir = Path.Combine(BackupsBaseDir, code);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
        #endregion

        #region Save Scanning (Ignoring DLC, Headers, Runtime cache, and achievements)
        public static List<SaveSlotInfo> ScanSlots(string recompRoot, string profile)
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
            if (string.IsNullOrEmpty(recompRoot) || string.IsNullOrEmpty(profile)) return slots;

            string code = NormalizeProfile(profile) ?? DefaultProfile;
            bool isLegacy;
            string savesRoot = ResolveProfileSavesDir(recompRoot, code, out isLegacy);
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
                        string.Equals(folderName, "Headers", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(folderName, AchievementsFolderName, StringComparison.OrdinalIgnoreCase))
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
                    Profile = code,
                    IsLegacyProfile = isLegacy,
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
        public static string CreateBackup(string sourceDatPath, string profile, string slotName, int slotNumber = 0)
        {
            if (!File.Exists(sourceDatPath))
                throw new FileNotFoundException("Save payload file to backup not found: " + sourceDatPath);

            // New backups always land in the canonical profile folder (e.g. USA\, EUROPE\),
            // even when the source save was read from a legacy NTSC-U\ / PAL\ install.
            string profileCode = NormalizeProfile(profile) ?? DefaultProfile;
            string profileDir = GetBackupsDirForProfile(profileCode);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string slotTag = (slotNumber > 0)
                ? string.Format("{0:D4}", slotNumber)
                : slotName.Replace("InfiniteUndiscovery_", "").Replace(".bin", "");

            string backupFileName = string.Format("InfiniteUndiscovery_{0}_{1}.bin", slotTag, ts);
            string backupPath = Path.Combine(profileDir, backupFileName);
            if (File.Exists(backupPath))
            {
                int seq = 1;
                while (File.Exists(backupPath))
                {
                    backupFileName = string.Format("InfiniteUndiscovery_{0}_{1}_{2:D2}.bin", slotTag, ts, seq);
                    backupPath = Path.Combine(profileDir, backupFileName);
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
                string thumbBackup = Path.Combine(profileDir, string.Format("InfiniteUndiscovery_{0}_{1}.png", slotTag, ts));
                try { File.Copy(sourceThumb, thumbBackup, true); } catch { }
            }

            // Create sidecar metadata
            try
            {
                SavePayload p = SavePayload.FromFile(backupPath);
                string meta = string.Format(
                    "Date: {0}\r\nProfile: {1}\r\nSlot: {2}\r\nSource: {3}\r\nSHA256: {4}\r\nFol: {5}\r\n",
                    DateTime.Now.ToString("s"),
                    profileCode,
                    slotTag,
                    sourceDatPath,
                    p.Sha256Hash,
                    p.Fol);
                File.WriteAllText(backupPath + ".meta", meta);
            }
            catch { }

            return backupPath;
        }

        /// <summary>
        /// Enumerates the backup directories that belong to a profile, including legacy folders.
        /// When <paramref name="profile"/> is null, all supported profiles (and their legacy folders)
        /// are scanned.
        /// </summary>
        private static List<KeyValuePair<string, string>> GetBackupScanDirs(string profile)
        {
            List<KeyValuePair<string, string>> dirs = new List<KeyValuePair<string, string>>();
            if (!Directory.Exists(BackupsBaseDir)) return dirs;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<string> profiles = new List<string>();
            if (string.IsNullOrEmpty(profile))
            {
                foreach (string p in SupportedProfiles) profiles.Add(p);
            }
            else
            {
                profiles.Add(NormalizeProfile(profile) ?? DefaultProfile);
            }

            foreach (string p in profiles)
            {
                // Canonical folder: backups\<PROFILE>\
                string native = Path.Combine(BackupsBaseDir, p);
                if (seen.Add(native)) dirs.Add(new KeyValuePair<string, string>(native, p));

                // Legacy folder aliases: backups\NTSC-U\ -> USA, backups\PAL\ -> EUROPE.
                string legacy = GetLegacyFolderForProfile(p);
                if (legacy != null)
                {
                    string legacyDir = Path.Combine(BackupsBaseDir, legacy);
                    if (seen.Add(legacyDir)) dirs.Add(new KeyValuePair<string, string>(legacyDir, p));
                }
            }

            return dirs;
        }

        public static List<BackupItemInfo> ScanBackups(string profile = null)
        {
            List<BackupItemInfo> list = new List<BackupItemInfo>();
            List<KeyValuePair<string, string>> dirsToScan = GetBackupScanDirs(profile);

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> target in dirsToScan)
            {
                string dir = target.Key;
                string canonicalProfile = target.Value;
                if (!Directory.Exists(dir)) continue;

                bool isLegacy = string.Equals(Path.GetFileName(dir), LegacyFolderNtscU, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Path.GetFileName(dir), LegacyFolderPal, StringComparison.OrdinalIgnoreCase);

                string[] files = Directory.GetFiles(dir, "InfiniteUndiscovery_*.bin");
                foreach (string f in files)
                {
                    if (seen.Contains(f)) continue;
                    seen.Add(f);

                    FileInfo fi = new FileInfo(f);
                    string name = fi.Name;

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
                        Profile = canonicalProfile,
                        IsLegacyBackup = isLegacy,
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

        public static string RestoreBackup(string backupFilePath, string targetDatPath, string profile, string slotName, int slotNumber)
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found: " + backupFilePath);

            // Step 1: Validate backup file payload
            SavePayload backupPayload = SavePayload.FromFile(backupFilePath);

            // Step 2: Safety backup of the CURRENT save before replacing it!
            if (File.Exists(targetDatPath))
            {
                CreateBackup(targetDatPath, profile, slotName, slotNumber);
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

        public static string SavePayloadDirect(SavePayload payload, string targetDatPath, string profile, string slotName, int slotNumber)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            if (string.IsNullOrEmpty(targetDatPath)) throw new ArgumentNullException("targetDatPath");

            // Step 1: Mandatory auto-backup of original save before ANY modification
            if (File.Exists(targetDatPath))
            {
                CreateBackup(targetDatPath, profile, slotName, slotNumber);
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
