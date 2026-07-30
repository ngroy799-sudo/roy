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
    /// </summary>
    public static class AlignmentEngine
    {
        public class AnnotationItem
        {
            public Element Element { get; set; }
            public XYZ OriginalHead { get; set; }
            public XYZ HostPoint { get; set; }
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

            double ang = Math.Atan2(dy, dx); // -PI..PI
            double absDeg = Math.Abs(ang) * 180.0 / Math.PI;
            if (absDeg > 90.0)
            {
                // Flip so we measure the acute leader angle from horizontal.
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

            // Red arrow direction (elbow → element). Prefer mouse pick; else build from slider + corner.
            XYZDir arrowDir;
            double angleAbsDeg;
            if (pickedAngle != null)
            {
                arrowDir = pickedAngle.Direction;
                angleAbsDeg = pickedAngle.AngleDegreesAbs;
                // Ensure arrow points toward host side (away from tag text side).
                if (tagsOnLeft && arrowDir.X < 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
                if (!tagsOnLeft && arrowDir.X > 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
            }
            else
            {
                double a = cfg.AngleDegrees * Math.PI / 180.0;
                double sx = tagsOnLeft ? 1.0 : -1.0;
                double sy = stackDown ? -1.0 : 1.0; // upper stack → arrows go down toward hosts
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

            double landingSign = tagsOnLeft ? 1.0 : -1.0; // landing extends from text toward hosts

            for (int i = 0; i < items.Count; i++)
            {
                int col = i / perColumn;
                int row = i % perColumn;

                double y = stackDown
                    ? tagPosition.Y - row * cfg.VerticalSpacingFt
                    : tagPosition.Y + row * cfg.VerticalSpacingFt;

                double x = tagPosition.X + col * cfg.HorizontalSpacingFt * (tagsOnLeft ? -1.0 : 1.0);
                XYZ head = new XYZ(x, y, tagPosition.Z);

                AnnotationItem item = items[i];
                XYZ host = item.HostPoint ?? head;

                double landing = cfg.ConstantLanding
                    ? cfg.LandingDistanceFt
                    : ComputeLandingFromHost(head, host, arrowDir, landingSign);

                XYZ elbow = new XYZ(head.X + landingSign * landing, head.Y, head.Z);

                // Free end on the angled ray so red segment angle is exact (when not attached).
                XYZ freeEnd = ProjectHostOntoArrow(elbow, host, arrowDir);

                if (item.Element is IndependentTag tag)
                    PlaceTag(tag, head, elbow, freeEnd, host, cfg, tagsOnLeft);
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
            // Place elbow so horizontal landing + angled ray aims near host.
            // Elbow.Y = head.Y; elbow.X chosen so (host - elbow) aligns with arrowDir.
            // host = elbow + t * arrowDir  =>  host.Y = head.Y + t * arrowDir.Y
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
            // Closest point on ray elbow + t*dir (t>=0) to host — keeps arrow angle exact.
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
            bool tagsOnLeft)
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
                catch { }
            }
            else
            {
                try
                {
                    tag.LeaderEndCondition = LeaderEndCondition.Free;
                    tag.SetLeaderEnd(firstRef, freeEnd);
                }
                catch
                {
                    try { tag.SetLeaderEnd(firstRef, originalHost); }
                    catch { }
                }
            }

            try { tag.SetLeaderElbow(firstRef, elbow); }
            catch { }
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
                    if (!cfg.AttachedEndTags)
                        leader.End = freeEnd;
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
