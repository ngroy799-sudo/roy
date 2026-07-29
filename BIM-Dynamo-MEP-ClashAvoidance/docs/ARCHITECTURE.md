# Architecture — MEP Auto Avoid ARC/STC + Overlap Guard

## 目標

1. **MEP 自動避開 ARC / STC**：風管、水管、橋架、電線管遇到建築／結構實體時自動改線。
2. **避免所有模型重疊**：MEP vs ARC、MEP vs STC、MEP vs MEP，以及可選的 ARC↔STC 警示。

## 資料流

```
┌─────────────┐   ┌─────────────┐
│ ARC Link(s) │   │ STC Link(s) │
└──────┬──────┘   └──────┬──────┘
       │                 │
       ▼                 ▼
  collect_obstacles (AABB + optional Solid)
       │
       ▼
┌──────────────────┐     ┌─────────────────┐
│ MEP elements     │────▶│ clash_detect    │
│ Duct/Pipe/Tray   │     │ + clearance     │
└────────┬─────────┘     └────────┬────────┘
         │                        │
         │         clash hits     │
         ▼                        ▼
   auto_offset_route ◀────────────┘
         │
         ▼
   overlap_guard (MEP↔MEP / residual)
         │
         ├─▶ apply_to_revit (Transaction)
         └─▶ report (JSON/CSV)
```

## 策略（務實可落地）

完整「像 MagiCAD 一樣」自動繞障是重度求解問題。本專案採用 **分層策略**：

| 層級 | 方法 | 適用 |
|------|------|------|
| L1 | Expanded AABB clash + 固定軸向偏移（±X/±Y/±Z） | 大多數直線段、快速 QC |
| L2 | 多候選路徑評分（最短偏移、最少轉彎、淨距最大） | 可解但需人工確認的衝突 |
| L3 | 標記為 Manual Review | 複雜彎頭組、設備本體、豎井交叉 |

## 障礙來源（預設 Category）

**ARC**：Walls, Floors, Ceilings, Roofs, Columns (建築), Doors/Windows openings 不當作實體障礙（可選）

**STC**：Structural Framing, Structural Columns, Structural Foundations, Floors (結構)

**MEP**：Ducts, Duct Fittings, Pipes, Pipe Fittings, Cable Trays, Conduits, Mechanical Equipment, Electrical Equipment, Plumbing Fixtures（設備預設只檢測、不自動移動）

## 淨距（Clearance）

由 `config/clearance_rules.json` 控制。預設範例：

- Duct vs Structure：150 mm
- Pipe vs Structure：100 mm
- CableTray vs Structure：50 mm
- MEP vs MEP：依系統類型（見設定檔）

## 寫回 Revit 的安全規則

1. 僅修改 **MEP 主模型** 內的 MEPCurve（Duct/Pipe/CableTray/Conduit）。
2. 不修改 Link 內 ARC/STC。
3. 每個元素一次 Transaction；失敗則 rollback 並記入報告。
4. Fitting 群組：若移動主線導致 fitting 斷開，標記 Manual Review，不強制拆組。

## 已知限制

- 不取代專業 MEP 路由軟體的水力／氣流計算。
- 不處理 Fabrication Parts 細節。
- Solid 精確碰撞比 AABB 慢；預設用 Expanded BoundingBox，可切換 Solid（慢）。
- 垂直立管與多層交叉需人工覆核。
