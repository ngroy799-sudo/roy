# Phase 1 — Clash Report：Dynamo Player 使用說明

## 檔案列表

| 檔案 | 說明 |
|------|------|
| `MEP_Clash_Report_Player.dyn` | **IronPython2** 版本（Dynamo 2.12 及更早預設） |
| `MEP_Clash_Report_Player_CPython3.dyn` | **CPython3** 版本（Dynamo 2.13+ 預設） |

> **不確定用哪個？** 先試 CPython3 版，失敗再試 IronPython2 版。

---

## 下載

ZIP：https://github.com/ngroy799-sudo/roy/archive/refs/heads/cursor/revit-dynamo-mep-avoid-scope-2156.zip

解壓到本機任意位置，例如：

```
C:\revit-dynamo-mep-avoid-2023\
└── dynamo\
    ├── MEP_Clash_Report_Player.dyn          ← IronPython2
    └── MEP_Clash_Report_Player_CPython3.dyn ← CPython3
```

---

## Dynamo Player 操作步驟

### 1. 開啟 Dynamo Player

Revit 2023 → 管理（Manage）頁籤 → **Dynamo Player** 按鈕

### 2. 指定資料夾

- Player 頂部點 **📁 瀏覽** 圖示
- 選到你解壓的 `dynamo\` 資料夾
- 列表會出現 **MEP_Clash_Report_Player**

### 3. 展開輸入

點名稱左邊的 **▶** 展開，會看到 4 個輸入：

| # | 輸入 | 操作 | 預設 |
|---|------|------|------|
| 1 | **Select MEP (Pipe/Duct)** | 點 `Select` → 在模型中選管／風管 → `完成` | — |
| 2 | **Select Obstacles** | 點 `Select` → 選障礙（梁、牆等）→ `完成` | — |
| 3 | **Clearance (mm)** | 拖拉滑桿或輸入數字 | 100 |
| 4 | **Merge Gap (mm)** | 拖拉滑桿或輸入數字 | 300 |

### 4. 執行

點右邊 **▶ Play** 按鈕 → 等待幾秒 → 結果顯示在 Player 下方

### 5. 查看報告

報告會顯示在 **Clash Report Output** 欄位，例如：

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

---

## 排錯指南

### 問題 1：Dynamo Player 內看不到 .dyn 檔

- 確認瀏覽的是 **`dynamo\`** 資料夾（不是上一層）
- 確認檔案副檔名是 `.dyn`（不是 `.dyn.txt`）

### 問題 2：按 Play 無反應 / 報錯 Python Engine

**症狀**：Player 顯示紅色錯誤或完全無輸出

**原因**：Revit 2023 的 Dynamo 有兩種 Python 引擎：
- **Dynamo 2.12 及之前** → 預設 IronPython2
- **Dynamo 2.13+**（Revit 2023.1 更新後）→ 預設 CPython3

**解法**：
1. 先試 `MEP_Clash_Report_Player_CPython3.dyn`
2. 不行就試 `MEP_Clash_Report_Player.dyn`（IronPython2）

### 問題 3：按 Play 後顯示「No MEP subjects selected」

- 你需要先點 **Select** 按鈕選取元素
- 選 Pipe 或 Duct（不可選 Fitting、設備等）
- 選好後點 **完成 / Finish**

### 問題 4：選了元素但報告說 0 clashes

- 確認 MEP 與障礙 **確實有空間重疊或淨空不足**
- 淨空判斷是基於 BoundingBox（含保溫）的 **垂直 Z 值**
- 兩者 XY 投影不重疊的話不會被判為衝突

### 問題 5：想在 Dynamo 內部打開（非 Player）

1. Revit → 管理 → **Dynamo**（非 Player）
2. File → Open → 選 `.dyn` 檔
3. 可以看到完整節點圖
4. 點各個 Select 節點的 **Select** 按鈕
5. 設定滑桿數值
6. Run → 看 Watch 節點結果

### 問題 6：確認 Dynamo 版本

在 Dynamo 裡：Help → About → 查看版本號
- 2.12.x → 用 IronPython2 版
- 2.13.x+ → 用 CPython3 版

---

## 節點圖結構

```
┌─────────────────────────────────────────────┐
│  INPUTS (Dynamo Player 可見)                 │
│                                              │
│  [Select MEP (Pipe/Duct)]  ──┐               │
│  [Select Obstacles]        ──┤               │
│  [Clearance Slider: 100]   ──┼──→ [Python]   │
│  [Merge Gap Slider: 300]   ──┘       │       │
│                                      ▼       │
│                              [Watch: Report]  │
└─────────────────────────────────────────────┘
```

---

## 重要提醒

- **不修改模型** — Phase 1 只報告，絕不移動任何元素
- **含保溫** — 有 PipeInsulation / DuctInsulation 時，以外表面量測
- **只支援 Pipe / Duct** — Cable Tray / Conduit 在 Phase 3 加入
- 可多次執行：改模型後再 Play 會即時更新結果

---

## 下一步

Phase 1 報告確認正確後 → Phase 2：自動向下 Fitting 繞行
