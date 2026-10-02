<p align="center">
  <img src="assets/banner.png" alt="Infinite Undiscovery Recomp Save Editor" width="100%" />
</p>

# Infinite Undiscovery Recomp Save Editor (IU Save Bridge v2.2.0)

**IU Save Bridge v2.2.0** is the official companion save editor designed specifically for [**Infinite Undiscovery Recomp**](https://github.com/doc-haz/infinite-undiscovery-recomp).

It provides a modern, high-contrast, crystal fantasy user interface and direct save manipulation with zero staging complexity, mandatory automated timestamped backups, and byte-perfect dual Tri-Ace CRC32 integrity verification.

---

## Key Highlights

- **Direct & Transparent Workflow:** `Open Save -> Edit -> Save Changes`. The editor eliminates legacy staging folders and CON/STFS wrapping from the user experience while maintaining atomic file replacement.
- **Mandatory Safety Backups:** Before modifying any live save file, a non-colliding timestamped backup is automatically created in `backups\<REGION>\`. If a backup fails, the save is left completely untouched.
- **Native Infinite Undiscovery Recomp Integration:** Detects portable Recomp installations (`<recompRoot>\<REGION>\saves\`) supporting both **NTSC-U** and **PAL** regions.
- **Strict DLC & Runtime Isolation:** Safely skips DLC and runtime directories (such as `0000000000000000\535107DB\00000002` and `cache\`), strictly targeting verified save slots (`00000001\InfiniteUndiscovery_*.bin`).
- **Complete Recomp Aesthetics:** Crystal fantasy deep sapphire header with gold accents, semi-translucent frosted panels, high-contrast typography, and embedded artwork (`IU_Recomp_Save_Editor_Menu.png`).
- **100% Portable:** Zero writes to Windows Registry, `%APPDATA%`, or user profile directories. Settings are saved to a clean `config.json` located directly alongside the executable.
- **Self-Contained Executable:** Embedded multi-resolution application icon (`.ico`), full 1,023-item catalog (`ItemNames.txt`), and menu artwork with no external file dependencies required at runtime.
- **Instant Bilingual Switching:** Live toggle between English (`en`) and Spanish (`es`) with 100% string parity and canonical game terminology.

---

## Features by Tab

### 1. Saves (`Saves`)
- Automatic detection of valid save slots in the active Recomp region.
- Slot metadata display: Slot number, folder name, modification timestamp, file size, current Fol, SHA-256 hash, and dual CRC status.
- In-game save thumbnail preview (`__thumbnail.png`).
- Direct Fol editing (up to 99,999,999) with real-time Tri-Ace dual CRC32 recalculation.
- One-click **Save Changes** with automatic safety backup, atomic `.tmp` swap, and instant UI refresh.

### 2. Characters (`Characters`)
- Full support for all 18 playable party members:
  *Capell, Aya, Eugene, Michelle, Kiriya, Sigmund, Edward, Komachi, Rico, Rucha, Kristofer, Balbagan, Touma, Savio, Vic, Gustav, Dominica, Seraphina*.
- Editable attributes per character:
  - Level (1 – 255) and EXP (0 – 99,999,999)
  - Current HP & Max HP (1 – 99,999)
  - Current MP & Max MP (1 – 99,999, stored at binary x1000 scale)
  - Base Stats: ATK, DEF, HIT, AGL, INT (0 – 9,999)
  - Action Points / AP (0 – 99,999)
  - Party flags: *In Party* and *Active Party*
- Quick action presets: **Max Stats** button (sets HP/MP to 9999, stats to 999, AP to 10,000).

### 3. Inventory (`Inventory`)
- Complete database of **1,023 items** (weapons, armor, accessories, consumables, materials, and grimoires) embedded directly within the binary.
- Real-time search filter by item name or ID.
- "Show Owned Only" checkbox filter.
- Individual item quantity adjustment (0 – 99) with auto-updating total owned count.
- Batch action: **Max All (x99)** button to instantly set all items in catalog to 99.

### 4. Backups (`Backups`)
- Region-segregated backup repository (`backups\NTSC-U\` and `backups\PAL\`).
- Non-colliding timestamped filenames: `InfiniteUndiscovery_<slot>_<yyyyMMdd_HHmmss>[_seq].bin`.
- Metadata sidecar (`.meta`) storing Fol, timestamp, slot number, and SHA-256 hash.
- Backup thumbnail preservation (`.png` alongside backup).
- One-click **Restore Backup** with mandatory safety backup of the active save before restoration.

### 5. Settings (`Settings`)
- Recomp directory discovery: Automatically detects portable Recomp installations near the editor or allows manual folder browsing.
- Region selector: Switch seamlessly between **NTSC-U** and **PAL** saves without restarting.
- Live language selector: Switch between **English** and **Español** instantly.
- Portable storage status and directory shortcuts.

---

## Directory Structure

A typical deployment alongside Infinite Undiscovery Recomp:

```
InfiniteUndiscoveryRecomp\
├── InfiniteUndiscoveryRecomp.exe
├── setup.json
├── NTSC-U\
│   └── saves\
│       ├── 0000000000000000\      <- Strictly ignored (DLC & shared)
│       └── 1234567890ABCDEF\
│           └── 535107DB\
│               └── 00000001\      <- Save slots detected here
│                   ├── InfiniteUndiscovery_0001.bin\
│                   │   ├── InfiniteUndiscovery.dat
│                   │   └── __thumbnail.png
│                   └── InfiniteUndiscovery_0002.bin\
├── PAL\
│   └── saves\
└── IU Save Bridge\
    ├── IU_Save_Bridge.exe         <- Standalone executable
    ├── config.json                <- Portable settings
    └── backups\
        ├── NTSC-U\
        └── PAL\
```

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
