using System;
using System.Windows;

namespace RevitTagAlign
{
    public partial class AlignOptionsWindow : Window
    {
        public AlignmentMode SelectedMode { get; private set; }
        public double LeaderAngleDegrees { get; private set; }

        public AlignOptionsWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (rbLeft.IsChecked == true) SelectedMode = AlignmentMode.AlignLeft;
            else if (rbRight.IsChecked == true) SelectedMode = AlignmentMode.AlignRight;
            else if (rbTop.IsChecked == true) SelectedMode = AlignmentMode.AlignTop;
            else if (rbBottom.IsChecked == true) SelectedMode = AlignmentMode.AlignBottom;
            else if (rbMiddleH.IsChecked == true) SelectedMode = AlignmentMode.AlignMiddleHorizontal;
            else if (rbMiddleV.IsChecked == true) SelectedMode = AlignmentMode.AlignMiddleVertical;

            if (!double.TryParse(tbAngle.Text, out double angle))
                angle = 45.0;
            LeaderAngleDegrees = angle;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
