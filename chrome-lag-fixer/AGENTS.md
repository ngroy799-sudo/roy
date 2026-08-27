# AGENTS — chrome-lag-fixer

## Purpose

Explain and fix **Google Chrome stutter / lag / freeze** on a Windows desktop
(typical setup: NVIDIA GPU, dual monitors, AVerMedia capture card, ad-heavy sites).

This folder is a **local diagnostic + A/B test kit**. It does not patch Chrome
binaries. Scripts only read system info, optionally close Chrome with confirmation,
clear GPU caches, or relaunch Chrome with extra flags.

## Keywords (search)

Chrome lag, stutter, 卡機, 延遲, hardware acceleration, NVIDIA, RTX, dual monitor,
mixed refresh rate, G-SYNC, HAGS, GPUCache, AVerMedia, GC553, OBS, happymh, uBlock

## Entry points

| File | Role |
|------|------|
| `index.html` | Human guide (open in browser). Answers *why* Chrome lags. |
| `README.md` | Short start + file map |
| `01-diagnose.bat` | Runs `diagnose.ps1`, writes `reports/chrome-lag-report.txt` |
| `diagnose.ps1` | Collects GPU, monitors, Chrome RAM, profile size, capture/OBS processes |
| `02-test-no-gpu.bat` | A/B test: relaunch Chrome with `--disable-gpu` |
| `03-clean-gpu-cache.bat` | Deletes Chrome GPUCache / ShaderCache / Code Cache |
| `04-launch-smooth.bat` | Relaunch with D3D11 + Skia Graphite/Vulkan off |
| `data/causes.json` | Structured cause list for agents |

## Out of scope

- Remote access to the user's live Chrome profile
- Bundling or pirating ad-block filter lists as site scrapers
- Overclocking / NVIDIA driver installer packages

## Typical root causes (ranked for this user)

1. Mixed-refresh dual monitors (game Hz vs desktop Hz) — Chrome VSync hitch
2. NVIDIA hardware acceleration / Skia Graphite / Vulkan compositor stutter
3. Capture card (GC553 Pro) enumerated as an extra display
4. OBS / Streaming Center / NVIDIA Overlay competing for GPU
5. Ad-heavy tabs (e.g. m.happymh.com), bloated profile, too many extensions
