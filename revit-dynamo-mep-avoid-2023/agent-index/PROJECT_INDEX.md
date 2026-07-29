# Agent Search Index — revit-dynamo-mep-avoid-2023

## Keywords

- Revit 2023, Dynamo, MEP, Pipe, Duct, downward fitting, drop-under, insulation, clearance 100mm
- 避開, 向下 Fitting, 含保溫, 跳過並記錄, 接回原標高
- PipeInsulation, DuctInsulation, Elbow, Connector, MEPCurve

## Locked rules (ADR-001..007)

1. Never move obstacles / non-subject.
2. Downward fittings only.
3. Obstacles = user-selected models.
4. Clearance 100 mm.
5. Measure outer surfaces **including insulation**.
6. Both ends return to original elevation.
7. Insufficient space → skip and log.
8. Phase 1–2 subjects = Pipe + Duct.

## Canonical files

- `docs/SCOPE.md`
- `docs/DECISIONS.md`
- `docs/TECH_DESIGN.md`
- `docs/ASSUMPTIONS.md`

## Phase

`TECH_DESIGN` ready — implement Phase 1 on user go-ahead.
