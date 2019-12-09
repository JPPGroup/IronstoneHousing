using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;
using System;
using System.Collections.Generic;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to generate 3d polyline from levels
    /// </summary>
    public static class PolylineCommands
    {
        private static readonly string[] ContinueKeywords = { "Yes", "No" };

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

            var initialBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectInitialBlock, ed, trans);
            if (initialBlock == null) return; // Assume user cancelled prompt

            var points = new List<Point3d>
            {
                GetPoint3dFromBlock(initialBlock)
            };

            var nextLevel = true;
            while (nextLevel)
            {
                var nextBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectNextBlock, ed, trans);
                if (nextBlock == null) return; // Assume user cancelled prompt

                points.Add(GetPoint3dFromBlock(nextBlock));
                nextLevel = ShouldContinue(ed);
            }

            CreatePolyline3dFromPoints(db, points);

            trans.Commit();
        }

        private static void CreatePolyline3dFromPoints(Database db, IEnumerable<Point3d> points)
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
        }

        private static Point3d GetPoint3dFromBlock(BlockReference block)
        {
            var level = LevelBlockHelper.GetLevelFromBlock(block);
            if (level.HasValue) return new Point3d(block.Position.X, block.Position.Y, level.Value);
            
            var ex = new ArgumentNullException(nameof(level));
            HousingExtensionApplication.Current.Logger.LogException(ex);
            throw ex;
        }

        private static bool ShouldContinue(Editor ed)
        {
            var result = ed.PromptForKeywords(Resources.Command_Prompt_IncludeGradientBlock, ContinueKeywords);
            return result == ContinueKeywords[0];
        }
    }
}
