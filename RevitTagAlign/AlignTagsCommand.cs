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
    /// <summary>
    /// Bird Tools style:
    /// - 4 corner presets fix the leader DIRECTION
    /// - Angle slider fixes the leader ANGLE (does not change by mouse)
    /// - 1 click = tag text stack position (stretch/shorten leaders only)
    /// - Repeat clicks to re-adjust until ESC
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

                AlignConfig cfg;
                var optionsWindow = new AlignOptionsWindow();
                TrySetRevitOwner(optionsWindow, uiapp);
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;
                cfg = optionsWindow.Config;

                // Angle is fixed by corner preset + Angle slider — never from mouse.
                cfg.PickAngleThenTagPosition = false;

                ActivateRevitWindow(uiapp);

                if (!WorkPlaneHelper.TryEnsureForPicking(uidoc, out planeError))
                {
                    TaskDialog.Show("TagAlign", planeError);
                    return Result.Cancelled;
                }

                bool anySuccess = false;
                int round = 0;

                // 1-click loop: each click moves tag text position (stretch leaders), angle stays fixed.
                // ESC finishes.
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
                                "Click {0}: TAG TEXT position  |  {1}  |  angle={2:0.#}° fixed  |  ESC=finish",
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
                            // pickedAngle = null → use corner + AngleDegrees from Configure
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
