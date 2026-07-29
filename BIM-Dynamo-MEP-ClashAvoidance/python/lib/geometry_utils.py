# -*- coding: utf-8 -*-
"""
geometry_utils.py — Bounding box, clearance expansion, overlap tests.
Usable inside Dynamo IronPython/CPython or standalone unit tests.
Units: millimeters unless noted. Revit internal units are feet; convert at boundary.
"""
from __future__ import print_function
import math

MM_PER_FOOT = 304.8


def feet_to_mm(v):
    return float(v) * MM_PER_FOOT


def mm_to_feet(v):
    return float(v) / MM_PER_FOOT


def aabb_from_minmax(min_pt, max_pt):
    """min_pt/max_pt: (x,y,z) in mm."""
    return {
        "min": (float(min_pt[0]), float(min_pt[1]), float(min_pt[2])),
        "max": (float(max_pt[0]), float(max_pt[1]), float(max_pt[2])),
    }


def expand_aabb(aabb, clearance_mm):
    c = float(clearance_mm)
    mn, mx = aabb["min"], aabb["max"]
    return {
        "min": (mn[0] - c, mn[1] - c, mn[2] - c),
        "max": (mx[0] + c, mx[1] + c, mx[2] + c),
    }


def aabb_intersects(a, b):
    for i in range(3):
        if a["max"][i] < b["min"][i] or b["max"][i] < a["min"][i]:
            return False
    return True


def aabb_penetration_mm(a, b):
    """Positive overlap depth per axis (mm). 0 if no overlap on that axis."""
    depths = []
    for i in range(3):
        overlap = min(a["max"][i], b["max"][i]) - max(a["min"][i], b["min"][i])
        depths.append(max(0.0, overlap))
    if any(d <= 0 for d in depths):
        return 0.0
    return min(depths)


def aabb_center(aabb):
    mn, mx = aabb["min"], aabb["max"]
    return ((mn[0] + mx[0]) * 0.5, (mn[1] + mx[1]) * 0.5, (mn[2] + mx[2]) * 0.5)


def aabb_size(aabb):
    mn, mx = aabb["min"], aabb["max"]
    return (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2])


def translate_aabb(aabb, offset_mm):
    ox, oy, oz = offset_mm
    mn, mx = aabb["min"], aabb["max"]
    return {
        "min": (mn[0] + ox, mn[1] + oy, mn[2] + oz),
        "max": (mx[0] + ox, mx[1] + oy, mx[2] + oz),
    }


AXIS_VECTORS = {
    "X": (1.0, 0.0, 0.0),
    "Y": (0.0, 1.0, 0.0),
    "Z": (0.0, 0.0, 1.0),
    "-X": (-1.0, 0.0, 0.0),
    "-Y": (0.0, -1.0, 0.0),
    "-Z": (0.0, 0.0, -1.0),
}


def offset_from_axis(axis_name, distance_mm):
    vx, vy, vz = AXIS_VECTORS[axis_name]
    d = float(distance_mm)
    return (vx * d, vy * d, vz * d)


def score_candidate(offset_mm, remaining_clashes, axis_name, prefer_axis, weights):
    length = math.sqrt(sum(c * c for c in offset_mm))
    axis_penalty = 0.0 if axis_name.replace("-", "") == prefer_axis.replace("-", "") else 1.0
    return (
        weights.get("offset_length", 1.0) * length
        + weights.get("remaining_clash", 1000.0) * remaining_clashes
        + weights.get("axis_preference", 0.25) * axis_penalty * 100.0
    )


def classify_mep_family(category_name):
    n = (category_name or "").lower()
    if "duct" in n:
        return "Duct"
    if "pipe" in n:
        return "Pipe"
    if "cable" in n or "tray" in n:
        return "CableTray"
    if "conduit" in n:
        return "Conduit"
    if "equipment" in n or "fixture" in n:
        return "Equipment"
    return "MEP"


def clearance_for(mep_kind, other_role, rules):
    """other_role: ARC | STC | MEP"""
    table = rules.get("clearance_mm", {})
    key = "{0}_vs_{1}".format(mep_kind, other_role)
    if key in table:
        return float(table[key])
    if other_role == "MEP":
        return float(table.get("MEP_vs_MEP", table.get("default", 50)))
    if mep_kind == "Equipment":
        return float(table.get("Equipment_vs_Any", table.get("default", 50)))
    return float(table.get("default", 50))
