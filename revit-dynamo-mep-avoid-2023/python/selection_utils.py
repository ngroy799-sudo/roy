# selection_utils.py — Filter and validate Dynamo selection inputs
# For Dynamo for Revit 2023 IronPython

import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
clr.AddReference("RevitNodes")

from Autodesk.Revit.DB import (
    BuiltInCategory, ElementId,
    Pipe, Duct,
)
from RevitServices.Persistence import DocumentManager

import Revit
clr.ImportExtensions(Revit.Elements)

SUPPORTED_SUBJECT_CATEGORIES = {
    int(BuiltInCategory.OST_PipeCurves),
    int(BuiltInCategory.OST_DuctCurves),
}


def unwrap(dynamo_element):
    """Unwrap a Dynamo element to get the Revit API element."""
    try:
        return dynamo_element.InternalElement
    except AttributeError:
        return dynamo_element


def unwrap_list(dynamo_elements):
    """Unwrap a list; handle single element input."""
    if not isinstance(dynamo_elements, (list, tuple)):
        dynamo_elements = [dynamo_elements]
    return [unwrap(e) for e in dynamo_elements if e is not None]


def _cat_id(element):
    """Return integer category id, or -1 if none."""
    if element.Category is not None:
        return element.Category.Id.IntegerValue
    return -1


def filter_subjects(elements):
    """
    Separate into (supported_subjects, skipped_info).
    supported = Pipe or Duct
    skipped_info = list of (element_id, skip_reason)
    """
    supported = []
    skipped = []
    for elem in elements:
        cid = _cat_id(elem)
        if cid in SUPPORTED_SUBJECT_CATEGORIES:
            supported.append(elem)
        else:
            cat_name = elem.Category.Name if elem.Category else "Unknown"
            skipped.append((elem.Id.IntegerValue,
                            "SKIP_NOT_PIPE_DUCT",
                            cat_name))
    return supported, skipped


def validate_selection(subjects_raw, obstacles_raw):
    """
    Validate inputs. Returns (subjects, obstacles, errors).
    errors is a list of strings; empty means OK.
    """
    errors = []
    subjects = unwrap_list(subjects_raw) if subjects_raw else []
    obstacles = unwrap_list(obstacles_raw) if obstacles_raw else []

    if not subjects:
        errors.append("No MEP subjects selected.")
    if not obstacles:
        errors.append("No obstacle models selected.")

    return subjects, obstacles, errors
