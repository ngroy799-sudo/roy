using Autodesk.Revit.UI;
using System.Reflection;

namespace RevitTagAlign
{
    public class App : IExternalApplication
    {
        // Stable internal ids — used by Revit Keyboard Shortcuts CommandId.
        public const string AlignTagsButtonId = "AlignTags";
        public const string DashboardButtonId = "AnnotationDashboard";
        public const string TabName = "Tag Align";
        public const string PanelName = "Alignment";

        public Result OnStartup(UIControlledApplication application)
        {
            // Auto-create %AppData%\Roaming\RevitTagAlign\AlignConfig.xml on Revit start.
            try { ConfigStore.LoadOrCreate(); }
            catch { /* non-fatal */ }

            application.CreateRibbonTab(TabName);
            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Button Text is what appears in KS search ("Align Tags").
            PushButtonData alignBtn = new PushButtonData(
                AlignTagsButtonId,
                "Align\nTags",
                assemblyPath,
                "RevitTagAlign.AlignTagsCommand");
            alignBtn.ToolTip = "Align Tags — stack tags/text notes with parallel leaders (pick point).";
            alignBtn.LongDescription =
                "Keywords: Tag Align, Align Tags, Tag Alignment, Text Note Align, Leader Align, TAT.\n" +
                "Select tags/text notes → Configure → Proceed → pick angle then tag position.\n" +
                "Assign a Revit shortcut: type KS → search Align Tags.";
            panel.AddItem(alignBtn);

            PushButtonData dashboardBtn = new PushButtonData(
                DashboardButtonId,
                "Annotation\nDashboard",
                assemblyPath,
                "RevitTagAlign.AnnotationDashboardCommand");
            dashboardBtn.ToolTip = "Annotation Dashboard — live leader angle / landing / length control.";
            dashboardBtn.LongDescription =
                "Keywords: Annotation Dashboard, Tag Dashboard, Leader Angle, Landing Distance, AD.\n" +
                "Assign a Revit shortcut: type KS → search Annotation Dashboard.";
            panel.AddItem(dashboardBtn);

            PushButtonData settingsBtn = new PushButtonData(
                "OpenSettingsFolder",
                "Settings\nFolder",
                assemblyPath,
                "RevitTagAlign.OpenSettingsFolderCommand");
            settingsBtn.ToolTip = "Open the RevitTagAlign settings folder (AlignConfig.xml).";
            settingsBtn.LongDescription =
                "Opens:\n" + ConfigStore.SettingsDirectory + "\n\n" +
                "File: AlignConfig.xml";
            panel.AddItem(settingsBtn);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
