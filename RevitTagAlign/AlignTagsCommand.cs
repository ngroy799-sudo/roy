using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RevitTagAlign
{
    /// <summary>
    /// Bird Tools style (official Help):
    /// - Configure: corner + Angle slider (mouse never changes angle)
    /// - Optional: select tags after Configure if none preselected
    /// - 1 click = position of closest tag to tagged elements (taghead)
    /// - Repeat clicks until ESC
    /// </summary>
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

                string planeError;
                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                // Configure first (Bird Tools: form shows even if nothing preselected).
                AlignConfig cfg;
                var optionsWindow = new AlignOptionsWindow();
                TrySetRevitOwner(optionsWindow, uiapp);
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;
                cfg = optionsWindow.Config;
                cfg.PickAngleThenTagPosition = false;

                ActivateRevitWindow(uiapp);

                // If nothing was preselected, ask user to pick tags/text notes now.
                if (items.Count < 1)
                {
                    try
                    {
                        IList<Reference> refs = uidoc.Selection.PickObjects(
                            ObjectType.Element,
                            new AnnotationSelectionFilter(),
                            "Select Tags / Text Notes to align, then click Finish");
                        var ids = refs.Select(r => r.ElementId).ToList();
                        items = CollectAnnotations(doc, ids);
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }

                if (items.Count < 1)
                {
                    TaskDialog.Show("TagAlign",
                        "No Tags / Text Notes selected.\nSelect annotations, then run TagAlign again.");
                    return Result.Cancelled;
                }

                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                bool anySuccess = false;
                int round = 0;

                // 1-click loop: each click places the closest-to-host taghead; angle stays fixed.
                while (true)
                {
                    round++;
                    Reselect(uidoc, items);

                    if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                    {
                        TaskDialog.Show("TagAlign", planeError);
                        break;
                    }

                    XYZ tagPosition;
                    try
                    {
                        tagPosition = WorkPlaneHelper.PickPoint(
                            uidoc, cfg,
                            string.Format(
                                "Click {0}: closest tag to tagged elements  |  {1}  |  angle={2:0.#}°  |  ESC=finish",
                                round,
                                CornerLabel(cfg.Corner),
                                cfg.AngleDegrees));
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        TaskDialog.Show("TagAlign", ioe.Message);
                        break;
                    }

                    using (TransactionGroup tg = new TransactionGroup(doc, "TagAlign Place " + round))
                    {
                        tg.Start();
                        using (Transaction tx = new Transaction(doc, "TagAlign Stack"))
                        {
                            tx.Start();
                            AlignmentEngine.AlignInView(doc, view, items, tagPosition, cfg, null);
                            tx.Commit();
                        }
                        tg.Assimilate();
                    }

                    anySuccess = true;
                    try { uidoc.RefreshActiveView(); } catch { }
                    Reselect(uidoc, items);
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

        private static string CornerLabel(CornerAlignment c)
        {
            switch (c)
            {
                case CornerAlignment.UpperLeft: return "Upper-Left";
                case CornerAlignment.UpperRight: return "Upper-Right";
                case CornerAlignment.LowerLeft: return "Lower-Left";
                case CornerAlignment.LowerRight: return "Lower-Right";
                default: return c.ToString();
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
            try { return uiapp.MainWindowHandle; }
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
            if (selectedIds == null)
                return items;

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

        /// <summary>Allows picking IndependentTag and TextNote elements.</summary>
        private class AnnotationSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is IndependentTag || elem is TextNote;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
