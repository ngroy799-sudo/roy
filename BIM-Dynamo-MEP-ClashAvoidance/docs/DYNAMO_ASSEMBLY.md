# Dynamo 組裝清單（Copy-Paste Checklist）

在 Dynamo 新增 **Python Script** 節點，按順序貼上對應檔案內容，並設定 Inputs 數量。

| 節點名 | 檔案 | IN 數量 | 主要 OUT |
|--------|------|---------|----------|
| CollectObstacles | `python/00_collect_obstacles.py` | 4 | obstacles, count, msg |
| CollectMEP | `python/01_collect_mep.py` | 3 | mep, count, msg |
| ClashDetect | `python/02_clash_detect.py` | 5 | clashes, count, msg |
| AutoOffset | `python/03_auto_offset_route.py` | 8 | plan, fixable, msg |
| OverlapGuard | `python/04_overlap_guard.py` | 4 | validated, accepted, msg, warnings |
| ApplyRevit | `python/05_apply_to_revit.py` | 2 | results, success, msg |
| Report | `python/06_report.py` | 4 | path, summary, report |

## sys.path 設定（重要）

每個 Python 節點開頭若 `__file__` 唔存在（Dynamo 常見），請先加：

```python
import sys
sys.path.append(r"F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance\python")
```

（改成你實際放置專案的路徑。）

## 接線速查

```
ConfigPath ──┬─► CollectObstacles
             ├─► CollectMEP
             ├─► ClashDetect
             ├─► AutoOffset
             └─► OverlapGuard

ARC_Filter ──► CollectObstacles
STC_Filter ──► CollectObstacles

CollectObstacles.obstacles ──┬─► ClashDetect
                             ├─► AutoOffset
                             └─► OverlapGuard

CollectMEP.mep ──┬─► ClashDetect
                 ├─► AutoOffset
                 └─► OverlapGuard

ClashDetect.clashes ──┬─► AutoOffset
                      └─► Report

RunMode ──┬─► AutoOffset
          └─► ApplyRevit

AutoOffset.plan ──► OverlapGuard ──► ApplyRevit ──► Report
```
