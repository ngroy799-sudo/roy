# USAGE_GUIDE — Face Opening Elevation → CBWD B.L.

目標：**Revit 2023** + Dynamo Player。

## 兩個路徑唔好撈亂

| 邊度填 | 填咩 | 點解 |
|--------|------|------|
| **Dynamo Player 揀資料夾** | 一定要 `...\revit-face-opening-cbwd-bl\dynamo` | Player 靠呢度搵 `.dyn`；唔指去呢度會 Run 唔到 |
| **Input → ProjectRoot** | 可以填**同一個** `...\dynamo`，或者上一層專案資料夾 | 兩者程式都會自動搵到 `python/` + `config/` |

簡單記：**Player 同 ProjectRoot 都貼 `dynamo` 嗰條路徑就得。**

## 一次過準備

1. 解壓下載包，記住 `revit-face-opening-cbwd-bl` 位置（任意碟）
2. 確認 Face Opening 已有參數 **CBWD B.L.**（Dimensions）

## 跑法

1. Revit：**Manage → Dynamo Player**
2. 資料夾揀：`...\revit-face-opening-cbwd-bl\dynamo`（呢步一定要）
3. 開 **FaceOpening_CBWD_BL_Sync_Player**
4. Inputs：

| Input | 值 |
|-------|-----|
| **ProjectRoot** | 貼上一步同一個 `...\dynamo` 路徑（或上一層專案路徑） |
| SelectionMode | 先試 `AllOpenings`；只改選取項就用 `SelectedOnly` |
| FamilyNameContains | `Face Opening` |
| ParameterName | `CBWD B.L.` |
| ReportPath | `(auto)` |

5. `SelectedOnly` 先要在 Revit 選好 Opening，再 **Run**
6. 睇 `Status` / `SuccessCount` / `Warnings` / `ResultList`

## 模式說明

- **SelectedOnly**：只處理而家選取嘅 Face Opening  
- **AllOpenings**：文件內全部符合關鍵字嘅 Opening  

## 報告

`samples/last_sync_report.json`

## 常見問題

| 情況 | 處理 |
|------|------|
| Player 根本 Run 唔到 / 見唔到 script | Player 資料夾要指 `dynamo`，唔係上一層 |
| Outputs 空 / warnings | ProjectRoot 貼 `dynamo` 或專案根路徑 |
| `parameter_not_found` | Family 加入可寫 `CBWD B.L.` |
| Family 名唔同 | 改 `FamilyNameContains` |
