# -*- coding: utf-8 -*-
"""
04_overlap_guard.py
Validate proposed offsets so fixing one MEP does not create new overlaps.

IN[0] = fix_plan
IN[1] = mep_records
IN[2] = obstacle_records
IN[3] = ConfigPath

OUT = [validated_plan, accepted_count, message]
"""
from __future__ import print_function
import sys
import os

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE and _HERE not in sys.path:
    sys.path.append(_HERE)

try:
    fix_plan = IN[0]  # noqa: F821
    mep_records = IN[1]  # noqa: F821
    obstacle_records = IN[2]  # noqa: F821
    config_path = IN[3]  # noqa: F821
except NameError:
    fix_plan, mep_records, obstacle_records = [], [], []
    config_path = r"../config/clearance_rules.json"

from lib.revit_utils import load_rules
from lib.geometry_utils import expand_aabb, aabb_intersects, translate_aabb, clearance_for


def run(fix_plan, mep_records, obstacle_records, config_path):
    rules = load_rules(config_path)
    guard = rules.get("overlap_guard", {})
    reject = guard.get("reject_fix_if_new_clash", True)
    include_mep = guard.get("include_mep_vs_mep", True)

    mep_by_id = {m["id"]: dict(m) for m in mep_records}
    # Apply proposed offsets into a working copy
    proposed_aabbs = {}
    for item in fix_plan:
        mid = item["mep_id"]
        mep = mep_by_id.get(mid)
        if mep is None:
            continue
        if item.get("action") == "OffsetProposed":
            proposed_aabbs[mid] = translate_aabb(mep["aabb"], item.get("offset_mm", (0, 0, 0)))
        else:
            proposed_aabbs[mid] = mep["aabb"]

    validated = []
    accepted = 0
    for item in fix_plan:
        out = dict(item)
        if item.get("action") != "OffsetProposed":
            validated.append(out)
            continue

        mid = item["mep_id"]
        mep = mep_by_id[mid]
        trial = proposed_aabbs[mid]
        kind = mep.get("kind", "MEP")
        new_hits = []

        for obs in obstacle_records:
            clr = clearance_for(kind, obs.get("role", "ARC"), rules)
            if aabb_intersects(expand_aabb(trial, clr), obs["aabb"]):
                new_hits.append({"id": obs["id"], "role": obs["role"]})

        if include_mep:
            for other_id, other_aabb in proposed_aabbs.items():
                if other_id == mid:
                    continue
                other = mep_by_id.get(other_id)
                if other is None:
                    continue
                clr = clearance_for(kind, "MEP", rules)
                if aabb_intersects(expand_aabb(trial, clr), other_aabb):
                    new_hits.append({"id": other_id, "role": "MEP"})

        if new_hits and reject:
            out["action"] = "ManualReview"
            out["reason"] = "OverlapGuardRejected"
            out["residual_hits"] = new_hits
            out["offset_mm"] = (0, 0, 0)
        else:
            out["action"] = "OffsetApproved"
            out["residual_hits"] = new_hits
            accepted += 1
        validated.append(out)

    # Optional ARC vs STC warning (does not modify geometry)
    arc_stc_warnings = []
    if guard.get("include_arc_vs_stc_warning", True):
        arcs = [o for o in obstacle_records if o.get("role") == "ARC"]
        stcs = [o for o in obstacle_records if o.get("role") == "STC"]
        # Sample cap for performance
        max_pairs = 5000
        count = 0
        for a in arcs:
            for s in stcs:
                if aabb_intersects(a["aabb"], s["aabb"]):
                    arc_stc_warnings.append({"arc_id": a["id"], "stc_id": s["id"]})
                count += 1
                if count >= max_pairs:
                    break
            if count >= max_pairs:
                break

    msg = "Approved={0}/{1}; ARC-STC warnings={2}".format(
        accepted, len(fix_plan), len(arc_stc_warnings)
    )
    return validated, accepted, msg, arc_stc_warnings


result = run(fix_plan, mep_records, obstacle_records, config_path)
OUT = result
