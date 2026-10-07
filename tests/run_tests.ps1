$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "  IU Save Bridge v2.3.0 - Automated Test Suite"
Write-Host "  (profile model v1.0.0-rc1 + legacy NTSC-U/PAL)"
Write-Host "================================================="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$testDir = Join-Path $scriptDir "work"

# Always regenerate a clean work folder so fixture mutations never leak between runs.
if (Test-Path $testDir) { Remove-Item $testDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $testDir | Out-Null

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

$sources = @(
    "$repoRoot\src\AssemblyInfo.cs",
    "$repoRoot\src\SafePath.cs",
    "$repoRoot\src\SavePayload.cs",
    "$repoRoot\src\CharacterData.cs",
    "$repoRoot\src\ItemData.cs",
    "$repoRoot\src\SaveManager.cs",
    "$repoRoot\src\StfsReader.cs",
    "$repoRoot\src\Loc.cs",
    "$repoRoot\src\MainForm.cs",
    "$repoRoot\src\Program.cs"
)

# Compile IU_Save_Bridge.exe into the work directory
$bridgeExe = Join-Path $testDir "IU_Save_Bridge.exe"
Write-Host "Compiling test binary from source..."
& $csc /nologo /target:winexe /platform:x64 /optimize+ `
  /win32icon:"$repoRoot\assets\IU_Recomp_Save_Editor.ico" `
  /resource:"$repoRoot\assets\IU_Recomp_Save_Editor.ico,IU_Recomp_Save_Editor.ico" `
  /resource:"$repoRoot\assets\IU_Recomp_Save_Editor_Menu.png,IU_Recomp_Save_Editor_Menu.png" `
  /resource:"$repoRoot\resources\ItemNames.txt,ItemNames.txt" `
  /out:$bridgeExe `
  $sources | Out-Null

if ($LASTEXITCODE -ne 0) { throw "Compilation of IU_Save_Bridge failed!" }
Write-Host "[PASS] Test binary compiled successfully."

# ------------------------------------------------------------------
# Synthesize the synthetic Recomp environments (profiles + legacy + mixed)
# ------------------------------------------------------------------
$fixtureRecomp = Join-Path $testDir "MockRecomp"
$fixtureLegacy = Join-Path $testDir "MockLegacyRecomp"
$fixtureMixed  = Join-Path $testDir "MockMixedRecomp"

Write-Host "Synthesizing synthetic MockRecomp environments in work folder..."
$genCs = Join-Path $testDir "GenFixtures.cs"
$genExe = Join-Path $testDir "GenFixtures.exe"
$genSrc = @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using IUSaveBridge;

class Generator {
    static byte[] MakeSave(uint fol, int capellLvl, int ayaLvl) {
        byte[] data = new byte[409600];
        data[0] = 0x55; data[1] = 0x44; data[2] = 0x53; data[3] = 0x56;
        data[4] = 0; data[5] = 0; data[6] = 0; data[7] = 0x33;
        data[8] = 0x53; data[9] = 0x51; data[10] = 0x07; data[11] = 0xDB;

        // Fol at 0x2898
        SavePayload.WriteUInt32BE(data, 0x2898, fol);

        // Capell
        int capOff = 0x218F8;
        SavePayload.WriteUInt32BE(data, capOff + 0x00, 1);
        data[capOff + 0x08] = 0x08; data[capOff + 0x09] = 0x00;
        data[capOff + 0x0A] = 0x00; data[capOff + 0x0B] = 0x01;
        SavePayload.WriteUInt32BE(data, capOff + 0x18, (uint)capellLvl);
        SavePayload.WriteUInt32BE(data, capOff + 0x24, (uint)(capellLvl * 1000));
        SavePayload.WriteUInt32BE(data, capOff + 0x28, 1250);
        SavePayload.WriteUInt32BE(data, capOff + 0x2C, 1250);
        SavePayload.WriteUInt32BE(data, capOff + 0x30, 250000);
        SavePayload.WriteUInt32BE(data, capOff + 0x34, 250000);
        SavePayload.WriteUInt32BE(data, capOff + 0x38, 180);
        SavePayload.WriteUInt32BE(data, capOff + 0x3C, 120);
        SavePayload.WriteUInt32BE(data, capOff + 0x40, 95);
        SavePayload.WriteUInt32BE(data, capOff + 0x44, 110);
        SavePayload.WriteUInt32BE(data, capOff + 0x48, 85);
        SavePayload.WriteUInt32BE(data, capOff + 0x4C, 500);

        // Aya
        int ayaOff = 0x218F8 + 4840;
        SavePayload.WriteUInt32BE(data, ayaOff + 0x00, 2);
        data[ayaOff + 0x08] = 0x08; data[ayaOff + 0x09] = 0x00;
        SavePayload.WriteUInt32BE(data, ayaOff + 0x18, (uint)ayaLvl);
        SavePayload.WriteUInt32BE(data, ayaOff + 0x28, 980);
        SavePayload.WriteUInt32BE(data, ayaOff + 0x2C, 980);
        SavePayload.WriteUInt32BE(data, ayaOff + 0x30, 320000);
        SavePayload.WriteUInt32BE(data, ayaOff + 0x34, 320000);

        // Seraphina (character 17)
        int serOff = 0x218F8 + (17 * 4840);
        SavePayload.WriteUInt32BE(data, serOff + 0x00, 18);
        SavePayload.WriteUInt32BE(data, serOff + 0x18, 50);

        // Items at 0x6248
        int it1 = 0x6248;
        data[it1 + 0] = 0; data[it1 + 1] = 1;
        data[it1 + 2] = 0; data[it1 + 3] = 1;
        data[it1 + 4] = 1; data[it1 + 5] = 0; data[it1 + 6] = 1; data[it1 + 7] = 0;

        int it2 = 0x6248 + 8;
        data[it2 + 0] = 0; data[it2 + 1] = 2;
        data[it2 + 2] = 0; data[it2 + 3] = 5;
        data[it2 + 4] = 1; data[it2 + 5] = 0; data[it2 + 6] = 1; data[it2 + 7] = 0;

        SavePayload p = new SavePayload(data);
        p.RecalculateChecksums();
        return p.Data;
    }

    static void MakeThumb(string path) {
        using (Bitmap b = new Bitmap(64, 64)) {
            using (Graphics g = Graphics.FromImage(b)) {
                g.Clear(Color.FromArgb(16, 32, 64));
                using (Pen p = new Pen(Color.Goldenrod, 2)) {
                    g.DrawRectangle(p, 4, 4, 56, 56);
                }
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    static void NewRecomp(string root) {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "InfiniteUndiscoveryRecomp.exe"), "");
        File.WriteAllText(Path.Combine(root, "setup.json"), "{\r\n  \"language\": \"en\",\r\n  \"portable\": true\r\n}\r\n");
    }

    static void WriteSlot(string recomp, string folder, string user, int slot, uint fol, int capell, int aya, bool thumb) {
        string dir = Path.Combine(recomp, folder, "saves", user, "535107DB", "00000001",
            string.Format("InfiniteUndiscovery_{0:D4}.bin", slot));
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "InfiniteUndiscovery.dat"), MakeSave(fol, capell, aya));
        if (thumb) MakeThumb(Path.Combine(dir, "__thumbnail.png"));
    }

    static void WriteAchievementsDecoy(string recomp, string folder, int slot, uint fol) {
        string dir = Path.Combine(recomp, folder, "saves", "achievements", "535107DB", "00000001",
            string.Format("InfiniteUndiscovery_{0:D4}.bin", slot));
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "InfiniteUndiscovery.dat"), MakeSave(fol, 1, 1));
    }

    static void Main(string[] args) {
        string user = "1234567890ABCDEF";

        // 1) Full profile set + DLC decoy
        string main = args[0];
        NewRecomp(main);
        string dlcDir = Path.Combine(main, "USA", "saves", "0000000000000000", "535107DB", "00000002", "mock_dlc.bin");
        Directory.CreateDirectory(dlcDir);
        File.WriteAllText(Path.Combine(dlcDir, "mock_dlc.dat"), "MOCK DLC");

        WriteSlot(main, "USA", user, 1, 50000, 15, 12, true);
        WriteSlot(main, "USA", user, 2, 999999, 45, 40, true);
        WriteSlot(main, "USA-UNDUB", user, 1, 33333, 10, 9, true);
        WriteSlot(main, "EUROPE", user, 1, 120000, 20, 18, true);
        WriteSlot(main, "JAPAN", user, 1, 44444, 11, 10, true);
        WriteSlot(main, "ASIA", user, 1, 55555, 12, 11, true);
        WriteAchievementsDecoy(main, "USA", 1, 70001);

        // 2) Legacy-only install
        string legacy = args[1];
        NewRecomp(legacy);
        WriteSlot(legacy, "NTSC-U", user, 3, 777700, 30, 25, true);
        WriteSlot(legacy, "PAL", user, 4, 888800, 35, 30, true);
        WriteAchievementsDecoy(legacy, "NTSC-U", 1, 70002);

        // 3) Mixed install (native + legacy for the same profile)
        string mixed = args[2];
        NewRecomp(mixed);
        WriteSlot(mixed, "USA", user, 1, 111111, 12, 11, false);
        WriteSlot(mixed, "NTSC-U", user, 1, 222222, 22, 21, false);
        WriteSlot(mixed, "EUROPE", user, 1, 333333, 33, 32, false);
        WriteSlot(mixed, "PAL", user, 1, 444444, 44, 43, false);
    }
}
'@
Set-Content -Path $genCs -Value $genSrc
& $csc /nologo /target:exe /out:$genExe /reference:$bridgeExe $genCs | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Compilation of the fixture generator failed!" }
& $genExe "$fixtureRecomp" "$fixtureLegacy" "$fixtureMixed"
if ($LASTEXITCODE -ne 0) { throw "Fixture generation failed!" }
Remove-Item $genCs, $genExe -Force
Write-Host "[PASS] Synthetic environments generated (profiles + legacy + mixed)."

