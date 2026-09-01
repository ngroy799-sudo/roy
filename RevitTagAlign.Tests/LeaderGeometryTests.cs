using System;
using Xunit;
using RevitTagAlign;

namespace RevitTagAlign.Tests
{
    /// <summary>
    /// Bird Tools v1.4 geometry: equal horizontal landings, parallel reds,
    /// vertical stack, face pin, no fly-away on re-click.
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
        public void Landings_AreHorizontal()
        {
            V3 pick = new V3(0, 0, 10);
            V3 landingDir = Right;
            double shared = 1.0;
            V3 h0 = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 e0 = LeaderGeometry.ElbowFromHead(h0, landingDir, shared);
            Assert.Equal(shared, e0.DistanceTo(h0), 6);
            Assert.Equal(h0.Z, e0.Z, 6);
        }

        [Fact]
        public void StackAnchor_EqualLanding_ParallelReds()
        {
            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 45);
            V3 pick = new V3(0, 0, 10);
            V3 boxMin = new V3(8, -0.5, 7.0);
            V3 boxMax = new V3(10, 0.5, 8.0);
            V3 boxHighMin = new V3(8, -0.5, 8.0);
            V3 boxHighMax = new V3(10, 0.5, 9.0);
            V3 hostLow = new V3(8, 0, 7.5);
            V3 hostHigh = new V3(8, 0, 8.5);

            V3 anchorHead = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 h1 = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            double shared = LeaderGeometry.ResolveAnchorLandingLength(
                anchorHead, hostLow, boxMin, boxMax, landingDir, arrow, Right, Up, 1.0);

            HostFaceKind f0 = LeaderGeometry.ClassifyFace(hostLow, boxMin, boxMax, Right, Up);
            HostFaceKind f1 = LeaderGeometry.ClassifyFace(hostHigh, boxHighMin, boxHighMax, Right, Up);

            LeaderGeometry.ComputeStackCommonAngleLeader(
                anchorHead, boxMin, boxMax, f0, landingDir, arrow, Right, Up, shared, out V3 e0, out V3 end0);
            LeaderGeometry.ComputeStackCommonAngleLeader(
                h1, boxHighMin, boxHighMax, f1, landingDir, arrow, Right, Up, shared, out V3 e1, out V3 end1);

            double land0 = e0.DistanceTo(anchorHead);
            double land1 = e1.DistanceTo(h1);
            Assert.Equal(shared, land0, 4);
            Assert.Equal(shared, land1, 4);
            Assert.Equal(e0.X, e1.X, 4);

