# Dynamo graphs

| File | Purpose |
|------|---------|
| `FaceOpening_CBWD_BL_Sync_Player.dyn` | Dynamo Player entry: sync Face Opening bottom Elevation → `CBWD B.L.` |

## Player inputs

| Name | Default | Notes |
|------|---------|-------|
| ProjectRoot | `PASTE_DYNAMO_OR_PROJECT_FOLDER` | **Same as Player browse folder (`dynamo`) is OK**, or parent project folder |
| SelectionMode | `AllOpenings` | or `SelectedOnly` |
| FamilyNameContains | `Face Opening` | Family/Type keyword |
| ParameterName | `CBWD B.L.` | Target Dimensions parameter |
| ReportPath | `(auto)` | Writes `samples/last_sync_report.json` |

**Player browse folder** must be `dynamo/` (where this `.dyn` lives).  
**ProjectRoot** may be that same `dynamo/` path — the script walks up to find `python/` + `config/`.

See `../docs/USAGE_GUIDE.md`.
