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
        public bool AttachedEndTags { get; set; }
        public bool KeepSelectionAfterUse { get; set; }
        public bool TurnSnapsOff { get; set; }
        public TextNoteJustificationMode Justification { get; set; } = TextNoteJustificationMode.Automatic;

        /// <summary>
        /// Bird Tools style: after Proceed, pick angle with mouse (2 pts), then pick tag text position.
        /// When true, slider angle is only a fallback if mouse pick is cancelled mid-flow is N/A — angle comes from mouse.
        /// </summary>
        public bool PickAngleThenTagPosition { get; set; } = true;

        /// <summary>Fallback / slider angle in degrees (0–90). Overridden when mouse-picked.</summary>
        public double AngleDegrees { get; set; } = 45.0;

        public bool ConstantLanding { get; set; }
        /// <summary>Horizontal landing length (yellow segment) in feet.</summary>
        public double LandingDistanceFt { get; set; } = 5.0; // 1524 mm
        /// <summary>Vertical spacing between stacked tag texts in feet.</summary>
        public double VerticalSpacingFt { get; set; } = 0.2; // 60.96 mm
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
