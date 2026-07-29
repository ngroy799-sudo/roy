# Revit Dynamo MEP Auto-Avoid (Revit 2023)

獨立專案：MEP **只向下 Fitting** 避開已點選障礙模型；垂直淨空 **10 cm**；不移動其他部份。

## 快速導航

| 路徑 | 用途 |
|------|------|
| `docs/SCOPE.md` | 鎖定規則、範圍、剩餘問題 |
| `docs/DECISIONS.md` | 已確認 ADR |
| `docs/ASSUMPTIONS.md` | 待確認工作假設 |
| `agent-index/PROJECT_INDEX.md` | AI 搜尋索引 |
| `dynamo/` | 未來 `.dyn` |
| `python/` | 未來腳本 |
| `samples/` | 測試案例 |

## 環境

- **Revit**：2023
- **捷徑資料夾**：`C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Autodesk\Revit 2023`
- **工具**：Dynamo for Revit 2023

## 狀態

`SCOPE_LOCKED_PARTIAL` — 核心避開規則已確認；待 Q5–Q13（最少 Q6/Q8/Q12/Q13）後進入 TECH_DESIGN。
