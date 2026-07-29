# -*- coding: utf-8 -*-
"""
player_run_all.py — Dynamo Player entry (Revit 2023 / IronPython2).

Checks:
  - MEP vs ARC
  - MEP vs STC
  - MEP vs MEP (default ON)

IN[0] ProjectRoot
IN[1] RunMode            DetectOnly | DetectAndFix
IN[2] ARC_Filter
IN[3] STC_Filter
IN[4] ClearanceScale
IN[5] PreferOffsetAxis
IN[6] MaxOffsetMm
IN[7] OffsetStepMm
IN[8] IncludeMEPvsMEP     bool (default True) — check MEP overlaps too
IN[9] ReportPath          optional

OUT (all Player-safe primitives — avoids "Run completed with warnings"):
  [0] Summary (string)
  [1] ReportPath (string)
  [2] ClashCount total (int)
  [3] MepVsMepCount (int)
  [4] Warnings (string)
  [5] Status (string) OK | WARN | ERROR
"""
from __future__ import print_function
import os
import sys
import json
import csv
from collections import defaultdict
from datetime import datetime


def _s(v, default=""):
    if v is None:
        return default
    try:
        if hasattr(v, "ToString"):
            t = v.ToString()
            if t is None:
                return default
            return str(t)
    except Exception:
        pass
    try:
        return str(v)
    except Exception:
        return default


def _b(v, default=True):
    if v is None:
        return default
    if isinstance(v, bool):
        return v
    s = _s(v).strip().lower()
    if s in ("", "null", "none"):
        return default
    if s in ("false", "0", "no", "n", "off"):
        return False
    if s in ("true", "1", "yes", "y", "on"):
        return True
    return bool(v)


def _f(v, default=0.0):
    if v is None:
        return float(default)
    try:
        return float(v)
    except Exception:
        try:
            return float(_s(v))
        except Exception:
            return float(default)


def _out(summary, report_path, clash_count, mep_vs_mep, warnings, status):
    """Always return 6 primitives for Dynamo Player."""
    return [
        _s(summary),
        _s(report_path),
        int(clash_count or 0),
        int(mep_vs_mep or 0),
        _s(warnings) if warnings else "none",
        _s(status) or "OK",
    ]