$slot1Fixture = Join-Path $fixtureRecomp "USA\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0001.bin\InfiniteUndiscovery.dat"
$slot2Fixture = Join-Path $fixtureRecomp "USA\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0002.bin\InfiniteUndiscovery.dat"
$dlcFixture = Join-Path $fixtureRecomp "USA\saves\0000000000000000\535107DB\00000002\mock_dlc.bin\mock_dlc.dat"

if (!(Test-Path $slot1Fixture)) { throw "Fixture slot 1 missing: $slot1Fixture" }
if (!(Test-Path $slot2Fixture)) { throw "Fixture slot 2 missing: $slot2Fixture" }

$slot1Hash = (Get-FileHash $slot1Fixture -Algorithm SHA256).Hash
$slot2Hash = (Get-FileHash $slot2Fixture -Algorithm SHA256).Hash
Write-Host "Baseline Slot 1 SHA256: $slot1Hash"
Write-Host "Baseline Slot 2 SHA256: $slot2Hash"

# Work on copies
$slot2Copy = Join-Path $testDir "Slot2_Copy.dat"
Copy-Item $slot2Fixture $slot2Copy -Force

Write-Host "`n[TEST 1] CLI verify command on valid save..."
& $bridgeExe verify $slot2Copy | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Test 1 failed: verify returned non-zero!" }
Write-Host "[PASS] Test 1: CLI verify completed successfully."

