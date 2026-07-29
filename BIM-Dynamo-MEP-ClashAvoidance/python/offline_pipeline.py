# -*- coding: utf-8 -*-
"""
offline_pipeline.py — Run clash/offset/guard logic without Revit (unit / demo).

Usage:
  python offline_pipeline.py
  python offline_pipeline.py --config ../config/clearance_rules.json
"""
from __future__ import print_function
import argparse
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)

from lib.geometry_utils import aabb_from_minmax  # noqa: E402


def demo_records():
    """Synthetic ARC wall + STC beam + MEP duct/pipe with solvable clashes."""
    obstacles = [
        {
            "id": 1001,
            "category": "OST_Walls",
            "role": "ARC",
            "source": "ARC_Link",
            # short wall segment that a duct currently intersects
            "aabb": aabb_from_minmax((1900, 0, 0), (2100, 200, 3000)),
            "name": "Wall-Stub",
        },
        {
            "id": 2001,
            "category": "OST_StructuralFraming",
            "role": "STC",
            "source": "STC_Link",
            "aabb": aabb_from_minmax((0, 0, 2800), (6000, 400, 3100)),
            "name": "Beam-B1",
        },
    ]
    mep = [
        {
            "id": 9001,
            "category": "OST_DuctCurves",
            "category_name": "Ducts",
            "kind": "Duct",
            "role": "MEP",
            "source": "HOST",
            "system": "SA-01",
            "level": "L2",
            # clashes beam soffit; raising +Z should clear
            "aabb": aabb_from_minmax((0, 50, 2700), (5000, 300, 2950)),
            "movable": True,
            "name": "SA-Duct-01",
        },
        {
            "id": 9002,
            "category": "OST_PipeCurves",
            "category_name": "Pipes",
            "kind": "Pipe",
            "role": "MEP",
            "source": "HOST",
            "system": "CW-01",
            "level": "L2",
            # laterally separated; clashes wall stub — shift +Y should clear
            "aabb": aabb_from_minmax((0, 50, 2200), (4000, 120, 2280)),
            "movable": True,
            "name": "CW-Pipe-01",
        },
    ]
    return mep, obstacles


def load_rules(path):
    with open(path, "r") as f:
        return json.load(f)


def clash_detect(mep_records, obstacle_records, rules, include_mep=True):
    from lib.geometry_utils import expand_aabb, aabb_intersects, aabb_penetration_mm, clearance_for

    clashes = []
    for mep in mep_records:
        kind = mep.get("kind", "MEP")
        for obs in obstacle_records:
            clr = clearance_for(kind, obs.get("role", "ARC"), rules)
            expanded = expand_aabb(mep["aabb"], clr)
            if aabb_intersects(expanded, obs["aabb"]):
                clashes.append({
                    "mep_id": mep["id"],
                    "mep_kind": kind,
                    "mep_system": mep.get("system"),
                    "mep_movable": mep.get("movable", False),
                    "other_id": obs["id"],
                    "other_role": obs.get("role"),
                    "other_category": obs.get("category"),
                    "other_source": obs.get("source"),
                    "clearance_mm": clr,
                    "penetration_mm": aabb_penetration_mm(expanded, obs["aabb"]),
                    "clash_kind": "MEP_vs_{0}".format(obs.get("role")),
                    "mep_aabb": mep["aabb"],
                    "other_aabb": obs["aabb"],
                })
    if include_mep:
        for i in range(len(mep_records)):
            for j in range(i + 1, len(mep_records)):
                a, b = mep_records[i], mep_records[j]
                clr = clearance_for(a.get("kind", "MEP"), "MEP", rules)
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
    return clashes


