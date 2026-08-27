# chrome-lag-fixer

Chrome 成日 lag／卡機：本機診斷 + 三分鐘對證。  
**唔會改 Chrome 安裝檔**，只讀系統資料、清 GPU cache、或者用參數重新開 Chrome。

## 點解會 lag（短答）

你呢部機最常見唔係「Chrome 壞咗」，而係疊咗幾樣：

1. **雙芒唔同 Hertz**（遊戲芒高刷新 + 電腦芒 60Hz）→ Chrome 跟錯 VSync
2. **NVIDIA 硬件加速**（Skia Graphite / Vulkan）→ 合成畫面 stutter
3. **擷取卡 GC553 Pro** 被當成多一塊芒
4. **OBS / NVIDIA Overlay** 背景食 GPU
5. **廣告站 + 太多分頁 + Profile 脹**

詳細原因同步驟開 `index.html`。

## 快速開始（Windows）

1. 用 Chrome 開呢個資料夾入面嘅 `index.html`
2. 雙擊 `01-diagnose.bat` → 產生 `reports/chrome-lag-report.txt`
3. **一定要完全退出 Chrome** 之後先跑：
   - `02-test-no-gpu.bat`（對證係唔係硬件加速）
   - 確認係 GPU 問題再試 `04-launch-smooth.bat`
   - cache 好大就跑 `03-clean-gpu-cache.bat`

## 檔案一覽（方便搜尋）

| 檔案 | 用途 |
|------|------|
| `index.html` | 原因說明 + 對證清單 |
| `01-diagnose.bat` | 收集 GPU／芒／Chrome RAM／擷取卡／OBS |
| `diagnose.ps1` | 診斷主程式 |
| `02-test-no-gpu.bat` | 關 GPU 加速再開 Chrome |
| `03-clean-gpu-cache.bat` | 清 GPUCache / ShaderCache / Code Cache |
| `04-launch-smooth.bat` | D3D11 + 關掉 Graphite/Vulkan |
| `data/causes.json` | 原因清單（俾 AI agent） |
| `AGENTS.md` | 俾 AI agent 快速理解 |

## 注意

- 需要已安裝 Google Chrome。
- 對證 script 會問你同唔同意關閉現有 Chrome 視窗（未儲存分頁會冇）。
- 建議報告放 `reports/`，唔好提交入 git。
