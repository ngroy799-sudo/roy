# Dynamo Node Graph — MEP_AutoAvoid_ARC_STC

## 建議節點佈局（左→右）

```
[Inputs]
  String ConfigPath
  String RunMode          ("DetectOnly" | "DetectAndFix")
  String ARC_Filter
  String STC_Filter
  Number ClearanceScale
  String PreferOffsetAxis
  Number MaxOffsetMm
  Number OffsetStepMm
  Boolean IncludeMEPvsMEP
  Boolean SolidMode
       │
       ▼
[Python] 00_collect_obstacles.py
  IN: ARC_Filter, STC_Filter, SolidMode
  OUT: obstacle_records (list[dict])
       │
       ▼
[Python] 01_collect_mep.py
  IN: (optional) selected elements / active view level filter
  OUT: mep_records
       │
       ▼
[Python] 02_clash_detect.py
  IN: mep_records, obstacle_records, ConfigPath, ClearanceScale, IncludeMEPvsMEP
  OUT: clash_pairs
       │
       ├──────────────────────────────┐
       ▼                              ▼
[Python] 03_auto_offset_route.py    [Watch] Clash Count
  IN: clash_pairs, PreferOffsetAxis,
      MaxOffsetMm, OffsetStepMm, RunMode
  OUT: fix_plan
       │
       ▼
[Python] 04_overlap_guard.py
  IN: fix_plan, mep_records, obstacle_records, ConfigPath
  OUT: validated_plan
       │
       ▼
[Python] 05_apply_to_revit.py
  IN: validated_plan, RunMode
  OUT: apply_results
       │
       ▼
[Python] 06_report.py
  IN: apply_results, clash_pairs, output_path
  OUT: report_path, summary_string
```

## Dynamo 內建節點輔助（可選）

- `Categories` + `All Elements of Category`：若要改純節點收集 MEP
- `Element.GetParameterValueByName`：讀系統名稱
- `Select Model Elements`：限制處理範圍
- `File Path`：選 config / report 路徑

## 執行順序重點

1. 必須先 collect obstacles，再 collect MEP。
2. clash_detect 必須在 offset 之前。
3. overlap_guard 必須在 apply 之前（防止修好 A 又撞到 B）。
4. DetectOnly 時 `05_apply_to_revit` 應跳過寫入。
