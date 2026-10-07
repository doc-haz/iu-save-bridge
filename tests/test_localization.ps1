$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "  IU Save Editor v2.3 - Localization Test Suite"
Write-Host "================================================="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$workDir = Join-Path $scriptDir "work"
if (! (Test-Path $workDir)) { New-Item -ItemType Directory -Force -Path $workDir | Out-Null }

$fixtureDat = Join-Path $workDir "MockRecomp\USA\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0001.bin\InfiniteUndiscovery.dat"
$slot2Copy = Join-Path $workDir "Slot2_Copy.dat"
if (Test-Path $fixtureDat) {
    Copy-Item $fixtureDat $slot2Copy -Force
} else {
    $synthData = New-Object byte[] 409600
    $synthData[0] = 0x55; $synthData[1] = 0x44; $synthData[2] = 0x53; $synthData[3] = 0x56
    $synthData[7] = 0x33
    $synthData[8] = 0x53; $synthData[9] = 0x51; $synthData[10] = 0x07; $synthData[11] = 0xDB
    [System.IO.File]::WriteAllBytes($slot2Copy, $synthData)
}

# Compile temporary test binary if needed
$bridgeExe = Join-Path $workDir "IU_Save_Bridge.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
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
  "$repoRoot\src\StfsReader.cs" `
  "$repoRoot\src\Loc.cs" `
  "$repoRoot\src\MainForm.cs" `
  "$repoRoot\src\Program.cs" | Out-Null

$locTestScript = @"
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using IUSaveBridge;

class LocTestSuite {
    [STAThread]
    static int Main() {
        int failures = 0;

        Console.WriteLine("\n[LOC TEST 1] Verifying Resource Key Parity (English vs Spanish)...");
        var allKeys = Loc.GetAllKeys();
        int keyCount = 0;
        foreach (var k in allKeys) {
            keyCount++;
            if (!Loc.HasKey("en", k)) {
                Console.WriteLine("  [FAIL] Missing English key: " + k);
                failures++;
            }
            if (!Loc.HasKey("es", k)) {
                Console.WriteLine("  [FAIL] Missing Spanish key: " + k);
                failures++;
            }
        }
        Console.WriteLine(string.Format("  Total keys verified: {0}. Missing keys: {1}", keyCount, failures));
        if (failures > 0) return 1;
        Console.WriteLine("  [PASS] 100% key parity confirmed with zero missing translations!");

        Console.WriteLine("\n[LOC TEST 2] Testing Default Language on Clean Startup...");
        string cfgPath = SaveManager.ConfigFilePath;
        if (File.Exists(cfgPath)) File.Delete(cfgPath);
        string defaultLang = SaveManager.LoadLanguagePreference();
        if (defaultLang != "en") {
            Console.WriteLine("  [FAIL] Default language must be 'en', got: " + defaultLang);
            return 2;
        }
        Console.WriteLine("  [PASS] Default language confirmed: English ('en').");

        Console.WriteLine("\n[LOC TEST 3] Testing Language Persistence in config.json...");
        SaveManager.SaveLanguagePreference("es");
        string loadedLang = SaveManager.LoadLanguagePreference();
        if (loadedLang != "es") {
            Console.WriteLine("  [FAIL] Saved 'es' but loaded: " + loadedLang);
            return 3;
        }
        SaveManager.SaveLanguagePreference("en");
        loadedLang = SaveManager.LoadLanguagePreference();
        if (loadedLang != "en") {
            Console.WriteLine("  [FAIL] Saved 'en' but loaded: " + loadedLang);
            return 3;
        }
        Console.WriteLine("  [PASS] Language persistence round-trip verified (en <-> es).");

        Console.WriteLine("\n[LOC TEST 4] Proving Language Switching does NOT alter Save Output (Binary Parity)...");
        string slotSrc = @"$slot2Copy";

        Loc.SetLanguage("en");
        SavePayload pEn = SavePayload.FromFile(slotSrc);
        pEn.SetFol(777777);
        CharacterData cEn = pEn.GetCharacter(0);
        cEn.Level = 45;
        cEn.MaxHp = 8888;
        cEn.CurrentHp = 8888;
        cEn.Atk = 888;
        pEn.SaveCharacter(cEn);
        string outEn = Path.Combine(@"$workDir", "Save_Edited_EN.dat");
        pEn.SaveToFile(outEn);

        Loc.SetLanguage("es");
        SavePayload pEs = SavePayload.FromFile(slotSrc);
        pEs.SetFol(777777);
        CharacterData cEs = pEs.GetCharacter(0);
        cEs.Level = 45;
        cEs.MaxHp = 8888;
        cEs.CurrentHp = 8888;
        cEs.Atk = 888;
        pEs.SaveCharacter(cEs);
        string outEs = Path.Combine(@"$workDir", "Save_Edited_ES.dat");
        pEs.SaveToFile(outEs);

        byte[] bEn = File.ReadAllBytes(outEn);
        byte[] bEs = File.ReadAllBytes(outEs);
        if (bEn.Length != bEs.Length) {
            Console.WriteLine("  [FAIL] Size mismatch between EN and ES outputs!");
            return 4;
        }
        for (int i = 0; i < bEn.Length; i++) {
            if (bEn[i] != bEs[i]) {
                Console.WriteLine("  [FAIL] Byte mismatch at offset 0x" + i.ToString("X"));
                return 4;
            }
        }
        Console.WriteLine("  EN SHA256: " + pEn.Sha256Hash);
        Console.WriteLine("  ES SHA256: " + pEs.Sha256Hash);
        Console.WriteLine("  [PASS] Both English and Spanish workflows produce 100% byte-identical DAT output!");

        return 0;
    }
}
"@

$locCs = Join-Path $workDir "LocTestSuite.cs"
$locExe = Join-Path $workDir "LocTestSuite.exe"
[System.IO.File]::WriteAllText($locCs, $locTestScript)
& $csc /nologo /r:"$bridgeExe" /out:"$locExe" "$locCs"
& "$locExe"
if ($LASTEXITCODE -ne 0) { throw "Localization test suite failed with exit code $LASTEXITCODE!" }

Write-Host "`n================================================="
Write-Host "  ALL LOCALIZATION TESTS PASSED SUCCESSFULLY!"
Write-Host "================================================="
