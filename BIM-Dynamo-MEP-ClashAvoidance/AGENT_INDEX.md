# AGENT_INDEX — BIM Dynamo MEP Clash Avoidance

> 專案關鍵字（方便 AI Agent 搜尋）：`MEP`, `Dynamo`, `Revit`, `clash avoidance`, `ARC`, `STC`, `overlap`, `duct`, `pipe`, `conduit`, `auto-route`, `clearance`

## 專案目的

將 MEP 管線／風管／橋架自動避開 Architectural (ARC) 與 Structural (STC) 模型幾何，並避免所有模型元素重疊。

## 目錄地圖

| 路徑 | 用途 |
|------|------|
| `docs/ARCHITECTURE.md` | 系統架構、資料流、限制 |
| `docs/USAGE_GUIDE.md` | Revit + Dynamo 操作步驟 |
| `docs/NODE_GRAPH.md` | Dynamo 節點圖說明 |
| `config/clearance_rules.json` | 淨距／避讓規則設定 |
| `python/` | Dynamo Python Script 節點原始碼 |
| `dynamo/MEP_AutoAvoid_ARC_STC.dyn` | 主 Dynamo 圖（可直接開） |
| `samples/sample_clash_report.json` | 輸出報告範例 |

## 核心模組（python/）

| 檔案 | 職責 |
|------|------|
| `00_collect_obstacles.py` | 收集 ARC/STC linked model 障礙幾何 |
| `01_collect_mep.py` | 收集 MEP 元素（Duct/Pipe/CableTray/Conduit） |
| `02_clash_detect.py` | MEP vs ARC/STC 碰撞檢測 |
| `03_auto_offset_route.py` | 自動偏移路徑避開障礙 |
| `04_overlap_guard.py` | 全模型重疊防護（含 MEP vs MEP） |
| `05_apply_to_revit.py` | 將調整後路徑寫回 Revit |
| `06_report.py` | 產出 JSON/CSV 報告 |
| `lib/geometry_utils.py` | BoundingBox、淨距、偏移共用函式 |
| `lib/revit_utils.py` | Revit API 包裝（Link、Category、Transaction） |

## 輸入 / 輸出

- **輸入**：開啟中的 MEP 模型 + ARC/STC Revit Links；`clearance_rules.json`
- **輸出**：調整後的 MEP 路徑；Clash/Overlap 報告；未解決項目清單

## 依賴

- Autodesk Revit 2022+（建議 2023/2024）
- Dynamo for Revit（對應版本）
- 建議 Package：`Clockwork`, `Rhythm`, `spring nodes`（可選，本專案 Python 為主）
