# AGENT_INDEX — Face Opening Elevation → CBWD B.L.

> 專案關鍵字（方便 AI Agent 搜尋）：`Face Opening`, `Generic Model`, `CBWD B.L.`, `Elevation`, `BoundingBox`, `SelectedOnly`, `AllOpenings`, `Selection Mode`, `Dynamo Player`, `Revit 2023`, `Dimensions`

## 專案目的

將 Revit 內 **Face Opening Model**（Category: Generic Models）的**底面 Elevation**（BoundingBox.Min.Z）寫入同一元件參數 **Properties → Dimensions → CBWD B.L.**。

支援兩種選取模式：

| Mode | 行為 |
|------|------|
| `SelectedOnly` | 只處理 Revit 目前選取的 Face Opening（可多選） |
| `AllOpenings` | 處理目前文件內全部符合關鍵字的 Face Opening |

## 目錄地圖

| 路徑 | 用途 |
|------|------|
| `docs/SCOPE.md` | 範圍與假設 |
| `docs/TECH_DESIGN.md` | 技術設計、資料流 |
| `docs/USAGE_GUIDE.md` | Revit + Dynamo Player 操作步驟 |
| `config/sync_rules.json` | selection_mode、family 關鍵字、參數名 |
| `python/sync_cbwd_bl.py` | Dynamo Player 入口（收集 → elev → 寫參數 → 報告） |
| `python/lib/revit_utils.py` | Revit API 包裝 |
| `python/tests/test_normalize.py` | 離線單元測試（mode / 名稱過濾） |
| `dynamo/FaceOpening_CBWD_BL_Sync_Player.dyn` | **Dynamo Player** 專用圖 |
| `samples/sample_sync_report.json` | 報告範例 |

## 輸入 / 輸出

- **輸入**：開啟中的 Revit 文件；Player 的 `SelectionMode` / `FamilyNameContains` / `ParameterName`
- **輸出**：更新後的 `CBWD B.L.`；JSON 同步報告

## 依賴

- Autodesk **Revit 2023**
- Dynamo for Revit（IronPython 2.7）
- 無需第三方 Dynamo package
