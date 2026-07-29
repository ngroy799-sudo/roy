# TECH_DESIGN：MEP 向下 Fitting 避開（Revit 2023 Dynamo）

**對應決策**：`DECISIONS.md` ADR-001～007  
**狀態**：Ready for Phase 1 implementation skeleton

---

## 1. 目標行為

對已選 **Pipe / Duct**（Subject）：

1. 與已選障礙（Obstacle）做衝突／淨空檢查（**含保溫外表面**）。
2. 若需避開：在 Subject 上 **只向下** 插入 Fitting，形成下沉繞行。
3. 下沉段 **外頂面** 與障礙 **外底面** 垂直距 ≥ **100 mm**。
4. 兩端接回原標高。
5. 下方空間不足或 Fitting 無法建立 → **跳過並記錄**，不改該段。
6. **永不**移動 Obstacle。

---

## 2. 使用者輸入（Dynamo）

| 輸入 | 類型 | 說明 |
|------|------|------|
| `subjects` | Element[] | 要改線的 Pipe/Duct（手動選） |
| `obstacles` | Element[] | 已點選全部障礙模型 |
| `clearance_mm` | number | 預設 `100` |
| `run_mode` | string | `ReportOnly` / `Apply` |
| `merge_gap_mm` | number | 相鄰衝突合併間距，預設 `300` |

輸出：

| 輸出 | 說明 |
|------|------|
| `clash_report` | 衝突清單（Id、類型、Z、狀態） |
| `applied` | 成功改線的 Subject Id |
| `skipped` | 跳過清單（Id、原因代碼） |

---

## 3. 幾何與淨空（含保溫）

### 3.1 外廓取得優先序

1. 若元素有 **Insulation**（`PipeInsulation` / `DuctInsulation` 或宿主相關保溫）：用保溫外 Solid / 外 BoundingBox。
2. 否則用宿主外 Solid / BoundingBox。
3. Obstacle 同樣：有保溫用保溫外底；無則主體底。

### 3.2 垂直淨空定義

```
gap = obstacle_outer_bottom_Z - mep_outer_top_Z
目標：gap >= clearance_mm（100）
```

觸發避開：路徑與障礙水平投影重疊，且（相交 **或** `gap < 100` 且 MEP 在障礙上方需下鑽通過）。

### 3.3 目標下沉標高

```
target_mep_center_Z =
  obstacle_outer_bottom_Z
  - clearance_mm
  - mep_outer_half_height
```

其中 `mep_outer_half_height` 含保溫外徑／外高之半。

若 `target_mep_center_Z` 下方再撞其他已選障礙或樓板且無法維持 100 mm → **SKIP_INSUFFICIENT_SPACE**。

---

## 4. 避開幾何（drop-under）

```
原標高 ────●════════════●────
           │            │
        Elbow↓       Elbow↑
           ●════════════●   ← 下沉水平段（含保溫頂 ↔ 障礙底 ≥ 100）
```

步驟（單一衝突區）：

1. 求 Subject 曲線與障礙水平投影的重疊區間 `[t0, t1]`（沿 MEP 參數）。
2. 前後各加緩衝（建議 0.5×管徑或固定 100 mm）得切斷點 `P_in`、`P_out`。
3. 在 `P_in` / `P_out` 處切開原 `MEPCurve`。
4. 建立垂直下降段 + 下沉水平段 + 垂直上升段（或等價 Elbow 組合）。
5. 放置對應 Elbow／Fitting，連接 Connectors。
6. 同一 Transaction 提交。

多障礙：若沿線衝突區間距離 < `merge_gap_mm`，合併成一個下沉區（取最嚴 `min(obstacle_outer_bottom_Z)`）。

---

## 5. 演算法流程

