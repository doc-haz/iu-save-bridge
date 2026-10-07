<p align="center">
  <img src="assets/banner.png" alt="Infinite Undiscovery Recomp Save Editor" width="100%" />
</p>

# Infinite Undiscovery Recomp Save Editor (IU Save Bridge v2.3.0)

**IU Save Bridge v2.3.0** is the official companion save editor designed specifically for [**Infinite Undiscovery Recomp**](https://github.com/doc-haz/infinite-undiscovery-recomp) **v1.0.0-rc1**.

It provides a modern, high-contrast, crystal fantasy user interface and direct save manipulation with zero staging complexity, mandatory automated timestamped backups, and byte-perfect dual Tri-Ace CRC32 integrity verification.

---

## Download

Latest portable Windows x64 release:

https://github.com/doc-haz/iu-save-bridge/releases/latest

Download:

`IU-Save-Bridge-v2.3.0-win-x64.zip`

No installer is required. Extract the ZIP to a writable folder and run:

`IU_Save_Bridge.exe`

---

## Key Highlights

- **Direct & Transparent Workflow:** `Open Save -> Edit -> Save Changes`. The editor eliminates legacy staging folders and CON/STFS wrapping from the user experience while maintaining atomic file replacement.
- **Mandatory Safety Backups:** Before modifying any live save file, a non-colliding timestamped backup is automatically created in `backups\<PROFILE>\`. If a backup fails, the save is left completely untouched.
- **Native Infinite Undiscovery Recomp Integration:** Detects portable Recomp installations (`<recompRoot>\<PROFILE>\saves\`) for the five current profiles: **USA**, **USA-UNDUB**, **EUROPE**, **JAPAN**, and **ASIA**.
- **Legacy Folder Compatibility:** Older installations that still use `NTSC-U\` and `PAL\` are detected and mapped in memory to **USA** and **EUROPE** respectively. Legacy folders are read/edited in place and are never renamed, moved, or auto-migrated.
- **Strict DLC & Runtime Isolation:** Safely skips DLC and runtime directories (such as `0000000000000000\535107DB\00000002`, `cache\`, and `Headers\`), strictly targeting verified save slots (`00000001\InfiniteUndiscovery_*.bin`).
- **Achievements Exclusion:** The Recomp achievements tree (`saves\achievements\...`) is explicitly excluded from save discovery (case-insensitive). It can contain a save-shaped payload under `achievements\535107DB\00000001\...` that is **not** a playable save slot.
- **Complete Recomp Aesthetics:** Crystal fantasy deep sapphire header with gold accents, semi-translucent frosted panels, high-contrast typography, and embedded artwork (`IU_Recomp_Save_Editor_Menu.png`).
- **100% Portable:** Zero writes to Windows Registry, `%APPDATA%`, or user profile directories. Settings are saved to a clean `config.json` located directly alongside the executable.
- **Self-Contained Executable:** Embedded multi-resolution application icon (`.ico`), full 1,023-item catalog (`ItemNames.txt`), and menu artwork with no external file dependencies required at runtime.
- **Instant Bilingual Switching:** Live toggle between English (`en`) and Spanish (`es`) with 100% string parity and canonical game terminology.

---

## Profiles & Compatibility

IU Recomp v1.0.0-rc1 introduced its final **profile** model. The editor speaks that model natively:

| Profile (stable code) | UI name (EN) | UI name (ES) | Legacy folder |
|---|---|---|---|
| `USA` | USA | USA | `NTSC-U\` |
| `USA-UNDUB` | USA UNDUB (Japanese Voices) | USA UNDUB (Voces japonesas) | — |
| `EUROPE` | Europe | Europa | `PAL\` |
| `JAPAN` | Japan | Japón | — |
| `ASIA` | Asia (English) | Asia (Inglés) | — |

Behavior:

- The selector always shows all five supported profiles and marks which ones are actually installed (a `saves\` folder exists).
- `NTSC-U\` is treated as `USA`; `PAL\` is treated as `EUROPE`.
- If both a new folder and its legacy equivalent exist (`USA\` + `NTSC-U\`, or `EUROPE\` + `PAL\`), the **new** folder wins.
- Legacy folders are never modified on disk by the migration logic; mapping happens in memory only.

### Validation matrix

**REAL VALIDATION** (exercised end-to-end against real saves on disk):

| Profile | Status |
|---|---|
| `USA` | Real validation (real USA recomp install, 20 slots) |
| `NTSC-U` (legacy, mapped to `USA`) | Real validation (real legacy NTSC-U install) |

**SYNTHETIC / FORMAT VALIDATION** (profile + folder model, synthetic fixtures only):

| Profile | Status |
|---|---|
| `USA-UNDUB` | Synthetic / format validation |
| `EUROPE` | Synthetic / format validation |
| `JAPAN` | Synthetic / format validation |
| `ASIA` | Synthetic / format validation |

- The 409,600-byte `UDSV` payload, its magic, size, dual CRC32 checksums, SHA-256, atomic write, and mandatory backups are **validated against real Infinite Undiscovery saves** (retail NTSC-U, mapped to `USA`) plus synthetic fixtures.
- **No real retail saves have been validated for `USA-UNDUB`, `EUROPE`, `JAPAN`, or `ASIA`.** Those profiles are considered format-compatible only. Do not assume a saved-game payload from a different edition is interchangeable just because the editor opens it.

---

## Features by Tab

### 1. Saves (`Saves`)
- Automatic detection of valid save slots in the active Recomp profile.
- Slot metadata display: Slot number, folder name, modification timestamp, file size, current Fol, SHA-256 hash, and dual CRC status.
- In-game save thumbnail preview (`__thumbnail.png`).
- Direct Fol editing (up to 99,999,999) with real-time Tri-Ace dual CRC32 recalculation.
- One-click **Save Changes** with automatic safety backup, atomic `.tmp` swap, and instant UI refresh.

### 2. Characters (`Characters`)
- Full support for all 18 playable party members:
  *Capell, Aya, Eugene, Michelle, Kiriya, Sigmund, Edward, Komachi, Rico, Rucha, Kristofer, Balbagan, Touma, Savio, Vic, Gustav, Dominica, Seraphina*.
- Editable attributes per character:
  - Level (1 - 255) and EXP (0 - 99,999,999)
  - Current HP & Max HP (1 - 99,999)
  - Current MP & Max MP (1 - 99,999, stored at binary x1000 scale)
  - Base Stats: ATK, DEF, HIT, AGL, INT (0 - 9,999)
  - Action Points / AP (0 - 99,999)
  - Party flags: *In Party* and *Active Party*
- Quick action presets: **Max Stats** button (sets HP/MP to 9999, stats to 999, AP to 10,000).

### 3. Inventory (`Inventory`)
- Complete database of **1,023 items** (weapons, armor, accessories, consumables, materials, and grimoires) embedded directly within the binary.
- Real-time search filter by item name or ID.
- "Show Owned Only" checkbox filter.
- Individual item quantity adjustment (0 - 99) with auto-updating total owned count.
- Batch action: **Max All (x99)** button to instantly set all items in catalog to 99.

### 4. Backups (`Backups`)
- Profile-segregated backup repository (`backups\USA\`, `backups\USA-UNDUB\`, `backups\EUROPE\`, `backups\JAPAN\`, `backups\ASIA\`).
- Non-colliding timestamped filenames: `InfiniteUndiscovery_<slot>_<yyyyMMdd_HHmmss>[_seq].bin`.
- Metadata sidecar (`.meta`) storing Fol, timestamp, profile, slot number, and SHA-256 hash.
- Backup thumbnail preservation (`.png` alongside backup).
- One-click **Restore Backup** with mandatory safety backup of the active save before restoration.

### 5. Xbox 360 Save Import (`File > Import Xbox 360 Save...`)
- **Direct CON/STFS Conversion:** Seamlessly converts original Xbox 360 saves into Recomp portable format (`<PROFILE>\saves\<USER_ID>\535107DB\00000001\InfiniteUndiscovery_XXXX.bin\InfiniteUndiscovery.dat`).
- **Full Structural Parser:** Dynamically traverses STFS descriptors and file tables to extract `InfiniteUndiscovery.dat` (no hardcoded offset cuts).
- **Embedded Thumbnail Extraction:** Extracts original save thumbnail PNG (`__thumbnail.png`) from the STFS package.
- **Interactive Preview Dialog:** Previews slot number, Fol, Capell's level, Title ID, and checksum status before writing.
- **Conflict Protection & Auto-Backup:** Detects existing destination slots and prompts to replace (with mandatory automated safety backup) or renumber.
- **Read-Only Source Integrity:** Never modifies the original Xbox 360 source file.

### 6. Settings (`Settings`)
- Recomp directory discovery: Automatically detects portable Recomp installations near the editor or allows manual folder browsing.
- Profile selector: Switch seamlessly between **USA, USA-UNDUB, EUROPE, JAPAN, and ASIA** without restarting. Each entry shows whether that profile is installed.
- Live language selector: Switch between **English** and **Español** instantly.
- Portable storage status and directory shortcuts.

---

## Directory Structure

A typical deployment alongside Infinite Undiscovery Recomp v1.0.0-rc1:

```
InfiniteUndiscoveryRecomp\
├── InfiniteUndiscoveryRecomp.exe
├── setup.json
├── USA\
│   └── saves\
│       ├── 0000000000000000\      <- Strictly ignored (DLC & shared)
│       └── 1234567890ABCDEF\
│           └── 535107DB\
│               └── 00000001\      <- Save slots detected here
│                   ├── InfiniteUndiscovery_0001.bin\
│                   │   ├── InfiniteUndiscovery.dat
│                   │   └── __thumbnail.png
│                   └── InfiniteUndiscovery_0002.bin\
├── USA-UNDUB\
│   └── saves\
├── EUROPE\
│   └── saves\
├── JAPAN\
│   └── saves\
├── ASIA\
│   └── saves\
└── IU Save Bridge\
    ├── IU_Save_Bridge.exe         <- Standalone executable
    ├── config.json                <- Portable settings (profile-based)
    └── backups\
        ├── USA\
        ├── USA-UNDUB\
        ├── EUROPE\
        ├── JAPAN\
        └── ASIA\
```

Legacy installations that still use `NTSC-U\` and `PAL\` folders keep working:

```
InfiniteUndiscoveryRecomp\
├── NTSC-U\        <- detected and mapped to USA (never renamed)
│   └── saves\
└── PAL\           <- detected and mapped to EUROPE (never renamed)
    └── saves\
```

---

## Portable configuration (`config.json`)

The editor stores its settings next to the executable:

```json
{
  "language": "en",
  "recompPath": "C:\\Games\\InfiniteUndiscoveryRecomp",
  "profile": "USA"
}
```

Older `config.json` files that still use a `"region"` key are read transparently and upgraded in memory:

- `"region": "NTSC-U"` is treated as `"profile": "USA"`.
- `"region": "PAL"` is treated as `"profile": "EUROPE"`.

The next time the editor writes the configuration, it emits the new `"profile"` format only.

---

## Technical Specifications

- **Payload Size:** Exactly 409,600 bytes (`0x64000`).
- **Magic Signature:** `0x55445356` (`UDSV` in ASCII Big-Endian).
- **Format Version:** `0x00000033`.
- **Title ID:** `0x535107DB`.
- **Dual CRC32 Checksums (Tri-Ace standard polynomial `0x04C11DB7`):**
  - **CRC1 (offset `0x14`):** Checksum over the header section (offsets `0x00` to `0xE7`, 232 bytes) with both CRC fields zeroed during computation.
  - **CRC2 (offset `0x18`):** Checksum over the game data section (offsets `0xE8` to `0x63FFF`, 409,368 bytes).
- **Target Platform:** Windows x64 (.NET Framework 4.8 / Windows Forms).

The save format itself, all known offsets, and the CRC logic are unchanged from v2.2.0.

---

## CLI Reference

`IU_Save_Bridge.exe` supports command-line automation:

```bash
# Verify integrity, magic, version, Fol, SHA-256, and dual CRC32 checksums
IU_Save_Bridge.exe verify <path_to_save.dat>

# Modify Fol directly from terminal with automatic CRC recalculation
IU_Save_Bridge.exe set-fol <input.dat> <output.dat> <amount>

# Display command line syntax and available options
IU_Save_Bridge.exe --help
```

---

## License & Credits

- Developed for [Infinite Undiscovery Recomp](https://github.com/doc-haz/infinite-undiscovery-recomp).
- Infinite Undiscovery © Square Enix / tri-Ace.
