<#
.SYNOPSIS
  Build RDS.BUSplice.Impl.dll + deploy stub .cs vao catalog macro Tekla 2020 (ban DEV).

.DESCRIPTION
  Khuon lay tu RDS Auto Purlin V1.0 (10-Oct-2026), ban rut gon (khong obfuscate).
  Kien truc (KNOWLEDGE.md 7.17):
    macros\RDS_MyTool.cs (+ file khac khai bao trong $srcs) -> Roslyn (C# moi) -> RDS.BUSplice.Impl.dll (toan bo logic)
    macros\RDS_Tool_Stub.cs   -> <MacroName>.cs (entry catalog, C# 5, ghi UTF-8 CO BOM)
    macros\<MacroName>.bmp    -> icon tile 96x96 canh .cs (tuy chon)
  Doi ten: $MacroName, $implName, $srcs, $stub. Thu muc project: <project>\macros, <project>\libs, <project>\tools (file nay).
  KHONG ghi chu co dau trong file nay (PowerShell 5.1 doc .ps1 khong BOM theo ANSI).
  Catalog entry MOI chi hien sau khi restart Tekla; deploy lai entry da co thi khong can restart
  (impl bump AssemblyVersion + stub nap tu ban copy %TEMP%).
  Can: Tekla 2020, .NET SDK (Roslyn csc.dll), .NET Framework 4.8.
#>
param(
    [string]$MacroName = "RDS-BUSplice",
    [string]$MacroDir  = ""     # trong = tu do catalog (uu tien australasia, roi common)
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$srcs = @(@("Geo.cs","Layout.cs","Settings.cs","Engine.cs","MainForm.cs","Entry.cs") | ForEach-Object { Join-Path $repo "src\$_" })
$stub = Join-Path $repo "macros\RDS_Tool_Stub.cs"
foreach ($f in $srcs + @($stub)) { if (-not (Test-Path $f)) { throw "Khong thay: $f" } }

$roots = @("C:\TeklaStructures\2020.0", "C:\Program Files\Tekla Structures\2020.0")
$xsdir = $roots | Where-Object { Test-Path (Join-Path $_ "nt\bin\TeklaStructures.exe") } | Select-Object -First 1
if (-not $xsdir) { throw "Khong tim thay Tekla 2020" }
$bin = Join-Path $xsdir "nt\bin"; $plg = Join-Path $bin "plugins"

$macroRoots = @(
    "C:\ProgramData\Trimble\Tekla Structures\2020.0\environments\australasia\macros\modeling",
    "C:\ProgramData\Trimble\Tekla Structures\2020.0\environments\common\macros\modeling"
)
if ($MacroDir) { $macroRoots = @($MacroDir) }
$dir = $macroRoots | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $dir) { throw "Khong tim thay macros\modeling" }
Write-Host "Tekla : $xsdir"
Write-Host "Macros: $dir"

$roslyn = Get-ChildItem "C:\Program Files\dotnet\sdk" -Directory -ErrorAction SilentlyContinue |
          Where-Object { $_.Name -match '^\d+\.\d+' } |
          Sort-Object { [version]($_.Name -replace '-.*$','') } -Descending |
          ForEach-Object { Join-Path $_.FullName "Roslyn\bincore\csc.dll" } |
          Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $roslyn) { throw "Khong thay Roslyn csc.dll (can .NET SDK)" }
$fw = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

# Dll dang bi Tekla khoa: Remove fail nhung Rename duoc -> doi ten .old roi copy ban moi (KNOWLEDGE 1).
function Unlock-File([string]$path) {
    if (-not (Test-Path $path)) { return }
    try { Remove-Item $path -Force -ErrorAction Stop }
    catch { $bak = "$path.old$(Get-Date -Format HHmmss)"; Rename-Item $path $bak; Write-Host "$(Split-Path $path -Leaf) dang bi khoa -> $(Split-Path $bak -Leaf)" }
}
Get-ChildItem "$dir\*.old*" -ErrorAction SilentlyContinue | ForEach-Object { try { Remove-Item $_.FullName -Force -ErrorAction Stop } catch { } }

# --- 1. impl dll; AssemblyVersion bump moi build (LoadFrom cache identity, KNOWLEDGE 7.17) ---
$implName = "RDS.BUSplice.Impl.dll"
$implOut  = Join-Path $repo "libs\$implName"
New-Item -ItemType Directory -Force (Join-Path $repo "libs") | Out-Null
if (Test-Path $implOut) { Remove-Item $implOut -Force }
$refs = @("$fw\mscorlib.dll", "$fw\System.dll", "$fw\System.Core.dll", "$fw\System.Drawing.dll", "$fw\System.Windows.Forms.dll",
          "$bin\Tekla.Macros.Runtime.dll", "$plg\Tekla.Structures.dll", "$plg\Tekla.Structures.Model.dll",
          "$plg\Tekla.Structures.Catalogs.dll")   # them Tekla.Structures.Drawing.dll ... neu can
$now = Get-Date
$ver = "1.0.{0}.{1}" -f [int][math]::Floor((New-TimeSpan -Start (Get-Date "2026-01-01") -End $now).TotalDays), ($now.Hour * 60 + $now.Minute)
$verCs = Join-Path $env:TEMP "rds-busplice-ver.cs"
[System.IO.File]::WriteAllText($verCs, "[assembly: System.Reflection.AssemblyVersion(`"$ver`")]")
$argv = @("exec", $roslyn, "/nologo", "/nostdlib+", "/noconfig", "/target:library", "/langversion:latest", "/debug:pdbonly",
          "/nowarn:1701,1702", "/out:$implOut") + ($refs | ForEach-Object { "/reference:$_" }) + $srcs + @($verCs)
& dotnet $argv
if (-not (Test-Path $implOut)) { throw "Build impl dll FAILED - xem loi csc o tren" }
Write-Host ("Impl dll: {0} {1} ({2:N0} bytes)" -f $implName, $ver, (Get-Item $implOut).Length)

# --- 2. stub phai compile duoc bang C# 5 (Tekla nuot loi compile lang) ---
$tmpDll = Join-Path $env:TEMP "rds-busplice-stub-$(Get-Date -Format HHmmss).dll"
& "$fw\csc.exe" /nologo /target:library /out:$tmpDll /reference:"$bin\Tekla.Macros.Runtime.dll" /reference:"System.dll" /reference:"System.Windows.Forms.dll" $stub
if (-not (Test-Path $tmpDll)) { throw "Stub KHONG compile duoc bang C# 5" }
Remove-Item $tmpDll -Force
Write-Host "Stub: C# 5 OK"

# --- 3. deploy: stub ghi UTF-8 CO BOM + CRLF, impl copy canh, xoa .cs.dll cu de Tekla compile lai ---
$tgt = Join-Path $dir "$MacroName.cs"
[System.IO.File]::WriteAllText($tgt, (([System.IO.File]::ReadAllText($stub) -replace "`r`n", "`n") -replace "`n", "`r`n"), (New-Object System.Text.UTF8Encoding($true)))
$dest = Join-Path $dir $implName; Unlock-File $dest; Copy-Item $implOut $dest -Force
Unlock-File "$tgt.dll"; Unlock-File "$tgt.pdb"
$bmp = Join-Path $repo "macros\$MacroName.bmp"
if (Test-Path $bmp) { Copy-Item $bmp (Join-Path $dir "$MacroName.bmp") -Force }
Write-Host "Deployed '$MacroName' -> $dir"
