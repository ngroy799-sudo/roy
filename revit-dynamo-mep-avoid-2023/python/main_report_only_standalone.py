# main_report_only_standalone.py
# ===============================================================
# ALL-IN-ONE version: no external imports needed.
# Paste this entire script into a single Dynamo "Python Script" node.
#
# INPUTS:
#   IN[0] = subjects   — selected MEP elements (Pipe/Duct)
#   IN[1] = obstacles  — selected obstacle model elements
#   IN[2] = clearance  — clearance in mm (Number, default 100)
#   IN[3] = merge_gap  — merge gap in mm (Number, default 300)
#
# OUTPUTS:
#   OUT[0] = report text
#   OUT[1] = clash count
#   OUT[2] = skip count
#   OUT[3] = CSV rows
# ===============================================================

import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
clr.AddReference("RevitNodes")

from Autodesk.Revit.DB import (
    FilteredElementCollector, BuiltInCategory,
    BoundingBoxXYZ, XYZ, ElementId,
)
from Autodesk.Revit.DB.Plumbing import PipeInsulation
from Autodesk.Revit.DB.Mechanical import DuctInsulation
from RevitServices.Persistence import DocumentManager

import Revit
clr.ImportExtensions(Revit.Elements)

doc = DocumentManager.Instance.CurrentDBDocument
FEET_PER_MM = 1.0 / 304.8

SUPPORTED_CATS = {
    int(BuiltInCategory.OST_PipeCurves),
    int(BuiltInCategory.OST_DuctCurves),
}


# ── helpers ──────────────────────────────────────────────────
def unwrap(e):
    try:
        return e.InternalElement
    except:
        return e

def unwrap_list(els):
    if not isinstance(els, (list, tuple)):
        els = [els]
    return [unwrap(e) for e in els if e is not None]

def cat_id(e):
    return e.Category.Id.IntegerValue if e.Category else -1

def ft2mm(ft):
    return ft / FEET_PER_MM


# ── envelope ─────────────────────────────────────────────────
class Env(object):
    __slots__ = ("eid","mnx","mny","mnz","mxx","mxy","mxz","ins","cat")
    def __init__(s, eid, mnx,mny,mnz, mxx,mxy,mxz, ins, cat):
        s.eid=eid; s.mnx=mnx; s.mny=mny; s.mnz=mnz
        s.mxx=mxx; s.mxy=mxy; s.mxz=mxz; s.ins=ins; s.cat=cat
    @property
    def outer_top_z(s): return s.mxz
    @property
    def outer_bottom_z(s): return s.mnz
    @property
    def outer_half_height(s): return (s.mxz - s.mnz) / 2.0
    def overlaps_xy(s, o):
        return (s.mnx<o.mxx and s.mxx>o.mnx and s.mny<o.mxy and s.mxy>o.mny)

def find_insulation(doc, host_id):
    out = []
    for bic, cls in [(BuiltInCategory.OST_PipeInsulations, PipeInsulation),
                     (BuiltInCategory.OST_DuctInsulations, DuctInsulation)]:
        try:
            col = (FilteredElementCollector(doc).OfCategory(bic)
                   .WhereElementIsNotElementType().ToElements())
            for ins in col:
                if ins.HostElementId == host_id:
                    out.append(ins)
        except:
            pass
    return out

def get_env(elem, doc):
    bb = elem.get_BoundingBox(None)
    if bb is None:
        return None
    hid = elem.Id
    insulations = find_insulation(doc, hid)
    has_ins = len(insulations) > 0
    mnx,mny,mnz = bb.Min.X, bb.Min.Y, bb.Min.Z
    mxx,mxy,mxz = bb.Max.X, bb.Max.Y, bb.Max.Z
    for i in insulations:
        ib = i.get_BoundingBox(None)
        if ib is None: continue
        mnx=min(mnx,ib.Min.X); mny=min(mny,ib.Min.Y); mnz=min(mnz,ib.Min.Z)
        mxx=max(mxx,ib.Max.X); mxy=max(mxy,ib.Max.Y); mxz=max(mxz,ib.Max.Z)
    cn = elem.Category.Name if elem.Category else ""
    return Env(hid.IntegerValue, mnx,mny,mnz, mxx,mxy,mxz, has_ins, cn)


# ── clash detection ──────────────────────────────────────────
class CZ(object):
    __slots__ = ("sid","oids","menv","oenvs","gap","req","worst_bz",
                 "ox0","ox1","oy0","oy1","skip")
    def __init__(s, sid, menv, oenv, gap, req):
        s.sid=sid; s.oids=[oenv.eid]; s.menv=menv; s.oenvs=[oenv]
        s.gap=gap; s.req=req; s.worst_bz=oenv.outer_bottom_z
        s.ox0=max(menv.mnx,oenv.mnx); s.ox1=min(menv.mxx,oenv.mxx)
        s.oy0=max(menv.mny,oenv.mny); s.oy1=min(menv.mxy,oenv.mxy)
        s.skip=None
    @property
    def is_clash(s): return s.gap < s.req
    def merge(s, o):
        s.oids.extend(o.oids); s.oenvs.extend(o.oenvs)
        s.worst_bz=min(s.worst_bz, o.worst_bz)
        s.gap=min(s.gap, o.gap)
        s.ox0=min(s.ox0,o.ox0); s.ox1=max(s.ox1,o.ox1)
        s.oy0=min(s.oy0,o.oy0); s.oy1=max(s.oy1,o.oy1)