```
validate selection
  → filter subjects to Pipe/Duct (others → skip log)
  → for each subject:
       build outer geometry (with insulation)
       find clash zones vs obstacles
       if none: continue
       for each zone (merged):
         compute target Z
         if not feasible: append skipped; continue
         if run_mode == ReportOnly: append report; continue
         try rebuild drop-under in transaction
           success → applied
           fail → skipped (reason)
  → emit reports
```

### Skip 原因代碼

| Code | 含義 |
|------|------|
| `SKIP_NOT_PIPE_DUCT` | 非第一版支援類型 |
| `SKIP_SLOPED` | 坡度超出第一版容許 |
| `SKIP_INSUFFICIENT_SPACE` | 下方無法維持 100 mm |
| `SKIP_NO_ELBOW_TYPE` | 找不到合適 Fitting 類型 |
| `SKIP_CONNECT_FAIL` | Connector 連接失敗 |
| `SKIP_API_ERROR` | 其他 API 例外 |

---

## 6. Dynamo 圖分組（建議節點結構）

1. **Inputs** — Selection、clearance、mode  
2. **Filter** — 分類 Pipe/Duct vs 其他  
3. **Geometry** — 外廓（含 Insulation）  
4. **Clash** — 區間與 gap 計算（Python）  
5. **Plan** — target Z、切斷點、合併區（Python）  
6. **Apply** — Transaction 改線（Python，僅 `Apply`）  
7. **Output** — Watch / 字串報告  

主要邏輯放 `python/`，Dynamo 負責 I/O，方便維護。

---

## 7. Python 模組規劃

| 檔案 | 職責 |
|------|------|
| `python/selection_utils.py` | 過濾類別、讀取選取 |
| `python/envelope.py` | 含保溫外廓、bottom/top Z |
| `python/clash_zones.py` | 衝突區間、合併、淨空 |
| `python/drop_under.py` | 計算切斷點與目標路徑 |
| `python/mep_rebuild.py` | 切開、建段、Fitting、連接 |
| `python/report.py` | applied/skipped 結構化輸出 |

> 實際進 Dynamo 時可合併為少數 Python Script 節點；檔案拆分方便 AI／人工搜尋。

---

## 8. Revit API 注意（2023）

- 只對 Subject 建立／刪除／改曲線；禁止 `MoveElement` 於 Obstacle。
- Pipe：`Pipe.Create`、Elbow via routing / `FamilyInstance` fitting 依專案做法。
- Duct：對應 `Duct.Create` + Elbow。
- Insulation：查宿主相關 `PipeInsulation` / `DuctInsulation` 取外尺寸。
- 全部寫入包在一個或「每 Subject 一個」Transaction；失敗 `RollBack` 該 Subject。

---

## 9. 測試案例（`samples/` 後續補）

| ID | 情境 | 預期 |
|----|------|------|
| T1 | 水平管撞梁，下方充足 | 下沉成功，gap≥100，含保溫 |
| T2 | 下方不足 | skip + `SKIP_INSUFFICIENT_SPACE`，模型不變 |
| T3 | 無衝突 | 不修改 |
| T4 | 兩梁靠近 | 合併一個下沉區 |
| T5 | 選了牆但 Subject 是設備 | Subject 跳過非 Pipe/Duct |
| T6 | ReportOnly | 只輸出報告，無模型變更 |

---

## 10. 實作順序

1. **Phase 1**：`envelope` + `clash_zones` + ReportOnly `.dyn`  
2. **Phase 2**：單障礙 Pipe drop-under  
3. **Phase 2b**：Duct 同等邏輯  
4. **Phase 3**：多障礙合併、Tray/Conduit、失敗強化  
5. **Phase 4**：Dynamo Player、CSV 報告  

---

## 11. 仍開放（不擋 Phase 1）

- Q5 Linked Model  
- Q9 Player vs 手動  
- Q10 公司標準 Fitting 家族  
- Q11 Revit.exe 路徑確認  

Phase 1 可先做 ReportOnly（含保溫淨空計算），不依賴 Fitting 家族。
