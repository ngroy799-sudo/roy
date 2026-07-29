# Agent Search Index — revit-dynamo-mep-avoid-2023

## Keywords (EN / 中文)

- Revit 2023, Dynamo, MEP, Architecture, Structure, clash, clearance, offset, reroute
- 避開, 衝突, 淨空, 偏移, 繞行, 風管, 水管, 橋架, 結構梁, 建築牆
- ElementTransformUtils, MEPCurve, BoundingBox, Solid Intersection, Dynamo Player

## Discipline codes

- `MEP` → mechanical / electrical / plumbing elements
- `ARC` → architecture elements
- `STC` → structure elements

## Canonical files

- Scope truth: `docs/SCOPE.md`
- Assumptions: `docs/ASSUMPTIONS.md`
- Decisions: `docs/DECISIONS.md`
- Future graphs: `dynamo/*.dyn`
- Future scripts: `python/*.py`

## Current phase

`SCOPE_DRAFT` — no implementation until user answers open questions in SCOPE.md (especially Q1, Q2, Q3, Q5).

## Do not invent

Do not assume offset vs reroute, link handling, or clearance values beyond `ASSUMPTIONS.md` until `DECISIONS.md` is updated.
