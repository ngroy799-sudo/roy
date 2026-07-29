# -*- coding: utf-8 -*-
"""
03_auto_offset_route.py
Propose axis-aligned offsets for movable MEP curves to clear clashes.

IN[0] = clash_pairs
IN[1] = mep_records
IN[2] = obstacle_records
IN[3] = ConfigPath
IN[4] = RunMode ("DetectOnly" | "DetectAndFix")
IN[5] = PreferOffsetAxis ("Z"|"Y"|"X")
IN[6] = MaxOffsetMm
IN[7] = OffsetStepMm

OUT = [fix_plan, count_fixable, message]
"""
from __future__ import print_function
import sys
import os
from collections import defaultdict

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE and _HERE not in sys.path:
    sys.path.append(_HERE)

try:
    clash_pairs = IN[0]  # noqa: F821
    mep_records = IN[1]  # noqa: F821
    obstacle_records = IN[2]  # noqa: F821
    config_path = IN[3]  # noqa: F821
    run_mode = IN[4] if len(IN) > 4 else "DetectOnly"  # noqa: F821
    prefer_axis = IN[5] if len(IN) > 5 else "Z"  # noqa: F821
    max_offset = float(IN[6]) if len(IN) > 6 else 600.0  # noqa: F821
    step = float(IN[7]) if len(IN) > 7 else 50.0  # noqa: F821
except NameError:
    clash_pairs, mep_records, obstacle_records = [], [], []
    config_path = r"../config/clearance_rules.json"
    run_mode, prefer_axis, max_offset, step = "DetectOnly", "Z", 600.0, 50.0

from lib.revit_utils import load_rules
from lib.geometry_utils import (
    expand_aabb,
    aabb_intersects,
    translate_aabb,
    offset_from_axis,
    score_candidate,
    clearance_for,
)


def run(clash_pairs, mep_records, obstacle_records, config_path,
        run_mode="DetectOnly", prefer_axis="Z", max_offset=600.0, step=50.0):
    rules = load_rules(config_path)
    routing = rules.get("routing", {})
    axes = routing.get("axis_try_order", ["Z", "Y", "X", "-Z", "-Y", "-X"])
    # put preferred first
    prefer = prefer_axis or routing.get("prefer_offset_axis", "Z")
    axes = [prefer] + [a for a in axes if a != prefer and a != "-" + prefer]
    # ensure negative prefer also near front
    neg = "-" + prefer if not prefer.startswith("-") else prefer[1:]
    if neg in routing.get("axis_try_order", []):
        axes = [prefer, neg] + [a for a in axes if a not in (prefer, neg)]

    max_offset = float(max_offset or routing.get("max_offset_mm", 600))
    step = float(step or routing.get("offset_step_mm", 50))
    weights = routing.get("score_weights", {})

    mep_by_id = {m["id"]: m for m in mep_records}
    # Greedy: later MEP sees earlier proposed positions (reduces MEP-MEP re-clash)
    working_aabbs = {m["id"]: m["aabb"] for m in mep_records}
    clashes_by_mep = defaultdict(list)
    for c in clash_pairs:
        clashes_by_mep[c["mep_id"]].append(c)

    plan = []
    for mep_id, hits in clashes_by_mep.items():
        mep = mep_by_id.get(mep_id)
        if mep is None:
            continue
        base = {
            "mep_id": mep_id,
            "mep_kind": mep.get("kind"),
            "system": mep.get("system"),
            "clash_count": len(hits),
            "clash_with": [
                {"id": h["other_id"], "role": h["other_role"], "kind": h["clash_kind"]}
                for h in hits
            ],
        }
        if not mep.get("movable", False):
            base.update({"action": "ManualReview", "reason": "NotMovable", "offset_mm": (0, 0, 0)})
            plan.append(base)
            continue

        if str(run_mode) == "DetectOnly":
            base.update({"action": "None", "reason": "DetectOnly", "offset_mm": (0, 0, 0)})
            plan.append(base)
            continue

        best = None
        dist = step
        while dist <= max_offset + 1e-6:
            for axis in axes:
                off = offset_from_axis(axis, dist)
                trial = translate_aabb(mep["aabb"], off)
                rem = 0
                for obs in obstacle_records:
                    clr = clearance_for(mep.get("kind", "MEP"), obs.get("role", "ARC"), rules)
                    if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                        rem += 1
                for oid, oabb in working_aabbs.items():
                    if oid == mep_id:
                        continue
                    clr = clearance_for(mep.get("kind", "MEP"), "MEP", rules)
                    if aabb_intersects(expand_aabb(trial, clr), oabb):
                        rem += 1
                sc = score_candidate(off, rem, axis, prefer, weights)
                cand = {"offset_mm": off, "remaining": rem, "score": sc, "axis": axis, "distance": dist}
                if best is None or cand["score"] < best["score"]:
                    best = cand
                if rem == 0:
                    break
            if best and best["remaining"] == 0:
                break
            dist += step

        if best and best["remaining"] == 0:
            base.update({
                "action": "OffsetProposed",
                "reason": "Cleared",
                "offset_mm": best["offset_mm"],
                "axis": best["axis"],
                "score": best["score"],
            })
            working_aabbs[mep_id] = translate_aabb(mep["aabb"], best["offset_mm"])
        else:
            base.update({
                "action": "ManualReview",
                "reason": "NoClearOffsetWithinMax",
                "offset_mm": best["offset_mm"] if best else (0, 0, 0),
                "best_remaining": best["remaining"] if best else len(hits),
            })
        plan.append(base)

    fixable = sum(1 for p in plan if p.get("action") == "OffsetProposed")
    msg = "Plan items={0}, fixable={1}, mode={2}".format(len(plan), fixable, run_mode)
    return plan, fixable, msg


OUT = run(clash_pairs, mep_records, obstacle_records, config_path,
          run_mode, prefer_axis, max_offset, step)
