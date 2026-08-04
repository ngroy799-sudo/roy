# -*- coding: utf-8 -*-
"""
sync_cbwd_bl.py — Dynamo Player entry (Revit 2023 / IronPython2).

Face Opening (Generic Model) bottom Elevation → parameter CBWD B.L.

Selection modes:
  SelectedOnly  — use current Revit selection, filter to Face Opening
  AllOpenings   — all Generic Models in document matching family keyword

IN[0] ProjectRoot
IN[1] SelectionMode       SelectedOnly | AllOpenings
                          (also accepts: selected, all, true=All, false=Selected)
IN[2] FamilyNameContains  default from config / "Face Opening"
IN[3] ParameterName       default from config / "CBWD B.L."
IN[4] ReportPath          optional; (auto) → samples/last_sync_report.json

OUT (Player-safe primitives):
  [0] Summary (string)
  [1] ReportPath (string)
  [2] SuccessCount (int)
  [3] SkippedCount (int)
  [4] Warnings (string)
  [5] Status (string) OK | WARN | ERROR
"""
from __future__ import print_function
import os
import sys
import json
from datetime import datetime


def _s(v, default=""):
    if v is None:
        return default
    try:
        if hasattr(v, "ToString"):
            t = v.ToString()
            if t is None:
                return default
            return str(t)
    except Exception:
        pass
    try:
        return str(v)
    except Exception:
        return default


def _out(summary, report_path, success_count, skipped_count, warnings, status):
    return [
        _s(summary),
        _s(report_path),
        int(success_count or 0),
        int(skipped_count or 0),
        _s(warnings) if warnings else "none",
        _s(status) or "OK",
    ]


def normalize_selection_mode(value, default="SelectedOnly"):
    """
    Normalize Player / config input to SelectedOnly | AllOpenings.
    Accepts bools: True → AllOpenings, False → SelectedOnly.
    """
    if value is None:
        return default
    if isinstance(value, bool):
        return "AllOpenings" if value else "SelectedOnly"
    s = _s(value).strip()
    if not s or s.lower() in ("null", "none", ""):
        return default
    low = s.lower().replace(" ", "").replace("_", "")
    if low in ("allopenings", "all", "allopening", "processall", "processallopenings", "true", "1", "yes", "y", "on"):
        return "AllOpenings"
    if low in ("selectedonly", "selected", "selection", "partial", "false", "0", "no", "n", "off"):
        return "SelectedOnly"
    # Allow exact canonical names with mixed case
    if s in ("SelectedOnly", "AllOpenings"):
        return s
    return default


