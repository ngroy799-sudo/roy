using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RevitTagAlign
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AlignTagsCommand : IExternalCommand
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "No active document.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;

            try
            {
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds().ToList();
                var items = CollectAnnotations(doc, selectedIds);

                if (items.Count < 1)
                {
                    TaskDialog.Show("TagAlign",
                        "Select one or more Tags / Text Notes first, then run TagAlign Align Selected Tags.");
                    return Result.Cancelled;
                }

                // Work plane must exist BEFORE PickPoint (not caused by rename/KS changes).
                string planeError;
                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                AlignConfig cfg;
                var optionsWindow = new AlignOptionsWindow();
                TrySetRevitOwner(optionsWindow, uiapp);
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;
                cfg = optionsWindow.Config;

                ActivateRevitWindow(uiapp);

                // Re-ensure after dialog (some views clear context).
                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                using (TransactionGroup tg = new TransactionGroup(doc, "TagAlign Align"))
                {
                    tg.Start();

                    PickedAngle pickedAngle = null;
                    XYZ tagPosition;

                    try
                    {
                        if (cfg.PickAngleThenTagPosition)
                        {
                            XYZ hostCentroid = AlignmentEngine.AverageHostPoint(items);
                            XYZ anglePoint = WorkPlaneHelper.PickPoint(
                                uidoc, cfg,
                                "Click 1/2: LEADER ANGLE (red arrow) — tags update after click");

                            pickedAngle = AlignmentEngine.ComputeAngleFromReferenceAndPoint(
                                hostCentroid, anglePoint);

                            using (Transaction txPreview = new Transaction(doc, "TagAlign Preview Angle"))
                            {
                                txPreview.Start();
                                AlignmentEngine.PreviewAngleAtCurrentPositions(items, cfg, pickedAngle);
                                txPreview.Commit();
                            }

                            Reselect(uidoc, items);
                            try { uidoc.RefreshActiveView(); } catch { }

                            tagPosition = WorkPlaneHelper.PickPoint(
                                uidoc, cfg,
                                string.Format(
                                    "Click 2/2: TAG POSITION — stack moves here  [angle={0:0.#} deg]",
                                    pickedAngle.AngleDegreesAbs));
                        }
                        else
                        {
                            tagPosition = WorkPlaneHelper.PickPoint(
                                uidoc, cfg,
                                "Click: TAG POSITION (first tag text location)");
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        tg.RollBack();
                        Reselect(uidoc, items);
                        return Result.Cancelled;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        tg.RollBack();
                        Reselect(uidoc, items);
                        TaskDialog.Show("TagAlign", ioe.Message);
                        return Result.Cancelled;
                    }

                    using (Transaction txFinal = new Transaction(doc, "TagAlign Align Position"))
                    {
                        txFinal.Start();
                        AlignmentEngine.Align(doc, items, tagPosition, cfg, pickedAngle);
                        txFinal.Commit();
                    }

                    tg.Assimilate();
                }

                try { uidoc.RefreshActiveView(); } catch { }

                if (!cfg.KeepSelectionAfterUse)
                    uidoc.Selection.SetElementIds(new List<ElementId>());
                else
                    Reselect(uidoc, items);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("TagAlign Error", ex.Message);
                return Result.Failed;
            }
        }

        private static void Reselect(UIDocument uidoc, List<AlignmentEngine.AnnotationItem> items)
        {
            try
            {
                uidoc.Selection.SetElementIds(items.Select(i => i.Element.Id).ToList());
            }
            catch { }
        }

        private static void TrySetRevitOwner(Window window, UIApplication uiapp)
        {
            try
            {
                IntPtr handle = GetRevitMainWindowHandle(uiapp);
                if (handle != IntPtr.Zero && IsWindow(handle))
                {
                    var helper = new WindowInteropHelper(window);
                    helper.Owner = handle;
                }
            }
            catch { }
        }

        private static void ActivateRevitWindow(UIApplication uiapp)
        {
            try
            {
                IntPtr handle = GetRevitMainWindowHandle(uiapp);
                if (handle != IntPtr.Zero)
                    SetForegroundWindow(handle);
            }
            catch { }
        }

        private static IntPtr GetRevitMainWindowHandle(UIApplication uiapp)
        {
            try
            {
                return uiapp.MainWindowHandle;
            }
            catch
            {
                try { return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; }
                catch { return IntPtr.Zero; }
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
                        HostPoint = AlignmentEngine.GetTagHostPoint(tag),
                        OriginalLeaderEndCondition = AlignmentEngine.GetTagLeaderEndCondition(tag)
                    });
                }
                else if (elem is TextNote tn)
                {
                    items.Add(new AlignmentEngine.AnnotationItem
                    {
                        Element = tn,
                        OriginalHead = tn.Coord,
                        HostPoint = AlignmentEngine.GetTextNoteHostPoint(tn),
                        OriginalLeaderEndCondition = null
                    });
                }
            }

            return items;
        }
    }
}
