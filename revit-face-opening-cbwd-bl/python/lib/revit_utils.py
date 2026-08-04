# -*- coding: utf-8 -*-
"""
revit_utils.py — Revit API helpers for Dynamo Python nodes.
Target: Autodesk Revit 2023 + Dynamo IronPython 2.7 (CPython3 also OK).

Keywords: Revit, Generic Model, Face Opening, CBWD B.L., BoundingBox, Transaction
"""
from __future__ import print_function
import json

try:
    import clr
    clr.AddReference("RevitAPI")
    clr.AddReference("RevitServices")
    from Autodesk.Revit.DB import (
        FilteredElementCollector,
        BuiltInCategory,
        ElementId,
        Transaction,
    )
    from RevitServices.Persistence import DocumentManager
    REVIT_AVAILABLE = True
except Exception:
    REVIT_AVAILABLE = False
    DocumentManager = None
    FilteredElementCollector = None
    BuiltInCategory = None
    ElementId = None
    Transaction = None


def element_id_int(element_or_id):
    """Revit 2023-safe ElementId → int (IntegerValue)."""
    eid = element_or_id
    if hasattr(element_or_id, "Id") and not hasattr(element_or_id, "IntegerValue"):
        eid = element_or_id.Id
    if hasattr(eid, "IntegerValue"):
        return int(eid.IntegerValue)
    if hasattr(eid, "Value"):
        return int(eid.Value)
    return int(eid)


def get_doc():
    if not REVIT_AVAILABLE:
        raise RuntimeError("Revit API not available. Run inside Dynamo for Revit.")
    return DocumentManager.Instance.CurrentDBDocument


def get_uidoc():
    if not REVIT_AVAILABLE:
        raise RuntimeError("Revit API not available. Run inside Dynamo for Revit.")
    return DocumentManager.Instance.CurrentUIDocument


def load_rules(config_path):
    path = config_path
    if hasattr(config_path, "ToString"):
        path = config_path.ToString()
    with open(path, "r") as f:
        return json.load(f)


def family_and_type_names(element):
    """Return (family_name, type_name) best-effort for FamilyInstance / others."""
    family_name = ""
    type_name = ""
    try:
        if hasattr(element, "Symbol") and element.Symbol is not None:
            sym = element.Symbol
            try:
                type_name = sym.Name or ""
            except Exception:
                type_name = ""
            try:
                if hasattr(sym, "Family") and sym.Family is not None:
                    family_name = sym.Family.Name or ""
            except Exception:
                family_name = ""
    except Exception:
        pass
    if not type_name:
        try:
            type_name = element.Name or ""
        except Exception:
            type_name = ""
    return family_name, type_name


def is_face_opening_element(element, keyword):
    """True if family or type name contains keyword (case-insensitive)."""
    if element is None:
        return False
    key = (keyword or "").strip().lower()
    if not key:
        return True
    family_name, type_name = family_and_type_names(element)
    blob = ((family_name or "") + " " + (type_name or "")).lower()
    return key in blob


def collect_generic_models(doc):
    """All Generic Model elements in the current document (not types)."""
    if not REVIT_AVAILABLE:
        raise RuntimeError("Revit API not available.")
    return list(
        FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_GenericModel)
        .WhereElementIsNotElementType()
        .ToElements()
    )


def collect_selected_elements(doc, uidoc):
    """Elements currently selected in the UI."""
    ids = uidoc.Selection.GetElementIds()
    elements = []
    for eid in ids:
        el = doc.GetElement(eid)
        if el is not None:
            elements.append(el)
    return elements


def bottom_elevation_internal(element):
    """
    Bottom face elevation as BoundingBox.Min.Z in Revit internal units (feet).
    Returns None if bounding box is unavailable.
    """
    bb = element.get_BoundingBox(None)
    if bb is None:
        return None
    try:
        return float(bb.Min.Z)
    except Exception:
        return None


def set_length_parameter(element, parameter_name, value_internal):
    """
    Lookup instance parameter by name and set length value (internal units).
    Returns (ok, reason).
    """
    param = element.LookupParameter(parameter_name)
    if param is None:
        return False, "parameter_not_found"
    if param.IsReadOnly:
        return False, "parameter_read_only"
    try:
        # StorageType.Double == 2 in Revit API
        storage = int(param.StorageType) if hasattr(param.StorageType, "__int__") else param.StorageType
        storage_name = str(param.StorageType)
        if "Double" not in storage_name and storage != 2:
            return False, "parameter_not_double:" + storage_name
        ok = param.Set(float(value_internal))
        if not ok:
            return False, "parameter_set_failed"
        return True, "ok"
    except Exception as ex:
        return False, "parameter_set_error:" + str(ex)
