using System;

namespace RevitTagAlign
{
    public enum CornerAlignment
    {
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }

    public enum TextNoteJustificationMode
    {
        Unchanged,
        Left,
        Right,
        Automatic
    }

    public class AlignConfig
    {
        public CornerAlignment Corner { get; set; } = CornerAlignment.UpperLeft;
        public bool SwitchPickPointSide { get; set; }
        /// <summary>
        /// When true: force LeaderEndCondition.Attached (Revit may re-pick the host face).
        /// When false (default): Free end pinned to the original contact point — face never switches.
        /// </summary>
        public bool AttachedEndTags { get; set; }
        public bool KeepSelectionAfterUse { get; set; }
        public bool TurnSnapsOff { get; set; }
        public TextNoteJustificationMode Justification { get; set; } = TextNoteJustificationMode.Automatic;

        /// <summary>
        /// Deprecated: angle is fixed by corner preset + AngleDegrees.
        /// Kept for XML compatibility; always treated as false (1-click).
        /// </summary>
        public bool PickAngleThenTagPosition { get; set; } = false;

        /// <summary>
        /// Fixed leader angle in degrees (0–90) at the elbow between horizontal landing and red leader.
        /// Not the vertical gap between stacked tags (see VerticalSpacingFt).
        /// </summary>
        public double AngleDegrees { get; set; } = 45.0;

        /// <summary>
        /// When true: alignment uses a constant landing length rather than a common angle
        /// (Bird Tools Help). Angled segments aim at each host independently.
        /// When false: all angled leaders share AngleDegrees (common angle).
        /// </summary>
        public bool ConstantLanding { get; set; }
        /// <summary>Horizontal landing length (yellow segment) in feet.</summary>
        public double LandingDistanceFt { get; set; } = 5.0; // 1524 mm
        /// <summary>
        /// Vertical center-to-center spacing between stacked tag texts (feet).
        /// User-configurable; engine also enforces a no-overlap minimum from tag height.
        /// </summary>
        public double VerticalSpacingFt { get; set; } = 0.5; // 152.4 mm — clearer default gap
        public bool IntermittentAlignment { get; set; }
        public double HorizontalSpacingFt { get; set; } = 10.0; // 3048 mm
    }

    /// <summary>Result of mouse angle pick: absolute slope of the red arrow segment.</summary>
    public class PickedAngle
    {
        /// <summary>Angle from horizontal in radians, signed (positive = up to the right).</summary>
        public double AngleRadians { get; set; }
        /// <summary>0–90 display degrees (absolute).</summary>
        public double AngleDegreesAbs { get; set; }
        /// <summary>Unit direction of the angled arrow (elbow → host / element).</summary>
        public XYZDir Direction { get; set; }
    }

    public struct XYZDir
    {
        public double X;
        public double Y;
        public XYZDir(double x, double y) { X = x; Y = y; }
    }
}
