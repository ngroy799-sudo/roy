using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTagAlign
{
    /// <summary>
    /// Arranges tags / text notes into stacked columns with parallel leaders,
    /// matching Bird Tools Tag Alignment Tool behavior.
    /// </summary>
    public static class AlignmentEngine
    {
        public class AnnotationItem
        {
            public Element Element { get; set; }
            public XYZ OriginalHead { get; set; }
            public XYZ HostPoint { get; set; }
            public bool IsTag { get { return Element is IndependentTag; } }
        }

        public static void Align(
            Document doc,
            List<AnnotationItem> items,
            XYZ pickPoint,
            AlignConfig cfg)
        {
            if (items == null || items.Count == 0)
                return;

            bool stackDown = cfg.Corner == CornerAlignment.UpperLeft
                          || cfg.Corner == CornerAlignment.UpperRight;

            items = items
                .OrderBy(i => stackDown ? -i.OriginalHead.Y : i.OriginalHead.Y)
                .ThenBy(i => i.OriginalHead.X)
                .ToList();

            bool tagsOnLeft = IsTagsOnLeft(cfg);
            if (cfg.SwitchPickPointSide)
                tagsOnLeft = !tagsOnLeft;

            double angleRad = cfg.AngleDegrees * Math.PI / 180.0;

            int columnCount = 1;
            int perColumn = items.Count;
            if (cfg.IntermittentAlignment && cfg.HorizontalSpacingFt > 1e-9)
            {
                columnCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(items.Count)));
                perColumn = (int)Math.Ceiling(items.Count / (double)columnCount);
            }

            for (int i = 0; i < items.Count; i++)
            {
                int col = i / perColumn;
                int row = i % perColumn;

                XYZ head = ComputeHeadPosition(pickPoint, cfg, tagsOnLeft, col, row, angleRad, stackDown);
                AnnotationItem item = items[i];

                if (item.Element is IndependentTag tag)
                    PlaceTag(tag, head, item.HostPoint, cfg, tagsOnLeft, angleRad);
                else if (item.Element is TextNote tn)
                    PlaceTextNote(tn, head, item.HostPoint, cfg, tagsOnLeft, angleRad);
            }
        }

        private static bool IsTagsOnLeft(AlignConfig cfg)
        {
            return cfg.Corner == CornerAlignment.UpperLeft
                || cfg.Corner == CornerAlignment.LowerLeft;
        }

        private static XYZ ComputeHeadPosition(
            XYZ pick,
            AlignConfig cfg,
            bool tagsOnLeft,
            int col,
            int row,
            double angleRad,
            bool stackDown)
        {
            double landing = cfg.ConstantLanding
                ? cfg.LandingDistanceFt
                : Math.Max(0.5, cfg.VerticalSpacingFt * 2.0);

            // Diagonal run from pick (host side) up/down to the landing elbow height of row 0.
            double angledRun = Math.Abs(Math.Sin(angleRad)) < 1e-6
                ? cfg.VerticalSpacingFt
                : Math.Max(cfg.VerticalSpacingFt, cfg.VerticalSpacingFt / Math.Max(0.2, Math.Sin(angleRad)));

            // Tags sit opposite the pick-point side.
            double horizSign = tagsOnLeft ? -1.0 : 1.0;

            double dx = horizSign * (landing + Math.Cos(angleRad) * angledRun);
            double colShift = col * cfg.HorizontalSpacingFt * horizSign;

            double baseDy = Math.Abs(Math.Sin(angleRad) * angledRun);
            double dy = stackDown
                ? baseDy + row * cfg.VerticalSpacingFt
                : -(baseDy + row * cfg.VerticalSpacingFt);

            return new XYZ(pick.X + dx + colShift, pick.Y + dy, pick.Z);
        }

        private static void PlaceTag(
            IndependentTag tag,
            XYZ head,
            XYZ hostPoint,
            AlignConfig cfg,
            bool tagsOnLeft,
            double angleRad)
        {
            tag.TagHeadPosition = head;

            if (!tag.HasLeader)
                tag.HasLeader = true;

            IList<Reference> refs = tag.GetTaggedReferences();
            if (refs == null || refs.Count == 0)
                return;

            Reference firstRef = refs.First();

            if (cfg.AttachedEndTags)
            {
                try { tag.LeaderEndCondition = LeaderEndCondition.Attached; }
                catch { /* some categories disallow */ }
            }
            else
            {
                try
                {
                    tag.LeaderEndCondition = LeaderEndCondition.Free;
                    if (hostPoint != null)
                        tag.SetLeaderEnd(firstRef, hostPoint);
                }
                catch { /* ignore */ }
            }

            XYZ elbow = ComputeElbow(head, hostPoint ?? head, cfg, tagsOnLeft);
            try { tag.SetLeaderElbow(firstRef, elbow); }
            catch { /* ignore */ }
        }

        private static void PlaceTextNote(
            TextNote tn,
            XYZ head,
            XYZ hostPoint,
            AlignConfig cfg,
            bool tagsOnLeft,
            double angleRad)
        {
            tn.Coord = head;
            ApplyJustification(tn, cfg, tagsOnLeft);

            try
            {
                IList<Leader> leaders = tn.GetLeaders();
                if (leaders == null || leaders.Count == 0)
                    return;

                XYZ elbow = ComputeElbow(head, hostPoint ?? head, cfg, tagsOnLeft);
                foreach (Leader leader in leaders)
                {
                    leader.Elbow = elbow;
                    if (hostPoint != null && !cfg.AttachedEndTags)
                        leader.End = hostPoint;
                }
            }
            catch
            {
                // Head + justification still applied.
            }
        }

        private static void ApplyJustification(TextNote tn, AlignConfig cfg, bool tagsOnLeft)
        {
            switch (cfg.Justification)
            {
                case TextNoteJustificationMode.Left:
                    tn.HorizontalAlignment = HorizontalTextAlignment.Left;
                    break;
                case TextNoteJustificationMode.Right:
                    tn.HorizontalAlignment = HorizontalTextAlignment.Right;
                    break;
                case TextNoteJustificationMode.Automatic:
                    tn.HorizontalAlignment = tagsOnLeft
                        ? HorizontalTextAlignment.Right
                        : HorizontalTextAlignment.Left;
                    break;
            }
        }

        private static XYZ ComputeElbow(XYZ head, XYZ host, AlignConfig cfg, bool tagsOnLeft)
        {
            double landing = cfg.ConstantLanding
                ? cfg.LandingDistanceFt
                : Math.Max(0.25, Math.Abs(host.X - head.X) * 0.35);

            double horizSign = tagsOnLeft ? 1.0 : -1.0;
            return new XYZ(head.X + horizSign * landing, head.Y, head.Z);
        }

        public static XYZ GetTagHostPoint(IndependentTag tag)
        {
            try
            {
                IList<Reference> refs = tag.GetTaggedReferences();
                if (refs != null && refs.Count > 0 && tag.HasLeader)
                    return tag.GetLeaderEnd(refs.First());
            }
            catch { }

            return tag.TagHeadPosition;
        }

        public static XYZ GetTextNoteHostPoint(TextNote tn)
        {
            try
            {
                IList<Leader> leaders = tn.GetLeaders();
                if (leaders != null)
                {
                    foreach (Leader leader in leaders)
                        return leader.End;
                }
            }
            catch { }

            return tn.Coord;
        }
    }
}
