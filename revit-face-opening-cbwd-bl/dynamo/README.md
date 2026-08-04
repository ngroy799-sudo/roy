# Dynamo graphs

| File | Purpose |
|------|---------|
| `FaceOpening_CBWD_BL_Sync_Player.dyn` | Dynamo Player entry: sync Face Opening bottom Elevation → `CBWD B.L.` |

## Player inputs

| Name | Default | Notes |
|------|---------|-------|
| ProjectRoot | `F:\For Cursor\revit-face-opening-cbwd-bl` | Folder with `python/` + `config/` |
| SelectionMode | `SelectedOnly` | or `AllOpenings` |
| FamilyNameContains | `Face Opening` | Family/Type keyword |
| ParameterName | `CBWD B.L.` | Target Dimensions parameter |
| ReportPath | `(auto)` | Writes `samples/last_sync_report.json` |

See `../docs/USAGE_GUIDE.md`.
