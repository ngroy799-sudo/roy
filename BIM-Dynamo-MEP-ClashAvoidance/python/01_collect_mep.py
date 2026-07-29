# -*- coding: utf-8 -*-
"""
01_collect_mep.py
Dynamo Python Script — collect MEP elements from the host document.

IN[0] = ConfigPath (string)
IN[1] = Optional Element list (Dynamo selection). Empty = all MEP categories in rules.
IN[2] = LevelNameFilter (string, optional; "" = all)

OUT = [mep_records, count, message]
"""
from __future__ import print_function
import sys
import os

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE:
    if _HERE not in sys.path:
        sys.path.append(_HERE)

try:
    config_path = IN[0]  # noqa: F821
    selection = IN[1] if len(IN) > 1 else None  # noqa: F821
    level_filter = IN[2] if len(IN) > 2 else ""  # noqa: F821
except NameError:
    config_path = r"../config/clearance_rules.json"
    selection, level_filter = None, ""

from Autodesk.Revit.DB import FilteredElementCollector, BuiltInCategory
from lib.revit_utils import get_doc, load_rules, element_aabb_mm, mep_system_name, unwrap, category_enum, element_id_int
from lib.geometry_utils import classify_mep_family


def _level_name(el):
    try:
        p = el.LookupParameter("Reference Level") or el.LookupParameter("Level")
        if p and p.HasValue:
            return p.AsValueString() or ""
    except Exception:
        pass
    try:
        if hasattr(el, "ReferenceLevel") and el.ReferenceLevel:
            return el.ReferenceLevel.Name
    except Exception:
        pass
    return ""


def run(config_path, selection=None, level_filter=""):
    rules = load_rules(config_path)
    doc = get_doc()
    records = []

    elements = []
    if selection:
        raw = selection if isinstance(selection, list) else [selection]
        for item in raw:
            if item is None:
                continue
            elements.append(unwrap(item))
    else:
        for cname in rules.get("mep_categories", []):
            bic = category_enum(cname)
            if bic is None:
                continue
            col = FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType()
            elements.extend(list(col))

    movable = set(rules.get("mep_movable_categories", []))

    for el in elements:
        if el is None:
            continue
        try:
            cat = el.Category.Name if el.Category else ""
            # Prefer OST name when available via BuiltInCategory
            try:
                bic = el.Category.BuiltInCategory
                cname = str(bic)
            except Exception:
                cname = cat
            aabb = element_aabb_mm(el, None)
            if aabb is None:
                continue
            lvl = _level_name(el)
            if level_filter and level_filter not in lvl:
                continue
            eid = element_id_int(el)
            kind = classify_mep_family(cname + " " + cat)
            records.append({
                "id": eid,
                "category": cname,
                "category_name": cat,
                "kind": kind,
                "role": "MEP",
                "source": "HOST",
                "system": mep_system_name(el),
                "level": lvl,
                "aabb": aabb,
                "movable": any(m in cname for m in movable) or kind in ("Duct", "Pipe", "CableTray", "Conduit"),
                "name": getattr(el, "Name", "") or "",
            })
        except Exception:
            continue

    msg = "Collected {0} MEP elements".format(len(records))
    return records, len(records), msg


OUT = run(config_path, selection, level_filter)