Write-Host "`n[TEST 2] CLI set-fol command and verify CRC32 recalculation..."
$slot2Modified = Join-Path $testDir "Slot2_Modified.dat"
& $bridgeExe set-fol $slot2Copy $slot2Modified 777777 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Test 2 failed: set-fol returned non-zero!" }
& $bridgeExe verify $slot2Modified | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Test 2 failed: modified save verification failed!" }
Write-Host "[PASS] Test 2: Fol modified and dual CRC32 recalculated."

Write-Host "`n[TEST 3] Reject invalid magic..."
$badMagic = Join-Path $testDir "bad_magic.dat"
$badBytes = [System.IO.File]::ReadAllBytes($slot2Copy)
$badBytes[0] = 0x58
[System.IO.File]::WriteAllBytes($badMagic, $badBytes)
& $bridgeExe verify $badMagic | Out-Null
if ($LASTEXITCODE -eq 0) { throw "Test 3 failed: Should have rejected invalid magic!" }
Write-Host "[PASS] Test 3: Correctly rejected invalid magic."

Write-Host "`n[TEST 4] Reject invalid size..."
$badSize = Join-Path $testDir "bad_size.dat"
[System.IO.File]::WriteAllBytes($badSize, (New-Object byte[] 1000))
& $bridgeExe verify $badSize | Out-Null
if ($LASTEXITCODE -eq 0) { throw "Test 4 failed: Should have rejected invalid size!" }
Write-Host "[PASS] Test 4: Correctly rejected invalid size."

Write-Host "`n[TEST 5] Safety check: Reject writing into live input file..."
& $bridgeExe set-fol $slot2Copy $slot2Copy 999 | Out-Null
if ($LASTEXITCODE -eq 0) { throw "Test 5 failed: Should have blocked writing into same input path!" }
Write-Host "[PASS] Test 5: Input path protection verified."

Write-Host "`n[TEST 6] Safety check: Verify original fixtures remain 100% UNTOUCHED..."
if ((Get-FileHash $slot1Fixture -Algorithm SHA256).Hash -ne $slot1Hash) { throw "CRITICAL: Slot 1 save was modified!" }
if ((Get-FileHash $slot2Fixture -Algorithm SHA256).Hash -ne $slot2Hash) { throw "CRITICAL: Slot 2 save was modified!" }
if (!(Test-Path $dlcFixture)) { throw "CRITICAL: DLC decoy fixture was removed!" }
Write-Host "[PASS] Test 6: Fixture saves are 100% intact and unchanged."

Write-Host "`n================================================="
Write-Host "  RUNNING COMPREHENSIVE PROFILE TEST SUITE..."
Write-Host "================================================="

$suiteExe = Join-Path $testDir "TestSuite.exe"
$suiteSrc = Join-Path $scriptDir "TestSuite.cs"
& $csc /nologo /target:exe /out:$suiteExe /reference:$bridgeExe $suiteSrc | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Compilation of TestSuite failed!" }

& "$suiteExe" "$fixtureRecomp" "$fixtureLegacy" "$fixtureMixed"
if ($LASTEXITCODE -ne 0) { throw "TestSuite failed!" }

Write-Host "`n================================================="
Write-Host "  ALL TESTS PASSED SUCCESSFULLY (100% SUITE PASS)!"
Write-Host "================================================="
