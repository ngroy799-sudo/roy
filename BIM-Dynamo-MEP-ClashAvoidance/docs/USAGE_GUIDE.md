# Usage Guide — Revit + Dynamo

## 前置條件

1. 使用 **Autodesk Revit 2023**。
2. 開啟 **MEP** 工作模型（含要調整的管線）。
3. 已 Link **ARC**、**STC** 模型（座標對齊）。
4. Dynamo for Revit（隨 2023 內建）已可開啟；Python 引擎用 **IronPython2**。
5. 將本資料夾放到可存取路徑（本機或共享碟）。
6. 版本細節見 `docs/REVIT_2023.md`。

## 快速開始

1. 開啟 Dynamo → Open  
   `dynamo/MEP_AutoAvoid_ARC_STC.dyn`
2. 設定輸入節點：
   - `ConfigPath` → `config/clearance_rules.json` 完整路徑
   - `RunMode` → `DetectOnly`（先檢測）或 `DetectAndFix`（檢測+自動偏移）
   - `LinkNameFilter_ARC` → 例如 `ARC`（匹配 Link 名稱關鍵字）
   - `LinkNameFilter_STC` → 例如 `STC` 或 `STR`
3. 按 **Run**。
4. 查看 Watch 節點與輸出報告路徑（預設寫到 `samples/last_run_report.json`）。

## 建議工作流程

```
1. DetectOnly  → 產出 clash 清單
2. 人工過濾「可自動處理」項目
3. DetectAndFix → 只對直線 MEPCurve 做軸向偏移
4. 再跑 DetectOnly 驗證殘餘衝突
5. Manual Review 清單交由 BIM Coordinator / MEP 工程師處理
```

## 輸入參數說明

| 參數 | 說明 | 預設 |
|------|------|------|
| ClearanceScale | 整體淨距倍率 | 1.0 |
| PreferOffsetAxis | 優先偏移軸 `Z` / `Y` / `X` | `Z`（上抬） |
| MaxOffsetMm | 單次最大偏移 | 600 |
| OffsetStepMm | 嘗試步距 | 50 |
| IncludeMEPvsMEP | 是否做 MEP 自碰 | true |
| SolidMode | true=Solid 精確碰撞（慢） | false |

## 輸出報告欄位

- `element_id`, `category`, `system`
- `clash_with`（ARC/STC/MEP + element id）
- `severity`（mm）
- `action`：`None` / `OffsetApplied` / `ManualReview`
- `offset_vector`（mm）

## 常見問題

**Q: 跑完管線斷開？**  
A: Fitting 連接複雜時會標記 Manual Review。請先用 DetectOnly，或縮小選取範圍（只選一層／一個系統）。

**Q: Link 找不到？**  
A: 檢查 Link 名稱關鍵字；或將 Filter 設為 `*` 收集所有 Link 再按 Category 分類。

**Q: 效能慢？**  
A: 保持 `SolidMode=false`；用框選縮小 MEP 範圍；先處理單一樓層。
