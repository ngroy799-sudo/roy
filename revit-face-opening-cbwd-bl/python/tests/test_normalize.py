# -*- coding: utf-8 -*-
"""Offline unit tests for selection mode / name matching (no Revit required)."""
from __future__ import print_function
import os
import sys
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from sync_cbwd_bl import normalize_selection_mode  # noqa: E402


class FakeElement(object):
    def __init__(self, family, type_name):
        self._family = family
        self._type = type_name

        class Fam(object):
            def __init__(self, name):
                self.Name = name

        class Sym(object):
            def __init__(self, family_name, type_name):
                self.Name = type_name
                self.Family = Fam(family_name)

        self.Symbol = Sym(family, type_name)
        self.Name = type_name


class NormalizeModeTests(unittest.TestCase):
    def test_all_aliases(self):
        for v in ("AllOpenings", "all", "ALL", "true", "1", "Process All Openings", True):
            self.assertEqual(normalize_selection_mode(v), "AllOpenings")

    def test_selected_aliases(self):
        for v in ("SelectedOnly", "selected", "partial", "false", "0", False):
            self.assertEqual(normalize_selection_mode(v), "SelectedOnly")

    def test_default(self):
        self.assertEqual(normalize_selection_mode(None, "AllOpenings"), "AllOpenings")
        self.assertEqual(normalize_selection_mode("", "SelectedOnly"), "SelectedOnly")


class FaceOpeningNameTests(unittest.TestCase):
    def test_match(self):
        # Import matcher without requiring Revit — patch REVIT flag path
        from lib import revit_utils

        el = FakeElement("CBWD Face Opening", "Type A")
        self.assertTrue(revit_utils.is_face_opening_element(el, "Face Opening"))
        self.assertFalse(revit_utils.is_face_opening_element(el, "Door"))


if __name__ == "__main__":
    unittest.main()
