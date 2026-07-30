using Autodesk.Revit.UI;
using System.Reflection;

namespace RevitTagAlign
{
    public class App : IExternalApplication
    {
        // Stable internal ids — used by Revit Keyboard Shortcuts CommandId.
        // Unique prefix "TagAlign_" avoids collisions with other add-ins.
        public const string AlignTagsButtonId = "TagAlign_AlignSelectedTags";
        public const string DashboardButtonId = "TagAlign_LeaderDashboard";
        public const string SettingsButtonId = "TagAlign_OpenSettingsFolder";
        public const string TabName = "TagAlign Tool";
        public const string PanelName = "TagAlign Commands";

        public Result OnStartup(UIControlledApplication application)
        {
            try { ConfigStore.LoadOrCreate(); }
            catch { /* non-fatal */ }

            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch
            {
                // Tab may already exist from a previous load attempt.
            }

            RibbonPanel panel = null;
            foreach (RibbonPanel p in application.GetRibbonPanels(TabName))
            {
                if (p.Name == PanelName) { panel = p; break; }
            }
            if (panel == null)
                panel = application.CreateRibbonPanel(TabName, PanelName);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Button Text = what appears in KS search (must be unique / easy to find).
            PushButtonData alignBtn = new PushButtonData(
                AlignTagsButtonId,
                "TagAlign Align\nSelected Tags",
                assemblyPath,
                "RevitTagAlign.AlignTagsCommand");
            alignBtn.ToolTip = "TagAlign Align Selected Tags — stack tags/text notes with parallel leaders.";
            alignBtn.LongDescription =
                "Search keywords in KS: TagAlign Align Selected Tags, TagAlign, RTA.\n" +
                "Select tags/text notes → Configure → Proceed → pick angle then tag position.\n" +
                "Assign shortcut: type KS → search TagAlign.";
            panel.AddItem(alignBtn);

            PushButtonData dashboardBtn = new PushButtonData(
                DashboardButtonId,
                "TagAlign Leader\nDashboard",
                assemblyPath,
                "RevitTagAlign.AnnotationDashboardCommand");
            dashboardBtn.ToolTip = "TagAlign Leader Dashboard — live leader angle / landing / length.";
            dashboardBtn.LongDescription =
                "Search keywords in KS: TagAlign Leader Dashboard, TagAlign.\n" +
                "Assign shortcut: type KS → search TagAlign.";
            panel.AddItem(dashboardBtn);

            PushButtonData settingsBtn = new PushButtonData(
                SettingsButtonId,
                "TagAlign Settings\nFolder",
                assemblyPath,
                "RevitTagAlign.OpenSettingsFolderCommand");
            settingsBtn.ToolTip = "Open TagAlign settings folder (AlignConfig.xml).";
            settingsBtn.LongDescription =
                "Search keywords in KS: TagAlign Settings Folder.\n" +
                "Opens:\n" + ConfigStore.SettingsDirectory;
            panel.AddItem(settingsBtn);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
