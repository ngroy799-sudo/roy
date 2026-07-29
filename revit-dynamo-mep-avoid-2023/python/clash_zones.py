# clash_zones.py — Detect and merge vertical clearance violations
# For use in Dynamo for Revit 2023 IronPython nodes

import clr
clr.AddReference("RevitAPI")

from Autodesk.Revit.DB import Line, XYZ

FEET_PER_MM = 1.0 / 304.8


class ClashZone(object):
    """One clash zone between a subject MEP and one or more obstacles."""

    __slots__ = ("subject_id", "obstacle_ids",
                 "mep_env", "obstacle_envs",
                 "current_gap_ft", "required_gap_ft",
                 "worst_obstacle_bottom_z",
                 "overlap_x_range", "overlap_y_range",
                 "skip_reason")

    def __init__(self, subject_id, mep_env, obstacle_env,
                 current_gap_ft, required_gap_ft):
        self.subject_id = subject_id
        self.obstacle_ids = [obstacle_env.element_id]
        self.mep_env = mep_env
        self.obstacle_envs = [obstacle_env]
        self.current_gap_ft = current_gap_ft
        self.required_gap_ft = required_gap_ft
        self.worst_obstacle_bottom_z = obstacle_env.outer_bottom_z

        self.overlap_x_range = (
            max(mep_env.min_x, obstacle_env.min_x),
            min(mep_env.max_x, obstacle_env.max_x),
        )
        self.overlap_y_range = (
            max(mep_env.min_y, obstacle_env.min_y),
            min(mep_env.max_y, obstacle_env.max_y),
        )
        self.skip_reason = None

    @property
    def gap_mm(self):
        return self.current_gap_ft / FEET_PER_MM

    @property
    def required_gap_mm(self):
        return self.required_gap_ft / FEET_PER_MM

    @property
    def is_clash(self):
        return self.current_gap_ft < self.required_gap_ft

    def merge_with(self, other):
        """Merge another ClashZone into this one (same subject)."""
        self.obstacle_ids.extend(other.obstacle_ids)
        self.obstacle_envs.extend(other.obstacle_envs)
        self.worst_obstacle_bottom_z = min(
            self.worst_obstacle_bottom_z,
            other.worst_obstacle_bottom_z,
        )
        self.current_gap_ft = min(self.current_gap_ft, other.current_gap_ft)
        self.overlap_x_range = (
            min(self.overlap_x_range[0], other.overlap_x_range[0]),
            max(self.overlap_x_range[1], other.overlap_x_range[1]),
        )
        self.overlap_y_range = (
            min(self.overlap_y_range[0], other.overlap_y_range[0]),
            max(self.overlap_y_range[1], other.overlap_y_range[1]),
        )

    def __repr__(self):
        return ("ClashZone(subj={}, obs={}, gap={:.0f}mm, clash={})"
                .format(self.subject_id, self.obstacle_ids,
                        self.gap_mm, self.is_clash))


def detect_clashes(subject_envs, obstacle_envs, clearance_mm=100):
    """
    For each subject, check vertical clearance against all obstacles.

    Returns dict: { subject_element_id: [ClashZone, ...] }
    Only returns zones where clearance is violated (is_clash=True).
    """
    required_gap_ft = clearance_mm * FEET_PER_MM
    result = {}

    for mep in subject_envs:
        zones = []
        for obs in obstacle_envs:
            if not mep.overlaps_xy(obs):
                continue

            if mep.outer_bottom_z >= obs.outer_top_z:
                gap = mep.outer_bottom_z - obs.outer_top_z
            elif obs.outer_bottom_z >= mep.outer_top_z:
                gap = obs.outer_bottom_z - mep.outer_top_z
            else:
                gap = -1.0  # intersecting

            zone = ClashZone(mep.element_id, mep, obs,
                             gap, required_gap_ft)
            if zone.is_clash:
                zones.append(zone)

        if zones:
            result[mep.element_id] = zones

    return result


def merge_nearby_zones(clash_dict, merge_gap_mm=300):
    """
    Merge clash zones for the same subject whose X/Y overlap regions
    are close together (within merge_gap_mm).

    Modifies clash_dict in place and returns it.
    """
    merge_gap_ft = merge_gap_mm * FEET_PER_MM

    for subj_id, zones in clash_dict.items():
        if len(zones) <= 1:
            continue

        zones.sort(key=lambda z: z.overlap_x_range[0])
        merged = [zones[0]]
        for z in zones[1:]:
            prev = merged[-1]
            x_gap = z.overlap_x_range[0] - prev.overlap_x_range[1]
            y_overlap = (prev.overlap_y_range[0] < z.overlap_y_range[1] and
                         prev.overlap_y_range[1] > z.overlap_y_range[0])
            if x_gap < merge_gap_ft and y_overlap:
                prev.merge_with(z)
            else:
                merged.append(z)

        clash_dict[subj_id] = merged

    return clash_dict


def compute_target_z(zone, mep_outer_half_height_ft, clearance_mm=100):
    """
    Compute the target center-Z for the drop-under segment.

    Returns (target_center_z_ft, feasible: bool).
    """
    clearance_ft = clearance_mm * FEET_PER_MM
    target = zone.worst_obstacle_bottom_z - clearance_ft - mep_outer_half_height_ft

    feasible = True  # Phase 2 will add floor/other-obstacle checks
    return target, feasible
