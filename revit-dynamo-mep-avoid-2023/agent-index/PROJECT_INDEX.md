# Agent Search Index — revit-dynamo-mep-avoid-2023

## Keywords

- Revit 2023, Dynamo, MEP, downward fitting, drop-under, elbow, clearance 100mm, 10cm
- 避開, 向下, Fitting, 彎頭, 已點選, 不移動障礙, 上下表面, 淨空
- Pipe, Duct, Cable Tray, Conduit, Connector, MEPCurve

## Locked rules

1. Do not move obstacles or other non-subject parts.
2. MEP avoids by generating fittings **downward only**.
3. Obstacles = all user-selected model elements.
4. Vertical surface clearance target = **100 mm**.

## Canonical files

- `docs/SCOPE.md`
- `docs/DECISIONS.md` (ADR-001..005)
- `docs/ASSUMPTIONS.md`

## Phase

`SCOPE_LOCKED_PARTIAL` — await Q6/Q8/Q12/Q13 before TECH_DESIGN / implementation.
