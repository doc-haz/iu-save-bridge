$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "  IU Save Bridge v2.2.0 - Automated Test Suite"
Write-Host "================================================="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$testDir = Join-Path $scriptDir "work"
if (! (Test-Path $testDir)) { New-Item -ItemType Directory -Force -Path $testDir | Out-Null }

# Compile IU_Save_Bridge.exe into work directory
$bridgeExe = Join-Path $testDir "IU_Save_Bridge.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
Write-Host "Compiling test binary from source..."
& $csc /target:winexe /platform:x64 /optimize+ `
  /win32icon:"$repoRoot\assets\IU_Recomp_Save_Editor.ico" `
  /resource:"$repoRoot\assets\IU_Recomp_Save_Editor.ico,IU_Recomp_Save_Editor.ico" `
  /resource:"$repoRoot\assets\IU_Recomp_Save_Editor_Menu.png,IU_Recomp_Save_Editor_Menu.png" `
  /resource:"$repoRoot\resources\ItemNames.txt,ItemNames.txt" `
  /out:$bridgeExe `
  "$repoRoot\src\AssemblyInfo.cs" `
  "$repoRoot\src\SafePath.cs" `
  "$repoRoot\src\SavePayload.cs" `
  "$repoRoot\src\CharacterData.cs" `
  "$repoRoot\src\ItemData.cs" `
  "$repoRoot\src\SaveManager.cs" `
  "$repoRoot\src\Loc.cs" `
  "$repoRoot\src\MainForm.cs" `
  "$repoRoot\src\Program.cs" | Out-Null

if ($LASTEXITCODE -ne 0) { throw "Compilation of IU_Save_Bridge failed!" }
Write-Host "[PASS] Test binary compiled successfully."

# Dynamically generate synthetic MockRecomp test environment
$fixtureRecomp = Join-Path $testDir "MockRecomp"
if (! (Test-Path (Join-Path $fixtureRecomp "setup.json"))) {
    Write-Host "Synthesizing dynamic MockRecomp test environment in work folder..."
    $genCs = Join-Path $testDir "GenFixtures.cs"
    $genExe = Join-Path $testDir "GenFixtures.exe"
    $genSrc = @"
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

    static void Main(string[] args) {
        string target = args[0];
        Directory.CreateDirectory(target);

        File.WriteAllText(Path.Combine(target, "InfiniteUndiscoveryRecomp.exe"), "");
        File.WriteAllText(Path.Combine(target, "setup.json"), "{\r\n  \"language\": \"en\",\r\n  \"portable\": true\r\n}\r\n");

        string dlcDir = Path.Combine(target, @"NTSC-U\saves\0000000000000000\535107DB\00000002\mock_dlc.bin");
        Directory.CreateDirectory(dlcDir);
        File.WriteAllText(Path.Combine(dlcDir, "mock_dlc.dat"), "MOCK DLC");

        string userNtsc = Path.Combine(target, @"NTSC-U\saves\1234567890ABCDEF\535107DB\00000001");
        string slot1Dir = Path.Combine(userNtsc, "InfiniteUndiscovery_0001.bin");
        string slot2Dir = Path.Combine(userNtsc, "InfiniteUndiscovery_0002.bin");
        Directory.CreateDirectory(slot1Dir);
        Directory.CreateDirectory(slot2Dir);

        File.WriteAllBytes(Path.Combine(slot1Dir, "InfiniteUndiscovery.dat"), MakeSave(50000, 15, 12));
        MakeThumb(Path.Combine(slot1Dir, "__thumbnail.png"));

        File.WriteAllBytes(Path.Combine(slot2Dir, "InfiniteUndiscovery.dat"), MakeSave(999999, 45, 40));
        MakeThumb(Path.Combine(slot2Dir, "__thumbnail.png"));

        string userPal = Path.Combine(target, @"PAL\saves\1234567890ABCDEF\535107DB\00000001");
        string palSlot1 = Path.Combine(userPal, "InfiniteUndiscovery_0001.bin");
        Directory.CreateDirectory(palSlot1);
        File.WriteAllBytes(Path.Combine(palSlot1, "InfiniteUndiscovery.dat"), MakeSave(120000, 20, 18));
        MakeThumb(Path.Combine(palSlot1, "__thumbnail.png"));
    }
}
"@
    Set-Content -Path $genCs -Value $genSrc
    & $csc /target:exe /out:$genExe /reference:$bridgeExe $genCs | Out-Null
    & $genExe "$fixtureRecomp"
    Remove-Item $genCs, $genExe -Force
    Write-Host "[PASS] Synthetic MockRecomp generated."
}

$slot1Fixture = Join-Path $fixtureRecomp "NTSC-U\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0001.bin\InfiniteUndiscovery.dat"
$slot2Fixture = Join-Path $fixtureRecomp "NTSC-U\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0002.bin\InfiniteUndiscovery.dat"

# Baseline hashes
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
$badSizeBytes = New-Object byte[] 1000
[System.IO.File]::WriteAllBytes($badSize, $badSizeBytes)
& $bridgeExe verify $badSize | Out-Null
if ($LASTEXITCODE -eq 0) { throw "Test 4 failed: Should have rejected invalid size!" }
Write-Host "[PASS] Test 4: Correctly rejected invalid size."

Write-Host "`n[TEST 5] Safety check: Reject writing into live input file..."
& $bridgeExe set-fol $slot2Copy $slot2Copy 999 | Out-Null
if ($LASTEXITCODE -eq 0) { throw "Test 5 failed: Should have blocked writing into same input path!" }
Write-Host "[PASS] Test 5: Input path protection verified."

Write-Host "`n[TEST 6] Safety check: Verify original fixtures remain 100% UNTOUCHED..."
$slot1Check = (Get-FileHash $slot1Fixture -Algorithm SHA256).Hash
$slot2Check = (Get-FileHash $slot2Fixture -Algorithm SHA256).Hash

if ($slot1Check -ne $slot1Hash) { throw "CRITICAL: Slot 1 save was modified!" }
if ($slot2Check -ne $slot2Hash) { throw "CRITICAL: Slot 2 save was modified!" }
Write-Host "[PASS] Test 6: Fixture saves are 100% intact and unchanged."

Write-Host "`n================================================="
Write-Host "  RUNNING 28-POINT V2.2.0 COMPREHENSIVE SUITE..."
Write-Host "================================================="

$suiteExe = Join-Path $testDir "TestV22Suite.exe"
$suiteSrc = Join-Path $scriptDir "TestV22Suite.cs"
& $csc /target:exe /out:$suiteExe /reference:$bridgeExe $suiteSrc | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Compilation of TestV22Suite failed!" }

& "$suiteExe"
if ($LASTEXITCODE -ne 0) { throw "TestV22Suite failed!" }

Write-Host "`n================================================="
Write-Host "  ALL TESTS PASSED SUCCESSFULLY (100% SUITE PASS)!"
Write-Host "================================================="
