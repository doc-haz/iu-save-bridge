$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outDir = Join-Path $root "bin"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
$outExe = Join-Path $outDir "IU_Save_Bridge.exe"

$resIco = Join-Path $root "assets\IU_Recomp_Save_Editor.ico"
$resMenu = Join-Path $root "assets\IU_Recomp_Save_Editor_Menu.png"
$resItems = Join-Path $root "resources\ItemNames.txt"

$sources = @(
    (Join-Path $root "src\AssemblyInfo.cs"),
    (Join-Path $root "src\SafePath.cs"),
    (Join-Path $root "src\SavePayload.cs"),
    (Join-Path $root "src\CharacterData.cs"),
    (Join-Path $root "src\ItemData.cs"),
    (Join-Path $root "src\SaveManager.cs"),
    (Join-Path $root "src\StfsReader.cs"),
    (Join-Path $root "src\Loc.cs"),
    (Join-Path $root "src\MainForm.cs"),
    (Join-Path $root "src\Program.cs")
)

Write-Host "Compiling IU Save Bridge v2.3.0..."
& $csc /target:winexe /platform:x64 /optimize+ `
  /win32icon:"$resIco" `
  /resource:"$resIco,IU_Recomp_Save_Editor.ico" `
  /resource:"$resMenu,IU_Recomp_Save_Editor_Menu.png" `
  /resource:"$resItems,ItemNames.txt" `
  /out:"$outExe" `
  $sources

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Build Successful! Output: $outExe ($((Get-Item $outExe).Length) bytes)"
