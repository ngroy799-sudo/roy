# Phase 1 — Clash Report：Dynamo Player 使用說明

## 前置條件

- Revit 2023 已開啟專案（含 Pipe / Duct 與障礙模型）
- Dynamo for Revit 2023（隨 Revit 內建）

## 下載並放置

1. 下載 ZIP：https://github.com/ngroy799-sudo/roy/archive/refs/heads/cursor/revit-dynamo-mep-avoid-scope-2156.zip
2. 解壓到本機任意位置，例如：

```
C:\revit-dynamo-mep-avoid-2023\
├── dynamo\
│   └── MEP_Clash_Report_Player.dyn   ← Dynamo Player 用此檔
└── python\
    └── (模組化版本，進階用)
```

## 使用方式（Dynamo Player）

### 步驟 1：開啟 Dynamo Player

- Revit 2023 → 管理（Manage）→ **Dynamo Player**

### 步驟 2：指定資料夾

- 在 Dynamo Player 頂部，點 **瀏覽資料夾**
- 選擇 `dynamo/` 所在的資料夾
- 你會看到 **MEP_Clash_Report_Player** 出現在列表中

### 步驟 3：設定輸入

點擊 **MEP_Clash_Report_Player** 右邊的 **▶ Play** 或展開箭頭，你會看到 4 個輸入：

| 輸入 | 說明 | 預設 |
|------|------|------|
| **1. Select MEP (Pipe/Duct)** | 點 Select → 在模型中選取要檢查的管／風管 | — |
| **2. Select Obstacles** | 點 Select → 選取所有要閃避的障礙（梁、牆、設備等） | — |
| **3. Clearance (mm)** | 垂直淨空目標 | 100 |
| **4. Merge Gap (mm)** | 相鄰衝突合併距離 | 300 |

### 步驟 4：執行

- 點 **▶ Play**
- 等待幾秒（視元素數量）
- 結果顯示在 Dynamo Player 輸出區

### 輸出

| 輸出 | 內容 |
|------|------|
| **Clash Report** | 文字報告（衝突清單） |
| **Clash Count** | 衝突數量 |
| **Skipped Count** | 跳過的非 Pipe/Duct 數量 |

## 報告範例

```
============================================================
  MEP CLASH / CLEARANCE REPORT
  Clearance target (incl. insulation)
============================================================
Clashes: 2  |  Skipped: 0

[CLASH] Subject #12345 (Pipes)
  vs Obstacle(s): #67890
  Gap: 45 mm  (required: 100 mm)
  MEP top Z: 3200.0 mm | Obstacle bottom Z: 3245.0 mm
  MEP insulated: Yes
    Obs #67890 (Structural Framing) insulated: No
```

## 重要說明

- **不修改模型** — Phase 1 只報告，不移動任何元素。
- **含保溫** — 有 PipeInsulation / DuctInsulation 時，量測外表面。
- **只支援 Pipe / Duct** — 選到其他類型會跳過並記錄。
- 障礙可以是 **任何類別**（結構、建築、其他 MEP 等）。
- 可重複執行：修改模型後再 Play，報告即時更新。

## 也可以打開 Dynamo 編輯

雙擊 `.dyn` 檔在 Dynamo 內打開，可以看到完整節點圖：

```
[1. Select MEP]   ──┐
[2. Select Obs]   ──┤
[3. Clearance]    ──┼──→ [Python Script] ──→ [Watch: Report]
[4. Merge Gap]    ──┘                    ──→ [Watch: Count]
                                         ──→ [Watch: Skipped]
```

綠色群組 = 輸入（Player 可見）  
青色群組 = 輸出

## 下一步

Phase 1 報告正確後 → Phase 2 在衝突處 **自動向下 Fitting 繞行**。
