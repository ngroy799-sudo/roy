using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitTagAlign
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Tag Align";
            application.CreateRibbonTab(tabName);

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Alignment");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData alignBtn = new PushButtonData(
                "AlignTags", "Align\nTags", assemblyPath, "RevitTagAlign.AlignTagsCommand");
            alignBtn.ToolTip = "Align selected tags and text notes so leaders are parallel and landing lines are straight.";
            panel.AddItem(alignBtn);

            PushButtonData dashboardBtn = new PushButtonData(
                "AnnotationDashboard", "Annotation\nDashboard", assemblyPath, "RevitTagAlign.AnnotationDashboardCommand");
            dashboardBtn.ToolTip = "Open the Annotation Dashboard to dynamically control leader angles, landing distances and lengths.";
            panel.AddItem(dashboardBtn);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
