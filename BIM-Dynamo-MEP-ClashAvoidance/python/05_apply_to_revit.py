# -*- coding: utf-8 -*-
"""
05_apply_to_revit.py
Apply approved offsets to MEP curves in the host document.

IN[0] = validated_plan
IN[1] = RunMode ("DetectOnly" | "DetectAndFix")

OUT = [apply_results, success_count, message]
"""
from __future__ import print_function
import sys
import os

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else None
if _HERE and _HERE not in sys.path:
    sys.path.append(_HERE)

try:
    validated_plan = IN[0]  # noqa: F821
    run_mode = IN[1] if len(IN) > 1 else "DetectOnly"  # noqa: F821
except NameError:
    validated_plan, run_mode = [], "DetectOnly"

from Autodesk.Revit.DB import ElementId
from lib.revit_utils import get_doc, move_mep_curve_by_offset_mm


def run(validated_plan, run_mode="DetectOnly"):
    results = []
    success = 0

    if str(run_mode) != "DetectAndFix":
        for item in validated_plan:
            r = dict(item)
            r["apply_status"] = "SkippedDetectOnly"
            results.append(r)
        return results, 0, "Skipped apply (DetectOnly)"

    doc = get_doc()
    for item in validated_plan:
        r = dict(item)
        if item.get("action") != "OffsetApproved":
            r["apply_status"] = "Skipped"
            results.append(r)
            continue
        eid = item["mep_id"]
        try:
            # Revit 2024+ uses ElementId(long); older uses int
            try:
                el = doc.GetElement(ElementId(eid))
            except TypeError:
                el = doc.GetElement(ElementId(int(eid)))
            if el is None:
                r["apply_status"] = "ElementNotFound"
                results.append(r)
                continue
            ok, msg = move_mep_curve_by_offset_mm(doc, el, item.get("offset_mm", (0, 0, 0)))
            r["apply_status"] = "Applied" if ok else "Failed"
            r["apply_message"] = msg
            if ok:
                success += 1
                r["action"] = "OffsetApplied"
        except Exception as ex:
            r["apply_status"] = "Failed"
            r["apply_message"] = str(ex)
        results.append(r)

    msg = "Applied {0}/{1}".format(success, len(validated_plan))
    return results, success, msg


OUT = run(validated_plan, run_mode)
