# -*- coding: utf-8 -*-
"""
06_report.py
Write JSON (+ optional CSV) clash/fix report.

IN[0] = apply_results (or validated_plan)
IN[1] = clash_pairs
IN[2] = output_path (string)
IN[3] = summary extras (optional string / arc_stc_warnings)

OUT = [report_path, summary, report_dict]
"""
from __future__ import print_function
import json
import os
import csv
from datetime import datetime

try:
    apply_results = IN[0]  # noqa: F821
    clash_pairs = IN[1] if len(IN) > 1 else []  # noqa: F821
    output_path = IN[2] if len(IN) > 2 else "samples/last_run_report.json"  # noqa: F821
    extras = IN[3] if len(IN) > 3 else None  # noqa: F821
except NameError:
    apply_results, clash_pairs, output_path, extras = [], [], "samples/last_run_report.json", None


def run(apply_results, clash_pairs, output_path, extras=None):
    actions = {}
    for r in apply_results or []:
        a = r.get("action", "Unknown")
        actions[a] = actions.get(a, 0) + 1

    report = {
        "generated_at": datetime.utcnow().isoformat() + "Z",
        "summary": {
            "clash_pairs": len(clash_pairs or []),
            "plan_items": len(apply_results or []),
            "actions": actions,
        },
        "clashes": clash_pairs or [],
        "results": apply_results or [],
        "extras": extras,
    }

    out = output_path
    if hasattr(out, "ToString"):
        out = out.ToString()
    folder = os.path.dirname(out)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)

    with open(out, "w") as f:
        json.dump(report, f, indent=2)

    csv_path = os.path.splitext(out)[0] + ".csv"
    with open(csv_path, "w") as f:
        w = csv.writer(f)
        w.writerow([
            "mep_id", "mep_kind", "system", "action", "apply_status",
            "offset_x", "offset_y", "offset_z", "reason", "clash_count"
        ])
        for r in apply_results or []:
            off = r.get("offset_mm") or (0, 0, 0)
            if isinstance(off, dict):
                off = (off.get("x", 0), off.get("y", 0), off.get("z", 0))
            w.writerow([
                r.get("mep_id"),
                r.get("mep_kind"),
                r.get("system"),
                r.get("action"),
                r.get("apply_status", ""),
                off[0], off[1], off[2],
                r.get("reason", ""),
                r.get("clash_count", ""),
            ])

    summary = "clashes={0}; items={1}; actions={2}; wrote={3}".format(
        report["summary"]["clash_pairs"],
        report["summary"]["plan_items"],
        actions,
        out,
    )
    return out, summary, report


OUT = run(apply_results, clash_pairs, output_path, extras)
