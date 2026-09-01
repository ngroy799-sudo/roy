using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitTagAlign
{
    public partial class AnnotationDashboardWindow : Window
    {
        private readonly UIApplication _uiApp;

        public AnnotationDashboardWindow(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeComponent();
        }

        private void SliderAngle_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtAngleValue != null)
                txtAngleValue.Text = ((int)sliderAngle.Value).ToString();
        }

        private void SliderLanding_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLandingValue != null)
                txtLandingValue.Text = ((int)sliderLanding.Value).ToString();
        }

        private void SliderLength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLengthValue != null)
                txtLengthValue.Text = ((int)sliderLength.Value).ToString();
        }

        private void Preset45_Click(object sender, RoutedEventArgs e)
        {
            sliderAngle.Value = 45;
            sliderLanding.Value = 10;
            sliderLength.Value = 20;
        }

        private void Preset60_Click(object sender, RoutedEventArgs e)
        {
            sliderAngle.Value = 60;
            sliderLanding.Value = 8;
            sliderLength.Value = 25;
        }

        private void Preset30_Click(object sender, RoutedEventArgs e)
        {
            sliderAngle.Value = 30;
            sliderLanding.Value = 12;
            sliderLength.Value = 18;
        }

        private void PresetHoriz_Click(object sender, RoutedEventArgs e)
        {
            sliderAngle.Value = 0;
            sliderLanding.Value = 15;
            sliderLength.Value = 15;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UIDocument uidoc = _uiApp.ActiveUIDocument;
                if (uidoc == null) return;

                Document doc = uidoc.Document;
                double angle = sliderAngle.Value * Math.PI / 180.0;
                double landingDist = sliderLanding.Value / 304.8; // mm to feet
                double leaderLength = sliderLength.Value / 304.8;

                List<IndependentTag> tags = new List<IndependentTag>();

                if (rbSelected.IsChecked == true)
                {
                    foreach (ElementId id in uidoc.Selection.GetElementIds())
                    {
                        Element elem = doc.GetElement(id);
                        if (elem is IndependentTag tag)
                            tags.Add(tag);
                    }
                }
                else if (rbVisible.IsChecked == true)
                {
                    View activeView = doc.ActiveView;
                    FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id);
                    tags = collector.OfClass(typeof(IndependentTag))
                                    .Cast<IndependentTag>()
                                    .Where(t => t.HasLeader)
                                    .ToList();
                }

                if (tags.Count == 0)
                {
                    MessageBox.Show("No tags found to modify.", "Annotation Dashboard", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (Transaction tx = new Transaction(doc, "Adjust Tag Leaders"))
                {
                    tx.Start();

                    foreach (var tag in tags)
                    {
                        if (!tag.HasLeader) continue;

                        try
                        {
                            var refs = tag.GetTaggedReferences();
                            if (refs == null || refs.Count == 0) continue;

                            var firstRef = refs.First();
                            XYZ headPos = tag.TagHeadPosition;
                            XYZ leaderEnd = tag.GetLeaderEnd(firstRef);

                            XYZ direction = new XYZ(Math.Cos(angle), Math.Sin(angle), 0);

                            // Determine if leader goes left or right from head
                            if (leaderEnd.X < headPos.X)
                                direction = new XYZ(-Math.Cos(angle), -Math.Sin(angle), 0);

                            XYZ newElbow = headPos + direction * leaderLength;
                            tag.SetLeaderElbow(firstRef, newElbow);
                        }
                        catch
                        {
                            // Skip tags that don't support leader modification
                        }
                    }

                    tx.Commit();
                }

                MessageBox.Show($"Successfully adjusted {tags.Count} tag(s).", "Annotation Dashboard",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Annotation Dashboard",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
