using Autodesk.Revit.UI;
using System.Reflection;

namespace RevitTagAlign
{
    public class App : IExternalApplication
    {
        public const string AlignTagsButtonId = "TagAlign_AlignSelectedTags";
        public const string DashboardButtonId = "TagAlign_LeaderDashboard";
        public const string SettingsButtonId = "TagAlign_OpenSettingsFolder";
        public const string TabName = "TagAlign Tool";
        public const string PanelName = "TagAlign Commands";
        public const string AddInsPanelName = "TagAlign";

        public Result OnStartup(UIControlledApplication application)
        {
            try { ConfigStore.LoadOrCreate(); }
            catch { }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // 1) Built-in Add-Ins tab — most reliable for Keyboard Shortcuts listing
            try
            {
                RibbonPanel addInsPanel = GetOrCreatePanelOnAddIns(application, AddInsPanelName);
                AddButtons(addInsPanel, assemblyPath, idSuffix: "_AddIns");
            }
            catch
            {
                // Continue — custom tab / External Tools commands still available
            }

            // 2) Custom tab (same commands, easier to find visually)
            try
            {
                try { application.CreateRibbonTab(TabName); }
                catch { }

                RibbonPanel panel = GetOrCreatePanel(application, TabName, PanelName);
                AddButtons(panel, assemblyPath, idSuffix: "");
            }
            catch { }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static RibbonPanel GetOrCreatePanelOnAddIns(UIControlledApplication app, string panelName)
        {
            foreach (RibbonPanel p in app.GetRibbonPanels(Tab.AddIns))
            {
                if (p.Name == panelName)
                    return p;
            }
            return app.CreateRibbonPanel(Tab.AddIns, panelName);
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication app, string tabName, string panelName)
        {
            foreach (RibbonPanel p in app.GetRibbonPanels(tabName))
            {
                if (p.Name == panelName)
                    return p;
            }
            return app.CreateRibbonPanel(tabName, panelName);
        }

        private static void AddButtons(RibbonPanel panel, string assemblyPath, string idSuffix)
        {
            // Avoid duplicating if panel already has our buttons
            foreach (RibbonItem existing in panel.GetItems())
            {
                if (existing.Name != null && existing.Name.StartsWith("TagAlign_"))
                    return;
            }

            PushButtonData alignBtn = new PushButtonData(
                AlignTagsButtonId + idSuffix,
                "TagAlign Align\nSelected Tags",
                assemblyPath,
                "RevitTagAlign.AlignTagsCommand");
            alignBtn.ToolTip = "TagAlign Align Selected Tags";
            alignBtn.LongDescription =
                "KS search: TagAlign Align Selected Tags\n" +
                "Also available: Add-Ins > External Tools > TagAlign Align Selected Tags";

            PushButtonData dashboardBtn = new PushButtonData(
                DashboardButtonId + idSuffix,
                "TagAlign Leader\nDashboard",
                assemblyPath,
                "RevitTagAlign.AnnotationDashboardCommand");
            dashboardBtn.ToolTip = "TagAlign Leader Dashboard";
            dashboardBtn.LongDescription = "KS search: TagAlign Leader Dashboard";

            PushButtonData settingsBtn = new PushButtonData(
                SettingsButtonId + idSuffix,
                "TagAlign Settings\nFolder",
                assemblyPath,
                "RevitTagAlign.OpenSettingsFolderCommand");
            settingsBtn.ToolTip = "TagAlign Settings Folder";
            settingsBtn.LongDescription = "KS search: TagAlign Settings Folder";

            panel.AddItem(alignBtn);
            panel.AddItem(dashboardBtn);
            panel.AddItem(settingsBtn);
        }
    }
}
