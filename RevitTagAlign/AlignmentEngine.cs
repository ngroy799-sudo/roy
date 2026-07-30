using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTagAlign
{
    /// <summary>
    /// Bird Tools–style geometry:
    /// - Tag text / head position (stack)
    /// - Yellow = horizontal landing (head → elbow)
    /// - Red = angled arrow (elbow → host) at a common parallel angle
    /// - Leader end condition: preserve original unless Force Attached is on
    /// </summary>
    public static class AlignmentEngine
    {
        public class AnnotationItem
        {
            public Element Element { get; set; }
            public XYZ OriginalHead { get; set; }
            public XYZ HostPoint { get; set; }
            /// <summary>Original IndependentTag leader end condition (Attached/Free). Null for text notes.</summary>
            public LeaderEndCondition? OriginalLeaderEndCondition { get; set; }
        }

        public static PickedAngle ComputeAngleFromTwoPoints(XYZ p1, XYZ p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-9)
            {
                return new PickedAngle
                {
                    AngleRadians = Math.PI / 4.0,
                    AngleDegreesAbs = 45.0,
                    Direction = new XYZDir(Math.Cos(Math.PI / 4.0), -Math.Sin(Math.PI / 4.0))
                };
            }

            dx /= len;
            dy /= len;

            double ang = Math.Atan2(dy, dx);
            double absDeg = Math.Abs(ang) * 180.0 / Math.PI;
            if (absDeg > 90.0)
            {
                dx = -dx;
                dy = -dy;
                ang = Math.Atan2(dy, dx);
                absDeg = Math.Abs(ang) * 180.0 / Math.PI;
            }

            absDeg = Math.Max(0.0, Math.Min(90.0, absDeg));

            return new PickedAngle
            {
                AngleRadians = ang,
                AngleDegreesAbs = absDeg,
                Direction = new XYZDir(dx, dy)
            };
        }

        /// <summary>
        /// 2-click angle: direction from reference (host centroid) toward the picked angle point.
        /// </summary>
        public static PickedAngle ComputeAngleFromReferenceAndPoint(XYZ reference, XYZ anglePoint)
        {
            return ComputeAngleFromTwoPoints(reference, anglePoint);
        }

        public static XYZ AverageHostPoint(IList<AnnotationItem> items)
        {
            if (items == null || items.Count == 0)
                return XYZ.Zero;

            double x = 0, y = 0, z = 0;
            int n = 0;
            foreach (var item in items)
            {
                XYZ p = item.HostPoint ?? item.OriginalHead;
                if (p == null) continue;
                x += p.X; y += p.Y; z += p.Z;
                n++;
            }
            if (n == 0) return XYZ.Zero;
            return new XYZ(x / n, y / n, z / n);
        }

        public static void Align(
            Document doc,
            List<AnnotationItem> items,
            XYZ tagPosition,
            AlignConfig cfg,
            PickedAngle pickedAngle)
        {
            if (items == null || items.Count == 0 || tagPosition == null)
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

            XYZDir arrowDir;
            double angleAbsDeg;
            if (pickedAngle != null)
            {
                arrowDir = pickedAngle.Direction;
                angleAbsDeg = pickedAngle.AngleDegreesAbs;
                if (tagsOnLeft && arrowDir.X < 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
                if (!tagsOnLeft && arrowDir.X > 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
            }
            else
            {
                double a = cfg.AngleDegrees * Math.PI / 180.0;
                double sx = tagsOnLeft ? 1.0 : -1.0;
                double sy = stackDown ? -1.0 : 1.0;
                arrowDir = new XYZDir(sx * Math.Cos(a), sy * Math.Sin(a));
                angleAbsDeg = cfg.AngleDegrees;
            }

            cfg.AngleDegrees = angleAbsDeg;

            int perColumn = items.Count;
            if (cfg.IntermittentAlignment && cfg.HorizontalSpacingFt > 1e-9)
            {
                int columnCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(items.Count)));
                perColumn = (int)Math.Ceiling(items.Count / (double)columnCount);
            }

            double landingSign = tagsOnLeft ? 1.0 : -1.0;

            for (int i = 0; i < items.Count; i++)
            {
                int col = i / perColumn;
                int row = i % perColumn;

                double y = stackDown
                    ? tagPosition.Y - row * cfg.VerticalSpacingFt
                    : tagPosition.Y + row * cfg.VerticalSpacingFt;

                double x = tagPosition.X;
                if (cfg.IntermittentAlignment)
                    x = tagPosition.X + col * cfg.HorizontalSpacingFt * (tagsOnLeft ? -1.0 : 1.0);

                XYZ head = new XYZ(x, y, tagPosition.Z);

                AnnotationItem item = items[i];
                XYZ host = item.HostPoint ?? head;

                double landing = cfg.ConstantLanding
                    ? cfg.LandingDistanceFt
                    : ComputeLandingFromHost(head, host, arrowDir, landingSign);

                XYZ elbow = new XYZ(head.X + landingSign * landing, head.Y, head.Z);
                XYZ freeEnd = ProjectHostOntoArrow(elbow, host, arrowDir);

                if (item.Element is IndependentTag tag)
                    PlaceTag(tag, head, elbow, freeEnd, host, cfg, item);
                else if (item.Element is TextNote tn)
                    PlaceTextNote(tn, head, elbow, freeEnd, host, cfg, tagsOnLeft);
            }
        }

        private static bool IsTagsOnLeft(AlignConfig cfg)
        {
            return cfg.Corner == CornerAlignment.UpperLeft
                || cfg.Corner == CornerAlignment.LowerLeft;
        }

        private static double ComputeLandingFromHost(XYZ head, XYZ host, XYZDir arrowDir, double landingSign)
        {
            if (Math.Abs(arrowDir.Y) < 1e-9)
                return Math.Max(0.25, Math.Abs(host.X - head.X) * 0.5);

            double t = (host.Y - head.Y) / arrowDir.Y;
            if (t < 0) t = Math.Abs(t);
            double elbowX = host.X - t * arrowDir.X;
            double landing = (elbowX - head.X) * landingSign;
            if (landing < 0.1) landing = Math.Max(0.25, Math.Abs(host.X - head.X) * 0.35);
            return landing;
        }

        private static XYZ ProjectHostOntoArrow(XYZ elbow, XYZ host, XYZDir arrowDir)
        {
            XYZ dir = new XYZ(arrowDir.X, arrowDir.Y, 0);
            XYZ toHost = host - elbow;
            double t = toHost.DotProduct(dir);
            if (t < 0.1) t = Math.Max(0.5, toHost.GetLength());
            return elbow + dir.Multiply(t);
        }

        private static void PlaceTag(
            IndependentTag tag,
            XYZ head,
            XYZ elbow,
            XYZ freeEnd,
            XYZ originalHost,
            AlignConfig cfg,
            AnnotationItem item)
        {
            tag.TagHeadPosition = head;

            if (!tag.HasLeader)
                tag.HasLeader = true;

            IList<Reference> refs = tag.GetTaggedReferences();
            if (refs == null || refs.Count == 0)
                return;

            Reference firstRef = refs.First();

            // Preserve original Attached/Free unless user forces Attached.
            LeaderEndCondition desired = ResolveEndCondition(cfg, item.OriginalLeaderEndCondition);

            try
            {
                if (tag.LeaderEndCondition != desired)
                    tag.LeaderEndCondition = desired;
            }
            catch { /* some tags disallow changing end condition */ }

            if (desired == LeaderEndCondition.Free)
            {
                try { tag.SetLeaderEnd(firstRef, freeEnd); }
                catch
                {
                    try { tag.SetLeaderEnd(firstRef, originalHost); }
                    catch { }
                }
            }
            // Attached: do not SetLeaderEnd — keep host attachment.

            try { tag.SetLeaderElbow(firstRef, elbow); }
            catch { }
        }

        private static LeaderEndCondition ResolveEndCondition(
            AlignConfig cfg,
            LeaderEndCondition? original)
        {
            if (cfg.AttachedEndTags)
                return LeaderEndCondition.Attached;

            // Keep original setting (Attached or Free). Default to Attached if unknown.
            if (original.HasValue)
                return original.Value;

            return LeaderEndCondition.Attached;
        }

        private static void PlaceTextNote(
            TextNote tn,
            XYZ head,
            XYZ elbow,
            XYZ freeEnd,
            XYZ originalHost,
            AlignConfig cfg,
            bool tagsOnLeft)
        {
            tn.Coord = head;
            ApplyJustification(tn, cfg, tagsOnLeft);

            try
            {
                IList<Leader> leaders = tn.GetLeaders();
                if (leaders == null || leaders.Count == 0)
                    return;

                foreach (Leader leader in leaders)
                {
                    leader.Elbow = elbow;
                    // Text notes: only move end when forcing free-style placement and not preserving attach.
                    // Keep original end when AttachedEndTags is false (preserve).
                    if (cfg.AttachedEndTags)
                        continue;
                    // Preserve text note leader end unless it was free-style — leave End as originalHost.
                    // Do not overwrite with freeEnd projection when preserving.
                }
            }
            catch { }
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

        public static LeaderEndCondition? GetTagLeaderEndCondition(IndependentTag tag)
        {
            try
            {
                if (tag.HasLeader)
                    return tag.LeaderEndCondition;
            }
            catch { }
            return null;
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
