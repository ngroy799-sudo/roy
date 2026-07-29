# -*- coding: utf-8 -*-
"""
player_run_all.py — Single entry for Dynamo Player (Revit 2023 / IronPython2).

IN[0] ProjectRoot   e.g. F:\\For Cursor\\BIM-Dynamo-MEP-ClashAvoidance
IN[1] RunMode       DetectOnly | DetectAndFix
IN[2] ARC_Filter    e.g. ARC
IN[3] STC_Filter    e.g. STC
IN[4] ClearanceScale  number, default 1.0
IN[5] PreferOffsetAxis  Z/Y/X
IN[6] MaxOffsetMm
IN[7] OffsetStepMm
IN[8] IncludeMEPvsMEP  bool
IN[9] ReportPath   optional; empty = samples/last_run_report.json under ProjectRoot

OUT = [summary, report_path, clash_count, approved_count, apply_success, report]
"""
from __future__ import print_function
import os
import sys
import json

try:
    project_root = IN[0]  # noqa: F821
    run_mode = IN[1] if len(IN) > 1 else "DetectOnly"  # noqa: F821
    arc_filter = IN[2] if len(IN) > 2 else "ARC"  # noqa: F821
    stc_filter = IN[3] if len(IN) > 3 else "STC"  # noqa: F821
    clearance_scale = float(IN[4]) if len(IN) > 4 and IN[4] is not None else 1.0  # noqa: F821
    prefer_axis = IN[5] if len(IN) > 5 and IN[5] else "Z"  # noqa: F821
    max_offset = float(IN[6]) if len(IN) > 6 and IN[6] is not None else 600.0  # noqa: F821
    offset_step = float(IN[7]) if len(IN) > 7 and IN[7] is not None else 50.0  # noqa: F821
    include_mep = bool(IN[8]) if len(IN) > 8 and IN[8] is not None else True  # noqa: F821
    report_path = IN[9] if len(IN) > 9 else ""  # noqa: F821
except NameError:
    project_root = r"F:\For Cursor\BIM-Dynamo-MEP-ClashAvoidance"
    run_mode = "DetectOnly"
    arc_filter, stc_filter = "ARC", "STC"
    clearance_scale, prefer_axis = 1.0, "Z"
    max_offset, offset_step, include_mep = 600.0, 50.0, True
    report_path = ""


def _s(v):
    if v is None:
        return ""
    if hasattr(v, "ToString"):
        return v.ToString()
    return str(v)


project_root = _s(project_root).strip().strip('"')
run_mode = _s(run_mode).strip() or "DetectOnly"
arc_filter = _s(arc_filter).strip() or "ARC"
stc_filter = _s(stc_filter).strip() or "STC"
prefer_axis = _s(prefer_axis).strip() or "Z"
report_path = _s(report_path).strip()

python_dir = os.path.join(project_root, "python")
config_path = os.path.join(project_root, "config", "clearance_rules.json")
if not report_path:
    report_path = os.path.join(project_root, "samples", "last_run_report.json")

if python_dir not in sys.path:
    sys.path.append(python_dir)

errors = []
if not os.path.isdir(project_root):
    errors.append("ProjectRoot not found: " + project_root)
if not os.path.isfile(config_path):
    errors.append("Config not found: " + config_path)

if errors:
    OUT = [" | ".join(errors), "", 0, 0, 0, {"error": errors}]
