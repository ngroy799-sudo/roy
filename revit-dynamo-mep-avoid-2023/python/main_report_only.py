# main_report_only.py — Phase 1 Dynamo Python Script Node
# =========================================================
# Paste this entire script into a single Dynamo "Python Script" node.
# 
# INPUTS (connect to the node's IN ports):
#   IN[0] = subjects   — selected MEP elements (Pipe/Duct)
#   IN[1] = obstacles  — selected obstacle model elements
#   IN[2] = clearance  — clearance in mm (Number, default 100)
#   IN[3] = merge_gap  — merge gap in mm (Number, default 300)
#
# OUTPUTS:
#   OUT[0] = report text (multiline string for Watch node)
#   OUT[1] = clash count (integer)
#   OUT[2] = skip count (integer)
#   OUT[3] = CSV rows (list of lists, for export)
# =========================================================

import clr
import sys

clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
clr.AddReference("RevitNodes")

from RevitServices.Persistence import DocumentManager

doc = DocumentManager.Instance.CurrentDBDocument

# ---------- Import project modules ----------
# In Dynamo, place the python/*.py files in a known folder and add
# that folder to sys.path. Adjust the path below to match your setup.
#
# Option A: Place python files next to the .dyn file
import os
_script_dir = os.path.dirname(os.path.abspath(__file__)) if '__file__' in dir() else ""
if _script_dir:
    _project_python = os.path.join(os.path.dirname(_script_dir), "python")
    if os.path.isdir(_project_python) and _project_python not in sys.path:
        sys.path.insert(0, _project_python)

# Option B: Hard-code the path (user should update this)
# sys.path.insert(0, r"C:\path\to\revit-dynamo-mep-avoid-2023\python")

from selection_utils import validate_selection, filter_subjects
from envelope import get_envelopes
from clash_zones import detect_clashes, merge_nearby_zones
from report import format_clash_report, build_csv_rows

# ---------- Read inputs ----------
subjects_raw = IN[0] if isinstance(IN[0], list) else [IN[0]] if IN[0] else []
obstacles_raw = IN[1] if isinstance(IN[1], list) else [IN[1]] if IN[1] else []
clearance_mm = IN[2] if IN[2] is not None else 100
merge_gap_mm = IN[3] if IN[3] is not None else 300

# ---------- Validate ----------
subjects, obstacles, errors = validate_selection(subjects_raw, obstacles_raw)

if errors:
    OUT = ["\n".join(["ERROR: " + e for e in errors]), 0, 0, []]
else:
    # Filter subjects to Pipe/Duct
    supported, skipped_subjects = filter_subjects(subjects)

    if not supported:
        msg = "No supported MEP subjects (Pipe/Duct) in selection."
        if skipped_subjects:
            msg += "\nSkipped: " + ", ".join(
                "#{} ({})".format(s[0], s[2]) for s in skipped_subjects)
        OUT = [msg, 0, len(skipped_subjects), []]
    else:
        # Build envelopes (with insulation)
        subj_envs = get_envelopes(supported, doc)
        obs_envs = get_envelopes(obstacles, doc)

        if not obs_envs:
            OUT = ["ERROR: Could not read geometry for any obstacle.", 0, 0, []]
        else:
            # Detect clashes
            clash_dict = detect_clashes(subj_envs, obs_envs, clearance_mm)
            clash_dict = merge_nearby_zones(clash_dict, merge_gap_mm)

            # Format report
            report_text, _, clash_count, skip_count = format_clash_report(
                clash_dict, skipped_subjects)
            csv_rows = build_csv_rows(clash_dict, skipped_subjects)

            OUT = [report_text, clash_count, skip_count, csv_rows]
