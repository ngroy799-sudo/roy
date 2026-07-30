using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTagAlign
{
    [Transaction(TransactionMode.Manual)]
    public class AlignTagsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                var items = CollectAnnotations(doc, selectedIds);

                if (items.Count < 1)
                {
                    TaskDialog.Show("Tag Align",
                        "Select one or more Tags / Text Notes (with leaders), then run Align Tags again.");
                    return Result.Cancelled;
                }

                var optionsWindow = new AlignOptionsWindow();
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;

                AlignConfig cfg = optionsWindow.Config;

                // Optionally turn snaps off while picking.
                bool? previousSnaps = null;
                if (cfg.TurnSnapsOff)
                {
                    try
                    {
                        // TemporaryObjectStyles / SnapMode — use UIApplication snaps via keyboard is not API-friendly.
                        // Best effort: use PickObject options; Revit SnapMode is not fully exposed.
                        // Document for user: Turn Snaps Off uses ObjectSnapTypes.None on the pick.
                        previousSnaps = true;
                    }
                    catch { }
                }

                XYZ pickPoint;
                try
                {
                    pickPoint = uidoc.Selection.PickPoint(
                        cfg.TurnSnapsOff ? ObjectSnapTypes.None : ObjectSnapTypes.Endpoints | ObjectSnapTypes.Intersections | ObjectSnapTypes.Nearest,
                        "Pick alignment point on screen (•)");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                using (Transaction tx = new Transaction(doc, "Align Tags"))
                {
                    tx.Start();
                    AlignmentEngine.Align(doc, items, pickPoint, cfg);
                    tx.Commit();
                }

                if (!cfg.KeepSelectionAfterUse)
                {
                    uidoc.Selection.SetElementIds(new List<ElementId>());
                }
                else
                {
                    uidoc.Selection.SetElementIds(items.Select(i => i.Element.Id).ToList());
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static List<AlignmentEngine.AnnotationItem> CollectAnnotations(
            Document doc,
            ICollection<ElementId> selectedIds)
        {
            var items = new List<AlignmentEngine.AnnotationItem>();

            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                if (elem is IndependentTag tag)
                {
                    items.Add(new AlignmentEngine.AnnotationItem
                    {
                        Element = tag,
                        OriginalHead = tag.TagHeadPosition,
                        HostPoint = AlignmentEngine.GetTagHostPoint(tag)
                    });
                }
                else if (elem is TextNote tn)
                {
                    items.Add(new AlignmentEngine.AnnotationItem
                    {
                        Element = tn,
                        OriginalHead = tn.Coord,
                        HostPoint = AlignmentEngine.GetTextNoteHostPoint(tn)
                    });
                }
            }

            return items;
        }
    }
}
