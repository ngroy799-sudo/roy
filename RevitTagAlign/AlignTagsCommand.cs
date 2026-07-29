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

                if (selectedIds.Count < 2)
                {
                    TaskDialog.Show("Tag Align", "Please select at least 2 tags or text notes to align.");
                    return Result.Cancelled;
                }

                List<IndependentTag> tags = new List<IndependentTag>();
                List<TextNote> textNotes = new List<TextNote>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem is IndependentTag tag)
                        tags.Add(tag);
                    else if (elem is TextNote tn)
                        textNotes.Add(tn);
                }

                if (tags.Count + textNotes.Count < 2)
                {
                    TaskDialog.Show("Tag Align", "Please select at least 2 tags or text notes.");
                    return Result.Cancelled;
                }

                var optionsWindow = new AlignOptionsWindow();
                if (optionsWindow.ShowDialog() != true)
                    return Result.Cancelled;

                AlignmentMode mode = optionsWindow.SelectedMode;
                double leaderAngle = optionsWindow.LeaderAngleDegrees * Math.PI / 180.0;

                using (Transaction tx = new Transaction(doc, "Align Tags"))
                {
                    tx.Start();

                    List<XYZ> headPositions = new List<XYZ>();

                    foreach (var tag in tags)
                    {
                        headPositions.Add(tag.TagHeadPosition);
                    }
                    foreach (var tn in textNotes)
                    {
                        headPositions.Add(tn.Coord);
                    }

                    XYZ alignmentTarget = ComputeAlignmentTarget(headPositions, mode);

                    foreach (var tag in tags)
                    {
                        AlignTag(tag, alignmentTarget, mode, leaderAngle);
                    }
                    foreach (var tn in textNotes)
                    {
                        AlignTextNote(tn, alignmentTarget, mode, leaderAngle);
                    }

                    tx.Commit();
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

        private XYZ ComputeAlignmentTarget(List<XYZ> positions, AlignmentMode mode)
        {
            switch (mode)
            {
                case AlignmentMode.AlignLeft:
                    double minX = positions.Min(p => p.X);
                    return new XYZ(minX, 0, 0);
                case AlignmentMode.AlignRight:
                    double maxX = positions.Max(p => p.X);
                    return new XYZ(maxX, 0, 0);
                case AlignmentMode.AlignTop:
                    double maxY = positions.Max(p => p.Y);
                    return new XYZ(0, maxY, 0);
                case AlignmentMode.AlignBottom:
                    double minY = positions.Min(p => p.Y);
                    return new XYZ(0, minY, 0);
                case AlignmentMode.AlignMiddleHorizontal:
                    double avgY = positions.Average(p => p.Y);
                    return new XYZ(0, avgY, 0);
                case AlignmentMode.AlignMiddleVertical:
                    double avgX = positions.Average(p => p.X);
                    return new XYZ(avgX, 0, 0);
                default:
                    return positions.First();
            }
        }

        private void AlignTag(IndependentTag tag, XYZ target, AlignmentMode mode, double leaderAngle)
        {
            XYZ currentPos = tag.TagHeadPosition;
            XYZ newPos = ComputeNewPosition(currentPos, target, mode);
            tag.TagHeadPosition = newPos;

            if (tag.HasLeader)
            {
                AdjustTagLeader(tag, leaderAngle);
            }
        }

        private void AlignTextNote(TextNote tn, XYZ target, AlignmentMode mode, double leaderAngle)
        {
            XYZ currentPos = tn.Coord;
            XYZ newPos = ComputeNewPosition(currentPos, target, mode);
            tn.Coord = newPos;
        }

        private XYZ ComputeNewPosition(XYZ current, XYZ target, AlignmentMode mode)
        {
            switch (mode)
            {
                case AlignmentMode.AlignLeft:
                case AlignmentMode.AlignRight:
                case AlignmentMode.AlignMiddleVertical:
                    return new XYZ(target.X, current.Y, current.Z);
                case AlignmentMode.AlignTop:
                case AlignmentMode.AlignBottom:
                case AlignmentMode.AlignMiddleHorizontal:
                    return new XYZ(current.X, target.Y, current.Z);
                default:
                    return current;
            }
        }

        private void AdjustTagLeader(IndependentTag tag, double angle)
        {
            // Adjust leader elbow to enforce parallel angles
            if (tag.LeaderEndCondition == LeaderEndCondition.Free)
            {
                XYZ headPos = tag.TagHeadPosition;
                XYZ leaderEnd = tag.GetLeaderEnd(tag.GetTaggedReferences().First());
                double dist = headPos.DistanceTo(leaderEnd);

                XYZ direction = new XYZ(Math.Cos(angle), Math.Sin(angle), 0);
                XYZ newElbow = headPos + direction * (dist * 0.5);

                tag.SetLeaderElbow(tag.GetTaggedReferences().First(), newElbow);
            }
        }
    }

    public enum AlignmentMode
    {
        AlignLeft,
        AlignRight,
        AlignTop,
        AlignBottom,
        AlignMiddleHorizontal,
        AlignMiddleVertical
    }
}
