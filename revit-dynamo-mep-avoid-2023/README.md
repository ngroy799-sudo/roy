# Revit Dynamo MEP Auto-Avoid (Revit 2023)

MEP（Pipe/Duct）**只向下 Fitting** 避開已點選障礙；垂直淨空 **100 mm（含保溫）**；不移動其他部份；空間不足則跳過並記錄。

## 快速導航

| 路徑 | 用途 |
|------|------|
| `docs/SCOPE.md` | 範圍與鎖定規則 |
| `docs/DECISIONS.md` | ADR-001～007 |
| `docs/TECH_DESIGN.md` | **技術設計（可實作）** |
| `docs/ASSUMPTIONS.md` | 剩餘工程假設 |
| `agent-index/PROJECT_INDEX.md` | AI 搜尋索引 |
| `dynamo/` | 未來 `.dyn` |
| `python/` | 未來腳本 |
| `samples/` | 測試案例 |

## 下載

- PR：https://github.com/ngroy799-sudo/roy/pull/9  
- 分支 ZIP：https://github.com/ngroy799-sudo/roy/archive/refs/heads/cursor/revit-dynamo-mep-avoid-scope-2156.zip  
- 專案資料夾：`revit-dynamo-mep-avoid-2023/`

## 狀態

`SCOPE_LOCKED` + `TECH_DESIGN` 已就緒 — 等待指示開始 Phase 1 實作。