            V3 d0 = (end0 - e0).Normalize();
            V3 d1 = (end1 - e1).Normalize();
            Assert.True(Math.Abs(d0.Dot(d1) - 1.0) < 0.02);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, d0), 44.0, 46.0);
        }

        [Fact]
        public void EqualLanding_RedVariesWhenHostIsFartherRight()
        {
            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 60);
            V3 pick = new V3(0, 0, 10);
            double shared = 1.0;

            V3 boxNearMin = new V3(8, -0.5, 7.0);
            V3 boxNearMax = new V3(10, 0.5, 8.0);
            V3 boxFarMin = new V3(14, -0.5, 7.0);
            V3 boxFarMax = new V3(16, 0.5, 8.0);
            V3 hostNear = new V3(8, 0, 7.5);
            V3 hostFar = new V3(14, 0, 7.5);

            V3 headNear = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
            V3 headFar = LeaderGeometry.StackHead(pick, Up, Right, 1, 0.5, 1, 0);
            HostFaceKind fNear = LeaderGeometry.ClassifyFace(hostNear, boxNearMin, boxNearMax, Right, Up);
            HostFaceKind fFar = LeaderGeometry.ClassifyFace(hostFar, boxFarMin, boxFarMax, Right, Up);

            LeaderGeometry.ComputeStackCommonAngleLeader(
                headNear, boxNearMin, boxNearMax, fNear, landingDir, arrow, Right, Up, shared, out V3 eNear, out V3 endNear);
            LeaderGeometry.ComputeStackCommonAngleLeader(
                headFar, boxFarMin, boxFarMax, fFar, landingDir, arrow, Right, Up, shared, out V3 eFar, out V3 endFar);

            Assert.Equal(shared, eNear.DistanceTo(headNear), 4);
            Assert.Equal(shared, eFar.DistanceTo(headFar), 4);
            Assert.Equal(eNear.X, eFar.X, 4);

            double redNear = endNear.DistanceTo(eNear);
            double redFar = endFar.DistanceTo(eFar);
            Assert.True(redFar > redNear + 0.3, "farther host should lengthen red segment only");

            V3 dNear = (endNear - eNear).Normalize();
            V3 dFar = (endFar - eFar).Normalize();
            Assert.True(Math.Abs(dNear.Dot(dFar) - 1.0) < 0.02);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, dNear), 59.0, 61.0);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, dFar), 59.0, 61.0);

            // Pinned host contact stays on original face (Revit end pin).
            V3 pinnedNear = LeaderGeometry.ClampPointToOriginalFace(hostNear, boxNearMin, boxNearMax, fNear, Right, Up);
            V3 pinnedFar = LeaderGeometry.ClampPointToOriginalFace(hostFar, boxFarMin, boxFarMax, fFar, Right, Up);
            Assert.True(LeaderGeometry.PointOnFace(pinnedNear, boxNearMin, boxNearMax, fNear, Right, Up, 0.08));
            Assert.True(LeaderGeometry.PointOnFace(pinnedFar, boxFarMin, boxFarMax, fFar, Right, Up, 0.08));
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
            double shared = LeaderGeometry.ResolveAnchorLandingLength(
                h0, contact0, box0Min, box0Max, landingDir, arrow, Right, Up, 1.0);

            LeaderGeometry.ComputeStackCommonAngleLeader(
                h0, box0Min, box0Max, f0, landingDir, arrow, Right, Up, shared, out V3 e0, out V3 end0);
            LeaderGeometry.ComputeStackCommonAngleLeader(
                h1, box1Min, box1Max, f1, landingDir, arrow, Right, Up, shared, out V3 e1, out V3 end1);

            end0 = LeaderGeometry.ClampPointToOriginalFace(contact0, box0Min, box0Max, HostFaceKind.Left, Right, Up);
            end1 = LeaderGeometry.ClampPointToOriginalFace(contact1, box1Min, box1Max, HostFaceKind.Left, Right, Up);

            Assert.Equal(shared, e0.DistanceTo(h0), 4);
            Assert.Equal(shared, e1.DistanceTo(h1), 4);
            Assert.Equal(e0.X, e1.X, 4);

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
            double shared = 1.0;

            double lastLen = 0;
            for (int click = 0; click < 8; click++)
            {
                V3 pick = new V3(-click * 2.0, 0, 7.5);
                V3 head = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);
                LeaderGeometry.ComputeStackCommonAngleLeader(
                    head, boxMin, boxMax, face, landingDir, arrow, Right, Up, shared, out V3 elbow, out V3 end);
                end = LeaderGeometry.ClampPointToOriginalFace(contact, boxMin, boxMax, face, Right, Up);

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
            V3 boxNearMin = new V3(8, -0.5, 7.0);
            V3 boxNearMax = new V3(10, 0.5, 8.0);
            V3 boxFarMin = new V3(14, -0.5, 7.0);
            V3 boxFarMax = new V3(16, 0.5, 8.0);
            V3 contactNear = new V3(8, 0, 7.5);
            V3 contactFar = new V3(14, 0, 7.5);

            HostFaceKind fNear = LeaderGeometry.ClassifyFace(contactNear, boxNearMin, boxNearMax, Right, Up);
            HostFaceKind fFar = LeaderGeometry.ClassifyFace(contactFar, boxFarMin, boxFarMax, Right, Up);

            V3 landingDir = Right;
            V3 arrow = LeaderGeometry.CommonAngleArrow(Right, Up, true, true, 60);
            double shared = 1.0;

            V3 pick = new V3(0, 0, 10);
            V3 head = LeaderGeometry.StackHead(pick, Up, Right, 0, 0.5, 1, 0);

            LeaderGeometry.ComputeStackCommonAngleLeader(
                head, boxNearMin, boxNearMax, fNear, landingDir, arrow, Right, Up, shared, out V3 eNear, out V3 endNear);
            LeaderGeometry.ComputeStackCommonAngleLeader(
                head, boxFarMin, boxFarMax, fFar, landingDir, arrow, Right, Up, shared, out V3 eFar, out V3 endFar);

            Assert.Equal(shared, eNear.DistanceTo(head), 4);
            Assert.Equal(shared, eFar.DistanceTo(head), 4);

            double redNear = endNear.DistanceTo(eNear);
            double redFar = endFar.DistanceTo(eFar);
            Assert.True(redFar > redNear + 0.3, "farther host should lengthen red segment");

            V3 dNear = (endNear - eNear).Normalize();
            V3 dFar = (endFar - eFar).Normalize();
            Assert.True(Math.Abs(dNear.Dot(dFar) - 1.0) < 0.02);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, dNear), 59.0, 61.0);
            Assert.InRange(LeaderGeometry.ElbowAngleDegrees(landingDir, dFar), 59.0, 61.0);
        }

        [Fact]
        public void HostVerticalSwap_ChangesStackRowOrder()
        {
            V3 host1Low = new V3(10, 0, 7.0);
            V3 host2Mid = new V3(10, 0, 8.0);
            V3 host1Mid = new V3(10, 0, 8.0);
            V3 host2Low = new V3(10, 0, 7.0);

            Assert.True(LeaderGeometry.CompareHostStackOrder(host1Low, host2Mid, Right, Up, isUpper: true) < 0);
            Assert.True(LeaderGeometry.CompareHostStackOrder(host2Low, host1Mid, Right, Up, isUpper: true) < 0);
        }
    }
}
