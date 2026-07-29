# BIM Dynamo — MEP 自動避開 ARC / STC + 防重疊

獨立專案資料夾：`BIM-Dynamo-MEP-ClashAvoidance/`  
**目標環境：Revit 2023**（Dynamo IronPython 2.7）— 詳見 [`docs/REVIT_2023.md`](docs/REVIT_2023.md)

## 做咩

在 Revit + Dynamo 入面：

1. 從 **ARC / STC Link** 收集障礙幾何  
2. 收集 **MEP**（Duct / Pipe / CableTray / Conduit）  
3. 用淨距規則做 **Clash 檢測**  
4. 對可移動直線段提出 **軸向自動偏移**（優先上抬 Z）  
5. **Overlap Guard** 防止修好 A 又撞到 B（含 MEP vs MEP）  
6. 可選寫回 Revit，並輸出 JSON/CSV 報告  

## 點樣開始

詳見：

- [`AGENT_INDEX.md`](AGENT_INDEX.md) — AI Agent 搜尋地圖  
- [`docs/USAGE_GUIDE.md`](docs/USAGE_GUIDE.md) — 操作步驟  
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 架構同限制  
- [`config/clearance_rules.json`](config/clearance_rules.json) — 淨距規則  

### 離線 Demo（唔使 Revit）

```bash
cd BIM-Dynamo-MEP-ClashAvoidance
python python/tests/test_geometry_utils.py
python python/offline_pipeline.py
```

### Revit 流程（建議）

1. 開 MEP 模型，確認 ARC/STC 已 Link  
2. 開 `dynamo/MEP_AutoAvoid_ARC_STC.dyn`  
3. 按 `docs/NODE_GRAPH.md` 將 `python/00`–`06` 貼入 Python Script 節點並接線  
4. `RunMode = DetectOnly` 先跑  
5. 覆核報告後先改 `DetectAndFix`

## 重要提醒

- 自動偏移係 **L1/L2 實用方案**（AABB + 軸向候選），唔係完整水力／氣流自動路由引擎。  
- Fitting／設備唔會自動亂移；解唔開會入 **ManualReview**。  
- 生產環境請先備份／用工作集副本測試。

## 路徑備註

若本機習慣存去 `F:\For Cursor`，可將成個 `BIM-Dynamo-MEP-ClashAvoidance` 資料夾複製過去，並更新 Dynamo 入面 `ConfigPath`。