def auto_offset(clash_pairs, mep_records, obstacle_records, rules, prefer="Z", max_offset=600, step=50):
    from collections import defaultdict
    from lib.geometry_utils import (
        expand_aabb, aabb_intersects, translate_aabb, offset_from_axis,
        score_candidate, clearance_for,
    )

    axes = rules.get("routing", {}).get("axis_try_order", ["Z", "Y", "X", "-Z", "-Y", "-X"])
    axes = [prefer] + [a for a in axes if a != prefer]
    weights = rules.get("routing", {}).get("score_weights", {})
    mep_by_id = {m["id"]: m for m in mep_records}
    # Working AABBs update greedily so later MEP sees earlier proposed moves
    working = {m["id"]: m["aabb"] for m in mep_records}
    by = defaultdict(list)
    for c in clash_pairs:
        by[c["mep_id"]].append(c)

    plan = []
    for mep_id, hits in by.items():
        mep = mep_by_id[mep_id]
        best = None
        dist = step
        while dist <= max_offset + 1e-6:
            for axis in axes:
                off = offset_from_axis(axis, dist)
                trial = translate_aabb(mep["aabb"], off)

                n = 0
                for obs in obstacle_records:
                    clr = clearance_for(mep.get("kind", "MEP"), obs.get("role", "ARC"), rules)
                    if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                        n += 1
                for oid, oabb in working.items():
                    if oid == mep_id:
                        continue
                    clr = clearance_for(mep.get("kind", "MEP"), "MEP", rules)
                    if aabb_intersects(expand_aabb(trial, clr), oabb):
                        n += 1

                sc = score_candidate(off, n, axis, prefer, weights)
                cand = {"offset_mm": off, "remaining": n, "score": sc, "axis": axis}
                if best is None or cand["score"] < best["score"]:
                    best = cand
                if n == 0:
                    break
            if best and best["remaining"] == 0:
                break
            dist += step

        item = {
            "mep_id": mep_id,
            "mep_kind": mep.get("kind"),
            "system": mep.get("system"),
            "clash_count": len(hits),
        }
        if best and best["remaining"] == 0:
            item.update({"action": "OffsetProposed", "offset_mm": best["offset_mm"], "axis": best["axis"]})
            working[mep_id] = translate_aabb(mep["aabb"], best["offset_mm"])
        else:
            item.update({
                "action": "ManualReview",
                "offset_mm": best["offset_mm"] if best else (0, 0, 0),
                "reason": "NoClearOffsetWithinMax",
            })
        plan.append(item)
    return plan


def overlap_guard(plan, mep_records, obstacle_records, rules):
    from lib.geometry_utils import expand_aabb, aabb_intersects, translate_aabb, clearance_for

    mep_by_id = {m["id"]: m for m in mep_records}
    proposed = {}
    for item in plan:
        mep = mep_by_id[item["mep_id"]]
        if item.get("action") == "OffsetProposed":
            proposed[item["mep_id"]] = translate_aabb(mep["aabb"], item["offset_mm"])
        else:
            proposed[item["mep_id"]] = mep["aabb"]

    out = []
    for item in plan:
        row = dict(item)
        if item.get("action") != "OffsetProposed":
            out.append(row)
            continue
        mid = item["mep_id"]
        mep = mep_by_id[mid]
        trial = proposed[mid]
        kind = mep.get("kind", "MEP")
        bad = False
        for obs in obstacle_records:
            clr = clearance_for(kind, obs.get("role", "ARC"), rules)
            if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                bad = True
                break
        if not bad:
            for oid, oabb in proposed.items():
                if oid == mid:
                    continue
                clr = clearance_for(kind, "MEP", rules)
                if aabb_intersects(expand_aabb(trial, clr), oabb):
                    bad = True
                    break
        if bad:
            row["action"] = "ManualReview"
            row["reason"] = "OverlapGuardRejected"
            row["offset_mm"] = (0, 0, 0)
        else:
            row["action"] = "OffsetApproved"
        out.append(row)
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--config",
        default=os.path.join(ROOT, "config", "clearance_rules.json"),
    )
    parser.add_argument(
        "--out",
        default=os.path.join(ROOT, "samples", "last_run_report.json"),
    )
    args = parser.parse_args()
    rules = load_rules(args.config)
    mep, obstacles = demo_records()
    clashes = clash_detect(mep, obstacles, rules, include_mep=True)
    plan = auto_offset(clashes, mep, obstacles, rules)
    validated = overlap_guard(plan, mep, obstacles, rules)

    report = {
        "mode": "offline_demo",
        "clash_count": len(clashes),
        "clashes": clashes,
        "plan": validated,
        "summary_actions": {},
    }
    for v in validated:
        a = v.get("action")
        report["summary_actions"][a] = report["summary_actions"].get(a, 0) + 1

    out_dir = os.path.dirname(args.out)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    with open(args.out, "w") as f:
        json.dump(report, f, indent=2)

    print("clashes:", len(clashes))
    print("actions:", report["summary_actions"])
    print("wrote:", args.out)
    return 0 if len(clashes) > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
