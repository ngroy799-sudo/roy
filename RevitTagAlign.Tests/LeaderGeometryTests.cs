using System;
using Xunit;
using RevitTagAlign;

namespace RevitTagAlign.Tests
{
    /// <summary>
    /// Reproduces Bird Tools v1.4 demo geometry (equal landings, parallel leaders,
    /// vertical text, face pin, no fly-away on re-click).
    /// </summary>
    public class LeaderGeometryTests
    {
        private static readonly V3 Right = new V3(1, 0, 0);
        private static readonly V3 Up = new V3(0, 0, 1); // section: up is world Z

        [Fact]
        public void TagHeads_AreVerticallyAligned()
        {
            V3 pick = new V3(0, 0, 10);
            double step = 0.5;
            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, step, 1, 0);
            V3 h1 = LeaderGeometry.StackHead(pick, Up, Right, 1, step, 1, 0);
            Assert.Equal(h0.X, h1.X, 6);
            Assert.Equal(h0.Y, h1.Y, 6);
            Assert.Equal(0.5, h1.Z - h0.Z, 6);
        }

        [Fact]
        public void Landings_AreEqualLength_AndHorizontal()
        {
            V3 pick = new V3(0, 0, 10);
            double landing = 1.0;
            V3 landingDir = Right;
            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 h1 = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            V3 e0 = LeaderGeometry.ElbowFromHead(h0, landingDir, landing);
            V3 e1 = LeaderGeometry.ElbowFromHead(h1, landingDir, landing);
            Assert.Equal(landing, e0.DistanceTo(h0), 6);
            Assert.Equal(landing, e1.DistanceTo(h1), 6);
            Assert.Equal(e0.Z, h0.Z, 6); // horizontal in section
            Assert.Equal(e1.X, e0.X, 6); // elbows also vertically aligned
        }

        [Fact]
        public void CommonAngle_EndsStayOnOriginalLeftFace_AndLeadersParallel()
        {
            // Section: Up = world Z. Tags sit ABOVE/LEFT of hosts (Upper-Left, 45°).
            // Horizontal run elbow→left-face = 7 ft ⇒ 45° drop = 7 ft.
            V3 box0Min = new V3(8, -0.5, 7.0);
            V3 box0Max = new V3(10, 0.5, 8.0);
            V3 box1Min = new V3(8, -0.5, 7.5);
            V3 box1Max = new V3(10, 0.5, 8.5);
            V3 contact0 = new V3(8, 0, 7.5);
            V3 contact1 = new V3(8, 0, 8.0);

            HostFaceKind f0 = LeaderGeometry.ClassifyFace(contact0, box0Min, box0Max, Right, Up);
            HostFaceKind f1 = LeaderGeometry.ClassifyFace(contact1, box1Min, box1Max, Right, Up);
            Assert.Equal(HostFaceKind.Left, f0);
            Assert.Equal(HostFaceKind.Left, f1);

            V3 pick = new V3(0, 0, 14.5);
            V3 landingDir = Right;
            V3 arrow = (Right * Math.Cos(Math.PI / 4) + Up * (-Math.Sin(Math.PI / 4))).Normalize();

            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 h1 = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            V3 e0 = LeaderGeometry.ElbowFromHead(h0, landingDir, 1.0);
            V3 e1 = LeaderGeometry.ElbowFromHead(h1, landingDir, 1.0);
            V3 end0 = LeaderGeometry.SnapEndToOriginalFace(e0, arrow, box0Min, box0Max, f0, Right, Up);
            V3 end1 = LeaderGeometry.SnapEndToOriginalFace(e1, arrow, box1Min, box1Max, f1, Right, Up);

            Assert.True(LeaderGeometry.PointOnFace(end0, box0Min, box0Max, HostFaceKind.Left, Right, Up, 0.08));
            Assert.True(LeaderGeometry.PointOnFace(end1, box1Min, box1Max, HostFaceKind.Left, Right, Up, 0.08));

            V3 d0 = (end0 - e0).Normalize();
            V3 d1 = (end1 - e1).Normalize();
            Assert.True(Math.Abs(d0.Dot(d1) - 1.0) < 0.02, "leaders must be parallel, got " + d0.Dot(d1));

            Assert.True(end0.DistanceTo(e0) < 20, "must not fly");
            Assert.True(end1.DistanceTo(e1) < 20, "must not fly");
        }

        [Fact]
        public void ReClick_DoesNotFlyOrSwitchFace()
        {
            V3 boxMin = new V3(8, -0.5, 7);
            V3 boxMax = new V3(10, 0.5, 8);
            V3 contact = new V3(8, 0, 7.5);
            HostFaceKind face = LeaderGeometry.ClassifyFace(contact, boxMin, boxMax, Right, Up);
            V3 landingDir = Right;
            V3 arrow = (Right * Math.Cos(Math.PI / 4) + Up * (-Math.Sin(Math.PI / 4))).Normalize();

            double lastLen = 0;
            for (int click = 0; click < 8; click++)
            {
                // Each click moves the stack further left (user re-picking).
                V3 pick = new V3(-click * 2.0, 0, 7.5);
                V3 head = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
                V3 elbow = LeaderGeometry.ElbowFromHead(head, landingDir, 1.0);
                V3 end = LeaderGeometry.SnapEndToOriginalFace(elbow, arrow, boxMin, boxMax, face, Right, Up);

                Assert.True(LeaderGeometry.PointOnFace(end, boxMin, boxMax, HostFaceKind.Left, Right, Up, 0.05));
                double len = end.DistanceTo(elbow);
                Assert.True(len < 40, "leader length exploded on re-click: " + len);
                lastLen = len;
            }
            Assert.True(lastLen > 0.1);
        }

        [Fact]
        public void Classify_DoesNotCallTopWhenContactIsLeft()
        {
            V3 bbMin = new V3(0, 0, 0);
            V3 bbMax = new V3(2, 1, 4);
            V3 left = new V3(0, 0.5, 2);
            V3 top = new V3(1, 0.5, 4);
            Assert.Equal(HostFaceKind.Left, LeaderGeometry.ClassifyFace(left, bbMin, bbMax, Right, Up));
            Assert.Equal(HostFaceKind.Top, LeaderGeometry.ClassifyFace(top, bbMin, bbMax, Right, Up));
        }
    }
}
