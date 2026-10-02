$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "  IU Save Bridge v2.2 - Extended Feature Tests"
Write-Host "================================================="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$workDir = Join-Path $scriptDir "work"
if (! (Test-Path $workDir)) { New-Item -ItemType Directory -Force -Path $workDir | Out-Null }

$fixtureDat = Join-Path $scriptDir "fixtures\MockRecomp\NTSC-U\saves\1234567890ABCDEF\535107DB\00000001\InfiniteUndiscovery_0001.bin\InfiniteUndiscovery.dat"
$slot2Copy = Join-Path $workDir "Slot2_Copy.dat"
Copy-Item $fixtureDat $slot2Copy -Force

# Compile test binary
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
  "$repoRoot\src\Loc.cs" `
  "$repoRoot\src\MainForm.cs" `
  "$repoRoot\src\Program.cs" | Out-Null

# 1. Test CLI set-fol vs Core set-fol byte-perfection
Write-Host "`n[TEST 1] Proving CLI set-fol vs Core/GUI set-fol produce 100% byte-identical output..."
$cliOutput = Join-Path $workDir "Fol_CLI_123456.dat"
& $bridgeExe set-fol $slot2Copy $cliOutput 123456
if ($LASTEXITCODE -ne 0) { throw "CLI set-fol failed!" }

$coreScript = @"
using System;
using System.IO;
using IUSaveBridge;

class CoreTest {
    static void Main() {
        SavePayload p = SavePayload.FromFile(@"$slot2Copy");
        p.SetFol(123456);
        p.SaveToFile(@"$workDir\Fol_Core_123456.dat");
    }
}
"@
$coreCs = Join-Path $workDir "CoreTest.cs"
$coreExe = Join-Path $workDir "CoreTest.exe"
[System.IO.File]::WriteAllText($coreCs, $coreScript)
& $csc /nologo /r:"$bridgeExe" /out:"$coreExe" "$coreCs"
& "$coreExe"

$cliHash = (Get-FileHash $cliOutput -Algorithm SHA256).Hash
$coreHash = (Get-FileHash (Join-Path $workDir "Fol_Core_123456.dat") -Algorithm SHA256).Hash

Write-Host "  CLI Output SHA256:  $cliHash"
Write-Host "  Core Output SHA256: $coreHash"

if ($cliHash -ne $coreHash) {
    throw "TEST 1 FAILED! Hashes do not match!"
}
Write-Host "[PASS] Test 1: CLI and Core/GUI produce 100% byte-identical binary outputs!"

# 2. Test Character Modification and Checksum Validation
Write-Host "`n[TEST 2] Testing Character Stats Modification and Dual-CRC Recalculation..."
$charScript = @"
using System;
using System.IO;
using IUSaveBridge;

class CharTest {
    static void Main() {
        SavePayload p = SavePayload.FromFile(@"$slot2Copy");
        CharacterData c = p.GetCharacter(0); // Capell
        c.Level = 99;
        c.Exp = 1234567;
        c.CurrentHp = 9999;
        c.MaxHp = 9999;
        c.CurrentMp = 9999;
        c.MaxMp = 9999;
        c.Atk = 999;
        c.Def = 999;
        c.Agl = 999;
        c.Hit = 999;
        c.Int = 999;
        c.Ap = 10000;
        p.SaveCharacter(c);

        string outDat = Path.Combine(@"$workDir", "Capell_Mod.dat");
        p.SaveToFile(outDat);

        SavePayload reloaded = SavePayload.FromFile(outDat);
        if (reloaded.StoredCrc1 != reloaded.CalculatedCrc1 || reloaded.StoredCrc2 != reloaded.CalculatedCrc2) {
            Console.WriteLine("CRC Validation FAILED!");
            Environment.Exit(1);
        }
        CharacterData reloadedChar = reloaded.GetCharacter(0);
        if (reloadedChar.Level != 99 || reloadedChar.MaxHp != 9999 || reloadedChar.CurrentMp != 9999 || reloadedChar.Atk != 999) {
            Console.WriteLine("Stat reload verification FAILED!");
            Environment.Exit(2);
        }
        Console.WriteLine("Capell modification verified with 100% CRC integrity!");
    }
}
"@
$charCs = Join-Path $workDir "CharTest.cs"
$charExe = Join-Path $workDir "CharTest.exe"
[System.IO.File]::WriteAllText($charCs, $charScript)
& $csc /nologo /r:"$bridgeExe" /out:"$charExe" "$charCs"
& "$charExe"
if ($LASTEXITCODE -ne 0) { throw "Test 2 failed!" }
Write-Host "[PASS] Test 2: Character modification and validation verified."

# 3. Test Inventory Modification
Write-Host "`n[TEST 3] Testing Inventory Adjustment..."
$itemScript = @"
using System;
using System.IO;
using IUSaveBridge;

class ItemTest {
    static void Main() {
        SavePayload p = SavePayload.FromFile(@"$slot2Copy");
        var items = p.GetAllItems();
        if (items.Count != 1023) {
            Console.WriteLine("Expected 1023 items, got: " + items.Count);
            Environment.Exit(1);
        }
        items[0].Amount = 77;
        p.SaveItem(items[0]);

        string outDat = Path.Combine(@"$workDir", "Items_Mod.dat");
        p.SaveToFile(outDat);

        SavePayload reloaded = SavePayload.FromFile(outDat);
        var reloadedItems = reloaded.GetAllItems();
        if (reloadedItems[0].Amount != 77) {
            Console.WriteLine("Item reload FAILED!");
            Environment.Exit(2);
        }
        Console.WriteLine("Item catalog adjustment verified successfully!");
    }
}
"@
$itemCs = Join-Path $workDir "ItemTest.cs"
$itemExe = Join-Path $workDir "ItemTest.exe"
[System.IO.File]::WriteAllText($itemCs, $itemScript)
& $csc /nologo /r:"$bridgeExe" /out:"$itemExe" "$itemCs"
& "$itemExe"
if ($LASTEXITCODE -ne 0) { throw "Test 3 failed!" }
Write-Host "[PASS] Test 3: Inventory modification verified."

Write-Host "`n================================================="
Write-Host "  ALL EXTENDED TESTS PASSED SUCCESSFULLY!"
Write-Host "================================================="
