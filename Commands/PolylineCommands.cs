using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to generate 3d polyline from levels
    /// </summary>
    public static class PolylineCommands
    {
        private static readonly string[] PolylineOptions = { "Close", "Open" };

        /// <summary>
        /// Custom command to generate 3d polyline from level blocks
        /// </summary>
        [CommandMethod("C_Polyline_FromLevels")]
        public static void GeneratePolyline3dFromLevels()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(PolylineCommands), nameof(GeneratePolyline3dFromLevels));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            var initial = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectInitialBlock, ed, trans);
            if(!initial.IsValid) return;

            var points = new List<Point3d>{ initial.Point3d };
            var nextLevel = true;

            var options = new PromptEntityOptions(Resources.Command_Prompt_SelectNextBlock);
            options.SetRejectMessage(Resources.Command_Prompt_RejectBlockReference);
            options.AddAllowedClass(typeof(BlockReference), true);
            options.AppendKeywordsToMessage = true;

            foreach (var keyword in PolylineOptions) options.Keywords.Add(keyword);

            PromptEntityResult result = null;
            while (nextLevel)
            {
                nextLevel = false;
                result = ed.GetEntity(options);

                if (result.Status == PromptStatus.OK)
                {
                    var details = new LevelBlockDetails(LevelBlockHelper.GetBlockReference(result.ObjectId, trans));
                    if (!details.IsValid)
                    {
                        HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected, Severity.Warning);
                        return;
                    }

                    if (points.Any(p => p.Y.Equals(details.Point3d.Y) && p.X.Equals(details.Point3d.X) && p.Z.Equals(details.Point3d.Z)))
                    {
                        HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Block_Already_Selected, Severity.Information);
                    }
                    else
                    {
                        points.Add(details.Point3d);
                    }

                    nextLevel = true;
                }
            }

            var close = false;
            if (result.Status == PromptStatus.Keyword) close = result.StringResult == PolylineOptions[0];

            CreatePolyline3dFromPoints(db, points, close);

            trans.Commit();
        }

        private static void CreatePolyline3dFromPoints(Database db, IEnumerable<Point3d> points, bool shouldClose)
        {
            var trans = db.TransactionManager.TopTransaction;

            var acBlkTbl = (BlockTable) trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var acBlkTblRec = (BlockTableRecord) trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var poly3d = new Polyline3d();

            acBlkTblRec.AppendEntity(poly3d);
            trans.AddNewlyCreatedDBObject(poly3d, true);

            foreach (var point in points)
            {
                using (var poly3dVertex = new PolylineVertex3d(point))
                {
                    poly3d.AppendVertex(poly3dVertex);
                }
            }

            poly3d.Closed = shouldClose;
        }
    }
}
