using System;
using System.Globalization;
using System.Windows;

namespace RevitTagAlign
{
    public partial class AlignOptionsWindow : Window
    {
        public AlignConfig Config { get; private set; }

        private const double MmPerFoot = 304.8;
        private bool _loadingUi;

        public AlignOptionsWindow()
        {
            InitializeComponent();
            // Create folder + XML on first open so the path is always findable.
            Config = ConfigStore.LoadOrCreate();
            ApplyConfigToUi(Config);
            RefreshPathLabel();
        }

        private void RefreshPathLabel()
        {
            if (txtSettingsPath == null)
                return;

            string exists = System.IO.File.Exists(ConfigStore.SettingsPath) ? "OK" : "missing";
            txtSettingsPath.Text = "Settings: " + ConfigStore.SettingsPath + "  [" + exists + "]";
        }

        private void ApplyConfigToUi(AlignConfig cfg)
        {
            if (cfg == null) cfg = new AlignConfig();
            _loadingUi = true;
            try
            {
                switch (cfg.Corner)
                {
                    case CornerAlignment.UpperRight: rbUR.IsChecked = true; break;
                    case CornerAlignment.LowerLeft: rbLL.IsChecked = true; break;
                    case CornerAlignment.LowerRight: rbLR.IsChecked = true; break;
                    default: rbUL.IsChecked = true; break;
                }

                cbSwitchSide.IsChecked = cfg.SwitchPickPointSide;
                cbAttachedEnd.IsChecked = cfg.AttachedEndTags;
                cbKeepSelection.IsChecked = cfg.KeepSelectionAfterUse;
                cbTurnSnapsOff.IsChecked = cfg.TurnSnapsOff;

                switch (cfg.Justification)
                {
                    case TextNoteJustificationMode.Unchanged: rbJustUnchanged.IsChecked = true; break;
                    case TextNoteJustificationMode.Left: rbJustLeft.IsChecked = true; break;
                    case TextNoteJustificationMode.Right: rbJustRight.IsChecked = true; break;
                    default: rbJustAuto.IsChecked = true; break;
                }

                double angle = Math.Max(0, Math.Min(90, cfg.AngleDegrees));
                sliderAngle.Value = angle;
                tbAngle.Text = angle.ToString("0.###", CultureInfo.InvariantCulture);

                cbConstantLanding.IsChecked = cfg.ConstantLanding;
                tbLanding.IsEnabled = cfg.ConstantLanding;
                tbLanding.Text = FeetToMmText(cfg.LandingDistanceFt, 1524);

                tbVertSpacing.Text = FeetToMmText(cfg.VerticalSpacingFt, 152.4);

                cbIntermittent.IsChecked = cfg.IntermittentAlignment;
                tbHorizSpacing.IsEnabled = cfg.IntermittentAlignment;
                tbHorizSpacing.Text = FeetToMmText(cfg.HorizontalSpacingFt, 3048);
            }
            finally
            {
                _loadingUi = false;
            }
        }

        private static string FeetToMmText(double feet, double defaultMm)
        {
            double mm = feet * MmPerFoot;
            if (double.IsNaN(mm) || double.IsInfinity(mm) || mm <= 0)
                mm = defaultMm;
            return mm.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void SliderAngle_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loadingUi || tbAngle == null)
                return;
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
                "TagAlign Configure (Bird Tools–style)\n\n" +
                "1. Choose a corner — locks leader DIRECTION.\n" +
                "2. Set Angle (°) — fixed; mouse does NOT change angle.\n" +
                "3. Proceed → if needed, select Tags/Text Notes.\n" +
                "4. Click the position of the closest tag to the tagged\n" +
                "   elements (taghead). Other tags stack away from hosts.\n" +
                "   Upper: stack grows up from pick. Lower: grows down.\n" +
                "   Texts stay in a vertical column; landings are equal;\n" +
                "   angled leaders stay parallel (Bird Tools v1.4).\n" +
                "5. Click again to re-adjust; ESC to finish.\n\n" +
                "Constant Landing: ON = fixed landing (not common angle).\n" +
                "Vertical Spacing: freely set gap between tag texts (mm);\n" +
                "  also enforces a minimum so texts never overlap.\n" +
                "Force Attached End Tags: OFF = pin leader to original face\n" +
                "  (left stays left). ON = Revit Attached (may switch face).\n\n" +
                "Settings: " + ConfigStore.SettingsPath,
                "Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Config = BuildConfig();
            bool ok = ConfigStore.Save(Config);
            RefreshPathLabel();
            if (ok)
            {
                MessageBox.Show(
                    "Saved:\n" + ConfigStore.SettingsPath,
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Could not save settings.\n" +
                    ConfigStore.SettingsPath + "\n\n" +
                    (ConfigStore.LastError ?? "Unknown error"),
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            ConfigStore.OpenSettingsFolder();
            RefreshPathLabel();
            if (!string.IsNullOrEmpty(ConfigStore.LastError))
            {
                MessageBox.Show(
                    "Could not open folder:\n" + ConfigStore.LastError +
                    "\n\nPath:\n" + ConfigStore.SettingsDirectory,
                    "Open Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Proceed_Click(object sender, RoutedEventArgs e)
        {
            Config = BuildConfig();
            ConfigStore.Save(Config);
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

            cfg.PickAngleThenTagPosition = false; // angle is never mouse-picked
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

            cfg.VerticalSpacingFt = ParseMmToFeet(tbVertSpacing.Text, 152.4);

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
