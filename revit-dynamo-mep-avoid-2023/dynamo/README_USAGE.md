# Phase 1 — Clash Report 使用說明（Dynamo for Revit 2023）

## 前置條件

- Revit 2023 已開啟專案（含 Pipe / Duct 與障礙模型）
- Dynamo for Revit 2023（隨 Revit 內建，或 Dynamo Sandbox 2.x）

## 檔案放置

將整個 `revit-dynamo-mep-avoid-2023/` 資料夾下載到本機，例如：

```
C:\revit-dynamo-mep-avoid-2023\
├── python\
│   ├── selection_utils.py
│   ├── envelope.py
│   ├── clash_zones.py
│   ├── report.py
│   └── main_report_only.py     ← 主腳本
└── dynamo\
    └── (此說明)
```

## 快速設定（手動建圖）

因雲端環境無法直接產出 `.dyn` 二進制檔，請在 Dynamo 內手動建立以下節點：

### 步驟 1：建立輸入節點

| 節點 | 名稱 | 設定 |
|------|------|------|
| **Select Model Elements** | `subjects` | 連線到 Python IN[0] |
| **Select Model Elements** | `obstacles` | 連線到 Python IN[1] |
| **Number Slider** | `clearance_mm` | 範圍 0–500，預設 100；連 IN[2] |
| **Number Slider** | `merge_gap_mm` | 範圍 0–1000，預設 300；連 IN[3] |

> `Select Model Elements` 節點在 Revit → Selection 類別下。

### 步驟 2：Python Script 節點

1. 搜尋 **Python Script** 節點，拖入畫布。
2. 點兩下打開，**貼上 `python/main_report_only.py` 的全部內容**。
3. 在腳本頂部找到 `sys.path.insert` 那行，確認路徑指向你放 `python/` 的位置。
   例如：
   ```python
   sys.path.insert(0, r"C:\revit-dynamo-mep-avoid-2023\python")
   ```
4. 確認節點有 **4 個輸入**（`+` 號可增加 IN 數量）。

### 步驟 3：輸出節點

| Python 輸出 | 連接到 |
|-------------|--------|
| OUT[0] | **Watch** 節點（顯示報告文字） |
| OUT[1] | **Watch** 節點（衝突數量） |
| OUT[2] | **Watch** 節點（跳過數量） |
| OUT[3] | （選配）**Watch** 或後續 CSV 導出用 |

### 節點連線圖

```
[Select Model Elements: subjects]  ──→  IN[0]
[Select Model Elements: obstacles] ──→  IN[1]     [Python Script]  ──→  OUT[0] → [Watch: Report]
[Number Slider: 100]               ──→  IN[2]                      ──→  OUT[1] → [Watch: Clashes]
[Number Slider: 300]               ──→  IN[3]                      ──→  OUT[2] → [Watch: Skipped]
```

## 執行

1. 在 Revit 3D / 平面視圖中，先選好要檢查的 **MEP（Pipe / Duct）**。
2. 點擊 `subjects` 選取節點的 **Select** 按鈕。
3. 再選好所有要避開的 **障礙模型**（梁、牆、設備等）。
4. 點擊 `obstacles` 選取節點的 **Select** 按鈕。
5. 調整 clearance（預設 100 mm）。
6. 點 Dynamo **Run** 按鈕。

## 報告解讀

```
============================================================
  MEP CLASH / CLEARANCE REPORT (Phase 1 ReportOnly)
  Clearance target: 100 mm (incl. insulation)
============================================================
Clashes found: 2  |  Subjects skipped: 0

[CLASH] Subject #12345  (Pipes)
  vs Obstacle(s): #67890
  Gap: 45 mm  (required: 100 mm)
  MEP top Z: 3200.0 mm  |  Obstacle bottom Z: 3245.0 mm
  MEP insulation: Yes
    Obstacle #67890 (Structural Framing) insulation: No
```

- **Gap < 0 / INTERSECTING**：元素實體重疊。
- **Gap < 100**：淨空不足，Phase 2 會在此處插入向下 Fitting。
- **MEP insulation: Yes**：已含保溫計算外表面。

## 注意事項

- Phase 1 **不修改模型**，只輸出報告。
- 保溫厚度來自 Revit 內 `PipeInsulation` / `DuctInsulation`；若未建模保溫，量測為裸管表面。
- 非 Pipe / Duct 的 Subject 會被跳過並記錄原因。
- 障礙可以是任何類別（ARC / STC / MEP / Generic Model 等）。

## 下一步

Phase 1 報告確認正確後，Phase 2 將在衝突處 **自動向下插入 Fitting** 繞行。
