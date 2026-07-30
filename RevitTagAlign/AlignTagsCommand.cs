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

                ObjectSnapTypes snapTypes = cfg.TurnSnapsOff
                    ? ObjectSnapTypes.None
                    : ObjectSnapTypes.Endpoints | ObjectSnapTypes.Intersections | ObjectSnapTypes.Nearest
                      | ObjectSnapTypes.Perpendicular | ObjectSnapTypes.Midpoints;

                PickedAngle pickedAngle = null;
                XYZ tagPosition;

                if (cfg.PickAngleThenTagPosition)
                {
                    // Bird Tools style:
                    // 1) Mouse-pick the RED arrow angle (two points along desired angled leader)
                    // 2) Mouse-pick the TAG TEXT position (first tag head / yellow-landing side)
                    XYZ angleP1;
                    XYZ angleP2;
                    try
                    {
                        angleP1 = uidoc.Selection.PickPoint(
                            snapTypes,
                            "1/2  Pick LEADER ANGLE — first point (start of red arrow / near elbow)");
                        angleP2 = uidoc.Selection.PickPoint(
                            snapTypes,
                            "1/2  Pick LEADER ANGLE — second point (along red arrow toward element)");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }

                    pickedAngle = AlignmentEngine.ComputeAngleFromTwoPoints(angleP1, angleP2);

                    try
                    {
                        tagPosition = uidoc.Selection.PickPoint(
                            snapTypes,
                            string.Format(
                                "2/2  Pick TAG POSITION — first tag text location  [angle={0:0.#}°]",
                                pickedAngle.AngleDegreesAbs));
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }
                else
                {
                    try
                    {
                        tagPosition = uidoc.Selection.PickPoint(
                            snapTypes,
                            "Pick TAG POSITION — first tag text location");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }

                using (Transaction tx = new Transaction(doc, "Align Tags"))
                {
                    tx.Start();
                    AlignmentEngine.Align(doc, items, tagPosition, cfg, pickedAngle);
                    tx.Commit();
                }

                if (!cfg.KeepSelectionAfterUse)
                    uidoc.Selection.SetElementIds(new List<ElementId>());
                else
                    uidoc.Selection.SetElementIds(items.Select(i => i.Element.Id).ToList());

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