def detect(subj_envs, obs_envs, clr_mm):
    req = clr_mm * FEET_PER_MM
    out = {}
    for m in subj_envs:
        zones = []
        for o in obs_envs:
            if not m.overlaps_xy(o): continue
            if m.mnz >= o.mxz:
                g = m.mnz - o.mxz
            elif o.mnz >= m.mxz:
                g = o.mnz - m.mxz
            else:
                g = -1.0
            z = CZ(m.eid, m, o, g, req)
            if z.is_clash:
                zones.append(z)
        if zones:
            out[m.eid] = zones
    return out

def merge_zones(cd, mg_mm):
    mg = mg_mm * FEET_PER_MM
    for sid, zs in cd.items():
        if len(zs) <= 1: continue
        zs.sort(key=lambda z: z.ox0)
        merged = [zs[0]]
        for z in zs[1:]:
            p = merged[-1]
            if (z.ox0 - p.ox1) < mg and (p.oy0 < z.oy1 and p.oy1 > z.oy0):
                p.merge(z)
            else:
                merged.append(z)
        cd[sid] = merged
    return cd


# ── report ───────────────────────────────────────────────────
def make_report(cd, skipped):
    lines = []
    cc = 0
    lines.append("=" * 60)
    lines.append("  MEP CLASH / CLEARANCE REPORT (Phase 1 ReportOnly)")
    lines.append("  Clearance target: 100 mm (incl. insulation)")
    lines.append("=" * 60)
    lines.append("")  # placeholder for summary

    if not cd and not skipped:
        lines.append("No clashes detected. All clear.")
        return "\n".join(lines), 0, len(skipped)

    for sid, zones in sorted(cd.items()):
        for z in zones:
            cc += 1
            lines.append("[CLASH] Subject #{} ({})".format(sid, z.menv.cat))
            lines.append("  vs Obstacle(s): {}".format(
                ", ".join("#{}".format(oid) for oid in z.oids)))
            gm = ft2mm(z.gap)
            rm = ft2mm(z.req)
            if z.gap < 0:
                lines.append("  Gap: INTERSECTING (elements overlap)")
            else:
                lines.append("  Gap: {:.0f} mm  (required: {:.0f} mm)".format(gm, rm))
            lines.append("  MEP top Z: {:.1f} mm  |  Obstacle bottom Z: {:.1f} mm"
                         .format(ft2mm(z.menv.outer_top_z), ft2mm(z.worst_bz)))
            lines.append("  MEP insulation: {}".format("Yes" if z.menv.ins else "No"))
            for oe in z.oenvs:
                lines.append("    Obstacle #{} ({}) insulation: {}".format(
                    oe.eid, oe.cat, "Yes" if oe.ins else "No"))
            lines.append("")

    if skipped:
        lines.append("-" * 40)
        lines.append("SKIPPED SUBJECTS (unsupported type):")
        for sid, reason, detail in skipped:
            lines.append("  #{} - {} ({})".format(sid, reason, detail))
        lines.append("")

    sc = len(skipped)
    lines[4] = "Clashes found: {}  |  Subjects skipped: {}".format(cc, sc)
    return "\n".join(lines), cc, sc


def make_csv(cd, skipped):
    hdr = ["Type","Subject_Id","Subject_Category","Obstacle_Ids",
           "Gap_mm","Required_mm","MEP_TopZ_mm","Obstacle_BottomZ_mm",
           "MEP_Insulated","Reason"]
    rows = [hdr]
    for sid, zones in sorted(cd.items()):
        for z in zones:
            gm = ft2mm(z.gap) if z.gap >= 0 else -1
            rows.append(["CLASH", sid, z.menv.cat,
                         ";".join(str(o) for o in z.oids),
                         "{:.0f}".format(gm), "{:.0f}".format(ft2mm(z.req)),
                         "{:.1f}".format(ft2mm(z.menv.outer_top_z)),
                         "{:.1f}".format(ft2mm(z.worst_bz)),
                         "Y" if z.menv.ins else "N", z.skip or ""])
    for sid, reason, detail in skipped:
        rows.append(["SKIP", sid, detail, "","","","","","", reason])
    return rows


# ── main ─────────────────────────────────────────────────────
subjects_raw = IN[0] if isinstance(IN[0], list) else [IN[0]] if IN[0] else []
obstacles_raw = IN[1] if isinstance(IN[1], list) else [IN[1]] if IN[1] else []
clr_mm = IN[2] if IN[2] is not None else 100
mg_mm = IN[3] if IN[3] is not None else 300

subjs = unwrap_list(subjects_raw)
obsts = unwrap_list(obstacles_raw)

errs = []
if not subjs: errs.append("No MEP subjects selected.")
if not obsts: errs.append("No obstacle models selected.")

if errs:
    OUT = ["\n".join(["ERROR: " + e for e in errs]), 0, 0, []]
else:
    supported = []
    skipped = []
    for e in subjs:
        if cat_id(e) in SUPPORTED_CATS:
            supported.append(e)
        else:
            cn = e.Category.Name if e.Category else "Unknown"
            skipped.append((e.Id.IntegerValue, "SKIP_NOT_PIPE_DUCT", cn))

    if not supported:
        msg = "No supported MEP (Pipe/Duct) in selection."
        if skipped:
            msg += "\nSkipped: " + ", ".join(
                "#{} ({})".format(s[0], s[2]) for s in skipped)
        OUT = [msg, 0, len(skipped), []]
    else:
        se = [env for env in [get_env(e, doc) for e in supported] if env]
        oe = [env for env in [get_env(e, doc) for e in obsts] if env]
        if not oe:
            OUT = ["ERROR: Could not read geometry for any obstacle.", 0, 0, []]
        else:
            cd = detect(se, oe, clr_mm)
            cd = merge_zones(cd, mg_mm)
            txt, cc, sc = make_report(cd, skipped)
            csv = make_csv(cd, skipped)
            OUT = [txt, cc, sc, csv]
