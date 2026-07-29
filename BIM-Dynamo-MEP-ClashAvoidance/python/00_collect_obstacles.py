# -*- coding: utf-8 -*-
"""
00_collect_obstacles.py
Dynamo Python Script node — collect ARC/STC obstacle AABBs from linked models.

IN[0] = ConfigPath (string)
IN[1] = ARC_Filter (string, keyword or role "ARC")
IN[2] = STC_Filter (string)
IN[3] = SolidMode (bool, reserved; currently AABB)

OUT = [obstacle_records, count, message]
"""
from __future__ import print_function
import sys
import os

# Allow importing sibling lib when pasted path is set in Dynamo
_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE:
    _LIB = os.path.join(os.path.dirname(_HERE), "python")
    if _LIB not in sys.path:
        sys.path.append(_LIB)
    if _HERE not in sys.path:
        sys.path.append(_HERE)

# --- Dynamo entry ---
try:
    config_path = IN[0]  # noqa: F821
    arc_filter = IN[1] if len(IN) > 1 else "ARC"  # noqa: F821
    stc_filter = IN[2] if len(IN) > 2 else "STC"  # noqa: F821
    solid_mode = bool(IN[3]) if len(IN) > 3 else False  # noqa: F821
except NameError:
    config_path = r"../config/clearance_rules.json"
    arc_filter, stc_filter, solid_mode = "ARC", "STC", False

from lib.revit_utils import get_doc, load_rules, iter_link_docs, collect_category_records

def run(config_path, arc_filter, stc_filter, solid_mode=False):
    rules = load_rules(config_path)
    doc = get_doc()
    obstacles = []

    # Collect by role from all links; filters refine matching
    for role, link, link_doc, xf in iter_link_docs(doc, rules, role_filter=None):
        name = link.Name
        # Determine effective role
        effective = role
        if effective not in ("ARC", "STC"):
            if arc_filter and arc_filter.upper() in name.upper():
                effective = "ARC"
            elif stc_filter and stc_filter.upper() in name.upper():
                effective = "STC"
            else:
                continue
        # Apply user filter
        if effective == "ARC" and arc_filter not in ("*", "", None):
            if arc_filter.upper() not in ("ARC",) and arc_filter.upper() not in name.upper():
                # still allow if role matched ARC via keywords
                if role != "ARC":
                    continue
        if effective == "STC" and stc_filter not in ("*", "", None):
            if stc_filter.upper() not in ("STC",) and stc_filter.upper() not in name.upper():
                if role != "STC":
                    continue

        cats = rules.get("obstacle_categories", {}).get(effective, [])
        obstacles.extend(
            collect_category_records(doc, link_doc, xf, cats, effective, name)
        )

    msg = "Collected {0} obstacles (SolidMode={1})".format(len(obstacles), solid_mode)
    return obstacles, len(obstacles), msg


OUT = run(config_path, arc_filter, stc_filter, solid_mode)
