using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.Autocad;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to generate prelim plot details
    /// </summary>
    public static class PrelimPlotCommands
    {
        public const string PLOT_LINE_LAYER = "JPP_Plot";
        public const string FFL_LINE_LAYER = "JPP_FFL Line";

        /// <summary>
        /// Custom command to generate prelim plot details
        /// </summary>
        [CommandMethod("C_Plot_PrelimDetails")]
        public static void PrelimPlotDetails()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(PrelimPlotCommands), nameof(PrelimPlotDetails));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            db.RegisterLayer("JPP_Plot");
            db.RegisterLayer("JPP_FFL Line");

            var plotObjectId = ed.PromptForEntity(Resources.Command_Prompt_PlotLine, typeof(Polyline), Resources.Command_Prompt_RejectPlotLine, true);
            if (!plotObjectId.HasValue) return; //Assume user cancelled

            var plotLine = (Polyline)trans.GetObject(plotObjectId.Value, OpenMode.ForWrite);
            if (!plotLine.Closed)
            {
                //Not closed
                return;
            }

            var finishFloorLevel = ed.PromptForDouble(Resources.Command_Prompt_FinishFloorLevel);
            if (!finishFloorLevel.HasValue) return; //Assume user cancelled

            BuildPrelimPlot(db, plotLine, finishFloorLevel.Value);

            plotLine.Layer = PLOT_LINE_LAYER;
            trans.Commit();
        }

        private static void BuildPrelimPlot(Database db, Polyline plotLine, double finishFloorLevel)
        {
            var trans = db.TransactionManager.TopTransaction;

            var acBlkTbl = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var acBlkTblRec = (BlockTableRecord)trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var finishFloorLevelLine = DrawFinishFloorLevelLine(plotLine, finishFloorLevel);

            acBlkTblRec.AppendEntity(finishFloorLevelLine);
            trans.AddNewlyCreatedDBObject(finishFloorLevelLine, true);

            var extents = finishFloorLevelLine.GeometricExtents;
            var center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;
            FinishFloorLevelBlockHelper.NewFinishFloorLevelAtPoint(db, center, finishFloorLevel);
        }

        private static Polyline DrawFinishFloorLevelLine(Polyline plotLine, double finishFloorLevel)
        {
            var offsetPlus = plotLine.GetOffsetCurves(0.01);
            var offsetMinus = plotLine.GetOffsetCurves(-0.01);
            var plusLine = (Polyline)offsetPlus[0];
            var minusLine = (Polyline)offsetMinus[0];

            var finishFloorLevelLine = plusLine.Area > minusLine.Area ? minusLine : plusLine;

            finishFloorLevelLine.Elevation = finishFloorLevel;
            finishFloorLevelLine.Layer = FFL_LINE_LAYER;
            return finishFloorLevelLine;
        }
    }
}
