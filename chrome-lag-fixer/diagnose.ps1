# Chrome lag diagnostic — write UTF-8 report next to this script.
$ErrorActionPreference = "SilentlyContinue"
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$reportDir = Join-Path $here "reports"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $reportDir "chrome-lag-report.txt"

function Line([string]$text) { $script:buf.Add($text) }
$script:buf = New-Object System.Collections.Generic.List[string]

function Hr { Line ("=" * 72) }

Hr
Line "Chrome lag diagnostic"
Line ("Time: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
Line ("Machine: " + $env:COMPUTERNAME)
Line ("User: " + $env:USERNAME)
Hr

# --- OS / CPU / RAM ---
$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$ramGb = [math]::Round($cs.TotalPhysicalMemory / 1GB, 1)
$freeGb = [math]::Round($os.FreePhysicalMemory / 1MB, 1)

Line ""
Line "[OS / CPU / RAM]"
Line ("OS: " + $os.Caption + " " + $os.Version + " (" + $os.OSArchitecture + ")")
Line ("CPU: " + $cpu.Name.Trim())
Line ("RAM total: " + $ramGb + " GB")
Line ("RAM free:  " + $freeGb + " GB")
if ($ramGb -lt 16) {
  Line "HINT: RAM < 16GB 時 Chrome 多分頁好易 swap → 成個系統頓。"
}

# --- GPU ---
Line ""
Line "[GPU]"
$gpus = Get-CimInstance Win32_VideoController
foreach ($g in $gpus) {
  $vram = if ($g.AdapterRAM -and $g.AdapterRAM -gt 0) { [math]::Round($g.AdapterRAM / 1GB, 1).ToString() + " GB (WMI, 可能唔準)" } else { "n/a" }
  Line ("- " + $g.Name)
  Line ("    Driver: " + $g.DriverVersion + "  (" + $g.DriverDate + ")")
  Line ("    Mode: " + $g.VideoModeDescription)
  Line ("    CurrentRefreshRate: " + $g.CurrentRefreshRate)
  Line ("    Status: " + $g.Status + "  PNP: " + $g.PNPDeviceID)
  Line ("    VRAM(WMI): " + $vram)
}
$gpuNames = ($gpus | ForEach-Object { $_.Name }) -join " | "
if ($gpuNames -match "NVIDIA|GeForce|RTX") {
  Line "HINT: NVIDIA + Chrome 硬件加速係最常見 stutter 來源之一。下一步跑 02-test-no-gpu.bat。"
}

# --- Monitors (best-effort) ---
Line ""
Line "[Displays / refresh]"
try {
  Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class DispEnum {
  [DllImport("user32.dll", CharSet=CharSet.Ansi)]
  public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
  [DllImport("user32.dll", CharSet=CharSet.Ansi)]
  public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
  public struct DISPLAY_DEVICE {
    public int cb;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceString;
    public int StateFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceKey;
  }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
  public struct DEVMODE {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmDeviceName;
    public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
    public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
    public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmFormName;
    public short dmLogPixels;
    public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
    public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
  }
}
"@ -ErrorAction Stop

  $rates = @()
  for ($i = 0; $i -lt 16; $i++) {
    $dd = New-Object DispEnum+DISPLAY_DEVICE
    $dd.cb = [System.Runtime.InteropServices.Marshal]::SizeOf($dd)
    if (-not [DispEnum]::EnumDisplayDevices($null, $i, [ref]$dd, 0)) { break }
    $active = ($dd.StateFlags -band 1) -ne 0
    $primary = ($dd.StateFlags -band 4) -ne 0
    $dm = New-Object DispEnum+DEVMODE
    $dm.dmSize = [System.Runtime.InteropServices.Marshal]::SizeOf($dm)
    $ok = [DispEnum]::EnumDisplaySettings($dd.DeviceName, -1, [ref]$dm)
    $hz = if ($ok) { $dm.dmDisplayFrequency } else { "?" }
    $res = if ($ok) { "$($dm.dmPelsWidth)x$($dm.dmPelsHeight)" } else { "?" }
    Line ("- " + $dd.DeviceName + "  " + $dd.DeviceString)
    Line ("    Active=" + $active + " Primary=" + $primary + "  " + $res + " @" + $hz + "Hz")
    if ($active -and $hz -is [int] -and $hz -gt 0) { $rates += $hz }
  }
  $uniq = $rates | Select-Object -Unique
  if ($uniq.Count -gt 1) {
    Line ("ALERT: 多個唔同刷新率: " + ($uniq -join " / ") + " Hz  ← 呢個好容易令 Chrome 跟錯 VSync 而 lag。")
  } elseif ($uniq.Count -eq 1) {
    Line ("Refresh rates look consistent: " + $uniq[0] + " Hz")
  }
} catch {
  Line ("(EnumDisplaySettings failed: " + $_.Exception.Message + ")")
}

# --- Capture / media devices ---
Line ""
Line "[Capture / display extras]"
$pnp = Get-PnpDevice | Where-Object {
  $_.FriendlyName -match "AVer|GC553|Elgato|capture|Live Gamer|Streaming|HDMI"
}
if ($pnp) {
  foreach ($d in $pnp) {
    Line ("- [" + $d.Status + "] " + $d.Class + "  " + $d.FriendlyName)
  }
  Line "HINT: 擷取卡如果被當成 Monitor，拔咗 HDMI 或者喺顯示設定唔好延伸桌面過去，再開 Chrome 對比。"
} else {
  Line "(no AVer/Elgato/capture-like PnP names found — 可能驅動名唔夾，唔代表冇卡)"
}

# --- Competing processes ---
Line ""
Line "[Processes that fight Chrome for GPU/CPU]"
$watch = @(
  "obs64","obs32","obs","AVerMedia","Streaming Center","NVIDIA Share","NVIDIA Overlay",
  "nvcontainer","nvsphelper64","TextInputHost","SearchHost","MsMpEng","WidgetBoard"
)
$procs = Get-Process | Sort-Object WorkingSet64 -Descending
$hit = $procs | Where-Object {
  $_.ProcessName -match "obs|AVer|NVIDIA Share|NVIDIA Overlay|nvcontainer|GeForce|GameBar|XboxPcApp"
}
if ($hit) {
  foreach ($p in ($hit | Select-Object -First 20)) {
    $mb = [math]::Round($p.WorkingSet64 / 1MB, 0)
    Line ("- " + $p.ProcessName + "  pid=" + $p.Id + "  RAM=" + $mb + " MB")
  }
} else {
  Line "(OBS / NVIDIA overlay / AVerMedia 而家睇唔到喺跑 — 好。)"
}

# --- Chrome processes ---
Line ""
Line "[Chrome processes]"
$chromePathCandidates = @(
  "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe",
  "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
  "${env:LocalAppData}\Google\Chrome\Application\chrome.exe"
)
$chromeExe = $chromePathCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($chromeExe) {
  $ver = (Get-Item $chromeExe).VersionInfo.FileVersion
  Line ("chrome.exe: " + $chromeExe)
  Line ("version: " + $ver)
} else {
  Line "ALERT: 搵唔到 chrome.exe"
}

$chromeProcs = @(Get-Process chrome)
if ($chromeProcs.Count -eq 0) {
  Line "Chrome 而家冇開。"
} else {
  $sumMb = [math]::Round((($chromeProcs | Measure-Object WorkingSet64 -Sum).Sum) / 1MB, 0)
  $cpuSum = ($chromeProcs | Measure-Object CPU -Sum).Sum
  Line ("Process count: " + $chromeProcs.Count)
  Line ("Working set total: " + $sumMb + " MB")
  Line ("CPU time total (seconds since start): " + [math]::Round($cpuSum, 0))
  if ($chromeProcs.Count -gt 40) {
    Line "ALERT: chrome 進程 > 40，分頁／擴充功能／廣告 iframe 可能過多。Shift+Esc 開 Chrome 工作管理員。"
  }
  if ($sumMb -gt 4000) {
    Line "ALERT: Chrome 食超過 4GB RAM。關背景分頁、廣告站、重型擴充功能。"
  }
}

# --- Profile size / GPU cache ---
Line ""
Line "[Chrome profile size]"
$userData = Join-Path $env:LOCALAPPDATA "Google\Chrome\User Data"
Line ("User Data: " + $userData)
if (Test-Path $userData) {
  $hotDirs = @("Default","Profile 1","Profile 2","Profile 3","Guest Profile","ShaderCache","GrShaderCache","GraphiteDawnCache")
  Get-ChildItem $userData -Directory | Where-Object { $_.Name -in $hotDirs -or $_.Name -like "Profile *" } | ForEach-Object {
    $profile = $_.FullName
    $gpuCache = Join-Path $profile "GPUCache"
    $codeCache = Join-Path $profile "Code Cache"
    $serviceWorkers = Join-Path $profile "Service Worker"
    function DirMB($p) {
      if (-not (Test-Path $p)) { return 0 }
      [math]::Round(((Get-ChildItem $p -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum) / 1MB, 1)
    }
    $gpuMb = DirMB $gpuCache
    $codeMb = DirMB $codeCache
    $swMb = DirMB $serviceWorkers
    Line ("- " + $_.Name)
    Line ("    GPUCache: " + $gpuMb + " MB")
    Line ("    Code Cache: " + $codeMb + " MB")
    Line ("    Service Worker: " + $swMb + " MB")
    if ($gpuMb -gt 400) {
      Line "    ALERT: GPUCache 好大，建議關 Chrome 之後跑 03-clean-gpu-cache.bat"
    }

    $prefPath = Join-Path $profile "Preferences"
    if (Test-Path $prefPath) {
      try {
        $raw = Get-Content -Raw -Path $prefPath -Encoding UTF8
        if ($raw -match '"hardware_acceleration_mode"\s*:\s*\{\s*"enabled"\s*:\s*(true|false)') {
          Line ("    hardware_acceleration_mode.enabled = " + $Matches[1])
        } else {
          Line "    hardware_acceleration_mode: (not found — Chrome 可能用預設 ON)"
        }
      } catch {
        Line "    Preferences parse skipped"
      }
    }
  }

  $extRoot = Join-Path $userData "Default\Extensions"
  if (Test-Path $extRoot) {
    $extCount = (Get-ChildItem $extRoot -Directory).Count
    Line ("Default extensions folders: " + $extCount)
    if ($extCount -gt 12) {
      Line "HINT: 擴充功能偏多。逐個停用對證，廣告攔截（uBlock Origin）建議留低。"
    }
  }
} else {
  Line "搵唔到 Chrome User Data。"
}

# --- HAGS ---
Line ""
Line "[Windows GPU scheduling]"
$hags = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -Name HwSchMode
if ($null -ne $hags.HwSchMode) {
  $mode = $hags.HwSchMode
  $label = switch ($mode) {
    1 { "Off" }
    2 { "On (Hardware-accelerated GPU scheduling)" }
    default { "Unknown($mode)" }
  }
  Line ("HwSchMode: " + $mode + "  →  " + $label)
  if ($mode -eq 2) {
    Line "HINT: HAGS 開住有時會令 Chrome/NVIDIA stutter。可去 Windows 設定 → 系統 → 顯示器 → 圖形 → 變更預設圖形設定 關掉對證。"
  }
} else {
  Line "HwSchMode: not set"
}

Line ""
Hr
Line "Next steps"
Line "1. 開 index.html 睇原因同對證清單"
Line "2. 完全退出 Chrome 之後跑 02-test-no-gpu.bat（最緊要嘅 A/B）"
Line "3. 如果關 GPU 即刻順：用 04-launch-smooth.bat 或者喺設定永久關硬件加速"
Line "4. 如果仍然 lag：統一兩塊芒 Hertz、退出 OBS/Overlay、拔擷取卡對證"
Hr

$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllLines($reportPath, $script:buf, $utf8)
Write-Host ""
Write-Host $script:buf.ToArray()
Write-Host ""
Write-Host "Report saved:"
Write-Host $reportPath
