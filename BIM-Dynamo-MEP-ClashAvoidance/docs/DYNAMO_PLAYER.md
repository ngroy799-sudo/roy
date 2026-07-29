# Dynamo Player 使用說明（Revit 2023）

專用圖檔：`dynamo/MEP_AutoAvoid_ARC_STC_Player.dyn`

## 檢測範圍（已加強）

| 檢測 | 預設 |
|------|------|
| MEP vs ARC | ON |
| MEP vs STC | ON |
| **MEP vs MEP**（主模型自碰 + linked MEP） | **ON**（`IncludeMEPvsMEP = true`） |
| 設備／配件／噴頭等 | ON（見 `mep_check_categories`） |

## 一次過準備

1. 專案放到例如：`F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance`
2. Revit 2023 開 **MEP** 模型（已 Link ARC / STC；如有其他 MEP Link 亦會檢查）
3. 下載最新檔後，用 Dynamo 開一次 Player 圖 → Save（更新腳本）

## 用 Dynamo Player 跑

1. **Manage → Dynamo Player**
2. 揀資料夾：`...\BIM-Dynamo-MEP-ClashAvoidance\dynamo`
3. 開 **MEP_AutoAvoid_ARC_STC_Player**
4. 填：

| Input | 填咩 |
|-------|------|
| ProjectRoot | `F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance` |
| RunMode | 先 `DetectOnly` |
| ARC_Filter / STC_Filter | Link 關鍵字 |
| **IncludeMEPvsMEP** | **true**（一定要開先 check 埋 MEP） |
| ReportPath | `(auto)` |

5. 撳 **Play**
6. 睇 Outputs：
   - **Summary**：總結（含 ARC/STC/MEP 分開數量）
   - **ClashCount**：總 clash
   - **MepVsMepCount**：MEP 互撞數量
   - **Warnings**：警告內容（唔再只係黃色 "with warnings"）
   - **ReportPathOut**：JSON/CSV 路徑

## 「Run completed with warnings」點睇

而家腳本會把真正原因寫入 **Warnings** output：

- `No ARC/STC obstacles found...` → Link 名稱／Filter 唔啱
- `No MEP elements found...` → 唔係 MEP 主模型，或 category 無嘢
- `none` → 無腳本警告（Dynamo 黃色有時係 UI null，可忽略若 Status/結果正常）

## 建議順序

```
DetectOnly + IncludeMEPvsMEP=true → 睇 Summary / MepVsMepCount / Warnings
→ 開 JSON 報告
→ 確認後先 DetectAndFix
```
