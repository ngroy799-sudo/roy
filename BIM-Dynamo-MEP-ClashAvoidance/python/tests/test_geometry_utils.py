# -*- coding: utf-8 -*-
"""Minimal tests for geometry helpers (no Revit required)."""
from __future__ import print_function
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.abspath(os.path.join(HERE, "..")))

from lib.geometry_utils import (
    aabb_from_minmax,
    expand_aabb,
    aabb_intersects,
    aabb_penetration_mm,
    translate_aabb,
    offset_from_axis,
    clearance_for,
)


def test_aabb_intersect_and_clearance():
    a = aabb_from_minmax((0, 0, 0), (100, 100, 100))
    b = aabb_from_minmax((90, 90, 90), (200, 200, 200))
    assert aabb_intersects(a, b)
    assert aabb_penetration_mm(a, b) == 10

    c = aabb_from_minmax((200, 0, 0), (300, 100, 100))
    assert not aabb_intersects(a, c)

    expanded = expand_aabb(a, 50)
    assert expanded["min"] == (-50, -50, -50)
    assert expanded["max"] == (150, 150, 150)


def test_offset_and_translate():
    a = aabb_from_minmax((0, 0, 0), (10, 10, 10))
    off = offset_from_axis("Z", 100)
    assert off == (0.0, 0.0, 100.0)
    t = translate_aabb(a, off)
    assert t["min"][2] == 100
    assert t["max"][2] == 110


def test_clearance_table():
    rules = {
        "clearance_mm": {
            "default": 50,
            "Duct_vs_STC": 150,
            "MEP_vs_MEP": 30,
        }
    }
    assert clearance_for("Duct", "STC", rules) == 150
    assert clearance_for("Pipe", "MEP", rules) == 30
    assert clearance_for("Pipe", "ARC", rules) == 50


def main():
    test_aabb_intersect_and_clearance()
    test_offset_and_translate()
    test_clearance_table()
    print("All geometry tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
