using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;
using System;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to add level blocks
    /// </summary>
    public static class LevelBlockCommands
    {
        private static readonly string[] GradientKeywords = { "No", "Yes" };
        private static readonly string[] BlockKeywords = { "New", "Existing" };

        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient
        private static double _invert; //cache of previous invert
        private static string _includeGradient = GradientKeywords[0]; //cache of previous gradient selection

        /// <summary>
        /// Custom command to calculate a level at given point between two existing levels
        /// </summary>
        [CommandMethod("C_LevelBlock_BetweenLevels")]
        public static void CalculateLevelBetweenLevels()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelBetweenLevels));
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            var startBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if (startBlock == null) return; //Assume user cancelled
            
            var startLevel = LevelBlockHelper.GetLevelFromBlock(startBlock);
            if (!startLevel.HasValue) return;

            var endBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectEndLevelBlock, ed, trans);
            if (endBlock == null) return; //Assume user cancelled

            var endLevel = LevelBlockHelper.GetLevelFromBlock(endBlock);
            if (!endLevel.HasValue) return;
            
            var s = new Point3d(startBlock.Position.X, startBlock.Position.Y, 0);
            var e = new Point3d(endBlock.Position.X, endBlock.Position.Y, 0);

            Point3d? midPoint;

            using var line = new Line(s, e) { Color = Color.FromRgb(0, 255, 0) };
            using (var transForLine = db.TransactionManager.StartTransaction())
            {
                var acBlkTbl = (BlockTable) transForLine.GetObject(db.BlockTableId, OpenMode.ForRead);
                var acBlkTblRec = (BlockTableRecord) transForLine.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                acBlkTblRec.AppendEntity(line);
                transForLine.AddNewlyCreatedDBObject(line, true);

                db.TransactionManager.QueueForGraphicsFlush();

                midPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectMidPoint);
                while (midPoint.HasValue)
                {
                    var m = new Point3d(midPoint.Value.X, midPoint.Value.Y, 0);
                    if (line.GetGeCurve().IsOn(m)) break;

                    midPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectMidPoint);
                }

                transForLine.Abort();
                db.TransactionManager.QueueForGraphicsFlush();
            }

            if (!midPoint.HasValue)
            {
                return; //Assume user cancelled
            }

            var gradient = 1 / ((endLevel.Value - startLevel.Value) / line.Length);
            var midLevel = CalculateLevel(startBlock.Position, midPoint.Value, startLevel.Value, gradient);

            LevelBlockHelper.NewLevelBlockAtPoint(db, midPoint.Value, midLevel, startBlock.Rotation);

            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given point at gradient
        /// </summary>
        [CommandMethod("C_LevelBlock_FromPointAtGradient")]
        public static void CalculateLevelFromPointAtGradient()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromPointAtGradient));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectStartPoint);
            if (!startPoint.HasValue) return; //Assume user cancelled

            var startLevel = ed.PromptForDouble(Resources.Command_Prompt_EnterLevel, _level);
            if (!startLevel.HasValue) return; //Assume user cancelled

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return; //Assume user cancelled

            var endBlock = GenerateOrUpdateBlockWithCalcLevel(ed, startPoint.Value, startLevel.Value, gradient.Value, db);
            if (endBlock == null) return; //Assume user cancelled

            _level = startLevel.Value;
            _gradient = gradient.Value;
            
            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given block at gradient
        /// </summary>
        [CommandMethod("C_LevelBlock_FromBlockAtGradient")]
        public static void CalculateLevelFromLevelBlockAtGradient()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromLevelBlockAtGradient));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if (startBlock == null) return; //Assume user cancelled

            var startLevel = LevelBlockHelper.GetLevelFromBlock(startBlock);
            if (!startLevel.HasValue) return;

            var startPoint = startBlock.Position;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return; //Assume user cancelled

            var endBlock = GenerateOrUpdateBlockWithCalcLevel(ed, startPoint, startLevel.Value, gradient.Value, db, startBlock.Rotation);
            if (endBlock == null) return; //Assume user cancelled

            if (ShouldIncludeGradient(ed)) GradientBlockHelper.GenerateBlock(db, startBlock, endBlock);

            _gradient = gradient.Value;

            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given block with a set invert
        /// </summary>
        [CommandMethod("C_LevelBlock_FromBlockWithInvert")]
        public static void CalculateLevelFromLevelBlockWithInvert()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromLevelBlockWithInvert));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if (startBlock == null) return; //Assume user cancelled

            var startLevel = LevelBlockHelper.GetLevelFromBlock(startBlock);
            if (!startLevel.HasValue) return;

            var invert = ed.PromptForDouble(Resources.Command_Prompt_EnterInvert, _invert);
            if (!invert.HasValue) return; //Assume user cancelled

            var endLevel = startLevel.Value - invert.Value;

            var endBlock = GenerateOrUpdateBlockWithSetLevel(ed, endLevel, db, startBlock.Rotation);
            if (endBlock == null) return; //Assume user cancelled

            if (ShouldIncludeGradient(ed)) GradientBlockHelper.GenerateBlock(db, startBlock, endBlock);

            _invert = invert.Value;

            trans.Commit();
        }

        private static BlockReference GenerateOrUpdateBlockWithSetLevel(Editor ed, double level, Database db, double? rotation = null)
        {
            BlockReference endBlock;
            if (IsNewBlock(ed))
            {
                var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
                if (!endPoint.HasValue) return null; //Assume user cancelled prompted, therefore return null block

                endBlock = LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint.Value, level, rotation);
            }
            else
            {
                var trans = db.TransactionManager.TopTransaction;
                var existingBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectExistingLevelBlock, ed, trans);
                if (existingBlock == null) return null; //Assume user cancelled prompted, therefore return null block

                endBlock =  LevelBlockHelper.UpdateExistingLevelBlock(existingBlock, level);
            }

            return endBlock;
        }

        private static BlockReference GenerateOrUpdateBlockWithCalcLevel(Editor ed, Point3d startPoint, double startLevel, double gradient, Database db, double? rotation = null)
        {
            BlockReference endBlock;

            if (IsNewBlock(ed))
            {
                var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
                if (!endPoint.HasValue) return null; //Assume user cancelled prompted, therefore return null block
                
                var endLevel = CalculateLevel(startPoint, endPoint.Value, startLevel, gradient);
                endBlock = LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint.Value, endLevel, rotation);
            }
            else
            {
                var trans = db.TransactionManager.TopTransaction;
                var existingBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectExistingLevelBlock, ed, trans);
                if (existingBlock == null) return null; //Assume user cancelled prompted, therefore return null block

                var endLevel = CalculateLevel(startPoint, existingBlock.Position, startLevel, gradient);
                endBlock = LevelBlockHelper.UpdateExistingLevelBlock(existingBlock, endLevel);
            }

            return endBlock;
        }

        private static bool ShouldIncludeGradient(Editor ed)
        {
            var result = ed.PromptForKeywords(Resources.Command_Prompt_IncludeGradientBlock, GradientKeywords, _includeGradient);
            _includeGradient = result;
            return result == GradientKeywords[1];
        }

        private static bool IsNewBlock(Editor ed)
        {
            var result = ed.PromptForKeywords(Resources.Command_Prompt_NewOrExistingLevelBlock, BlockKeywords);
            return result == BlockKeywords[0];
        }

        private static double CalculateLevel(Point3d startPoint, Point3d endPoint, double startLevel, double gradient)
        {
            var s = new Point3d(startPoint.X, startPoint.Y, 0); //Remove z for line length
            var e = new Point3d(endPoint.X, endPoint.Y, 0); //Remove z for line length
            using var line = new Line(s, e);

            return startLevel + line.Length * (1 / gradient);
        }
    }
}
