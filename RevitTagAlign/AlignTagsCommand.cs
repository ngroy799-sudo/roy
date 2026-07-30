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
            View view = doc.ActiveView;

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

                string planeError;
                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                // Configure once; then loop 1-click → 2-click until ESC.
                AlignConfig cfg;
                var optionsWindow = new AlignOptionsWindow();
                TrySetRevitOwner(optionsWindow, uiapp);
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;
                cfg = optionsWindow.Config;

                ActivateRevitWindow(uiapp);

                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                bool anySuccess = false;
                int round = 0;

                // Repeat adjust loop: ESC cancels current pick and exits tool.
                while (true)
                {
                    round++;
                    Reselect(uidoc, items);

                    if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                    {
                        TaskDialog.Show("TagAlign", planeError);
                        break;
                    }

                    using (TransactionGroup tg = new TransactionGroup(doc, "TagAlign Adjust " + round))
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
                                    string.Format(
                                        "Round {0} — Click 1/2: LEADER ANGLE  (ESC = finish)",
                                        round));

                                pickedAngle = AlignmentEngine.ComputeAngleInView(
                                    view, hostCentroid, anglePoint);

                                using (Transaction txPreview = new Transaction(doc, "TagAlign Preview Angle"))
                                {
                                    txPreview.Start();
                                    AlignmentEngine.PreviewAngleAtCurrentPositions(
                                        items, cfg, pickedAngle, view);
                                    txPreview.Commit();
                                }

                                Reselect(uidoc, items);
                                try { uidoc.RefreshActiveView(); } catch { }

                                tagPosition = WorkPlaneHelper.PickPoint(
                                    uidoc, cfg,
                                    string.Format(
                                        "Round {0} — Click 2/2: TAG POSITION  [angle={1:0.#}°]  (ESC = finish)",
                                        round, pickedAngle.AngleDegreesAbs));
                            }
                            else
                            {
                                tagPosition = WorkPlaneHelper.PickPoint(
                                    uidoc, cfg,
                                    string.Format(
                                        "Round {0} — Click: TAG POSITION  (ESC = finish)",
                                        round));
                            }
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            // ESC: end loop; keep previous successful rounds.
                            tg.RollBack();
                            break;
                        }
                        catch (InvalidOperationException ioe)
                        {
                            tg.RollBack();
                            TaskDialog.Show("TagAlign", ioe.Message);
                            break;
                        }

                        using (Transaction txFinal = new Transaction(doc, "TagAlign Stack Position"))
                        {
                            txFinal.Start();
                            AlignmentEngine.AlignInView(
                                doc, view, items, tagPosition, cfg, pickedAngle);
                            txFinal.Commit();
                        }

                        tg.Assimilate();
                    }

                    anySuccess = true;
                    try { uidoc.RefreshActiveView(); } catch { }
                    Reselect(uidoc, items);

                    // Continue loop for another 1→2 click adjust (no need to reopen tool).
                    // When PickAngleThenTagPosition is OFF, still loop position-only picks.
                }

                if (!cfg.KeepSelectionAfterUse && anySuccess)
                    uidoc.Selection.SetElementIds(new List<ElementId>());
                else
                    Reselect(uidoc, items);

                return anySuccess ? Result.Succeeded : Result.Cancelled;
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
