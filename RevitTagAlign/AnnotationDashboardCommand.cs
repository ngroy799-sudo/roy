using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RevitTagAlign
{
    [Transaction(TransactionMode.Manual)]
    public class AnnotationDashboardCommand : IExternalCommand
    {
        private static AnnotationDashboardWindow _dashboardWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_dashboardWindow == null || !_dashboardWindow.IsVisible)
                {
                    _dashboardWindow = new AnnotationDashboardWindow(commandData.Application);
                    _dashboardWindow.Show();
                }
                else
                {
                    _dashboardWindow.Activate();
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
