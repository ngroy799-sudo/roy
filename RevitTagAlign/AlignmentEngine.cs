using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTagAlign
{
    /// <summary>
    /// Bird Tools–style geometry (view-aware), matching official Help + Configure diagrams:
    /// - Pick = taghead of the tag closest to tagged elements
    /// - Upper: that tag at stack bottom; others grow +Up
    /// - Lower: that tag at stack top; others grow -Up
    /// - Yellow = horizontal landing along view Right (aligned)
    /// - Red = parallel angled arrows (common-angle mode) toward hosts
    /// - Constant Landing ON: fixed landing length; angled segments aim at hosts (not common angle)
    /// - Leader end: pinned to original host contact point (face does not switch)
    ///   unless Force Attached is on
    /// - Stack uses full view Right/Up offsets (no Z wipe — required for sections)
    /// </summary>
    public static class AlignmentEngine
    {
        public class AnnotationItem
        {
            public Element Element { get; set; }
            public XYZ OriginalHead { get; set; }
            /// <summary>Original leader end on the host (contact point / face). Never relocate.</summary>
            public XYZ HostPoint { get; set; }
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

        public static PickedAngle ComputeAngleFromReferenceAndPoint(XYZ reference, XYZ anglePoint)
        {
            return ComputeAngleFromTwoPoints(reference, anglePoint);
        }

        /// <summary>
        /// Angle in the active view plane: project points onto view Right/Up, then measure.
        /// </summary>
        public static PickedAngle ComputeAngleInView(View view, XYZ reference, XYZ anglePoint)
        {
            if (view == null)
                return ComputeAngleFromTwoPoints(reference, anglePoint);

            XYZ right = view.RightDirection.Normalize();
            XYZ up = view.UpDirection.Normalize();
            XYZ delta = anglePoint - reference;
            double x = delta.DotProduct(right);
            double y = delta.DotProduct(up);
            XYZ dirWorld = (right * x + up * y);
            if (dirWorld.GetLength() < 1e-9)
                return ComputeAngleFromTwoPoints(reference, anglePoint);

            XYZ p2 = reference + dirWorld;
            return ComputeAngleFromTwoPoints(reference, p2);
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
            View view = doc != null ? doc.ActiveView : null;
            AlignInView(doc, view, items, tagPosition, cfg, pickedAngle);
        }

        /// <summary>
        /// Place stack so <paramref name="tagPosition"/> is the taghead of the
        /// tag closest to the tagged elements (Bird Tools Help / Configure •).
        /// </summary>
        public static void AlignInView(
            Document doc,
            View view,
            List<AnnotationItem> items,
            XYZ tagPosition,
            AlignConfig cfg,
            PickedAngle pickedAngle)
        {
            if (items == null || items.Count == 0 || tagPosition == null || cfg == null)
                return;

            XYZ right = view != null ? view.RightDirection.Normalize() : XYZ.BasisX;
            XYZ up = view != null ? view.UpDirection.Normalize() : XYZ.BasisY;

            // Upper: closest tag at bottom, stack grows +Up.
            // Lower: closest tag at top, stack grows -Up.
            bool isUpper = cfg.Corner == CornerAlignment.UpperLeft
                        || cfg.Corner == CornerAlignment.UpperRight;
            double stackAwaySign = isUpper ? 1.0 : -1.0;

            // Order so index 0 = closest-to-hosts tag for this corner.
            // Upper hosts are "below" → lowest original heads first.
            // Lower hosts are "above" → highest original heads first.
            items = items
                .OrderBy(i =>
                {
                    XYZ h = i.OriginalHead ?? XYZ.Zero;
                    double u = h.DotProduct(up);
                    return isUpper ? u : -u;
                })
                .ThenBy(i =>
                {
                    XYZ h = i.OriginalHead ?? XYZ.Zero;
                    return h.DotProduct(right);
                })
                .ToList();

            bool tagsOnLeft;
            XYZ arrowWorld;
            ResolveSideAndArrow(cfg, pickedAngle, isUpper, right, up, out tagsOnLeft, out arrowWorld);

            // Landing toward host side along view right (left-side tags → landing goes +Right).
            double landingSign = tagsOnLeft ? 1.0 : -1.0;
            XYZ landingDir = right * landingSign;

            int perColumn = items.Count;
            int columnCount = 1;
            if (cfg.IntermittentAlignment && cfg.HorizontalSpacingFt > 1e-9)
            {
                columnCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(items.Count)));
                perColumn = (int)Math.Ceiling(items.Count / (double)columnCount);
            }

            double step = ComputeStackStep(items, view, cfg);
            double landing = ResolveUniformLanding(items, tagPosition, landingDir, arrowWorld, cfg);
            bool commonAngle = !cfg.ConstantLanding;

            for (int i = 0; i < items.Count; i++)
            {
                int col = i / perColumn;
                int row = i % perColumn;

                // row 0 = closest tag at pick; others grow away from hosts.
                double alongUp = stackAwaySign * row * step;
                double alongRight = 0.0;
                if (cfg.IntermittentAlignment)
                    alongRight = col * cfg.HorizontalSpacingFt * (tagsOnLeft ? -1.0 : 1.0);

                // Full view-plane offset — do NOT clamp Z (section Up is often world Z).
                XYZ head = tagPosition + up * alongUp + right * alongRight;

                // Pin end to original host contact point so the face never switches.
                XYZ host = items[i].HostPoint ?? head;
                XYZ elbow = ComputeElbow(head, host, landingDir, landing, arrowWorld, commonAngle);

                ApplyItemGeometry(items[i], head, elbow, host, host, cfg, tagsOnLeft);
            }
        }

        /// <summary>
        /// After angle click: update leaders only (keep current head positions).
        /// </summary>
        public static void PreviewAngleAtCurrentPositions(
            List<AnnotationItem> items,
            AlignConfig cfg,
            PickedAngle pickedAngle,
            View view)
        {
            if (items == null || items.Count == 0 || cfg == null)
                return;

            XYZ right = view != null ? view.RightDirection.Normalize() : XYZ.BasisX;
            XYZ up = view != null ? view.UpDirection.Normalize() : XYZ.BasisY;
            bool isUpper = cfg.Corner == CornerAlignment.UpperLeft
                        || cfg.Corner == CornerAlignment.UpperRight;

            bool tagsOnLeft;
            XYZ arrowWorld;
            ResolveSideAndArrow(cfg, pickedAngle, isUpper, right, up, out tagsOnLeft, out arrowWorld);

            double landingSign = tagsOnLeft ? 1.0 : -1.0;
            XYZ landingDir = right * landingSign;
            double landing = cfg.ConstantLanding
                ? Math.Max(0.1, cfg.LandingDistanceFt)
                : Math.Max(0.25, cfg.LandingDistanceFt > 0 ? cfg.LandingDistanceFt * 0.5 : 1.0);
            bool commonAngle = !cfg.ConstantLanding;

            foreach (AnnotationItem item in items)
            {
                XYZ head = GetCurrentHead(item);
                if (head == null) continue;
                XYZ host = item.HostPoint ?? head;
                XYZ elbow = ComputeElbow(head, host, landingDir, landing, arrowWorld, commonAngle);
                ApplyItemGeometry(item, head, elbow, host, host, cfg, tagsOnLeft);
            }
        }

        /// <summary>
        /// Horizontal landing from head; when commonAngle, nudge elbow so the
        /// angled segment stays parallel while the free end stays on the original host point.
        /// </summary>
        private static XYZ ComputeElbow(
            XYZ head,
            XYZ host,
            XYZ landingDir,
            double landing,
            XYZ arrowWorld,
            bool commonAngle)
        {
            XYZ elbow = head + landingDir * landing;
            if (!commonAngle || arrowWorld == null || arrowWorld.GetLength() < 1e-9)
                return elbow;

            // Prefer elbow on ray host - t*arrow so (host - elbow) || arrow, while
            // landing stays as horizontal as possible (match head along "up" via arrow).
            XYZ arrow = arrowWorld.Normalize();
            XYZ L = landingDir.Normalize();
            // Build an "up-ish" axis perpendicular to L in the plane of L and arrow.
            XYZ upApprox = arrow - L.Multiply(arrow.DotProduct(L));
            if (upApprox.GetLength() < 1e-9)
                return elbow;
            upApprox = upApprox.Normalize();

            // Solve: host - t*arrow = head + k*L  ⇒  (host-head)·upApprox = t*(arrow·upApprox)
            double denom = arrow.DotProduct(upApprox);
            if (Math.Abs(denom) < 1e-9)
                return elbow;

            double t = (host - head).DotProduct(upApprox) / denom;
            if (t < 0.1)
                t = 0.1;

            XYZ elbowParallel = host - arrow.Multiply(t);
            // Keep landing on the host side of the head.
            double alongLand = (elbowParallel - head).DotProduct(L);
            if (alongLand < 0.05)
                return elbow;

            return elbowParallel;
        }

        /// <summary>
        /// Center-to-center stack step from Configure → Vertical Spacing (mm→ft).
        /// Never smaller than tallest tag height + padding, so texts cannot overlap.
        /// </summary>
        private static double ComputeStackStep(List<AnnotationItem> items, View view, AlignConfig cfg)
        {
            // User spacing is free to set; clamp only pathological zeros.
            double user = Math.Max(0.01, cfg.VerticalSpacingFt);
            double maxH = 0.0;
            foreach (var item in items)
                maxH = Math.Max(maxH, EstimateAnnotationHeight(item.Element, view));

            // ~15mm padding beyond bbox so glyphs do not touch.
            const double padding = 0.05;
            double minNoOverlap = Math.Max(0.15, maxH + padding);
            return Math.Max(user, minNoOverlap);
        }

        private static double EstimateAnnotationHeight(Element elem, View view)
        {
            try
            {
                BoundingBoxXYZ bb = view != null ? elem.get_BoundingBox(view) : elem.get_BoundingBox(null);
                if (bb == null)
                    return 0.35; // ~107mm fallback

                XYZ up = view != null ? view.UpDirection.Normalize() : XYZ.BasisY;
                double minU = double.MaxValue;
                double maxU = double.MinValue;
                XYZ[] corners =
                {
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Min.Z),
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Max.Z),
                    new XYZ(bb.Min.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Min.X, bb.Max.Y, bb.Max.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Min.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Max.Z),
                    new XYZ(bb.Max.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Max.X, bb.Max.Y, bb.Max.Z),
                };
                foreach (XYZ c in corners)
                {
                    double u = c.DotProduct(up);
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                }
                double h = maxU - minU;
                if (h < 0.1) h = 0.35;
                return h;
            }
            catch
            {
                return 0.35;
            }
        }

        private static double ResolveUniformLanding(
            List<AnnotationItem> items,
            XYZ tagPosition,
            XYZ landingDir,
            XYZ arrowWorld,
            AlignConfig cfg)
        {
            if (cfg.ConstantLanding)
                return Math.Max(0.1, cfg.LandingDistanceFt);

            // Auto: stable yellow landing from average host distance along landing.
            double sum = 0;
            int n = 0;
            foreach (var item in items)
            {
                XYZ host = item.HostPoint ?? tagPosition;
                XYZ delta = host - tagPosition;
                double along = delta.DotProduct(landingDir);
                if (along > 0.1)
                {
                    sum += along * 0.35;
                    n++;
                }
            }
            double auto = n > 0 ? sum / n : 1.0;
            return Math.Max(0.25, Math.Min(auto, 10.0));
        }

        private static void ResolveSideAndArrow(
            AlignConfig cfg,
            PickedAngle pickedAngle,
            bool isUpper,
            XYZ right,
            XYZ up,
            out bool tagsOnLeft,
            out XYZ arrowWorld)
        {
            tagsOnLeft = IsTagsOnLeft(cfg);
            if (cfg.SwitchPickPointSide)
                tagsOnLeft = !tagsOnLeft;

            // Arrow from elbow toward hosts: +Right when tags on left; -Up when Upper, +Up when Lower.
            double sx = tagsOnLeft ? 1.0 : -1.0;
            double sy = isUpper ? -1.0 : 1.0;

            if (pickedAngle != null)
            {
                XYZDir arrowDir = pickedAngle.Direction;
                if (tagsOnLeft && arrowDir.X < 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
                if (!tagsOnLeft && arrowDir.X > 0) { arrowDir = new XYZDir(-arrowDir.X, -arrowDir.Y); }
                cfg.AngleDegrees = pickedAngle.AngleDegreesAbs;

                XYZ raw = new XYZ(arrowDir.X, arrowDir.Y, 0);
                double ar = raw.DotProduct(right);
                double au = raw.DotProduct(up);
                if (Math.Abs(ar) + Math.Abs(au) > 1e-9)
                    arrowWorld = (right * ar + up * au).Normalize();
                else
                    arrowWorld = (right * sx + up * sy * 0.7).Normalize();
            }
            else
            {
                double a = Math.Max(0.0, Math.Min(90.0, cfg.AngleDegrees)) * Math.PI / 180.0;
                // In view plane: horizontal component Cos(a) toward hosts, vertical Sin(a) toward hosts.
                arrowWorld = (right * (sx * Math.Cos(a)) + up * (sy * Math.Sin(a))).Normalize();
            }
        }

        private static XYZ GetCurrentHead(AnnotationItem item)
        {
            if (item.Element is IndependentTag tag)
                return tag.TagHeadPosition;
            if (item.Element is TextNote tn)
                return tn.Coord;
            return item.OriginalHead;
        }

        private static void ApplyItemGeometry(
            AnnotationItem item,
            XYZ head,
            XYZ elbow,
            XYZ freeEnd,
            XYZ host,
            AlignConfig cfg,
            bool tagsOnLeft)
        {
            if (item.Element is IndependentTag tag)
                PlaceTag(tag, head, elbow, freeEnd, host, cfg, item);
            else if (item.Element is TextNote tn)
                PlaceTextNote(tn, head, elbow, freeEnd, host, cfg, tagsOnLeft);
        }

        private static bool IsTagsOnLeft(AlignConfig cfg)
        {
            return cfg.Corner == CornerAlignment.UpperLeft
                || cfg.Corner == CornerAlignment.LowerLeft;
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

            // Unless Force Attached: always Free + pin to original contact point
            // so Revit cannot re-pick another face (left → top/bottom/right).
            LeaderEndCondition desired = cfg.AttachedEndTags
                ? LeaderEndCondition.Attached
                : LeaderEndCondition.Free;

            try
            {
                if (tag.LeaderEndCondition != desired)
                    tag.LeaderEndCondition = desired;
            }
            catch { }

            XYZ pin = originalHost ?? freeEnd ?? item.HostPoint;
            if (desired == LeaderEndCondition.Free && pin != null)
            {
                try { tag.SetLeaderEnd(firstRef, pin); }
                catch { }
            }

            try { tag.SetLeaderElbow(firstRef, elbow); }
            catch { }

            // Re-assert end after elbow — some Revit builds move the end when elbow changes.
            if (desired == LeaderEndCondition.Free && pin != null)
            {
                try { tag.SetLeaderEnd(firstRef, pin); }
                catch { }
            }
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

                XYZ pin = originalHost ?? freeEnd;
                foreach (Leader leader in leaders)
                {
                    leader.Elbow = elbow;
                    if (cfg.AttachedEndTags)
                        continue;
                    if (pin == null)
                        continue;
                    try { leader.End = pin; }
                    catch { }
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
                {
                    // Works for Free and Attached — captures the contact point on the chosen face.
                    XYZ end = tag.GetLeaderEnd(refs.First());
                    if (end != null)
                        return end;
                }
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
