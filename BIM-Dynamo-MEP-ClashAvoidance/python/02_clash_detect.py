# -*- coding: utf-8 -*-
"""
02_clash_detect.py
Detect MEP vs ARC/STC (and optional MEP vs MEP) using expanded AABB.

IN[0] = mep_records
IN[1] = obstacle_records
IN[2] = ConfigPath
IN[3] = ClearanceScale (float)
IN[4] = IncludeMEPvsMEP (bool)

OUT = [clash_pairs, count, message]
"""
from __future__ import print_function
import sys
import os

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE and _HERE not in sys.path:
    sys.path.append(_HERE)

try:
    mep_records = IN[0]  # noqa: F821
    obstacle_records = IN[1]  # noqa: F821
    config_path = IN[2]  # noqa: F821
    clearance_scale = float(IN[3]) if len(IN) > 3 else 1.0  # noqa: F821
    include_mep = bool(IN[4]) if len(IN) > 4 else True  # noqa: F821
except NameError:
    mep_records, obstacle_records, config_path = [], [], r"../config/clearance_rules.json"
    clearance_scale, include_mep = 1.0, True

from lib.revit_utils import load_rules
from lib.geometry_utils import expand_aabb, aabb_intersects, aabb_penetration_mm, clearance_for


def _pair(a, b, clearance, penetration, kind):
    return {
        "mep_id": a["id"],
        "mep_kind": a.get("kind"),
        "mep_system": a.get("system"),
        "mep_movable": a.get("movable", False),
        "other_id": b["id"],
        "other_role": b.get("role"),
        "other_category": b.get("category"),
        "other_source": b.get("source"),
        "clearance_mm": clearance,
        "penetration_mm": penetration,
        "clash_kind": kind,
        "mep_aabb": a["aabb"],
        "other_aabb": b["aabb"],
    }


def run(mep_records, obstacle_records, config_path, clearance_scale=1.0, include_mep=True):
    rules = load_rules(config_path)
    clashes = []

    for mep in mep_records:
        kind = mep.get("kind", "MEP")
        for obs in obstacle_records:
            role = obs.get("role", "ARC")
            clr = clearance_for(kind, role, rules) * clearance_scale
            expanded = expand_aabb(mep["aabb"], clr)
            if aabb_intersects(expanded, obs["aabb"]):
                pen = aabb_penetration_mm(expanded, obs["aabb"])
                clashes.append(_pair(mep, obs, clr, pen, "MEP_vs_{0}".format(role)))

    if include_mep and rules.get("overlap_guard", {}).get("include_mep_vs_mep", True):
        n = len(mep_records)
        for i in range(n):
            for j in range(i + 1, n):
                a, b = mep_records[i], mep_records[j]
                clr = clearance_for(a.get("kind", "MEP"), "MEP", rules) * clearance_scale
                expanded = expand_aabb(a["aabb"], clr)
                if aabb_intersects(expanded, b["aabb"]):
                    pen = aabb_penetration_mm(expanded, b["aabb"])
                    clashes.append(_pair(a, b, clr, pen, "MEP_vs_MEP"))

    msg = "Found {0} clash pairs".format(len(clashes))
    return clashes, len(clashes), msg


OUT = run(mep_records, obstacle_records, config_path, clearance_scale, include_mep)