def run(inputs):
    warnings = []
    try:
        project_root = _s(inputs[0] if len(inputs) > 0 else "").strip().strip('"')
        selection_mode_in = inputs[1] if len(inputs) > 1 else None
        family_override = _s(inputs[2] if len(inputs) > 2 else "").strip()
        parameter_override = _s(inputs[3] if len(inputs) > 3 else "").strip()
        report_path = _s(inputs[4] if len(inputs) > 4 else "").strip()
        if report_path.lower() in ("(auto)", "auto", "null", "none"):
            report_path = ""
    except Exception as ex:
        return _out("Input parse failed: " + str(ex), "", 0, 0, str(ex), "ERROR")

    if not project_root:
        return _out(
            "ProjectRoot is empty",
            "",
            0,
            0,
            "Set ProjectRoot to folder with python/ and config/",
            "ERROR",
        )

    python_dir = os.path.join(project_root, "python")
    config_path = os.path.join(project_root, "config", "sync_rules.json")
    if not report_path:
        report_path = os.path.join(project_root, "samples", "last_sync_report.json")

    if not os.path.isdir(project_root):
        return _out("ProjectRoot not found: " + project_root, "", 0, 0, "Bad ProjectRoot", "ERROR")
    if not os.path.isdir(python_dir):
        return _out("python/ missing under ProjectRoot", "", 0, 0, python_dir, "ERROR")
    if not os.path.isfile(config_path):
        return _out("Config not found", "", 0, 0, config_path, "ERROR")

    if python_dir not in sys.path:
        sys.path.insert(0, python_dir)

    try:
        from lib.revit_utils import (
            get_doc,
            get_uidoc,
            load_rules,
            collect_generic_models,
            collect_selected_elements,
            is_face_opening_element,
            family_and_type_names,
            bottom_elevation_internal,
            set_length_parameter,
            element_id_int,
        )
        from Autodesk.Revit.DB import Transaction
    except Exception as ex:
        return _out("Import failed (run inside Revit Dynamo)", "", 0, 0, str(ex), "ERROR")

    try:
        rules = load_rules(config_path)
        doc = get_doc()
        uidoc = get_uidoc()
    except Exception as ex:
        return _out("Cannot load Revit doc/config", "", 0, 0, str(ex), "ERROR")

    default_mode = rules.get("selection_mode", "SelectedOnly")
    selection_mode = normalize_selection_mode(selection_mode_in, default_mode)
    family_keyword = family_override or rules.get("family_name_contains", "Face Opening")
    parameter_name = parameter_override or rules.get("parameter_name", "CBWD B.L.")
    if family_keyword.lower() in ("null", "none", "(default)"):
        family_keyword = rules.get("family_name_contains", "Face Opening")
    if parameter_name.lower() in ("null", "none", "(default)"):
        parameter_name = rules.get("parameter_name", "CBWD B.L.")

    # Collect candidates by mode
    skipped = []
    candidates = []
    try:
        if selection_mode == "AllOpenings":
            all_gm = collect_generic_models(doc)
            for el in all_gm:
                if is_face_opening_element(el, family_keyword):
                    candidates.append(el)
                else:
                    # do not list every non-matching GM in All mode (noise)
                    pass
            if not candidates:
                warnings.append(
                    "No Face Opening found in document (keyword=%s)" % family_keyword
                )
        else:
            selected = collect_selected_elements(doc, uidoc)
            if not selected:
                return _out(
                    "SelectedOnly: no elements selected. Select Face Opening(s) first.",
                    report_path,
                    0,
                    0,
                    "Empty selection",
                    "ERROR",
                )
            for el in selected:
                eid = element_id_int(el)
                if is_face_opening_element(el, family_keyword):
                    candidates.append(el)
                else:
                    fam, typ = family_and_type_names(el)
                    skipped.append(
                        {
                            "element_id": eid,
                            "reason": "not_face_opening",
                            "family": fam,
                            "type": typ,
                        }
                    )
            if not candidates:
                return _out(
                    "SelectedOnly: selection has no Face Opening matching '%s'"
                    % family_keyword,
                    report_path,
                    0,
                    len(skipped),
                    "No matching Face Opening in selection",
                    "ERROR",
                )
    except Exception as ex:
        return _out("Collect failed", report_path, 0, 0, str(ex), "ERROR")

    successes = []
    errors = []

    t = Transaction(doc, "Sync Face Opening Elevation to CBWD B.L.")
    try:
        t.Start()
        for el in candidates:
            eid = element_id_int(el)
            fam, typ = family_and_type_names(el)
            elev = bottom_elevation_internal(el)
            if elev is None:
                skipped.append(
                    {
                        "element_id": eid,
                        "reason": "no_bounding_box",
                        "family": fam,
                        "type": typ,
                    }
                )
                continue
            ok, reason = set_length_parameter(el, parameter_name, elev)
            if ok:
                successes.append(
                    {
                        "element_id": eid,
                        "family": fam,
                        "type": typ,
                        "bottom_elevation_internal": elev,
                        "parameter": parameter_name,
                    }
                )
            else:
                if reason in ("parameter_not_found", "parameter_read_only") or reason.startswith(
                    "parameter_not_double"
                ):
                    skipped.append(
                        {
                            "element_id": eid,
                            "reason": reason,
                            "family": fam,
                            "type": typ,
                            "bottom_elevation_internal": elev,
                        }
                    )
                else:
                    errors.append(
                        {
                            "element_id": eid,
                            "reason": reason,
                            "family": fam,
                            "type": typ,
                        }
                    )
        t.Commit()
    except Exception as ex:
        try:
            t.RollBack()
        except Exception:
            pass
        return _out("Transaction failed", report_path, 0, len(skipped), str(ex), "ERROR")

    report = {
        "schema_version": "1.0",
        "generated_at": datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ"),
        "project": "Face Opening bottom Elevation to CBWD B.L.",
        "selection_mode": selection_mode,
        "family_name_contains": family_keyword,
        "parameter_name": parameter_name,
        "success_count": len(successes),
        "skipped_count": len(skipped),
        "error_count": len(errors),
        "successes": successes,
        "skipped": skipped,
        "errors": errors,
    }

    try:
        report_dir = os.path.dirname(report_path)
        if report_dir and not os.path.isdir(report_dir):
            os.makedirs(report_dir)
        with open(report_path, "w") as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
    except Exception as ex:
        warnings.append("Report write failed: " + str(ex))

    if errors:
        warnings.append("%d write errors" % len(errors))
    if skipped:
        warnings.append("%d skipped" % len(skipped))

    summary = (
        "mode=%s | keyword=%s | param=%s | success=%d | skipped=%d | errors=%d"
        % (
            selection_mode,
            family_keyword,
            parameter_name,
            len(successes),
            len(skipped),
            len(errors),
        )
    )

    status = "OK"
    if errors:
        status = "WARN" if successes else "ERROR"
    elif not successes:
        status = "WARN"
        warnings.append("Nothing updated")

    warn_text = "; ".join(warnings) if warnings else "none"
    return _out(summary, report_path, len(successes), len(skipped), warn_text, status)


# Dynamo Python Script node calls run(IN) via loader; allow direct IN when embedded.
try:
    IN  # noqa: F821 — provided by Dynamo
except NameError:
    IN = []

if IN is not None and len(IN) > 0:
    try:
        OUT = run(IN)
    except Exception as _ex:
        OUT = _out("Unhandled error", "", 0, 0, str(_ex), "ERROR")
else:
    # Module import / offline — no auto-run
    OUT = None