def run(inputs):
    warnings = []
    try:
        project_root = _s(inputs[0] if len(inputs) > 0 else "").strip().strip('"')
        run_mode = _s(inputs[1] if len(inputs) > 1 else "", "DetectOnly").strip() or "DetectOnly"
        arc_filter = _s(inputs[2] if len(inputs) > 2 else "", "ARC").strip() or "ARC"
        stc_filter = _s(inputs[3] if len(inputs) > 3 else "", "STC").strip() or "STC"
        clearance_scale = _f(inputs[4] if len(inputs) > 4 else 1.0, 1.0)
        prefer_axis = _s(inputs[5] if len(inputs) > 5 else "", "Z").strip() or "Z"
        max_offset = _f(inputs[6] if len(inputs) > 6 else 600.0, 600.0)
        offset_step = _f(inputs[7] if len(inputs) > 7 else 50.0, 50.0)
        # Default TRUE: user asked to check MEP as well
        include_mep = _b(inputs[8] if len(inputs) > 8 else True, True)
        report_path = _s(inputs[9] if len(inputs) > 9 else "").strip()
    if report_path.lower() in ("(auto)", "auto", "null", "none"):
        report_path = ""
    except Exception as ex:
        return _out("Input parse failed: " + str(ex), "", 0, 0, str(ex), "ERROR")

    if not project_root:
        return _out("ProjectRoot is empty", "", 0, 0, "Set ProjectRoot to folder with python/ and config/", "ERROR")

    python_dir = os.path.join(project_root, "python")
    config_path = os.path.join(project_root, "config", "clearance_rules.json")
    if not report_path:
        report_path = os.path.join(project_root, "samples", "last_run_report.json")

    if not os.path.isdir(project_root):
        return _out("ProjectRoot not found: " + project_root, "", 0, 0, "Bad ProjectRoot", "ERROR")
    if not os.path.isdir(python_dir):
        return _out("python/ missing under ProjectRoot", "", 0, 0, python_dir, "ERROR")
    if not os.path.isfile(config_path):
        return _out("Config not found", "", 0, 0, config_path, "ERROR")

    if python_dir not in sys.path:
        sys.path.insert(0, python_dir)

    try:
        from lib.revit_utils import (
            get_doc, load_rules, iter_link_docs, collect_category_records,
            category_enum, element_aabb_mm, mep_system_name, element_id_int,
            move_mep_curve_by_offset_mm, make_element_id, link_role,
        )
        from lib.geometry_utils import (
            expand_aabb, aabb_intersects, aabb_penetration_mm, clearance_for,
            translate_aabb, offset_from_axis, score_candidate, classify_mep_family,
        )
        from Autodesk.Revit.DB import FilteredElementCollector, RevitLinkInstance
    except Exception as ex:
        return _out("Import failed (run inside Revit Dynamo)", "", 0, 0, str(ex), "ERROR")

    try:
        rules = load_rules(config_path)
        doc = get_doc()
    except Exception as ex:
        return _out("Cannot load Revit doc/config", "", 0, 0, str(ex), "ERROR")

    # Extra MEP categories for checking (not all movable)
    mep_cats = list(rules.get("mep_categories", []))
    for extra in rules.get("mep_check_categories", []):
        if extra not in mep_cats:
            mep_cats.append(extra)

    # --- obstacles ARC/STC from links ---
    obstacles = []
    link_names = []
    try:
        for role, link, link_doc, xf in iter_link_docs(doc, rules, role_filter=None):
            name = link.Name
            link_names.append(name)
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
    except Exception as ex:
        warnings.append("Obstacle collect warning: " + str(ex))

    if len(obstacles) == 0:
        warnings.append("No ARC/STC obstacles found. Check Link names / ARC_Filter / STC_Filter. Links=" + ",".join(link_names[:8]))

    # --- collect MEP from HOST ---
    mep_records = []
    movable = set(rules.get("mep_movable_categories", []))

    def _append_mep(el, source_name, transform=None):
        try:
            cat = el.Category.Name if el.Category else ""
            try:
                bic_name = str(el.Category.BuiltInCategory)
            except Exception:
                bic_name = cat
            aabb = element_aabb_mm(el, transform)
            if aabb is None:
                return
            kind = classify_mep_family(bic_name + " " + cat)
            mep_records.append({
                "id": element_id_int(el),
                "category": bic_name,
                "category_name": cat,
                "kind": kind,
                "role": "MEP",
                "source": source_name,
                "system": mep_system_name(el),
                "aabb": aabb,
                "movable": (source_name == "HOST") and (
                    any(m in bic_name for m in movable) or kind in ("Duct", "Pipe", "CableTray", "Conduit")
                ),
                "name": getattr(el, "Name", "") or "",
            })
        except Exception:
            return

    try:
        for cname in mep_cats:
            bic = category_enum(cname)
            if bic is None:
                warnings.append("Unknown category skipped: " + cname)
                continue
            for el in FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType():
                _append_mep(el, "HOST", None)
    except Exception as ex:
        warnings.append("Host MEP collect warning: " + str(ex))

    # --- also collect MEP from linked MEP models (check only, not movable) ---
    mep_keywords = rules.get("link_name_keywords", {}).get("MEP", ["MEP", "MECH", "ELEC", "PLUMB", "消防", "機電"])
    try:
        links = FilteredElementCollector(doc).OfClass(RevitLinkInstance).ToElements()
        for link in links:
            try:
                name = link.Name or ""
            except Exception:
                name = ""
            is_mep_link = False
            for k in mep_keywords:
                if k.upper() in name.upper():
                    is_mep_link = True
                    break
            # skip if already treated as ARC/STC
            role = link_role(name, rules)
            if role in ("ARC", "STC"):
                continue
            if not is_mep_link:
                continue
            link_doc = link.GetLinkDocument()
            if link_doc is None:
                warnings.append("MEP link unloaded: " + name)
                continue
            xf = link.GetTotalTransform()
            for cname in mep_cats:
                bic = category_enum(cname)
                if bic is None:
                    continue
                for el in FilteredElementCollector(link_doc).OfCategory(bic).WhereElementIsNotElementType():
                    _append_mep(el, "MEP_LINK:" + name, xf)
    except Exception as ex:
        warnings.append("Linked MEP collect warning: " + str(ex))

    if len(mep_records) == 0:
        warnings.append("No MEP elements found in host (and linked MEP). Open an MEP model or check categories.")

    # --- clash detect ---
    clashes = []
    count_arc = 0
    count_stc = 0
    count_mep = 0

    for mep in mep_records:
        # Only clash-report host MEP as primary (linked MEP used as "other")
        if not str(mep.get("source", "")).startswith("HOST"):
            continue
        kind = mep.get("kind", "MEP")
        for obs in obstacles:
            role = obs.get("role", "ARC")
            clr = clearance_for(kind, role, rules) * clearance_scale
            expanded = expand_aabb(mep["aabb"], clr)
            if aabb_intersects(expanded, obs["aabb"]):
                pen = aabb_penetration_mm(expanded, obs["aabb"])
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
                    "penetration_mm": pen,
                    "clash_kind": "MEP_vs_{0}".format(role),
                })
                if role == "ARC":
                    count_arc += 1
                elif role == "STC":
                    count_stc += 1

    if include_mep:
        host_mep = [m for m in mep_records if str(m.get("source", "")).startswith("HOST")]
        other_mep = mep_records  # host + linked
        # Host vs Host
        for i in range(len(host_mep)):
            for j in range(i + 1, len(host_mep)):
                a, b = host_mep[i], host_mep[j]
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
                    })
                    count_mep += 1
        # Host vs Linked MEP
        linked = [m for m in other_mep if not str(m.get("source", "")).startswith("HOST")]
        for a in host_mep:
            for b in linked:
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
                        "clash_kind": "MEP_vs_MEP_LINK",
                    })
                    count_mep += 1
    else:
        warnings.append("IncludeMEPvsMEP=false — MEP self-check skipped")

    # --- auto offset (only host movable, only DetectAndFix) ---
    routing = rules.get("routing", {})
    axes = list(routing.get("axis_try_order", ["Z", "Y", "X", "-Z", "-Y", "-X"]))
    prefer = prefer_axis or routing.get("prefer_offset_axis", "Z")
    axes = [prefer] + [a for a in axes if a != prefer]
    weights = routing.get("score_weights", {})
    mep_by_id = {m["id"]: m for m in mep_records if str(m.get("source", "")).startswith("HOST")}
    working = {mid: mep_by_id[mid]["aabb"] for mid in mep_by_id}
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
            "clash_kinds": list(set(h.get("clash_kind") for h in hits)),
        }
        if not mep.get("movable", False):
            item.update({"action": "ManualReview", "reason": "NotMovable", "offset_mm": [0, 0, 0]})
            plan.append(item)
            continue
        if run_mode != "DetectAndFix":
            item.update({"action": "None", "reason": "DetectOnly", "offset_mm": [0, 0, 0]})
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
                if include_mep:
                    for oid, oabb in working.items():
                        if oid == mep_id:
                            continue
                        clr = clearance_for(mep.get("kind", "MEP"), "MEP", rules) * clearance_scale
                        if aabb_intersects(expand_aabb(trial, clr), oabb):
                            rem += 1
                sc = score_candidate(off, rem, axis, prefer, weights)
                cand = {"offset_mm": list(off), "remaining": rem, "score": sc, "axis": axis}
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
                "offset_mm": best["offset_mm"] if best else [0, 0, 0],
            })
        plan.append(item)

    # --- overlap guard ---
    proposed = {}
    for item in plan:
        mep = mep_by_id[item["mep_id"]]
        if item.get("action") == "OffsetProposed":
            proposed[item["mep_id"]] = translate_aabb(mep["aabb"], item.get("offset_mm", [0, 0, 0]))
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
        if include_mep and not bad:
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
            out["offset_mm"] = [0, 0, 0]
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
                ok, msg = move_mep_curve_by_offset_mm(doc, el, item.get("offset_mm", [0, 0, 0]))
                r["apply_status"] = "Applied" if ok else "Failed"
                r["apply_message"] = msg
                if ok:
                    apply_success += 1
                    r["action"] = "OffsetApplied"
                else:
                    warnings.append("Move failed id={0}: {1}".format(item["mep_id"], msg))
            except Exception as ex:
                r["apply_status"] = "Failed"
                r["apply_message"] = str(ex)
                warnings.append("Apply error id={0}: {1}".format(item["mep_id"], str(ex)))
            results.append(r)
    else:
        for item in validated:
            r = dict(item)
            r["apply_status"] = "SkippedDetectOnly"
            results.append(r)

    # --- report (JSON may contain lists; Player OUT stays primitive) ---
    actions = {}
    for r in results:
        a = r.get("action", "Unknown")
        actions[a] = actions.get(a, 0) + 1

    report = {
        "generated_at": datetime.utcnow().isoformat() + "Z",
        "player": True,
        "run_mode": run_mode,
        "include_mep_vs_mep": include_mep,
        "project_root": project_root,
        "summary": {
            "obstacles": len(obstacles),
            "mep_total": len(mep_records),
            "mep_host": len([m for m in mep_records if str(m.get("source", "")).startswith("HOST")]),
            "clash_total": len(clashes),
            "clash_vs_arc": count_arc,
            "clash_vs_stc": count_stc,
            "clash_vs_mep": count_mep,
            "approved": approved,
            "apply_success": apply_success,
            "actions": actions,
        },
        "warnings": warnings,
        "clashes": clashes,
        "results": results,
    }

    try:
        out_dir = os.path.dirname(report_path)
        if out_dir and not os.path.isdir(out_dir):
            os.makedirs(out_dir)
        with open(report_path, "w") as f:
            json.dump(report, f, indent=2)
        csv_path = os.path.splitext(report_path)[0] + ".csv"
        with open(csv_path, "w") as f:
            w = csv.writer(f)
            w.writerow(["mep_id", "mep_kind", "system", "clash_kind_hint", "action", "apply_status", "offset_x", "offset_y", "offset_z", "reason"])
            for r in results:
                off = r.get("offset_mm") or [0, 0, 0]
                kinds = ",".join(r.get("clash_kinds") or [])
                w.writerow([r.get("mep_id"), r.get("mep_kind"), r.get("system"), kinds, r.get("action"), r.get("apply_status", ""), off[0], off[1], off[2], r.get("reason", "")])
    except Exception as ex:
        warnings.append("Report write failed: " + str(ex))

    summary = (
        "mode={0}; MEP={1}; Obst={2}; ClashTotal={3} (ARC={4}, STC={5}, MEP={6}); "
        "approved={7}; applied={8}; IncludeMEPvsMEP={9}"
    ).format(
        run_mode,
        len(mep_records),
        len(obstacles),
        len(clashes),
        count_arc,
        count_stc,
        count_mep,
        approved,
        apply_success,
        include_mep,
    )

    status = "OK"
    if warnings:
        status = "WARN"
    if len(mep_records) == 0 and len(obstacles) == 0:
        status = "ERROR"

    warn_text = " | ".join(warnings) if warnings else "none"
    return _out(summary, report_path, len(clashes), count_mep, warn_text, status)


# --- Dynamo entry ---
try:
    _inputs = list(IN)  # noqa: F821
except NameError:
    _inputs = []

try:
    OUT = run(_inputs)
except Exception as _ex:
    OUT = _out("Unhandled error: " + str(_ex), "", 0, 0, str(_ex), "ERROR")
