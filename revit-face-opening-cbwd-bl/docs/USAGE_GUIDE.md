# USAGE_GUIDE — Face Opening Elevation → CBWD B.L.

目標：**Revit 2023** + Dynamo Player。

## 一次過準備

1. 解壓／放好下載的 `revit-face-opening-cbwd-bl`（任意碟都可以，唔需要 F 盤）
2. 喺檔案總管複製呢層資料夾嘅**完整路徑**（入面要同時有 `python` 同 `config`）
3. 確認 Face Opening family 已有 instance 參數 **CBWD B.L.**（Dimensions）

## 關鍵：一定要填 ProjectRoot

Dynamo Player 入面展開 **Inputs**，將 **ProjectRoot** 改成你本機路徑，例如：

`C:\Users\你嘅名\Downloads\revit-face-opening-cbwd-bl`

- 預設 `PASTE_YOUR_PATH_HERE` / 舊版 `F:\...` **唔會**行得通
- 唔好指去 `dynamo` 子資料夾；要指去有 `python` + `config` 嗰層

如果 Outputs 全空 +「Run complete with warnings」→ 幾乎一定係 ProjectRoot 未設對。睇 **ResultList** / **Warnings** / **Status**。

## 模式 A — 只同步部份 Opening（SelectedOnly）

1. 在 Revit 選取一個或多個 Face Opening
2. **Manage → Dynamo Player** → 資料夾 `...\revit-face-opening-cbwd-bl\dynamo`
3. 開 **FaceOpening_CBWD_BL_Sync_Player**
4. 設定：

| Input | 值 |
|-------|-----|
| ProjectRoot | 你本機下載路徑（見上） |
| SelectionMode | `SelectedOnly` |
| FamilyNameContains | `Face Opening`（或你的 family 關鍵字） |
| ParameterName | `CBWD B.L.` |
| ReportPath | `(auto)` |

5. 按 **Run**
6. 檢查 Properties → Dimensions → **CBWD B.L.** 是否已等於底面 Elevation
7. 看 Player Outputs：`Status`、`SuccessCount`、`Warnings`、`ResultList`

若未預先選取，Status 會是 `ERROR`，Warnings：`Empty selection`。

## 模式 B — 同步全部 Opening（AllOpenings）

1. 無需在 Revit 預選
2. Dynamo Player 同上，填好 **ProjectRoot**
3. 將 **SelectionMode** 設為 `AllOpenings`
4. 按 **Run**
5. 文件內所有 Generic Model（Family/Type 名稱含關鍵字）都會被更新

## 輸出報告

預設：`samples/last_sync_report.json`

內容包含 `selection_mode`、`successes`、`skipped`、`errors`（含 ElementId）。

## 常見問題

| 情況 | 處理 |
|------|------|
| Outputs 全空 / Run complete with warnings | 展開 Inputs，將 **ProjectRoot** 改成有 `python`+`config` 嘅本機路徑；再睇 `ResultList` |
| Status=`ERROR`、Warnings 講 ProjectRoot | 路徑指錯（唔好指 `dynamo\`）；用檔案總管複製地址列 |
| Success=0、skipped 含 `parameter_not_found` | 在 family 加入可寫入的 `CBWD B.L.` 長度參數 |
| SelectedOnly 選了牆／其他 GM | 會記 `not_face_opening` 並跳過 |
| Family 名稱不含 "Face Opening" | 改 `FamilyNameContains` 或改 config |
| Loader failed / invalid ProjectRoot | 確認 ProjectRoot 指向含 `python/sync_cbwd_bl.py` 的資料夾 |

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
