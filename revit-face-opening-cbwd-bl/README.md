# Face Opening 底面 Elevation → CBWD B.L.

Revit 2023 Dynamo 工具：把 **Face Opening**（Generic Model）底面 Elevation 寫入參數 **CBWD B.L.**。

## 功能

- 讀取每個 Face Opening 的 BoundingBox 底面 Z（專案 Elevation）
- 寫入 Properties → Dimensions → **CBWD B.L.**
- **SelectionMode**
  - `SelectedOnly`：只處理你在 Revit 選好的 Opening（可多選）
  - `AllOpenings`：處理文件內全部符合關鍵字的 Opening

## 快速開始（Dynamo Player）

1. 把本資料夾放到本機任意位置（唔需要 F 盤）
2. Revit 2023 開啟專案模型
3. **Manage → Dynamo Player** → 指向 `...\revit-face-opening-cbwd-bl\dynamo`
4. 開 **FaceOpening_CBWD_BL_Sync_Player**
5. 展開 **Inputs**，**一定要改 ProjectRoot** 成你本機完整路徑（資料夾內要有 `python` + `config`）
6. 其他：

| Input | 說明 |
|-------|------|
| ProjectRoot | 例如 `C:\Users\...\Downloads\revit-face-opening-cbwd-bl` |
| SelectionMode | `SelectedOnly` 或 `AllOpenings`（建議先試 `AllOpenings`） |
| FamilyNameContains | 預設 `Face Opening` |
| ParameterName | 預設 `CBWD B.L.` |
| ReportPath | `(auto)` |

7. 若用 `SelectedOnly`：先在 Revit 選好 Face Opening，再按 **Run**
8. 看 Outputs：`Status` / `SuccessCount` / `Warnings` / `ResultList`  
   - 若全空 + warnings → ProjectRoot 仲未指對

詳見 [docs/USAGE_GUIDE.md](docs/USAGE_GUIDE.md)。Agent 搜尋入口：[AGENT_INDEX.md](AGENT_INDEX.md)。

## 專案結構

```
revit-face-opening-cbwd-bl/
  AGENT_INDEX.md
  README.md
  config/sync_rules.json
  python/sync_cbwd_bl.py
  python/lib/revit_utils.py
  dynamo/FaceOpening_CBWD_BL_Sync_Player.dyn
  docs/
  samples/
```

## 離線測試

```bash
cd revit-face-opening-cbwd-bl/python
python -m unittest tests.test_normalize -v
```
