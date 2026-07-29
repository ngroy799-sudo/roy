# report.py — Structured output for clash report
# For Dynamo for Revit 2023 IronPython

FEET_PER_MM = 1.0 / 304.8


def _ft_to_mm(ft):
    return ft / FEET_PER_MM


def format_clash_report(clash_dict, skipped_subjects):
    """
    Build human-readable report lines and structured data.

    Parameters
    ----------
    clash_dict : { subject_id: [ClashZone, ...] }
    skipped_subjects : [(element_id, reason, detail), ...]

    Returns
    -------
    (summary_text, detail_lines, clash_count, skip_count)
    """
    lines = []
    clash_count = 0

    lines.append("=" * 60)
    lines.append("  MEP CLASH / CLEARANCE REPORT (Phase 1 ReportOnly)")
    lines.append("  Clearance target: 100 mm (incl. insulation)")
    lines.append("=" * 60)
    lines.append("")

    if not clash_dict and not skipped_subjects:
        lines.append("No clashes detected. All clear.")
        return "\n".join(lines), lines, 0, 0

    for subj_id, zones in sorted(clash_dict.items()):
        for z in zones:
            clash_count += 1
            status = "CLASH" if z.is_clash else "OK"
            lines.append(
                "[{status}] Subject #{sid}  ({cat})"
                .format(status=status, sid=subj_id,
                        cat=z.mep_env.category_name)
            )
            lines.append(
                "  vs Obstacle(s): {obs}"
                .format(obs=", ".join("#{}".format(oid)
                                      for oid in z.obstacle_ids))
            )
            gap_mm = _ft_to_mm(z.current_gap_ft)
            req_mm = _ft_to_mm(z.required_gap_ft)
            if z.current_gap_ft < 0:
                lines.append(
                    "  Gap: INTERSECTING (elements overlap)")
            else:
                lines.append(
                    "  Gap: {g:.0f} mm  (required: {r:.0f} mm)"
                    .format(g=gap_mm, r=req_mm))
            lines.append(
                "  MEP top Z: {tz:.1f} mm  |  Obstacle bottom Z: {bz:.1f} mm"
                .format(tz=_ft_to_mm(z.mep_env.outer_top_z),
                        bz=_ft_to_mm(z.worst_obstacle_bottom_z)))
            lines.append(
                "  MEP insulation: {ins}"
                .format(ins="Yes" if z.mep_env.has_insulation else "No"))
            for obs_env in z.obstacle_envs:
                lines.append(
                    "    Obstacle #{oid} ({cat}) insulation: {ins}"
                    .format(oid=obs_env.element_id,
                            cat=obs_env.category_name,
                            ins="Yes" if obs_env.has_insulation else "No"))
            if z.skip_reason:
                lines.append(
                    "  >> Will SKIP in Apply mode: {r}"
                    .format(r=z.skip_reason))
            lines.append("")

    if skipped_subjects:
        lines.append("-" * 40)
        lines.append("SKIPPED SUBJECTS (unsupported type):")
        for sid, reason, detail in skipped_subjects:
            lines.append(
                "  #{sid} — {reason} ({detail})"
                .format(sid=sid, reason=reason, detail=detail))
        lines.append("")

    skip_count = len(skipped_subjects)
    summary = ("Clashes found: {c}  |  Subjects skipped: {s}"
               .format(c=clash_count, s=skip_count))
    lines.insert(4, summary)

    return "\n".join(lines), lines, clash_count, skip_count


def build_csv_rows(clash_dict, skipped_subjects):
    """
    Build CSV-style rows for export.
    Returns list of lists: [header_row, data_rows...]
    """
    header = [
        "Type", "Subject_Id", "Subject_Category",
        "Obstacle_Ids", "Gap_mm", "Required_mm",
        "MEP_TopZ_mm", "Obstacle_BottomZ_mm",
        "MEP_Insulated", "Reason",
    ]
    rows = [header]

    for subj_id, zones in sorted(clash_dict.items()):
        for z in zones:
            gap_mm = _ft_to_mm(z.current_gap_ft) if z.current_gap_ft >= 0 else -1
            rows.append([
                "CLASH",
                subj_id,
                z.mep_env.category_name,
                ";".join(str(oid) for oid in z.obstacle_ids),
                "{:.0f}".format(gap_mm),
                "{:.0f}".format(_ft_to_mm(z.required_gap_ft)),
                "{:.1f}".format(_ft_to_mm(z.mep_env.outer_top_z)),
                "{:.1f}".format(_ft_to_mm(z.worst_obstacle_bottom_z)),
                "Y" if z.mep_env.has_insulation else "N",
                z.skip_reason or "",
            ])

    for sid, reason, detail in skipped_subjects:
        rows.append([
            "SKIP", sid, detail,
            "", "", "", "", "", "", reason,
        ])

    return rows