else:
    # Import module functions by executing the node scripts' run() APIs where possible.
    # Prefer direct imports from lib + inline orchestration for Player stability.
    from lib.revit_utils import get_doc, load_rules, iter_link_docs, collect_category_records
    from lib.geometry_utils import (
        expand_aabb, aabb_intersects, aabb_penetration_mm, clearance_for,
        translate_aabb, offset_from_axis, score_candidate, classify_mep_family,
    )
    from Autodesk.Revit.DB import FilteredElementCollector
    from lib.revit_utils import category_enum, element_aabb_mm, mep_system_name, element_id_int, move_mep_curve_by_offset_mm, make_element_id
    from collections import defaultdict
    from datetime import datetime
    import csv

    rules = load_rules(config_path)
    doc = get_doc()

    # --- collect obstacles ---
    obstacles = []
    for role, link, link_doc, xf in iter_link_docs(doc, rules, role_filter=None):
        name = link.Name
        effective = role
        if effective not in ("ARC", "STC"):
            if arc_filter.upper() in name.upper():
                effective = "ARC"
            elif stc_filter.upper() in name.upper():
                effective = "STC"
            else:
                continue
        if effective == "ARC" and arc_filter not in ("*", ""):
            if role != "ARC" and arc_filter.upper() not in name.upper() and arc_filter.upper() != "ARC":
                continue
        if effective == "STC" and stc_filter not in ("*", ""):
            if role != "STC" and stc_filter.upper() not in name.upper() and stc_filter.upper() != "STC":
                continue
        cats = rules.get("obstacle_categories", {}).get(effective, [])
        obstacles.extend(collect_category_records(doc, link_doc, xf, cats, effective, name))

    # --- collect MEP ---
    mep_records = []
    movable = set(rules.get("mep_movable_categories", []))
    for cname in rules.get("mep_categories", []):
        bic = category_enum(cname)
        if bic is None:
            continue
        for el in FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType():
            try:
                cat = el.Category.Name if el.Category else ""
                try:
                    bic_name = str(el.Category.BuiltInCategory)
                except Exception:
                    bic_name = cat
                aabb = element_aabb_mm(el, None)
                if aabb is None:
                    continue
                kind = classify_mep_family(bic_name + " " + cat)
                mep_records.append({
                    "id": element_id_int(el),
                    "category": bic_name,
                    "category_name": cat,
                    "kind": kind,
                    "role": "MEP",
                    "source": "HOST",
                    "system": mep_system_name(el),
                    "aabb": aabb,
                    "movable": any(m in bic_name for m in movable) or kind in ("Duct", "Pipe", "CableTray", "Conduit"),
                    "name": getattr(el, "Name", "") or "",
                })
            except Exception:
                continue

    # --- clash detect ---
    clashes = []
    for mep in mep_records:
        kind = mep.get("kind", "MEP")
        for obs in obstacles:
            role = obs.get("role", "ARC")
            clr = clearance_for(kind, role, rules) * clearance_scale
            expanded = expand_aabb(mep["aabb"], clr)
            if aabb_intersects(expanded, obs["aabb"]):
                clashes.append({
                    "mep_id": mep["id"],
                    "mep_kind": kind,
                    "mep_system": mep.get("system"),
                    "mep_movable": mep.get("movable", False),
                    "other_id": obs["id"],
                    "other_role": role,
                    "other_category": obs.get("category"),
                    "other_source": obs.get("source"),
                    "clearance_mm": clr,
                    "penetration_mm": aabb_penetration_mm(expanded, obs["aabb"]),
                    "clash_kind": "MEP_vs_{0}".format(role),
                    "mep_aabb": mep["aabb"],
                    "other_aabb": obs["aabb"],
                })
    if include_mep:
        n = len(mep_records)
        for i in range(n):
            for j in range(i + 1, n):
                a, b = mep_records[i], mep_records[j]
                clr = clearance_for(a.get("kind", "MEP"), "MEP", rules) * clearance_scale
                expanded = expand_aabb(a["aabb"], clr)
                if aabb_intersects(expanded, b["aabb"]):
                    clashes.append({
                        "mep_id": a["id"],
                        "mep_kind": a.get("kind"),
                        "mep_system": a.get("system"),
                        "mep_movable": a.get("movable", False),
                        "other_id": b["id"],
                        "other_role": "MEP",
                        "other_category": b.get("category"),
                        "other_source": b.get("source"),
                        "clearance_mm": clr,
                        "penetration_mm": aabb_penetration_mm(expanded, b["aabb"]),
                        "clash_kind": "MEP_vs_MEP",
                        "mep_aabb": a["aabb"],
                        "other_aabb": b["aabb"],
                    })

    # --- auto offset ---
    routing = rules.get("routing", {})
    axes = routing.get("axis_try_order", ["Z", "Y", "X", "-Z", "-Y", "-X"])
    prefer = prefer_axis or routing.get("prefer_offset_axis", "Z")
    axes = [prefer] + [a for a in axes if a != prefer]
    weights = routing.get("score_weights", {})
    mep_by_id = {m["id"]: m for m in mep_records}
    working = {m["id"]: m["aabb"] for m in mep_records}
    by = defaultdict(list)
    for c in clashes:
        by[c["mep_id"]].append(c)

    plan = []
    for mep_id, hits in by.items():
        mep = mep_by_id.get(mep_id)
        if mep is None:
            continue
        item = {
            "mep_id": mep_id,
            "mep_kind": mep.get("kind"),
            "system": mep.get("system"),
            "clash_count": len(hits),
        }
        if not mep.get("movable", False):
            item.update({"action": "ManualReview", "reason": "NotMovable", "offset_mm": (0, 0, 0)})
            plan.append(item)
            continue
        if run_mode == "DetectOnly":
            item.update({"action": "None", "reason": "DetectOnly", "offset_mm": (0, 0, 0)})
            plan.append(item)
            continue
        best = None
        dist = offset_step
        while dist <= max_offset + 1e-6:
            for axis in axes:
                off = offset_from_axis(axis, dist)
                trial = translate_aabb(mep["aabb"], off)
                rem = 0
                for obs in obstacles:
                    clr = clearance_for(mep.get("kind", "MEP"), obs.get("role", "ARC"), rules) * clearance_scale
                    if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                        rem += 1
                for oid, oabb in working.items():
                    if oid == mep_id:
                        continue
                    clr = clearance_for(mep.get("kind", "MEP"), "MEP", rules) * clearance_scale
                    if aabb_intersects(expand_aabb(trial, clr), oabb):
                        rem += 1
                sc = score_candidate(off, rem, axis, prefer, weights)
                cand = {"offset_mm": off, "remaining": rem, "score": sc, "axis": axis}
                if best is None or cand["score"] < best["score"]:
                    best = cand
                if rem == 0:
                    break
            if best and best["remaining"] == 0:
                break
            dist += offset_step
        if best and best["remaining"] == 0:
            item.update({"action": "OffsetProposed", "reason": "Cleared", "offset_mm": best["offset_mm"], "axis": best["axis"]})
            working[mep_id] = translate_aabb(mep["aabb"], best["offset_mm"])
        else:
            item.update({
                "action": "ManualReview",
                "reason": "NoClearOffsetWithinMax",
                "offset_mm": best["offset_mm"] if best else (0, 0, 0),
            })
        plan.append(item)

    # --- overlap guard ---
    proposed = {}
    for item in plan:
        mep = mep_by_id[item["mep_id"]]
        if item.get("action") == "OffsetProposed":
            proposed[item["mep_id"]] = translate_aabb(mep["aabb"], item.get("offset_mm", (0, 0, 0)))
        else:
            proposed[item["mep_id"]] = mep["aabb"]

    validated = []
    approved = 0
    for item in plan:
        out = dict(item)
        if item.get("action") != "OffsetProposed":
            validated.append(out)
            continue
        mid = item["mep_id"]
        mep = mep_by_id[mid]
        trial = proposed[mid]
        kind = mep.get("kind", "MEP")
        bad = False
        for obs in obstacles:
            clr = clearance_for(kind, obs.get("role", "ARC"), rules) * clearance_scale
            if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                bad = True
                break
        if not bad:
            for oid, oabb in proposed.items():
                if oid == mid:
                    continue
                clr = clearance_for(kind, "MEP", rules) * clearance_scale
                if aabb_intersects(expand_aabb(trial, clr), oabb):
                    bad = True
                    break
        if bad:
            out["action"] = "ManualReview"
            out["reason"] = "OverlapGuardRejected"
            out["offset_mm"] = (0, 0, 0)
        else:
            out["action"] = "OffsetApproved"
            approved += 1
        validated.append(out)

    # --- apply ---
    apply_success = 0
    results = []
    if run_mode == "DetectAndFix":
        for item in validated:
            r = dict(item)
            if item.get("action") != "OffsetApproved":
                r["apply_status"] = "Skipped"
                results.append(r)
                continue
            try:
                el = doc.GetElement(make_element_id(item["mep_id"]))
                if el is None:
                    r["apply_status"] = "ElementNotFound"
                    results.append(r)
                    continue
                ok, msg = move_mep_curve_by_offset_mm(doc, el, item.get("offset_mm", (0, 0, 0)))
                r["apply_status"] = "Applied" if ok else "Failed"
                r["apply_message"] = msg
                if ok:
                    apply_success += 1
                    r["action"] = "OffsetApplied"
            except Exception as ex:
                r["apply_status"] = "Failed"
                r["apply_message"] = str(ex)
            results.append(r)
    else:
        for item in validated:
            r = dict(item)
            r["apply_status"] = "SkippedDetectOnly"
            results.append(r)

    # --- report ---
    actions = {}
    for r in results:
        a = r.get("action", "Unknown")
        actions[a] = actions.get(a, 0) + 1
    report = {
        "generated_at": datetime.utcnow().isoformat() + "Z",
        "player": True,
        "run_mode": run_mode,
        "project_root": project_root,
        "summary": {
            "obstacles": len(obstacles),
            "mep": len(mep_records),
            "clash_pairs": len(clashes),
            "plan_items": len(results),
            "approved": approved,
            "apply_success": apply_success,
            "actions": actions,
        },
        "clashes": clashes,
        "results": results,
    }
    out_dir = os.path.dirname(report_path)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    with open(report_path, "w") as f:
        json.dump(report, f, indent=2)
    csv_path = os.path.splitext(report_path)[0] + ".csv"
    with open(csv_path, "w") as f:
        w = csv.writer(f)
        w.writerow(["mep_id", "mep_kind", "system", "action", "apply_status", "offset_x", "offset_y", "offset_z", "reason"])
        for r in results:
            off = r.get("offset_mm") or (0, 0, 0)
            w.writerow([r.get("mep_id"), r.get("mep_kind"), r.get("system"), r.get("action"), r.get("apply_status", ""), off[0], off[1], off[2], r.get("reason", "")])

    summary = "mode={0}; obstacles={1}; mep={2}; clashes={3}; approved={4}; applied={5}; report={6}".format(
        run_mode, len(obstacles), len(mep_records), len(clashes), approved, apply_success, report_path
    )
    OUT = [summary, report_path, len(clashes), approved, apply_success, report]
