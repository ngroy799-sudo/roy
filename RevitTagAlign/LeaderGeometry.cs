using System;

namespace RevitTagAlign
{
    /// <summary>
    /// Pure view-plane math matching Bird Tools Tag Alignment Tool v1.4
    /// (https://www.youtube.com/watch?v=YVjbYY0tf6E):
    /// stacked tagheads, parallel angled leaders (per-tag adaptive landing + red lengths in common-angle mode),
    /// ends snapped to the ORIGINAL host face (never switches, never flies).
    /// No Revit types — unit-tested independently.
    /// </summary>
    public enum HostFaceKind
    {
        Left = 0,
        Right = 1,
        Bottom = 2,
        Top = 3
    }

    public struct V3
    {
        public double X;
        public double Y;
        public double Z;

        public V3(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public static V3 operator +(V3 a, V3 b) { return new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static V3 operator -(V3 a, V3 b) { return new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static V3 operator *(V3 a, double s) { return new V3(a.X * s, a.Y * s, a.Z * s); }
        public static V3 operator *(double s, V3 a) { return a * s; }

        public double Dot(V3 b) { return X * b.X + Y * b.Y + Z * b.Z; }

        public double Length
        {
            get { return Math.Sqrt(X * X + Y * Y + Z * Z); }
        }

        public V3 Normalize()
        {
            double len = Length;
            if (len < 1e-12)
                return new V3(1, 0, 0);
            return this * (1.0 / len);
        }

        public static V3 Cross(V3 a, V3 b)
        {
            return new V3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public double DistanceTo(V3 other)
        {
            return (this - other).Length;
        }
    }

    public static class LeaderGeometry
    {
        public const double Eps = 1e-9;

        /// <summary>
        /// Common-angle red leader direction (Bird Tools v1.4).
        /// Angle is measured at the elbow between horizontal landing and red segment — not between tags.
        /// </summary>
        public static V3 CommonAngleArrow(V3 right, V3 up, bool tagsOnLeft, bool isUpper, double angleDegrees)
        {
            double sx = tagsOnLeft ? 1.0 : -1.0;
            double sy = isUpper ? -1.0 : 1.0;
            double a = Math.Max(0.0, Math.Min(90.0, angleDegrees)) * Math.PI / 180.0;
            return (right * (sx * Math.Cos(a)) + up * (sy * Math.Sin(a))).Normalize();
        }

        /// <summary>
        /// Absolute angle (degrees) between horizontal landing and red leader at the elbow.
        /// </summary>
        public static double ElbowAngleDegrees(V3 landingDir, V3 redDir)
        {
            V3 h = landingDir.Normalize();
            V3 r = redDir.Normalize();
            double dot = Math.Max(-1.0, Math.Min(1.0, Math.Abs(h.Dot(r))));
            return Math.Acos(dot) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Sort key for stack row 0 = lowest host (Upper corners) or highest (Lower corners).
        /// Matches AlignmentEngine host-height ordering.
        /// </summary>
        public static int CompareHostStackOrder(V3 hostA, V3 hostB, V3 right, V3 up, bool isUpper)
        {
            double uA = hostA.Dot(up);
            double uB = hostB.Dot(up);
            if (isUpper)
            {
                int c = uA.CompareTo(uB);
                if (c != 0) return c;
            }
            else
            {
                int c = uB.CompareTo(uA);
                if (c != 0) return c;
            }
            return hostA.Dot(right).CompareTo(hostB.Dot(right));
        }

        public static V3 StackHead(V3 pick, V3 up, V3 right, int row, double step, double stackAwaySign, double alongRight)
        {
            return pick + up * (stackAwaySign * row * step) + right * alongRight;
        }

        public static V3 ElbowFromHead(V3 head, V3 landingDir, double landing)
        {
            return head + landingDir.Normalize() * Math.Max(0.05, landing);
        }

        /// <summary>
        /// Adaptive common-angle leader: horizontal landing length L and red length t vary so
        /// head → elbow (horizontal) → end (along parallel arrow) meets the pinned end.
        /// head + landingDir·L + arrow·t = end
        /// </summary>
        public static bool TrySolveAdaptiveElbow(
            V3 head,
            V3 end,
            V3 landingDir,
            V3 arrow,
            out V3 elbow,
            double minLanding = 0.05,
            double minRed = 0.02)
        {
            elbow = head;
            V3 ld = landingDir.Normalize();
            V3 ad = arrow.Normalize();
            V3 delta = end - head;

            double a12 = ld.Dot(ad);
            double det = 1.0 - a12 * a12;
            if (Math.Abs(det) < 1e-12)
                return false;

            double b1 = delta.Dot(ld);
            double b2 = delta.Dot(ad);
            double landing = (b1 - a12 * b2) / det;
            double red = (b2 - a12 * b1) / det;

            if (red < minRed || landing < minLanding)
                return false;

            elbow = head + ld * landing;
            return true;
        }

        /// <summary>Common-angle geometry with per-tag adaptive landing + red lengths.</summary>
        public static bool TryComputeAdaptiveCommonAngleLeader(
            V3 head,
            V3 hostPoint,
            V3 bbMin,
            V3 bbMax,
            V3 landingDir,
            V3 arrow,
            V3 right,
            V3 up,
            out V3 elbow,
            out V3 end,
            double fallbackLanding = 1.0)
        {
            HostFaceKind face = ClassifyFace(hostPoint, bbMin, bbMax, right, up);
            end = ClampPointToOriginalFace(hostPoint, bbMin, bbMax, face, right, up);
            if (TrySolveAdaptiveElbow(head, end, landingDir, arrow, out elbow))
                return true;

            elbow = ElbowFromHead(head, landingDir, fallbackLanding);
            end = SnapEndToOriginalFace(elbow, arrow, bbMin, bbMax, face, right, up);
            return false;
        }

        /// <summary>Which of the four view-aligned bbox faces the contact point sits on.</summary>
        public static HostFaceKind ClassifyFace(V3 point, V3 bbMin, V3 bbMax, V3 right, V3 up)
        {
            Range ru = ProjectRange(bbMin, bbMax, right);
            Range uu = ProjectRange(bbMin, bbMax, up);
            double r = point.Dot(right);
            double u = point.Dot(up);

            double dL = Math.Abs(r - ru.Min);
            double dR = Math.Abs(r - ru.Max);
            double dB = Math.Abs(u - uu.Min);
            double dT = Math.Abs(u - uu.Max);

            double best = dL;
            HostFaceKind face = HostFaceKind.Left;
            if (dR < best) { best = dR; face = HostFaceKind.Right; }
            if (dB < best) { best = dB; face = HostFaceKind.Bottom; }
            if (dT < best) { face = HostFaceKind.Top; }
            return face;
        }

        /// <summary>
        /// Common-angle mode (video default): ray from elbow along <paramref name="arrow"/>,
        /// snapped onto the original host face. End stays on that face of the bbox —
        /// never another face, never far outside the element.
        /// </summary>
        public static V3 SnapEndToOriginalFace(
            V3 elbow,
            V3 arrow,
            V3 bbMin,
            V3 bbMax,
            HostFaceKind face,
            V3 right,
            V3 up)
        {
            V3 n = V3.Cross(right, up).Normalize();
            Range rr = ProjectRange(bbMin, bbMax, right);
            Range uu = ProjectRange(bbMin, bbMax, up);
            Range nn = ProjectRange(bbMin, bbMax, n);

            double rFace, uFace;
            bool verticalFace = face == HostFaceKind.Left || face == HostFaceKind.Right;
            if (face == HostFaceKind.Left) rFace = rr.Min;
            else if (face == HostFaceKind.Right) rFace = rr.Max;
            else rFace = Clamp(elbow.Dot(right), rr.Min, rr.Max);

            if (face == HostFaceKind.Bottom) uFace = uu.Min;
            else if (face == HostFaceKind.Top) uFace = uu.Max;
            else uFace = Clamp(elbow.Dot(up), uu.Min, uu.Max);

            V3 dir = arrow.Normalize();
            double t;
            if (verticalFace)
            {
                double den = dir.Dot(right);
                if (Math.Abs(den) < 1e-8)
                    t = -1;
                else
                    t = (rFace - elbow.Dot(right)) / den;
            }
            else
            {
                double den = dir.Dot(up);
                if (Math.Abs(den) < 1e-8)
                    t = -1;
                else
                    t = (uFace - elbow.Dot(up)) / den;
            }

            double r, u, depth;
            if (t >= 0.02)
            {
                V3 hit = elbow + dir * t;
                r = verticalFace ? rFace : Clamp(hit.Dot(right), rr.Min, rr.Max);
                u = verticalFace ? Clamp(hit.Dot(up), uu.Min, uu.Max) : uFace;
                depth = Clamp(hit.Dot(n), nn.Min, nn.Max);
            }
            else
            {
                // Ray missed / went backwards: closest point still ON the original face.
                r = verticalFace ? rFace : Clamp(elbow.Dot(right), rr.Min, rr.Max);
                u = verticalFace ? Clamp(elbow.Dot(up), uu.Min, uu.Max) : uFace;
                depth = Clamp(elbow.Dot(n), nn.Min, nn.Max);
            }

            return right * r + up * u + n * depth;
        }

        /// <summary>
        /// Constant-landing mode: aim at original contact, then clamp onto the original face.
        /// Angles may differ; landing length stays uniform.
        /// </summary>
        public static V3 ClampPointToOriginalFace(
            V3 target,
            V3 bbMin,
            V3 bbMax,
            HostFaceKind face,
            V3 right,
            V3 up)
        {
            V3 n = V3.Cross(right, up).Normalize();
            Range rr = ProjectRange(bbMin, bbMax, right);
            Range uu = ProjectRange(bbMin, bbMax, up);
            Range nn = ProjectRange(bbMin, bbMax, n);

            double r = Clamp(target.Dot(right), rr.Min, rr.Max);
            double u = Clamp(target.Dot(up), uu.Min, uu.Max);
            double depth = Clamp(target.Dot(n), nn.Min, nn.Max);

            if (face == HostFaceKind.Left) r = rr.Min;
            else if (face == HostFaceKind.Right) r = rr.Max;
            else if (face == HostFaceKind.Bottom) u = uu.Min;
            else if (face == HostFaceKind.Top) u = uu.Max;

            return right * r + up * u + n * depth;
        }

        public static bool PointOnFace(V3 p, V3 bbMin, V3 bbMax, HostFaceKind face, V3 right, V3 up, double tol)
        {
            Range rr = ProjectRange(bbMin, bbMax, right);
            Range uu = ProjectRange(bbMin, bbMax, up);
            double r = p.Dot(right);
            double u = p.Dot(up);
            if (r < rr.Min - tol || r > rr.Max + tol) return false;
            if (u < uu.Min - tol || u > uu.Max + tol) return false;
            if (face == HostFaceKind.Left) return Math.Abs(r - rr.Min) <= tol;
            if (face == HostFaceKind.Right) return Math.Abs(r - rr.Max) <= tol;
            if (face == HostFaceKind.Bottom) return Math.Abs(u - uu.Min) <= tol;
            return Math.Abs(u - uu.Max) <= tol;
        }

        private struct Range
        {
            public double Min;
            public double Max;
        }

        private static Range ProjectRange(V3 bbMin, V3 bbMax, V3 axis)
        {
            V3[] c =
            {
                new V3(bbMin.X, bbMin.Y, bbMin.Z),
                new V3(bbMin.X, bbMin.Y, bbMax.Z),
                new V3(bbMin.X, bbMax.Y, bbMin.Z),
                new V3(bbMin.X, bbMax.Y, bbMax.Z),
                new V3(bbMax.X, bbMin.Y, bbMin.Z),
                new V3(bbMax.X, bbMin.Y, bbMax.Z),
                new V3(bbMax.X, bbMax.Y, bbMin.Z),
                new V3(bbMax.X, bbMax.Y, bbMax.Z),
            };
            double mn = double.MaxValue, mx = double.MinValue;
            for (int i = 0; i < c.Length; i++)
            {
                double v = c[i].Dot(axis);
                if (v < mn) mn = v;
                if (v > mx) mx = v;
            }
            return new Range { Min = mn, Max = mx };
        }

        private static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
