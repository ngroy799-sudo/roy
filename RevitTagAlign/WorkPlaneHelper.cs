using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitTagAlign
{
    /// <summary>
    /// PickPoint requires a sketch/work plane in the active view.
    /// Ensures one exists so Align Tags 1-click picking works in plan/section/elevation/drafting.
    /// </summary>
    internal static class WorkPlaneHelper
    {
        public static bool TryEnsureForPicking(UIDocument uidoc, out string error)
        {
            error = null;
            Document doc = uidoc.Document;
            View view = uidoc.ActiveView;

            if (view == null)
            {
                error = "No active view.";
                return false;
            }

            if (view is ViewSheet)
            {
                error = "TagAlign cannot pick points on a Sheet.\nOpen a Plan / Section / Elevation / Drafting view.";
                return false;
            }

            // Already has a work plane — PickPoint can proceed.
            try
            {
                if (view.SketchPlane != null)
                    return true;
            }
            catch
            {
                // Some views throw when reading SketchPlane — try to set one.
            }

            try
            {
                using (Transaction tx = new Transaction(doc, "TagAlign Ensure Work Plane"))
                {
                    tx.Start();

                    Plane plane = Plane.CreateByOriginAndBasis(
                        view.Origin,
                        view.RightDirection.Normalize(),
                        view.UpDirection.Normalize());

                    SketchPlane sp = SketchPlane.Create(doc, plane);
                    view.SketchPlane = sp;

                    tx.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                error =
                    "No work plane set in current view.\n\n" +
                    "Fix: open a Floor Plan / Ceiling Plan / Section / Elevation,\n" +
                    "or set a Work Plane (Architecture → Work Plane → Set),\n" +
                    "then run TagAlign again.\n\n" +
                    "Detail: " + ex.Message;
                return false;
            }
        }

        public static XYZ PickPoint(UIDocument uidoc, AlignConfig cfg, string prompt)
        {
            string ensureError;
            if (!TryEnsureForPicking(uidoc, out ensureError))
                throw new InvalidOperationException(ensureError ?? "No work plane set in current view.");

            if (cfg != null && cfg.TurnSnapsOff)
            {
                try
                {
                    return uidoc.Selection.PickPoint(ObjectSnapTypes.Points, prompt);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    return uidoc.Selection.PickPoint(prompt);
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    // Retry after re-ensuring plane once.
                    string err2;
                    if (!TryEnsureForPicking(uidoc, out err2))
                        throw new InvalidOperationException(err2 ?? "No work plane set in current view.");
                    return uidoc.Selection.PickPoint(prompt);
                }
            }

            try
            {
                return uidoc.Selection.PickPoint(prompt);
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                string err2;
                if (!TryEnsureForPicking(uidoc, out err2))
                    throw new InvalidOperationException(err2 ?? "No work plane set in current view.");
                return uidoc.Selection.PickPoint(prompt);
            }
        }
    }
}
