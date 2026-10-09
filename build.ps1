# ===========================================================================
#  RealChute 汉化版构建脚本 / RealChute localization build script
#
#  用法 / Usage:
#     powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
#     powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -KspDir "D:\KSP"
#
#  说明 / Notes:
#    - 本机没有 .NET Framework 4.8 的“引用程序集”（targeting pack），也没有
#      可用的 MSBuild 工程还原环境；因此直接用 Visual Studio 自带的 Roslyn
#      csc.exe 编译，并把引用指向 KSP 安装目录里随游戏分发的程序集。
#      There is no .NET Framework 4.8 targeting pack on this machine, so the
#      Roslyn compiler shipped with Visual Studio is invoked directly and the
#      references point at the assemblies that ship with the KSP install.
#    - 只覆盖 Plugins/RealChute.dll，Output 目录的其余文件原样保留。
# ===========================================================================

[CmdletBinding()]
param(
    # RealChute 源码目录 / RealChute source directory (default: <script dir>\RealChute)
    [string]$SourceDir,

    # 输出 DLL 路径 / Output DLL path (default: Output\GameData\RealChute\Plugins\RealChute.dll)
    [string]$OutputDll,

    # KSP 安装目录 / KSP installation directory
    [string]$KspDir = 'G:\Kerbal Space Program',

    # Roslyn 编译器 / Roslyn compiler
    [string]$Csc = 'F:\Visual Studio\MSBuild\Current\Bin\Roslyn\csc.exe',

    # .NET Framework 运行时程序集目录 / .NET Framework runtime assembly directory
    [string]$FrameworkDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
)

$ErrorActionPreference = 'Stop'

# --- 解析默认路径 / Resolve default paths --------------------------------------
# 注意：$PSScriptRoot 在 param 默认值里可能为空，必须在脚本体内解析。
# Note: $PSScriptRoot can be empty inside param defaults; resolve it in the body.
$root = $PSScriptRoot
if (-not $root) { $root = Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $root) { $root = (Get-Location).Path }

if (-not $SourceDir) { $SourceDir = Join-Path $root 'RealChute' }
if (-not $OutputDll) { $OutputDll = Join-Path $root 'Output\GameData\RealChute\Plugins\RealChute.dll' }

Write-Host "工程根目录 / Project root : $root"

# --- 检查输入 / Validate inputs ------------------------------------------------
foreach ($p in @($SourceDir, $KspDir, $Csc, $FrameworkDir)) {
    if (-not (Test-Path $p)) { throw "Path not found / 找不到路径: $p" }
}

$managed  = Join-Path $KspDir 'KSP_x64_Data\Managed'
$gameData = Join-Path $KspDir 'GameData'
if (-not (Test-Path $managed)) { throw "KSP Managed dir not found / 找不到 KSP 托管程序集目录: $managed" }

# --- 源文件 / Source files -----------------------------------------------------
$sources = @(Get-ChildItem $SourceDir -Recurse -Filter *.cs | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw "No .cs sources found under / 没有找到 .cs 源文件: $SourceDir" }

# --- 引用 / References ---------------------------------------------------------
$references = @(
    # .NET Framework
    "$FrameworkDir\mscorlib.dll"
    "$FrameworkDir\System.dll"
    "$FrameworkDir\System.Core.dll"
    "$FrameworkDir\System.Xml.dll"
    "$FrameworkDir\System.Xml.Linq.dll"
    "$FrameworkDir\System.Data.dll"
    "$FrameworkDir\System.Data.DataSetExtensions.dll"
    "$FrameworkDir\System.Net.Http.dll"
    "$FrameworkDir\Microsoft.CSharp.dll"
    # KSP / Unity
    "$managed\Assembly-CSharp.dll"
    "$managed\UnityEngine.dll"
    "$managed\UnityEngine.CoreModule.dll"
    "$managed\UnityEngine.IMGUIModule.dll"
    "$managed\UnityEngine.PhysicsModule.dll"
    "$managed\UnityEngine.AnimationModule.dll"
    "$managed\UnityEngine.ImageConversionModule.dll"
    "$managed\UnityEngine.TextRenderingModule.dll"
    "$managed\UnityEngine.UI.dll"
    # Hard dependencies declared through KSPAssemblyDependency
    "$gameData\000_ClickThroughBlocker\Plugins\ClickThroughBlocker.dll"
    "$gameData\001_ToolbarControl\Plugins\ToolbarControl.dll"
)

$missing = @($references | Where-Object { -not (Test-Path $_) })
if ($missing.Count -gt 0) {
    throw "Missing reference assemblies / 缺少引用程序集:`n$($missing -join "`n")"
}

# --- 编译 / Compile ------------------------------------------------------------
$outDir = Split-Path $OutputDll -Parent
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

$cscArgs = @(
    '/nologo'
    '/target:library'
    '/langversion:12.0'
    '/optimize+'
    '/nostdlib+'
    "/out:$OutputDll"
) + @($references | ForEach-Object { "/r:$_" }) + $sources

Write-Host "Compiling / 正在编译: $($sources.Count) source files"
& $Csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "Compilation failed / 编译失败 (exit $LASTEXITCODE)" }

# --- 结果 / Report -------------------------------------------------------------
$dll  = Get-Item $OutputDll
$info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($OutputDll)
Write-Host ''
Write-Host 'Build succeeded / 构建成功'
Write-Host "  File    / 文件: $($dll.FullName)"
Write-Host "  Size    / 大小: $($dll.Length) bytes"
Write-Host "  Product / 产品版本: $($info.ProductVersion)"
Write-Host "  FileVer / 文件版本: $($info.FileVersion)"
