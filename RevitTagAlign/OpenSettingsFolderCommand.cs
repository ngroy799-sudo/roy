using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RevitTagAlign
{
    [Transaction(TransactionMode.Manual)]
    public class OpenSettingsFolderCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ConfigStore.LoadOrCreate();
                ConfigStore.OpenSettingsFolder();

                if (!string.IsNullOrEmpty(ConfigStore.LastError))
                {
                    TaskDialog.Show("Tag Align Settings",
                        "Could not open folder:\n" + ConfigStore.LastError +
                        "\n\nExpected path:\n" + ConfigStore.SettingsPath);
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
