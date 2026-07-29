# envelope.py — Revit element outer envelope (including insulation)
# For use in Dynamo for Revit 2023 IronPython nodes
# Provides top/bottom Z and horizontal extents of an element's
# outer surface, accounting for PipeInsulation / DuctInsulation.

import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
clr.AddReference("RevitNodes")

from Autodesk.Revit.DB import (
    FilteredElementCollector, BuiltInCategory,
    BoundingBoxXYZ, XYZ, ElementId,
    Pipe, Duct,
)
from Autodesk.Revit.DB.Plumbing import PipeInsulation
from Autodesk.Revit.DB.Mechanical import DuctInsulation
from RevitServices.Persistence import DocumentManager

FEET_PER_MM = 1.0 / 304.8


class Envelope(object):
    """Axis-aligned bounding envelope of an element including insulation."""

    __slots__ = ("element_id", "min_x", "min_y", "min_z",
                 "max_x", "max_y", "max_z",
                 "has_insulation", "category_name")

    def __init__(self, element_id,
                 min_x, min_y, min_z,
                 max_x, max_y, max_z,
                 has_insulation, category_name):
        self.element_id = element_id
        self.min_x = min_x
        self.min_y = min_y
        self.min_z = min_z
        self.max_x = max_x
        self.max_y = max_y
        self.max_z = max_z
        self.has_insulation = has_insulation
        self.category_name = category_name

    @property
    def outer_top_z(self):
        return self.max_z

    @property
    def outer_bottom_z(self):
        return self.min_z

    @property
    def height(self):
        return self.max_z - self.min_z

    @property
    def outer_half_height(self):
        return self.height / 2.0

    def overlaps_xy(self, other):
        """True if XY projections overlap (axis-aligned)."""
        return (self.min_x < other.max_x and self.max_x > other.min_x and
                self.min_y < other.max_y and self.max_y > other.min_y)

    def vertical_gap(self, other_below):
        """Gap between self bottom and other_below top (positive = space)."""
        return self.outer_bottom_z - other_below.outer_top_z

    def __repr__(self):
        return ("Envelope(id={}, z=[{:.1f},{:.1f}], insul={})"
                .format(self.element_id, self.min_z, self.max_z,
                        self.has_insulation))


def _get_bbox(element):
    """Get BoundingBoxXYZ of element; returns None if unavailable."""
    bb = element.get_BoundingBox(None)
    return bb


def _find_insulation_for_host(doc, host_id):
    """Find insulation elements hosted on the given element id."""
    insulations = []
    for cat, collector_type in [
        (BuiltInCategory.OST_PipeInsulations, PipeInsulation),
        (BuiltInCategory.OST_DuctInsulations, DuctInsulation),
    ]:
        try:
            col = (FilteredElementCollector(doc)
                   .OfCategory(cat)
                   .WhereElementIsNotElementType()
                   .ToElements())
            for ins in col:
                if ins.HostElementId == host_id:
                    insulations.append(ins)
        except Exception:
            pass
    return insulations


def _expand_bbox(base_bb, insulation_elements):
    """Expand a bbox to include insulation bboxes."""
    min_x, min_y, min_z = base_bb.Min.X, base_bb.Min.Y, base_bb.Min.Z
    max_x, max_y, max_z = base_bb.Max.X, base_bb.Max.Y, base_bb.Max.Z

    for ins in insulation_elements:
        ibb = _get_bbox(ins)
        if ibb is None:
            continue
        min_x = min(min_x, ibb.Min.X)
        min_y = min(min_y, ibb.Min.Y)
        min_z = min(min_z, ibb.Min.Z)
        max_x = max(max_x, ibb.Max.X)
        max_y = max(max_y, ibb.Max.Y)
        max_z = max(max_z, ibb.Max.Z)

    return min_x, min_y, min_z, max_x, max_y, max_z


def get_envelope(element, doc=None):
    """
    Build an Envelope for a Revit element, including insulation if present.

    Parameters
    ----------
    element : Revit Element (unwrapped)
    doc : Document (optional; defaults to current)

    Returns
    -------
    Envelope or None if bbox unavailable
    """
    if doc is None:
        doc = DocumentManager.Instance.CurrentDBDocument

    bb = _get_bbox(element)
    if bb is None:
        return None

    host_id = element.Id
    insulations = _find_insulation_for_host(doc, host_id)
    has_insulation = len(insulations) > 0

    if has_insulation:
        coords = _expand_bbox(bb, insulations)
    else:
        coords = (bb.Min.X, bb.Min.Y, bb.Min.Z,
                  bb.Max.X, bb.Max.Y, bb.Max.Z)

    cat_name = ""
    if element.Category is not None:
        cat_name = element.Category.Name

    return Envelope(
        element_id=host_id.IntegerValue,
        min_x=coords[0], min_y=coords[1], min_z=coords[2],
        max_x=coords[3], max_y=coords[4], max_z=coords[5],
        has_insulation=has_insulation,
        category_name=cat_name,
    )


def get_envelopes(elements, doc=None):
    """Build Envelope list for multiple elements, skipping failures."""
    results = []
    for elem in elements:
        env = get_envelope(elem, doc)
        if env is not None:
            results.append(env)
    return results
