# TECH_DESIGN — Face Opening Elevation → CBWD B.L.

## Data flow

```
SelectionMode?
  ├─ SelectedOnly → UIDocument.Selection → filter keyword
  └─ AllOpenings  → FilteredElementCollector(OST_GenericModel) → filter keyword
        ↓
  BoundingBox.Min.Z  (internal feet)
        ↓
  LookupParameter("CBWD B.L.").Set(z)
        ↓
  JSON report (success / skipped / errors)
```

## Selection mode normalization

Player 輸入經 `normalize_selection_mode()` 轉成 canonical 值：

| 輸入別名 | 結果 |
|----------|------|
| `SelectedOnly`, `selected`, `false`, `0` | SelectedOnly |
| `AllOpenings`, `all`, `true`, `1`, `Process All Openings` | AllOpenings |

Config 預設見 `config/sync_rules.json` → `selection_mode`。

## Elevation

- `element.get_BoundingBox(None).Min.Z`
- 以 Revit **internal units**（feet）寫入長度參數；與 API `Parameter.Set(double)` 契約一致
- 無 BoundingBox → skip（`no_bounding_box`）

## Parameter write

1. `LookupParameter(parameter_name)`
2. 檢查存在、非 read-only、StorageType Double
3. 單一 `Transaction`：`Sync Face Opening Elevation to CBWD B.L.`

## Filtering

`is_face_opening_element`：Family.Name 或 Type.Name（Symbol.Name）含 `family_name_contains`（不區分大小寫）。

## Dynamo Player loader

`dynamo/FaceOpening_CBWD_BL_Sync_Player.dyn` 內嵌短 loader：

1. 讀 `ProjectRoot`
2. `sys.path` 加入 `python/`
3. `import sync_cbwd_bl` → `run(IN)`
4. 回傳 6 個 primitive（避免 Player warnings）

## Error / skip reasons

| reason | 含義 |
|--------|------|
| `not_face_opening` | SelectedOnly 下選了不符合關鍵字的元素 |
| `no_bounding_box` | 無法取得幾何範圍 |
| `parameter_not_found` | 缺少 CBWD B.L. |
| `parameter_read_only` | 參數唯讀 |
| `parameter_not_double` | 參數型別非長度/Double |
| `Empty selection` | SelectedOnly 且未選取任何元素 |
