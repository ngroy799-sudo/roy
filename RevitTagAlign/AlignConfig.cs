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
        public double AngleDegrees { get; set; } = 45.0;
        public bool ConstantLanding { get; set; }
        /// <summary>Landing distance in Revit internal feet.</summary>
        public double LandingDistanceFt { get; set; } = 5.0; // 1524 mm
        /// <summary>Vertical spacing between stacked annotations in feet.</summary>
        public double VerticalSpacingFt { get; set; } = 0.2; // 60.96 mm
        public bool IntermittentAlignment { get; set; }
        /// <summary>Horizontal spacing between intermittent columns in feet.</summary>
        public double HorizontalSpacingFt { get; set; } = 10.0; // 3048 mm
    }
}
