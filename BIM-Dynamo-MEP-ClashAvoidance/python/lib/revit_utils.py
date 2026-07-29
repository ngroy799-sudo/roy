# -*- coding: utf-8 -*-
"""
revit_utils.py — Revit API helpers for Dynamo Python nodes.
Designed for Dynamo for Revit (IronPython 2.7 or CPython3 engine).
"""
from __future__ import print_function
import json
import os

try:
    import clr
    clr.AddReference("RevitAPI")
    clr.AddReference("RevitServices")
    from Autodesk.Revit.DB import (
        FilteredElementCollector,
        BuiltInCategory,
        ElementId,
        XYZ,
        BoundingBoxXYZ,
        RevitLinkInstance,
        Options,
        Transaction,
        LocationCurve,
        ViewDetailLevel,
    )
    from RevitServices.Persistence import DocumentManager
    from RevitServices.Transactions import TransactionManager
    REVIT_AVAILABLE = True
except Exception:
    REVIT_AVAILABLE = False
    DocumentManager = None


def get_doc():
    if not REVIT_AVAILABLE:
        raise RuntimeError("Revit API not available. Run inside Dynamo for Revit.")
    return DocumentManager.Instance.CurrentDBDocument


def load_rules(config_path):
    path = config_path
    if hasattr(config_path, "ToString"):
        path = config_path.ToString()
    with open(path, "r") as f:
        return json.load(f)


def category_enum(name):
    """Map OST_* string to BuiltInCategory."""
    if not REVIT_AVAILABLE:
        return None
    return getattr(BuiltInCategory, name)


def link_role(link_name, rules):
    name = (link_name or "").upper()
    for role, keys in rules.get("link_name_keywords", {}).items():
        for k in keys:
            if k.upper() in name:
                return role
    return None


def iter_link_docs(doc, rules, role_filter=None):
    """Yield (role, link_instance, link_doc, transform)."""
    links = FilteredElementCollector(doc).OfClass(RevitLinkInstance).ToElements()
    for link in links:
        try:
            name = link.Name
        except Exception:
            name = ""
        role = link_role(name, rules)
        if role_filter and role != role_filter:
            # also allow keyword direct filter string
            if role_filter != "*" and role_filter.upper() not in (name or "").upper():
                if role is None:
                    continue
                if role != role_filter:
                    continue
        link_doc = link.GetLinkDocument()
        if link_doc is None:
            continue
        yield role or "LINK", link, link_doc, link.GetTotalTransform()


def element_aabb_mm(element, transform=None):
    """Return AABB dict in mm, or None."""
    from geometry_utils import feet_to_mm, aabb_from_minmax

    bb = element.get_BoundingBox(None)
    if bb is None:
        return None

    def xf(pt):
        if transform is not None:
            pt = transform.OfPoint(pt)
        return (feet_to_mm(pt.X), feet_to_mm(pt.Y), feet_to_mm(pt.Z))

    mn = xf(bb.Min)
    mx = xf(bb.Max)
    # ensure min < max
    mins = (min(mn[0], mx[0]), min(mn[1], mx[1]), min(mn[2], mx[2]))
    maxs = (max(mn[0], mx[0]), max(mn[1], mx[1]), max(mn[2], mx[2]))
    return aabb_from_minmax(mins, maxs)


def collect_category_records(host_doc, link_doc, transform, category_names, role, source_name):
    records = []
    for cname in category_names:
        try:
            bic = category_enum(cname)
            if bic is None:
                continue
            collector = FilteredElementCollector(link_doc).OfCategory(bic).WhereElementIsNotElementType()
            for el in collector:
                aabb = element_aabb_mm(el, transform)
                if aabb is None:
                    continue
                records.append({
                    "id": int(el.Id.IntegerValue) if hasattr(el.Id, "IntegerValue") else int(el.Id.Value),
                    "category": cname,
                    "role": role,
                    "source": source_name,
                    "aabb": aabb,
                    "name": getattr(el, "Name", "") or "",
                })
        except Exception:
            continue
    return records


def mep_system_name(element):
    try:
        # Duct/Pipe system
        if hasattr(element, "MEPSystem") and element.MEPSystem is not None:
            return element.MEPSystem.Name
    except Exception:
        pass
    try:
        p = element.LookupParameter("System Name")
        if p and p.HasValue:
            return p.AsString()
    except Exception:
        pass
    return ""


def move_mep_curve_by_offset_mm(doc, element, offset_mm):
    """Translate LocationCurve of MEPCurve by offset (mm). Returns True on success."""
    from geometry_utils import mm_to_feet

    loc = element.Location
    if not isinstance(loc, LocationCurve):
        return False, "No LocationCurve"
    ox, oy, oz = offset_mm
    vec = XYZ(mm_to_feet(ox), mm_to_feet(oy), mm_to_feet(oz))
    try:
        TransactionManager.Instance.EnsureInTransaction(doc)
        ok = loc.Move(vec)
        TransactionManager.Instance.TransactionTaskDone()
        return bool(ok), "Moved" if ok else "Move returned False"
    except Exception as ex:
        try:
            TransactionManager.Instance.ForceCloseTransaction()
        except Exception:
            pass
        return False, str(ex)


def unwrap(item):
    """Unwrap Dynamo Revit.Elements wrappers if present."""
    try:
        import Revit
        if hasattr(item, "InternalElement"):
            return item.InternalElement
    except Exception:
        pass
    if hasattr(item, "InternalElement"):
        return item.InternalElement
    return item
