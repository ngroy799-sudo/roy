# Dynamo Player 使用說明（Revit 2023）

專用圖檔：`dynamo/MEP_AutoAvoid_ARC_STC_Player.dyn`

## 一次過準備

1. 將專案放到本機，例如：  
   `F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance`
2. Revit 2023 開 **MEP** 模型（已 Link ARC / STC）
3. 第一次用 Dynamo 開 Player 圖檔，確認 Python 引擎係 **IronPython2**：  
   Dynamo → Settings → Python → IronPython2  
   然後 **Save** 個 `.dyn`

## 用 Dynamo Player 跑

1. Revit 上方：**Manage** → **Dynamo Player**
2. 撳資料夾圖示，揀：  
   `...\BIM-Dynamo-MEP-ClashAvoidance\dynamo`
3. 見到 **MEP_AutoAvoid_ARC_STC_Player** → 撳左邊箭嘴展開 Inputs
4. 填以下參數：

| Input（Player 顯示名） | 填咩 | 例子 |
|------------------------|------|------|
| ProjectRoot | 專案根目錄完整路徑 | `F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance` |
| RunMode | 先 DetectOnly | `DetectOnly` |
| ARC_Filter | ARC Link 關鍵字 | `ARC` |
| STC_Filter | STC Link 關鍵字 | `STC` |
| ClearanceScale | 淨距倍率 | `1.0` |
| PreferOffsetAxis | 優先偏移方向 | `Z` |
| MaxOffsetMm | 最大偏移 mm | `600` |
| OffsetStepMm | 步距 mm | `50` |
| IncludeMEPvsMEP | MEP 自碰檢測 | `true` |
| ReportPath | 可留空 | （空 = `samples\last_run_report.json`） |

5. 撳 **Play（▶）**
6. 睇 Outputs：`Summary`、`ReportPath`、`ClashCount`

## 建議操作順序

```
1) RunMode = DetectOnly   → Play   （只檢測，唔改模型）
2) 打開 ReportPath 個 JSON/CSV 睇 clash
3) 確認 OK 之後改 RunMode = DetectAndFix → Play
4) 再改返 DetectOnly → Play 驗證
```

## 常見問題

**Player 搵唔到圖／Inputs 係空？**  
用 Dynamo 開一次 `MEP_AutoAvoid_ARC_STC_Player.dyn` → Save → 再開 Player。

**報 ProjectRoot not found？**  
路徑要指向有 `python` 同 `config` 嘅專案根目錄，唔好只指到 `dynamo` 資料夾。

**冇 clash / obstacles = 0？**  
檢查 Link 名稱是否含 `ARC`/`STC`；必要時改 Filter 關鍵字。

**想改淨距？**  
改 `config/clearance_rules.json`，唔使改 Dynamo。
