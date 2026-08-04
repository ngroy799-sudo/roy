# Dynamo graphs

| File | Purpose |
|------|---------|
| `FaceOpening_CBWD_BL_Sync_Player.dyn` | Dynamo Player entry: sync Face Opening bottom Elevation → `CBWD B.L.` |

## Player inputs

| Name | Default | Notes |
|------|---------|-------|
| ProjectRoot | `PASTE_YOUR_PATH_HERE` | **Required.** Full path to folder with `python/` + `config/` (not `dynamo/`) |
| SelectionMode | `AllOpenings` | or `SelectedOnly` |
| FamilyNameContains | `Face Opening` | Family/Type keyword |
| ParameterName | `CBWD B.L.` | Target Dimensions parameter |
| ReportPath | `(auto)` | Writes `samples/last_sync_report.json` |

See `../docs/USAGE_GUIDE.md`.
