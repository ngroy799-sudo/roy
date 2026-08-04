# USAGE_GUIDE — Face Opening Elevation → CBWD B.L.

目標：**Revit 2023** + Dynamo Player。

## 一次過準備

1. 複製專案到本機，建議：`F:\For Cursor\revit-face-opening-cbwd-bl`
2. 確認 Face Opening family 已有 instance 參數 **CBWD B.L.**（Dimensions）
3. 用 Dynamo 開一次 `dynamo/FaceOpening_CBWD_BL_Sync_Player.dyn` 並 Save（可選，方便更新腳本）

## 模式 A — 只同步部份 Opening（SelectedOnly）

1. 在 Revit 選取一個或多個 Face Opening
2. **Manage → Dynamo Player** → 資料夾 `...\revit-face-opening-cbwd-bl\dynamo`
3. 開 **FaceOpening_CBWD_BL_Sync_Player**
4. 設定：

| Input | 值 |
|-------|-----|
| ProjectRoot | `F:\For Cursor\revit-face-opening-cbwd-bl` |
| SelectionMode | `SelectedOnly` |
| FamilyNameContains | `Face Opening`（或你的 family 關鍵字） |
| ParameterName | `CBWD B.L.` |
| ReportPath | `(auto)` |

5. 按 **Play**
6. 檢查 Properties → Dimensions → **CBWD B.L.** 是否已等於底面 Elevation
7. 看 Player Outputs：`SuccessCount`、`SkippedCount`、`Warnings`、`Status`

若未預先選取，Status 會是 `ERROR`，Warnings：`Empty selection`。

## 模式 B — 同步全部 Opening（AllOpenings）

1. 無需在 Revit 預選
2. Dynamo Player 同上
3. 將 **SelectionMode** 設為 `AllOpenings`
4. 按 **Play**
5. 文件內所有 Generic Model（Family/Type 名稱含關鍵字）都會被更新

## 輸出報告

預設：`samples/last_sync_report.json`

內容包含 `selection_mode`、`successes`、`skipped`、`errors`（含 ElementId）。

## 常見問題

| 情況 | 處理 |
|------|------|
| Success=0、skipped 含 `parameter_not_found` | 在 family 加入可寫入的 `CBWD B.L.` 長度參數 |
| SelectedOnly 選了牆／其他 GM | 會記 `not_face_opening` 並跳過 |
| Family 名稱不含 "Face Opening" | 改 `FamilyNameContains` 或改 config |
| Loader failed | 確認 ProjectRoot 指向含 `python/` 與 `config/` 的資料夾 |

## Config 覆寫

編輯 `config/sync_rules.json`：

```json
{
  "selection_mode": "SelectedOnly",
  "family_name_contains": "Face Opening",
  "parameter_name": "CBWD B.L."
}
```

Player 輸入會覆寫同名設定（空白 / `(default)` 則用 config）。
