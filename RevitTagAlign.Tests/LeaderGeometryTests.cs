using System;
using Xunit;
using RevitTagAlign;

namespace RevitTagAlign.Tests
{
    /// <summary>
    /// Reproduces Bird Tools v1.4 demo geometry (parallel leaders, adaptive landing+red per tag,
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
        public void Landings_AreHorizontal_WhenAdaptive()
        {
            V3 pick = new V3(0, 0, 10);
            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 45);
            V3 boxMin = new V3(8, -0.5, 7);
            V3 boxMax = new V3(10, 0.5, 8);
            V3 host = new V3(8, 0, 7.5);

            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                h0, host, boxMin, boxMax, landingDir, arrow, Right, Up,
                out V3 e0, out V3 end0, 1.0);

            Assert.True(e0.Z == h0.Z || Math.Abs(e0.Z - h0.Z) < 0.01);
            Assert.True(e0.DistanceTo(h0) >= 0.05);
        }

        [Fact]
        public void AdaptiveLanding_VariesWhenHostIsFartherRight()
        {
            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 60);
            V3 pick = new V3(0, 0, 10);

            V3 boxNearMin = new V3(8, -0.5, 7.0);
            V3 boxNearMax = new V3(10, 0.5, 8.0);
            V3 boxFarMin = new V3(11, -0.5, 7.0);
            V3 boxFarMax = new V3(13, 0.5, 8.0);
            V3 hostNear = new V3(8, 0, 7.5);
            V3 hostFar = new V3(11, 0, 7.5);

            V3 hNear = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 hFar = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);

            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                hNear, hostNear, boxNearMin, boxNearMax, landingDir, arrow, Right, Up,
                out V3 elbowNear, out V3 endNear, 1.0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                hFar, hostFar, boxFarMin, boxFarMax, landingDir, arrow, Right, Up,
                out V3 elbowFar, out V3 endFar, 1.0);

            double landNear = elbowNear.DistanceTo(hNear);
            double landFar = elbowFar.DistanceTo(hFar);
            double redNear = endNear.DistanceTo(elbowNear);
            double redFar = endFar.DistanceTo(elbowFar);

            Assert.True(landFar > landNear + 0.5 || redFar > redNear + 0.5,
                "farther host should lengthen landing and/or red segment");

            V3 dNear = (endNear - elbowNear).Normalize();
            V3 dFar = (endFar - elbowFar).Normalize();
            Assert.True(Math.Abs(dNear.Dot(dFar) - 1.0) < 0.02);
        }

        [Fact]
        public void Landings_AreEqualLength_WhenHostsCoincide()
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
            Assert.Equal(e0.Z, h0.Z, 6);
            Assert.Equal(e1.X, e0.X, 6);
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
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 45);

            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 h1 = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                h0, contact0, box0Min, box0Max, landingDir, arrow, Right, Up,
                out V3 e0, out V3 end0, 1.0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                h1, contact1, box1Min, box1Max, landingDir, arrow, Right, Up,
                out V3 e1, out V3 end1, 1.0);

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
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 45);

            double lastLen = 0;
            for (int click = 0; click < 8; click++)
            {
                V3 pick = new V3(-click * 2.0, 0, 7.5);
                V3 head = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
                LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                    head, contact, boxMin, boxMax, landingDir, arrow, Right, Up,
                    out V3 elbow, out V3 end, 1.0);

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

        [Fact]
        public void Angle60_IsAtElbow_BetweenHorizontalLandingAndRedLeader()
        {
            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, tagsOnLeft: true, isUpper: true, angleDegrees: 60);
            double elbow = LeaderGeometry.ElbowAngleDegrees(landingDir, arrow);
            Assert.InRange(elbow, 59.0, 61.0);
        }

        [Fact]
        public void HostFartherRight_LongerRedSegment_LeadersStillParallel()
        {
            // Host1 bbox shifted +3 ft right vs Host2; same height.
            V3 boxNearMin = new V3(8, -0.5, 7.0);
            V3 boxNearMax = new V3(10, 0.5, 8.0);
            V3 boxFarMin = new V3(11, -0.5, 7.0);
            V3 boxFarMax = new V3(13, 0.5, 8.0);
            V3 contactNear = new V3(8, 0, 7.5);
            V3 contactFar = new V3(11, 0, 7.5);

            HostFaceKind fNear = LeaderGeometry.ClassifyFace(contactNear, boxNearMin, boxNearMax, Right, Up);
            HostFaceKind fFar = LeaderGeometry.ClassifyFace(contactFar, boxFarMin, boxFarMax, Right, Up);

            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 60);

            V3 pick = new V3(0, 0, 10);
            V3 hNear = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 hFar = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                hNear, contactNear, boxNearMin, boxNearMax, landingDir, arrow, Right, Up,
                out V3 eNear, out V3 endNear, 1.0);
            LeaderGeometry.TryComputeAdaptiveCommonAngleLeader(
                hFar, contactFar, boxFarMin, boxFarMax, landingDir, arrow, Right, Up,
                out V3 eFar, out V3 endFar, 1.0);

            double redNear = endNear.DistanceTo(eNear);
            double redFar = endFar.DistanceTo(eFar);
            double landNear = eNear.DistanceTo(hNear);
            double landFar = eFar.DistanceTo(hFar);
            Assert.True(redFar > redNear + 0.5 || landFar > landNear + 0.5,
                "farther host should lengthen landing and/or red segment");

            V3 dNear = (endNear - eNear).Normalize();
            V3 dFar = (endFar - eFar).Normalize();
            Assert.True(Math.Abs(dNear.Dot(dFar) - 1.0) < 0.02);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, arrow), 59.0, 61.0);
        }

        [Fact]
        public void HostVerticalSwap_ChangesStackRowOrder()
        {
            V3 host1Low = new V3(10, 0, 7.0);
            V3 host2Mid = new V3(10, 0, 8.0);
            V3 host1Mid = new V3(10, 0, 8.0);
            V3 host2Low = new V3(10, 0, 7.0);

            // Before: host1 lower → row 0
            Assert.True(LeaderGeometry.CompareHostStackOrder(host1Low, host2Mid, Right, Up, isUpper: true) < 0);
            // After swap: host2 lower → host2 should sort before host1
            Assert.True(LeaderGeometry.CompareHostStackOrder(host2Low, host1Mid, Right, Up, isUpper: true) < 0);
        }
    }
}
