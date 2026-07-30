using System;
using System.Globalization;
using System.Windows;

namespace RevitTagAlign
{
    public partial class AlignOptionsWindow : Window
    {
        public AlignConfig Config { get; private set; }

        private const double MmPerFoot = 304.8;

        public AlignOptionsWindow()
        {
            InitializeComponent();
            Config = new AlignConfig();
        }

        private void SliderAngle_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (tbAngle != null)
                tbAngle.Text = ((int)Math.Round(sliderAngle.Value)).ToString(CultureInfo.InvariantCulture);
        }

        private void TbAngle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(tbAngle.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            {
                v = Math.Max(0, Math.Min(90, v));
                sliderAngle.Value = v;
                tbAngle.Text = v.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                tbAngle.Text = ((int)sliderAngle.Value).ToString(CultureInfo.InvariantCulture);
            }
        }

        private void ConstantLanding_Changed(object sender, RoutedEventArgs e)
        {
            if (tbLanding != null)
                tbLanding.IsEnabled = cbConstantLanding.IsChecked == true;
        }

        private void Intermittent_Changed(object sender, RoutedEventArgs e)
        {
            if (tbHorizSpacing != null)
                tbHorizSpacing.IsEnabled = cbIntermittent.IsChecked == true;
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Tag Align Configure\n\n" +
                "1. Choose a corner preset (Upper/Lower × Left/Right).\n" +
                "2. Set leader angle, vertical spacing, and optional landing / intermittent spacing.\n" +
                "3. Click Proceed, then pick a point on screen.\n" +
                "4. Selected tags and text notes are stacked with parallel leaders.\n\n" +
                "Switch Pick Point Side: flip leader side relative to the stack.\n" +
                "Attached End Tags: keep tag leader ends attached to hosts.\n" +
                "Keep Selection After Use: leave elements selected when done.\n" +
                "Turn Snaps Off: temporarily disable snaps while picking.\n" +
                "Constant Landing: force a fixed landing distance for all leaders.\n" +
                "Intermittent Alignment: split into columns using Horizontal Spacing.",
                "Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Proceed_Click(object sender, RoutedEventArgs e)
        {
            Config = BuildConfig();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private AlignConfig BuildConfig()
        {
            var cfg = new AlignConfig();

            if (rbUL.IsChecked == true) cfg.Corner = CornerAlignment.UpperLeft;
            else if (rbUR.IsChecked == true) cfg.Corner = CornerAlignment.UpperRight;
            else if (rbLL.IsChecked == true) cfg.Corner = CornerAlignment.LowerLeft;
            else cfg.Corner = CornerAlignment.LowerRight;

            cfg.SwitchPickPointSide = cbSwitchSide.IsChecked == true;
            cfg.AttachedEndTags = cbAttachedEnd.IsChecked == true;
            cfg.KeepSelectionAfterUse = cbKeepSelection.IsChecked == true;
            cfg.TurnSnapsOff = cbTurnSnapsOff.IsChecked == true;

            if (rbJustUnchanged.IsChecked == true) cfg.Justification = TextNoteJustificationMode.Unchanged;
            else if (rbJustLeft.IsChecked == true) cfg.Justification = TextNoteJustificationMode.Left;
            else if (rbJustRight.IsChecked == true) cfg.Justification = TextNoteJustificationMode.Right;
            else cfg.Justification = TextNoteJustificationMode.Automatic;

            if (!double.TryParse(tbAngle.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double angle))
                angle = sliderAngle.Value;
            cfg.AngleDegrees = Math.Max(0, Math.Min(90, angle));

            cfg.ConstantLanding = cbConstantLanding.IsChecked == true;
            cfg.LandingDistanceFt = ParseMmToFeet(tbLanding.Text, 1524);

            cfg.VerticalSpacingFt = ParseMmToFeet(tbVertSpacing.Text, 60.96);

            cfg.IntermittentAlignment = cbIntermittent.IsChecked == true;
            cfg.HorizontalSpacingFt = ParseMmToFeet(tbHorizSpacing.Text, 3048);

            return cfg;
        }

        private static double ParseMmToFeet(string text, double defaultMm)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double mm))
                mm = defaultMm;
            return mm / MmPerFoot;
        }
    }
}
